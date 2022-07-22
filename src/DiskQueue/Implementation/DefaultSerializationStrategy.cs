using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DiskQueue.Implementation
{
    /// <summary>
    /// This class performs basic binary serialization from objects of Type T to byte arrays suitable for use in DiskQueue sessions.
    /// </summary>
    /// <remarks>
    /// You are free to implement your own <see cref="ISerializationStrategy{T}"/> and inject it into <see cref="PersistentQueue{T}"/>.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    internal class DefaultSerializationStrategy<T> : ISerializationStrategy<T>
    {
        /// <inheritdoc />
        public T? Deserialize(byte[]? bytes)
        {
            if (bytes == null)
            {
                return default;
            }

            BinaryFormatter bf = new();
            using MemoryStream ms = new(bytes);
            object obj = bf.Deserialize(ms);
            return (T)obj;
        }

        /// <inheritdoc />
        public byte[]? Serialize(T? obj)
        {
            if (obj == null)
            {
                return null;
            }

            BinaryFormatter bf = new();
            using MemoryStream ms = new();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }
}