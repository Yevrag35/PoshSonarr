﻿using MG.Sonarr.Next.Attributes;
using System.ComponentModel;

namespace MG.Sonarr.Next.Extensions
{
    public static class ListSortDirectionExtensions
    {
        public static int GetLength([ValidatedNotNull] this ListSortDirection direction)
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