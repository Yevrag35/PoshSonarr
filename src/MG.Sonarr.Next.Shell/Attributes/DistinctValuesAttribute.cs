using MG.Sonarr.Next.Shell.Checkers;
using System.Collections.Concurrent;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides the transformation of a list argument into an array of its distinct values, each converted to the element
/// type, so that a value given more than once binds once.
/// </summary>
/// <remarks>
/// <para>
/// Only a list argument is transformed, such as the <see cref="object"/> array of a named list. A single value, a
/// string, and each object of the pipeline, which reaches the attribute on its own, go on unchanged.
/// </para>
/// <para>
/// Each element converts to the element type the way PowerShell converts a value, and the first of each set of equal
/// values is kept, in the list's order. Enum values compare as their numeric values, a type that implements
/// <see cref="IEquatable{T}"/> of itself compares through it, and any other type compares the way PowerShell's
/// <c>-eq</c> does, ignoring case.
/// </para>
/// <para>
/// The attribute resolves the cache of its comparers from the module's connection, so a list argument fails to bind
/// while the module has no connection.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class DistinctValuesAttribute : EnumerableTransformAttribute
{
	/// <summary>
	/// Gets or sets the collection type that a list argument must convert to and that the distinct values are converted
	/// back to.
	/// </summary>
	/// <value>
	/// The collection type, or <see langword="null"/> to take any list argument and return the values as the argument's
	/// own type. The default is <see langword="null"/>.
	/// </value>
	[MaybeNull]
	public Type CollectionType
	{
		get => this.CollectionTypeCore;
		init => this.CollectionTypeCore = value;
	}

	/// <summary>
	/// Initializes a new <see cref="DistinctValuesAttribute"/> instance with the specified element type.
	/// </summary>
	/// <param name="elementType">The type that each value converts to, and that decides how the values compare.</param>
	public DistinctValuesAttribute(Type elementType) : base(elementType)
	{
	}

	/// <summary>
	/// Converts each element of a list argument to the element type and returns the distinct values.
	/// </summary>
	/// <remarks>
	/// The comparer for <paramref name="elementType"/> is created once and kept in the connection's singleton cache of
	/// comparers, which every attribute shares.
	/// </remarks>
	/// <param name="input">The elements of the argument, as PowerShell enumerates them.</param>
	/// <param name="elementType">The type that each element converts to.</param>
	/// <param name="inputType">The runtime type of the argument, which the method doesn't read.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument, which the method doesn't
	/// read.</param>
	/// <param name="provider">The provider of the scope that the transformation runs in, which the comparers' cache is
	/// resolved from.</param>
	/// <returns>The distinct converted values, in the order that they first appear.</returns>
	/// <exception cref="PSInvalidCastException">Thrown when an element can't be converted to
	/// <paramref name="elementType"/>.</exception>
	protected override Array TransformCore([DisallowNull] IEnumerable<object> input, Type elementType, Type inputType, EngineIntrinsics engineIntrinsics, IServiceProvider provider)
	{
		var checkers = provider.GetRequiredService<ConcurrentDictionary<Type, EqualityChecker>>();
		var checker = checkers.GetOrAdd(elementType, CreateChecker);

		IEnumerable<object> converted = input.Select(x => LanguagePrimitives.ConvertTo(x, elementType, CultureInfo.InvariantCulture));

		return converted.Distinct(checker).ToArray();
	}

	/// <summary>
	/// Determines whether the type at the specified index of an array implements <see cref="IEquatable{T}"/> of the
	/// array's types.
	/// </summary>
	/// <remarks>
	/// The array holds the element type alone, so the method determines whether the element type is equatable to itself.
	/// </remarks>
	/// <param name="types">The type arguments of <see cref="IEquatable{T}"/>, which also hold the type to test.</param>
	/// <param name="index">The index in <paramref name="types"/> of the type to test.</param>
	/// <returns><see langword="true"/> if the type implements the closed <see cref="IEquatable{T}"/>; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool IsEquatable(Type[] types, int index)
	{
		Type def = typeof(IEquatable<>).MakeGenericType(types);

		return def.IsAssignableFrom(types.AsSpan(index)[0]);
	}

	/// <summary>
	/// Creates the comparer that the distinct values of the specified element type are found with.
	/// </summary>
	/// <param name="elementType">The type that the values convert to.</param>
	/// <returns>
	/// An <see cref="EnumEqualityChecker{T}"/> for an enum, an <see cref="EqualityChecker{T}"/> for a type that is
	/// equatable to itself, and <see cref="EqualityChecker.Default"/> for any other type.
	/// </returns>
	private static EqualityChecker CreateChecker(Type elementType)
	{
		Type[] parameters = [elementType];
		if (elementType.IsEnum)
		{
			return (EqualityChecker)Activator.CreateInstance(typeof(EnumEqualityChecker<>).MakeGenericType(parameters))!;
		}

		if (IsEquatable(parameters, 0))
		{
			return (EqualityChecker)Activator.CreateInstance(typeof(EqualityChecker<>).MakeGenericType(parameters))!;
		}

		return EqualityChecker.Default;
	}
}