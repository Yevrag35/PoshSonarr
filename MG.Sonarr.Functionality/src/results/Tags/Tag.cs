using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/tag" endpoint.
    /// </summary>
    [Serializable]
    public class Tag : BaseResult
    {
        public int Id { get; set; }
        public string Label { get; set; }
    }
}
