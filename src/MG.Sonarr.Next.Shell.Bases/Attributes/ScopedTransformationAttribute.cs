using MG.Sonarr.Next.Shell.Context;

namespace MG.Sonarr.Next.Shell.Attributes;

public abstract class ScopedTransformationAttribute : ArgumentTransformationAttribute
{
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

    protected abstract bool IsCorrectInputType([DisallowNull] object inputData, Type inputType, IServiceProvider provider);
    protected abstract object? TransformCore([DisallowNull] object inputData, Type inputType, EngineIntrinsics engineIntrinsics, IServiceProvider provider);
}


