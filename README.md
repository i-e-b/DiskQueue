DiskQueue
=========
[![Build status](https://ci.appveyor.com/api/projects/status/vjb9wy6qn2qangyl)](https://ci.appveyor.com/project/i-e-b/diskqueue)

A thread-safe, multi-process(ish) persistent queue, based very heavily on  http://ayende.com/blog/3479/rhino-queues-storage-disk .

Requirements and Environment
----------------------------
Works on .Net 4+ and Mono 2.10.8+ (3.0.6+ recommended)

Requires access to filesystem storage


Basic Usage
-----------
 - `PersistentQueue.WaitFor(...)` is the main entry point. This will attempt to gain an exclusive lock
   on the given storage location. On first use, a directory will be created with the required files
   inside it.
 - This queue object can be shared among threads. Each thread should call `OpenSession()` to get its 
   own session object.
 - Both `IPersistentQueue`s and `IPersistentQueueSession`s should be wrapped in `using()` clauses, or otherwise
   disposed of properly.
   
Example
-------
Queue on one thread, consume on another; retry some exceptions.
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

If you need the transaction semantics of sessions across multiple processes, try a more robust solution like https://github.com/i-e-b/SevenDigital.Messaging

