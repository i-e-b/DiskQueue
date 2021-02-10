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
		public static TV GetOrCreateValue<TV,TK>(this IDictionary<TK,TV> self, TK key)
			where TV : new()
		{
			if (self.TryGetValue(key, out var value)) return value;
			
			value = new TV();
			self.Add(key, value);
			return value;
		}

		/// <summary>
		/// Return value for key if present, otherwise return default.
		/// No new keys or values will be added to the dictionary.
		/// </summary>
		public static TV? GetValueOrDefault<TV, TK>(this IDictionary<TK, TV> self, TK key)
		{
			return self.TryGetValue(key, out var value) == false ? default : value;
		}
	}
}