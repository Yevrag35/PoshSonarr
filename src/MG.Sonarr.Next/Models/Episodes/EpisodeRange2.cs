using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Models.Episodes
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct EpisodeRange2
    {
        public int Start { get; }
        public int End { get; }

        public bool AllMatch => this.Start == 0;
        public bool IsSingle => this.Start == this.End;

        public EpisodeRange2(int start, int end)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(end, start);
            this.Start = start;
            this.End = end;
        }

        public bool IsInRange(int episodeNumber)
        {
            if (this.AllMatch)
            {
                return true;
            }

            return this.IsSingle
                ? episodeNumber == this.Start
                : episodeNumber >= this.Start && episodeNumber <= this.End;
        }
    }
}
