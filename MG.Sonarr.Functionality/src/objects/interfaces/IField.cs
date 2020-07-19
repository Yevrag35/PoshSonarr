using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface IField : IJsonResult
    {
        object BackendValue { get; }
        string HelpText { get; }
        bool IsAdvanced { get; }
        string Label { get; }
        string Name { get; }
        int Order { get; }
        IEnumerable<ISelectOption> SelectOptions { get; }
        FieldType Type { get; }
        object Value { get; }

        Type GetDotNetTypeFromFieldType();
        string GetLabelNoSpaces();
    }
}
