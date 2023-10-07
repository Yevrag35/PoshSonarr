namespace MG.Sonarr.Next.Json.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class JsonIncludePrivateFieldsAttribute : Attribute
    {
        public string[] FieldNames { get; }

        public JsonIncludePrivateFieldsAttribute(params string[] fieldNames)
        {
            this.FieldNames = fieldNames ?? Array.Empty<string>();
        }
    }
}
