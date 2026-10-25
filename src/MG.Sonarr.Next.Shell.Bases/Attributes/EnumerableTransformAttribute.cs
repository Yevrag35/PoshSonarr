using System.Collections;

namespace MG.Sonarr.Next.Shell.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public abstract class EnumerableTransformAttribute : ScopedTransformationAttribute
{
	protected Type? CollectionTypeCore { get; set; }
	protected Type ElementTypeCore { get; }

	protected EnumerableTransformAttribute(Type elementType)
	{
		this.ElementTypeCore = elementType;
	}

	protected override bool IsCorrectInputType([DisallowNull] object inputData, Type inputType, IServiceProvider provider)
	{
		if (!LanguagePrimitives.IsObjectEnumerable(inputData))
		{
			return false;
		}

		return LanguagePrimitives.TryConvertTo(inputData, this.CollectionTypeCore ?? typeof(IEnumerable), Statics.DefaultProvider, out _);
	}
	protected virtual bool IsCorrectInputType<T>([DisallowNull] T inputData, IServiceProvider provider)
	{
		return this.IsCorrectInputType(inputData, inputData.GetType(), provider);
	}

	protected override object? TransformCore([DisallowNull] object inputData, Type inputType, EngineIntrinsics engineIntrinsics, IServiceProvider provider)
	{
		IEnumerable transformed = LanguagePrimitives.GetEnumerable(inputData);
		transformed = this.TransformCore(transformed.Cast<object>(), this.ElementTypeCore, inputType, engineIntrinsics, provider);
		return LanguagePrimitives.TryConvertTo(transformed, this.CollectionTypeCore ?? inputType, Statics.DefaultProvider, out object? convertedBack)
			? convertedBack
			: transformed;
	}

	protected abstract Array TransformCore([DisallowNull] IEnumerable<object> input, Type elementType, Type inputType, EngineIntrinsics engineIntrinsics, IServiceProvider provider);
}