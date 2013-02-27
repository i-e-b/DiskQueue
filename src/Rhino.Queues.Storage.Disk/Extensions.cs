namespace Rhino.Queues.Storage.Disk
{
	using System.Collections.Generic;

	public static class Extensions
	{
		public static T GetOrCreateValue<T,K>(this IDictionary<K,T> self, K key)
			where T : new()
		{
			T value;
			if (self.TryGetValue(key, out value) == false)
			{
				value = new T();
				self.Add(key, value);
			}
			return value;
		}

		public static T GetValueOrDefault<T, K>(this IDictionary<K, T> self, K key)
		{
			T value;
			if (self.TryGetValue(key, out value) == false)
				return default(T);
			return value;
		}
	}
}