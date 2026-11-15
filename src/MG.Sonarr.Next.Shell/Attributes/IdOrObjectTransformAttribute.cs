using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides the conversion of a parameter's argument to an <see cref="Either{T1, T2}"/> of <see cref="int"/> and
/// <typeparamref name="T"/>, which holds either an ID or the object that the ID stands for.
/// </summary>
/// <remarks>
/// <para>
/// PowerShell doesn't call the union's implicit conversion from <typeparamref name="T"/> when
/// <typeparamref name="T"/> derives from <see cref="PSObject"/>, as the module's models do. It treats a model as a
/// custom object instead: it creates a default union and sets each of the model's properties on it, which fails for a
/// model that has any properties. The attribute converts the argument before PowerShell does, so the binder gets a
/// union that already has the parameter's type.
/// </para>
/// <para>
/// An argument that is a <typeparamref name="T"/> becomes the union's object, as the same instance. Any other
/// argument converts to an <see cref="int"/> the way PowerShell converts the argument of an <see cref="int"/>
/// parameter, and becomes the union's ID. The attribute leaves a validation attribute on the same parameter, such as
/// a range check on the ID, to receive the union.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the object that the parameter takes in place of an ID.</typeparam>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class IdOrObjectTransformAttribute<T> : ArgumentTransformationAttribute where T : class
{
	/// <summary>
	/// Converts the argument to an <see cref="Either{T1, T2}"/> of <see cref="int"/> and <typeparamref name="T"/>.
	/// </summary>
	/// <param name="engineIntrinsics">The engine's intrinsics, which the method doesn't use.</param>
	/// <param name="inputData">The argument, or one pipeline object, before the binder converts it.</param>
	/// <returns>
	/// The boxed union, or <see langword="null"/> when <paramref name="inputData"/> is <see langword="null"/>, which
	/// leaves the binder to refuse it.
	/// </returns>
	/// <exception cref="ArgumentTransformationMetadataException">Thrown when <paramref name="inputData"/> is neither a
	/// <typeparamref name="T"/> nor a value that converts to an <see cref="int"/>.</exception>
	public override object? Transform(EngineIntrinsics engineIntrinsics, object? inputData)
	{
		if (inputData is null)
		{
			return null;
		}

		if (inputData is T obj)
		{
			return Either<int, T>.FromT2(obj);
		}

		if (LanguagePrimitives.TryConvertTo(inputData, CultureInfo.InvariantCulture, out int id))
		{
			return Either<int, T>.FromT1(id);
		}

		throw new ArgumentTransformationMetadataException(
			$"The argument of type '{GetTypeName(inputData)}' is neither an ID nor a '{typeof(T).Name}'.");
	}

	/// <summary>
	/// Gets the name of an argument's .NET type for an error message.
	/// </summary>
	/// <remarks>
	/// A model's first type name can be one that another command put there, such as the one that
	/// <c>Select-Object -First</c> inserts, so a model is named by its class instead.
	/// </remarks>
	/// <param name="inputData">The argument.</param>
	/// <returns>
	/// The full name of the type of the value that a plain <see cref="PSObject"/> wraps, or else of the argument's
	/// own type, which is a model's class.
	/// </returns>
	private static string GetTypeName(object inputData)
	{
		object value = inputData is PSObject pso && inputData.GetType() == typeof(PSObject)
			? pso.ImmediateBaseObject
			: inputData;

		Type type = value.GetType();
		return type.FullName ?? type.Name;
	}
}
