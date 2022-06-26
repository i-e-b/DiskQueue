namespace DiskQueue
{
    /// <summary>
    /// This class performs basic binary serialization from objects of Type T to byte arrays suitable for use in DiskQueue sessions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISerializationStrategy<T>
    {
        /// <summary>
        /// Deserializes byte array into object reference of type T.
        /// </summary>
        /// <param name="bytes">Byte array to deserialize</param>
        /// <returns>Object instance of type T.</returns>
        public T? Deserialize(byte[]? bytes);

        /// <summary>
        /// Serialized passed object into byte array suitable for queuing into a <see cref="PersistentQueue{T}"/>.
        /// </summary>
        /// <param name="obj">Object to serialize. Class must be decorated with Serializable annotation.</param>
        /// <returns>Byte array.</returns>
        public byte[]? Serialize(T? obj);
    }
}
