﻿using System.ComponentModel;

namespace MG.Sonarr.Next.Services.Extensions
{
    public static class ListSortDirectionExtensions
    {
        public static int GetLength(this ListSortDirection direction)
        {
            return direction switch
            {
                ListSortDirection.Ascending => 9,
                ListSortDirection.Descending => 10,
                _ => ((int)direction).GetLength(),
            };
        }
    }
}