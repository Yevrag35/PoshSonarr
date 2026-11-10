using MG.Sonarr.Next.Shell.Context;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides a base class for argument transformation attributes that resolve services from the module's connection
/// while they transform an argument.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="Transform(EngineIntrinsics, object?)"/> creates a service scope on the module's current
/// connection and disposes it before returning, so the services that a derived class resolves live only for that one
/// argument. The scope holds no cmdlet, so a request sent from it writes no verbose or debug output.
/// </para>
/// <para>
/// Because the scope comes from the connection, an argument other than <see langword="null"/> to a parameter that
/// carries a derived attribute fails to bind while the module has no connection.
/// </para>
/// </remarks>
public abstract class ScopedTransformationAttribute : ArgumentTransformationAttribute
{
	/// <summary>
	/// Transforms the specified argument within a service scope on the module's connection, when the argument is of a
	/// type that the derived class transforms.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A <see langword="null"/> argument comes back as it is, without a scope, and so does one for which
	/// <see cref="IsCorrectInputType(object, Type, IServiceProvider)"/> returns <see langword="false"/>. Otherwise, the
	/// result is what <see cref="TransformCore(object, Type, EngineIntrinsics, IServiceProvider)"/> returns.
	/// </para>
	/// <para>
	/// PowerShell calls the method with the argument before the binder converts it, so it can receive a single value,
	/// an <see cref="object"/> array of the values of a named list, a <see cref="PSObject"/> around a command's output, or
	/// each object of the pipeline on its own.
	/// </para>
	/// </remarks>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument.</param>
	/// <param name="inputData">The argument to transform, which can be <see langword="null"/>.</param>
	/// <returns>The transformed argument, or <paramref name="inputData"/> unchanged when it isn't transformed.</returns>
	/// <exception cref="MG.Sonarr.Next.Exceptions.Shell.ContextNotSetException">
	/// Thrown when <paramref name="inputData"/> isn't null and the module has no connection.
	/// </exception>
	public sealed override object? Transform(EngineIntrinsics engineIntrinsics, object? inputData)
	{
		if (inputData is null)
		{
			return inputData;
		}

		using IServiceScope scope = this.CreateScope();

		Type inputDataType = inputData.GetType();
		if (this.IsCorrectInputType(inputData, inputDataType, scope.ServiceProvider))
		{
			inputData = this.TransformCore(inputData, inputDataType, engineIntrinsics, scope.ServiceProvider);
		}

		return inputData;
	}

	/// <summary>
	/// When implemented in a derived class, determines whether the specified argument is one that the attribute
	/// transforms.
	/// </summary>
	/// <param name="inputData">The argument, which isn't <see langword="null"/>.</param>
	/// <param name="inputType">The runtime type of <paramref name="inputData"/>.</param>
	/// <param name="provider">The provider of the scope that the transformation runs in. It's disposed once
	/// <see cref="Transform(EngineIntrinsics, object?)"/> returns.</param>
	/// <returns><see langword="true"/> if <see cref="TransformCore(object, Type, EngineIntrinsics, IServiceProvider)"/>
	/// should transform <paramref name="inputData"/>; otherwise, <see langword="false"/>, and the argument goes on
	/// unchanged.</returns>
	protected abstract bool IsCorrectInputType([DisallowNull] object inputData, Type inputType, IServiceProvider provider);
	/// <summary>
	/// When implemented in a derived class, transforms the specified argument.
	/// </summary>
	/// <remarks>
	/// The method runs only for an argument for which <see cref="IsCorrectInputType(object, Type, IServiceProvider)"/>
	/// returned <see langword="true"/>. Services resolved from <paramref name="provider"/> are disposed once
	/// <see cref="Transform(EngineIntrinsics, object?)"/> returns, so the result doesn't keep a scoped or transient
	/// disposable service.
	/// </remarks>
	/// <param name="inputData">The argument, which isn't <see langword="null"/>.</param>
	/// <param name="inputType">The runtime type of <paramref name="inputData"/>.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument.</param>
	/// <param name="provider">The provider of the scope that the transformation runs in.</param>
	/// <returns>The value that the binder binds in place of <paramref name="inputData"/>.</returns>
	protected abstract object? TransformCore([DisallowNull] object inputData, Type inputType, EngineIntrinsics engineIntrinsics, IServiceProvider provider);
}


