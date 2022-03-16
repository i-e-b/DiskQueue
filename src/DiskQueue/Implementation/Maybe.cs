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
    internal class Maybe<T>
    {
        public T? Value { get; private set; }
        public Exception? Error { get; private set; }
        
        public bool IsSuccess { get; private set; }
        public bool IsFailure => !IsSuccess;
        
        public static Maybe<T> Success(T value)
        {
            return new Maybe<T>{
                Value = value,
                IsSuccess = true,
                Error = null
            };
        }
        
        public static Maybe<T> Fail(Exception error)
        {
            return new Maybe<T>{
                Value = default,
                IsSuccess = false,
                Error = error
            };
        }

    }
}