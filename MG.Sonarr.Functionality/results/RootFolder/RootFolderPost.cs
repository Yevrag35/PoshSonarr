using Newtonsoft.Json;
using System;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RootFolderPost : BaseResult
    {
        #region JSON PROPERTIES
        [JsonProperty("path")]
        public string Path { get; private set; }

        [JsonConstructor]
        internal RootFolderPost() { }

        public RootFolderPost(string pathToAdd) => this.Path = pathToAdd;

        #endregion
    }
}