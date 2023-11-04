namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class RuntimeParameterExtensions
    {
        public static void AddParameter(this RuntimeDefinedParameterDictionary paramDict, RuntimeDefinedParameter parameter)
        {
            ArgumentNullException.ThrowIfNull(paramDict);
            ArgumentNullException.ThrowIfNull(parameter);

            paramDict.Add(parameter.Name, parameter);
        }
    }
}

