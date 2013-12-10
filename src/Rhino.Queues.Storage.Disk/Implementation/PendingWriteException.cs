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
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace DiskQueue.Implementation
{
	/// <summary>
	/// Exception thrown when data can't be persisted
	/// </summary>
	[Serializable]
	public class PendingWriteException : Exception
	{
		private readonly Exception[] pendingWritesExceptions;

		/// <summary>
		/// Aggregate causing exceptions
		/// </summary>
		public PendingWriteException(Exception[] pendingWritesExceptions)
			: base("Error during pending writes")
		{
			this.pendingWritesExceptions = pendingWritesExceptions;
		}

		/// <summary>
		/// Set of causing exceptions
		/// </summary>
		public Exception[] PendingWritesExceptions
		{
			get { return pendingWritesExceptions; }
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		public override string Message
		{
			get
			{
				var sb = new StringBuilder(base.Message).Append(":");
				foreach (var exception in pendingWritesExceptions)
				{
					sb.AppendLine().Append(" - ").Append(exception.Message);
				}
				return sb.ToString();
			}
		}

		/// <summary>
		/// Creates and returns a string representation of the current exception.
		/// </summary>
		public override string ToString()
		{
			var sb = new StringBuilder(base.Message).Append(":");
			foreach (var exception in pendingWritesExceptions)
			{
				sb.AppendLine().Append(" - ").Append(exception);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with information about the exception.
		/// </summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param><param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination. </param><exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic). </exception><filterpriority>2</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter"/></PermissionSet>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("PendingWritesExceptions", PendingWritesExceptions);
		}
	}
}