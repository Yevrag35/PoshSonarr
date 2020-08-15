using System;
using MG.Sonarr.Functionality;

namespace MG.Sonarr.Functionality.Url
{
    public class FileSystemParameter : IUrlParameter
    {
        private const string FORMAT = "path={0}&includeFiles={1}";
        private string full;

        public int Length => full.Length;

        public FileSystemParameter(string path, bool includeFiles)
        {
            full = string.Format(FORMAT, path, includeFiles.ToString().ToLower());
        }

        public string AsString() => full;
    }
}
