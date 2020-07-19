using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    public interface ISelectOption : IJsonResult
    {
        string Name { get; }
        int Value { get; }
    }
}
