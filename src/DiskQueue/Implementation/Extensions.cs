using System.Collections.Generic;

namespace DiskQueue.Implementation
{
	/// <summary>
	/// Internal extension methods
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Ensure a key is present in the dictionary. Uses default value if needed.
		/// Return stored value for the key
		/// </summary>
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

		/// <summary>
		/// Return value for key if present, otherwise return default.
		/// No new keys or values will be added to the dictionary.
		/// </summary>
		public static T GetValueOrDefault<T, K>(this IDictionary<K, T> self, K key)
		{
			T value;
			return self.TryGetValue(key, out value) == false ? default(T) : value;
		}
	}
}