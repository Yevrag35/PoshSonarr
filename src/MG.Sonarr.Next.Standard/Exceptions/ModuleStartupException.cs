using System;
using System.Diagnostics;

#nullable enable

namespace MG.Sonarr.Next.Exceptions
{
	[DebuggerStepThrough]
	public sealed class ModuleStartupException : Exception
	{
		public Type? OffendingStartupType { get; }

		public ModuleStartupException(string? message)
			: this(message, offendingType: null, innerException: null)
		{
		}
		public ModuleStartupException(string? message, Type? offendingType)
			: this(message, offendingType, innerException: null)
		{
		}
		public ModuleStartupException(string? message, Exception? innerException)
			: this(message, offendingType: null, innerException)
		{
		}

		public ModuleStartupException(string? message, Type? offendingType, Exception? innerException)
			: base(message, innerException)
		{
			this.OffendingStartupType = offendingType;
		}
	}
}