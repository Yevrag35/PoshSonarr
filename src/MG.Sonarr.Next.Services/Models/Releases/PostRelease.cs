﻿namespace MG.Sonarr.Next.Services.Models.Releases
{
    public sealed class PostRelease
    {
        public required string Guid { get; init; }
        public required int IndexerId { get; init; }
    }
}
