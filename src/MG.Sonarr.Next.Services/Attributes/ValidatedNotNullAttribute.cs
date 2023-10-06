namespace MG.Sonarr.Next.Services.Attributes
{
    /// <summary>
    /// An attribute for Codacy to recognize that decorated method parameters have been validated against 
    /// being null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}
