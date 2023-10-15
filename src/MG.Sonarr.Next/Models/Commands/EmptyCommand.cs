using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Models.Commands
{
    internal readonly struct EmptyCommand : ICommand
    {
        const string NA = "N/A";

        public int Id => 0;
        public bool IsCompleted => false;
        public string Name => NA;
        public DateTimeOffset Started => DateTimeOffset.MinValue;
        public DateTimeOffset? Ended => null;

        internal static readonly EmptyCommand Default = default;
    }
}
