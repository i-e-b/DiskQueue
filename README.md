DiskQueue
=========

A thread-safe, multi-process(ish) persistent queue, based very heavily on  http://ayende.com/blog/3479/rhino-queues-storage-disk .

Requirements and Environment
----------------------------
Works on .Net 4+ and Mono 2.10.8+ (3.0.6+ recommended)

Requires access to filesystem storage.

The file system is used to hold locks, so any bug in your file system may cause
issues with DiskQueue -- although it tries to work around them.


Basic Usage
-----------

 - `PersistentQueue.WaitFor(...)` is the main entry point. This will attempt to gain an exclusive lock
   on the given storage location. On first use, a directory will be created with the required files
   inside it.
 - This queue object can be shared among threads. Each thread should call `OpenSession()` to get its 
   own session object.
 - Both `IPersistentQueue`s and `IPersistentQueueSession`s should be wrapped in `using()` clauses, or otherwise
   disposed of properly. Failure to do this will result in lock contention -- you will get errors that the queue
   is still in use.
   
Thanks to Tom Halter, there is also a generic-typed `PersistentQueue<T>(...);` which will handle the serialisation and deserialization of
elements in the queue, as long at the type is decorated with `[Serializable]`. You can also inject your own `ISerializationStrategy` 
into your `PersistentQueueSession<T>` if you wish to have more granular control over Serialization/Deserialization, or if you wish to 
use your own serializer (e.g Json.NET).

Use `new PersistentQueue<T>(...)` in place of `new PersistentQueue(...)`
or `PersistentQueue.WaitFor<T>(...)` in place of `PersistentQueue.WaitFor(...)` in any of the examples below.


Example
-------
Queue on one thread, consume on another; retry some exceptions.

**Note** this is one queue being shared between two sessions. You should not open two queue instances for one storage location at once.

```csharp
IPersistentQueue queue = new PersistentQueue("queue_a");
var t1 = new Thread(() =>
{
	while (HaveWork())
	{
		using (var session = queue.OpenSession())
		{
			session.Enqueue(NextWorkItem());
			session.Flush();
		}
	}
});
var t2 = new Thread(()=> {
	while (true) {
		using (var session = queue.OpenSession()) {
			var data = session.Dequeue();
			if (data == null) {Thread.Sleep(100); continue;}
			
			try {
				MaybeDoWork(data)
				session.Flush();
			} catch (RetryException) {
				continue;
			} catch {
				session.Flush();
			}
		}
	}
});

t1.Start();
t2.Start();
```

Example
-------
Batch up a load of work and have another thread work through it.
```csharp
IPersistentQueue queue = new PersistentQueue("batchQueue");
var worker = new Thread(()=> {
	using (var session = queue.OpenSession()) {
		byte[] data;
		while ((data = session.Dequeue()) != null) {
			MaybeDoWork(data)
			session.Flush();
		}
	}
});

using (var session = queue.OpenSession()) {
	foreach (var item in LoadsOfStuff()) {
		session.Enqueue(item);
	}
	session.Flush();
}

worker.IsBackground = true; // anything not complete when we close will be left on the queue for next time.
worker.Start();
```

Transactions
------------
Each session is a transaction. Any Enqueues or Dequeues will be rolled back when the session is disposed unless
you call `session.Flush()`. Data will only be visible between threads once it has been flushed.
Each flush incurs a performance penalty. By default, each flush is persisted to disk before continuing. You 
can get more speed at a safety cost by setting `queue.ParanoidFlushing = false;`

Data loss and transaction truncation
------------------------------------
By default, DiskQueue will silently discard transaction blocks that have been truncated; it will throw an `InvalidOperationException`
when transaction block markers are overwritten (this happens if more than one process is using the queue by mistake. It can also happen with some kinds of disk corruption).
If you construct your queue with `throwOnConflict: false`, all recoverable transaction errors will be silently truncated. This should only be used when
uptime is more important than data consistency.

```
using (var queue = new PersistentQueue(path, Constants._32Megabytes, throwOnConflict: false)) {
    . . .
}
```

Global default settings
-----------------------
Each instance of a `PersistentQueue` has it's own settings for flush levels and corruption behaviour. You can set these individually after creating an instance,
or globally with `PersistentQueue.DefaultSettings`. Default settings are applied to all queue instances in the same process created *after* the setting is changed.

For example, if performance is more important than crash safety:
```csharp
PersistentQueue.DefaultSettings.ParanoidFlushing = false;
PersistentQueue.DefaultSettings.TrimTransactionLogOnDispose = false;
```

Or if up-time is more important than detecting corruption early (often the case for embedded systems):
```csharp
PersistentQueue.DefaultSettings.AllowTruncatedEntries = true;
PersistentQueue.DefaultSettings.ParanoidFlushing = true;
```

Removing or resetting queues
----------------------------

Queues create a directory and set of files for storage. You can remove all files for a queue with the `HardDelete` method.
If you give true as the reset parameter, the directory will be written again.

This WILL delete ANY AND ALL files inside the queue directory. You should not call this method in normal use.
If you start a queue with the same path as an existing directory, this method will delete the entire directory, not just
the queue files.

```csharp
var subject = new PersistentQueue("queue_a");
subject.HardDelete(true); // wipe any existing data and start again
```

Multi-Process Usage
-------------------
Each `IPersistentQueue` gives exclusive access to the storage until it is disposed.
There is a static helper method `PersistentQueue.WaitFor("path", TimeSpan...)` which will wait to gain access until
other processes release the lock or the timeout expires.
If each process uses the lock for a short time and wait long enough, they can share a storage location.

E.g.
```csharp
...
void AddToQueue(byte[] data) {
	Thread.Sleep(150);
	using (var queue = PersistentQueue.WaitFor(SharedStorage, TimeSpan.FromSeconds(30)))
	using (var session = queue.OpenSession()) {
		session.Enqueue(data);
		session.Flush();
	}
}

byte[] ReadQueue() {
	Thread.Sleep(150);
	using (var queue = PersistentQueue.WaitFor(SharedStorage, TimeSpan.FromSeconds(30)))
	using (var session = queue.OpenSession()) {
		var data = session.Dequeue();
		session.Flush();
		return data;
	}
}
...

```

Cross-process Locking
---------------------

DiskQueue tries very hard to make sure the lock files are managed correctly.
You can use this as an inter-process lock if required. Simply open a session to
acquire the lock, and dispose of the session to release it.


If you need the transaction semantics of sessions across multiple processes, try a more robust solution like https://github.com/i-e-b/SevenDigital.Messaging

