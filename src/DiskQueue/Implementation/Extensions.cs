using System;
using System.Collections.Generic;
using System.Text.Json;

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

		/// <summary>
		/// Serializes the exception to JSON.
		/// </summary>
		/// <param name="ex">The exception to serialize to JSON.</param>
		/// <returns>String representation of the exception.</returns>
        public static string ToJson(this Exception ex)
        {
            if (ex == null)
            {
                return null!;
            }

            return JsonSerializer.Serialize(new
            {
                ex.Message,
                ex.StackTrace,
                InnerException = ex.InnerException?.ToJson()
            });
        }
    }
}