using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Resources;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#nullable enable

namespace MG.Sonarr.Next.Exceptions
{
	/// <summary>
	/// An exception thrown when a struct is empty (<see langword="default"/>) and not usable in that state.
	/// </summary>
	public sealed class EmptyStructException : PoshSonarrException
	{
		/// <summary>
		/// If provided, the name of the struct that is empty.
		/// </summary>
		public string? ObjectName { get; }
		/// <summary>
		/// If provided, the type of the struct that is empty.
		/// </summary>
		public Type? StructType { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="EmptyStructException"/> with the offending
		/// object's name, <see cref="Type"/>, and a reference to the inner exception that is the cause of this 
		/// exception.
		/// </summary>
		/// <param name="objectName">The object's name that caused the exception.</param>
		/// <param name="structType">The type of the struct that caused the exception.</param>
		/// <param name="innerException">
		///     <inheritdoc cref="Exception(string, Exception)" path="/param[last()]"/>
		/// </param>
		public EmptyStructException(string? objectName, Type? structType, Exception? innerException)
			: this(GetMessage(objectName, structType), objectName, structType, innerException)
		{
		}
		/// <summary>
		/// Initializes a new instance of <see cref="EmptyStructException"/> with the specified message, the offending
		/// object's name and <see cref="Type"/>, and a reference to the inner exception that is the cause of this 
		/// exception.
		/// </summary>
		/// <param name="message">
		///     <inheritdoc cref="OmniException(string?)" path="/param[1]"/>
		/// </param>
		/// <param name="objectName">
		///     <inheritdoc cref="EmptyStructException(string, Type, Exception)" path="/param[1]"/>
		/// </param>
		/// <param name="structType">
		///     <inheritdoc cref="EmptyStructException(string, Type, Exception)" path="/param[2]"/>
		/// </param>
		/// <param name="innerException">
		///     <inheritdoc cref="Exception(string, Exception)" path="/param[last()]"/>
		/// </param>
		public EmptyStructException(string? message, string? objectName, Type? structType, Exception? innerException)
			: base(message, innerException)
		{
			this.ObjectName = objectName;
			this.StructType = structType;
		}

		/// <summary>
		/// Throws an <see cref="EmptyStructException"/> if the specified <paramref name="condition"/>
		/// is <see langword="true"/>.
		/// </summary>
		/// <typeparam name="T">The type of struct being evaluated.</typeparam>
		/// <param name="condition">The condition to evaluate.</param>
		/// <param name="instance">
		///     The object whose type's name should be included in any resulting <see cref="EmptyStructException"/>.
		/// </param>
		/// <param name="instanceName">
		///     The name of the instance to include in any resulting <see cref="EmptyStructException"/>.
		/// </param>
		/// <exception cref="EmptyStructException"/>
		public static void ThrowIf<T>([DoesNotReturnIf(true)] bool condition, T instance, string? instanceName = null) where T : struct
		{
			if (condition)
			{
				throw new EmptyStructException(TrimThisDot(instanceName), typeof(T), innerException: null);
			}
		}
		/// <summary>
		/// Throws an <see cref="EmptyStructException"/> if the specified <paramref name="condition"/>
		/// is <see langword="true"/>.
		/// </summary>
		/// <param name="condition">The condition to evaluate.</param>
		/// <param name="structType">
		///     The type whose name should be included in any resulting <see cref="EmptyStructException"/>.
		/// </param>
		/// <exception cref="EmptyStructException"/>
		public static void ThrowIf([DoesNotReturnIf(true)] bool condition, Type structType)
		{
			if (condition)
			{
				throw new EmptyStructException(objectName: null, structType, innerException: null);
			}
		}

		private static string GetMessage(string? objectName, Type? structType)
		{
			if (string.IsNullOrWhiteSpace(objectName))
			{
				return null != structType
					? string.Format(CultureInfo.CurrentCulture, ExMessages.EmptyStruct_NoName_Message, structType.GetName())
					: ExMessages.EmptyStruct_NoName_NoType_Message;
			}
			else if (structType is null)
			{
				return string.Format(CultureInfo.CurrentCulture, ExMessages.EmptyStruct_NoType_Message, objectName);
			}
			else
			{
				return string.Format(CultureInfo.CurrentCulture, ExMessages.EmptyStruct_Full_Format, objectName, structType.GetName());
			}
		}
	}
}
