// Copyright (c) 2005 - 2008 Ayende Rahien (ayende@ayende.com)
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//     * Neither the name of Ayende Rahien nor the names of its
//     contributors may be used to endorse or promote products derived from this
//     software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Text.Json;

namespace DiskQueue.Implementation
{
	/// <summary>
	/// Exception thrown when data can't be persisted
	/// </summary>
	public class PendingWriteException : Exception
	{
		private readonly Exception[] _pendingWritesExceptions;

		/// <summary>
		/// Aggregate causing exceptions
		/// </summary>
		public PendingWriteException(Exception[] pendingWritesExceptions)
			: base("Error during pending writes")
		{
			_pendingWritesExceptions = pendingWritesExceptions ?? throw new ArgumentNullException(nameof(pendingWritesExceptions));
		}

		/// <summary>
		/// Set of causing exceptions
		/// </summary>
		public Exception[] PendingWritesExceptions => _pendingWritesExceptions;

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		public override string Message
		{
			get
			{
				var sb = new StringBuilder(base.Message ?? "Error").Append(':');
				foreach (var exception in _pendingWritesExceptions)
				{
					sb.AppendLine().Append(" - ").Append(exception.Message ?? "<unknown>");
				}
				return sb.ToString();
			}
		}

		/// <summary>
		/// Creates and returns a string representation of the current exception.
		/// </summary>
		public override string ToString()
		{
			var sb = new StringBuilder(base.Message ?? "Error").Append(':');
			foreach (var exception in _pendingWritesExceptions)
			{
				sb.AppendLine().Append(" - ").Append(exception);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Serializes the exception to a JSON string.
		/// </summary>
		/// <returns>String representation of the exception.</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(new
            {
                BaseMessage = base.Message, // Store the base message explicitly
                PendingWritesExceptions = _pendingWritesExceptions.Select(ex => new
                {
                    ex.Message,
                    ex.StackTrace,
                    InnerException = ex.InnerException?.ToJson() // Recursive serialization
                }).ToArray()
            });
        }

        /// <summary>
        /// Deserializes a JSON string back into a PendingWriteException.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static PendingWriteException FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<ExceptionData>(json);
            if (data == null)
            {
                throw new ArgumentException("Invalid JSON data for deserialization.", nameof(json));
            }

            var pendingExceptions = data.PendingWritesExceptions?.Select(ex =>
            {
                var innerEx = ex.InnerException != null ? FromJson(ex.InnerException) : null;
                return new Exception(ex.Message) { /* StackTrace can't be set directly */ };
            }).ToArray() ?? Array.Empty<Exception>();

            return new PendingWriteException(pendingExceptions);
        }

        // Helper class for JSON structure
        private class ExceptionData
        {
            public string? BaseMessage { get; set; }
            public ExceptionDetails[]? PendingWritesExceptions { get; set; }
        }

        private class ExceptionDetails
        {
            public string? Message { get; set; }
            public string? StackTrace { get; set; }
            public string? InnerException { get; set; }
        }
    }
}