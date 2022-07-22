using System;
using System.Collections.Generic;
using System.Text;

namespace DiskQueue.PublicInterfaces
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISerializationStrategy<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public T Deserialize(byte[] bytes);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public byte[] Serialize(T obj);
    }
}
