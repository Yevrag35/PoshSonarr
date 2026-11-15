using System.Collections;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides a base class for argument transformation attributes that transform a list argument as a whole, element by
/// element, within a service scope on the module's connection.
/// </summary>
/// <remarks>
/// <para>
/// An argument is transformed only when PowerShell enumerates it, as the pipeline does, and it converts to
/// <see cref="CollectionTypeCore"/>, or to <see cref="IEnumerable"/> when that's <see langword="null"/>. A single value,
/// a string, or a hashtable goes on unchanged, and so does each object of the pipeline, which reaches the attribute
/// on its own.
/// </para>
/// <para>
/// A derived class implements
/// <see cref="TransformCore(IEnumerable{object}, Type, Type, EngineIntrinsics, IServiceProvider)"/>, which gets the
/// argument's elements and returns them as an array. The base class then converts that array back to
/// <see cref="CollectionTypeCore"/>, or to the argument's own type.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public abstract class EnumerableTransformAttribute : ScopedTransformationAttribute
{
	/// <summary>
	/// Gets or sets the collection type that an argument must convert to and that the result is converted back to.
	/// </summary>
	/// <value>The collection type, or <see langword="null"/> to accept any enumerable argument and return the result as
	/// the argument's own type.</value>
	protected Type? CollectionTypeCore { get; set; }
	/// <summary>
	/// Gets the type of the elements that the transformation produces.
	/// </summary>
	/// <value>The element type, which
	/// <see cref="TransformCore(IEnumerable{object}, Type, Type, EngineIntrinsics, IServiceProvider)"/> receives as its
	/// <c>elementType</c>.</value>
	protected Type ElementTypeCore { get; }

	/// <summary>
	/// Initializes a new <see cref="EnumerableTransformAttribute"/> instance with the specified element type.
	/// </summary>
	/// <param name="elementType">The type of the elements that the transformation produces.</param>
	protected EnumerableTransformAttribute(Type elementType)
	{
		this.ElementTypeCore = elementType;
	}

	/// <summary>
	/// Determines whether the specified argument is a list that the attribute transforms.
	/// </summary>
	/// <remarks>
	/// The argument qualifies when PowerShell enumerates it, as <see cref="LanguagePrimitives.IsObjectEnumerable(object)"/>
	/// reports, and it converts to <see cref="CollectionTypeCore"/>, or to <see cref="IEnumerable"/> when that's
	/// <see langword="null"/>. The method converts the argument to test it and discards the result.
	/// </remarks>
	/// <param name="inputData">The argument, which isn't <see langword="null"/>.</param>
	/// <param name="inputType">The runtime type of <paramref name="inputData"/>. The method doesn't read it.</param>
	/// <param name="provider">The provider of the scope that the transformation runs in. The method doesn't read
	/// it.</param>
	/// <returns><see langword="true"/> if <paramref name="inputData"/> is an enumerable argument that converts to the
	/// collection type; otherwise, <see langword="false"/>.</returns>
	protected override bool IsCorrectInputType([DisallowNull] object inputData, Type inputType, IServiceProvider provider)
	{
		if (!LanguagePrimitives.IsObjectEnumerable(inputData))
		{
			return false;
		}

		return LanguagePrimitives.TryConvertTo(inputData, this.CollectionTypeCore ?? typeof(IEnumerable), CultureInfo.InvariantCulture, out _);
	}
	/// <summary>
	/// When overridden in a derived class, determines whether the specified strongly typed argument is a list that the
	/// attribute transforms.
	/// </summary>
	/// <remarks>
	/// The default implementation passes <paramref name="inputData"/> and its runtime type to
	/// <see cref="IsCorrectInputType(object, Type, IServiceProvider)"/>. The base class's transformation calls the
	/// non-generic overload only.
	/// </remarks>
	/// <typeparam name="T">The static type of the argument.</typeparam>
	/// <param name="inputData">The argument, which must not be <see langword="null"/>.</param>
	/// <param name="provider">The provider of the scope that the transformation runs in.</param>
	/// <returns><see langword="true"/> if <paramref name="inputData"/> is an enumerable argument that converts to the
	/// collection type; otherwise, <see langword="false"/>.</returns>
	protected virtual bool IsCorrectInputType<T>([DisallowNull] T inputData, IServiceProvider provider)
	{
		return this.IsCorrectInputType(inputData, inputData.GetType(), provider);
	}

	/// <summary>
	/// Transforms the specified list argument element by element and converts the result back to the collection type.
	/// </summary>
	/// <remarks>
	/// The method enumerates <paramref name="inputData"/> as PowerShell does, passes its elements to
	/// <see cref="TransformCore(IEnumerable{object}, Type, Type, EngineIntrinsics, IServiceProvider)"/> with
	/// <see cref="ElementTypeCore"/>, and converts the array that comes back to <see cref="CollectionTypeCore"/>, or to
	/// <paramref name="inputType"/> when that's <see langword="null"/>. When that conversion fails, it returns the array
	/// itself.
	/// </remarks>
	/// <param name="inputData">The argument, which isn't <see langword="null"/>.</param>
	/// <param name="inputType">The runtime type of <paramref name="inputData"/>.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument.</param>
	/// <param name="provider">The provider of the scope that the transformation runs in.</param>
	/// <returns>The transformed elements, as the collection type when they convert to it, or else as the array that the
	/// derived class returned.</returns>
	protected override object? TransformCore([DisallowNull] object inputData, Type inputType, EngineIntrinsics engineIntrinsics, IServiceProvider provider)
	{
		IEnumerable transformed = LanguagePrimitives.GetEnumerable(inputData);
		transformed = this.TransformCore(transformed.Cast<object>(), this.ElementTypeCore, inputType, engineIntrinsics, provider);
		return LanguagePrimitives.TryConvertTo(transformed, this.CollectionTypeCore ?? inputType, CultureInfo.InvariantCulture, out object? convertedBack)
			? convertedBack
			: transformed;
	}

	/// <summary>
	/// When implemented in a derived class, transforms the elements of a list argument.
	/// </summary>
	/// <remarks>
	/// The sequence is lazy and reads the argument as it's enumerated, so enumerate it once. Services resolved from
	/// <paramref name="provider"/> are disposed once the transformation returns.
	/// </remarks>
	/// <param name="input">The elements of the argument, as PowerShell enumerates them.</param>
	/// <param name="elementType">The type of the elements to produce, from <see cref="ElementTypeCore"/>.</param>
	/// <param name="inputType">The runtime type of the argument.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument.</param>
	/// <param name="provider">The provider of the scope that the transformation runs in.</param>
	/// <returns>The transformed elements, which the base class converts to the collection type.</returns>
	protected abstract Array TransformCore([DisallowNull] IEnumerable<object> input, Type elementType, Type inputType, EngineIntrinsics engineIntrinsics, IServiceProvider provider);
}