using MG.Api.Json;
using System;

namespace MG.Sonarr.Functionality
{
    public interface ISelectOption : IJsonObject
    {
        string Name { get; }
        int Value { get; }
    }
}
