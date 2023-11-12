namespace MG.Sonarr.Next.Attributes
{
    /// <summary>
    /// Indicates the decorated parameter will be not <see langword="null"/> even the Type allows it when the 
    /// other specified parameter is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     This attribute has no effect on the complier or C# analyzers.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class NotNullWhenTrueAttribute : AnalysisAttribute
    {
        public string ParameterName { get; }

        public NotNullWhenTrueAttribute(string parameterName)
        {
            this.ParameterName = parameterName;
        }
    }
}

