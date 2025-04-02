using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// This class performs basic serialization from objects of Type T to byte arrays suitable for use in DiskQueue sessions.
    /// <p></p>
    /// Due to the constraints of supporting a wide range of target runtimes, this serializer is very primitive.
    /// You are free to implement your own <see cref="ISerializationStrategy{T}"/> and inject it into <see cref="PersistentQueue{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type to be stored and retrieved. It must be either [Serializable] or a primitive type</typeparam>
    internal class DefaultSerializationStrategy<T> : ISerializationStrategy<T>
    {
        private readonly DataContractSerializer _serialiser;

        public DefaultSerializationStrategy()
        {
            var set = new DataContractSerializerSettings{
                PreserveObjectReferences = true,
                SerializeReadOnlyTypes = true
            };
            _serialiser = new DataContractSerializer(typeof(T), set);
        }
        
        /// <inheritdoc />
        public T? Deserialize(byte[]? bytes)
        {
            if (bytes == null)
            {
                return default;
            }
            
            if (typeof(T) == typeof(string)) return (T)((object)Encoding.UTF8.GetString(bytes));

            using MemoryStream ms = new(bytes);
            var obj = _serialiser.ReadObject(ms);
            if (obj == null)
            {
                return default;
            }
            return (T)obj;
        }

        /// <inheritdoc />
        public byte[]? Serialize(T? obj)
        {
            if (obj == null)
            {
                return null;
            }
            
            if (typeof(T) == typeof(string)) return Encoding.UTF8.GetBytes(obj.ToString() ?? string.Empty);

            using MemoryStream ms = new();
            _serialiser.WriteObject(ms, obj);
            return ms.ToArray();
        }
    }
}