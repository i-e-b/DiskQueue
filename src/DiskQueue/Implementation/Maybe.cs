using System;

namespace DiskQueue.Implementation
{
    internal static class MaybeExtensions
    {
        public static Maybe<T> Success<T>(this T value)
        {
            return Maybe<T>.Success(value);
        }
    }

    /// <summary>
    /// Represents the result of a computation that may fail
    /// </summary>
    public class Maybe<T>
    {
        /// <summary>
        /// Value of result, if successful
        /// </summary>
        public T? Value { get; private set; }
        /// <summary>
        /// Error if not successful.
        /// </summary>
        public Exception? Error { get; private set; }
        
        /// <summary>
        /// True if a value is set
        /// </summary>
        public bool IsSuccess { get; private set; }
        /// <summary>
        /// True if no value is set
        /// </summary>
        public bool IsFailure => !IsSuccess;
        
        /// <summary>
        /// Wrap a successful value
        /// </summary>
        public static Maybe<T> Success(T value)
        {
            return new Maybe<T>{
                Value = value,
                IsSuccess = true,
                Error = null
            };
        }
        
        /// <summary>
        /// Wrap a failure condition
        /// </summary>
        public static Maybe<T> Fail(Exception error)
        {
            return new Maybe<T>{
                Value = default,
                IsSuccess = false,
                Error = error
            };
        }

        /// <summary>
        /// Change a failure to a new Maybe&lt;T&gt; type
        /// </summary>
        public Maybe<T1> Chain<T1>()
        {
            if (IsSuccess) throw new Exception("Tried to wrap a failure case, but target was success");
            return Maybe<T1>.Fail(Error!);
        }
    }
}