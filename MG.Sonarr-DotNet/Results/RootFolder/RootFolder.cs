﻿using System;
using System.Collections.ObjectModel;

namespace MG.Sonarr.Results
{
    public class RootFolder : BaseResult
    {
        public long FreeSpace { get; set; }
        public int Id { get; set; }
        public string Path { get; set; }
        public long TotalSpace { get; set; }
        public Collection<UnmappedFolder> UnmappedFolders { get; set; }
    }

    public class UnmappedFolder : BaseResult
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}