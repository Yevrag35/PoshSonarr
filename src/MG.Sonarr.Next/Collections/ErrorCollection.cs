using MG.Sonarr.Next.Models.Errors;
using System.Collections;

namespace MG.Sonarr.Next.Collections
{
    /// <summary>
    /// A interface representing a collection of Error responses from a Sonarr server instance.
    /// </summary>
    public interface IErrorCollection : IReadOnlyList<SonarrServerError>
    {
        /// <summary>
        /// Combines all of the <see cref="IErrorCollection"/> instance's error messages into one
        /// <see cref="string"/>, joined by <see cref="Environment.NewLine"/>.
        /// </summary>
        string BuildMessage();
    }

    file sealed class EmptyReadOnlyErrorCollection : IErrorCollection
    {
        public SonarrServerError this[int index] => null!;

        public int Count => 0;

        public IEnumerator<SonarrServerError> GetEnumerator()
        {
            yield break;
        }

        public string BuildMessage()
        {
            return string.Empty;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal sealed class ErrorCollection : List<SonarrServerError>, IErrorCollection
    {
        internal ErrorCollection()
            : base(1)
        {
        }
        private ErrorCollection(SonarrServerError one)
            : this()
        {
            this.Add(one);
        }
        public ErrorCollection(IEnumerable<SonarrServerError> errors)
            : base(errors)
        {
        }

        /// <summary>
        /// A <see langword="static"/>, read-only empty collection of errors.
        /// </summary>
        public static readonly IErrorCollection Empty = new EmptyReadOnlyErrorCollection();
        internal static IErrorCollection FromOne(SonarrServerError? one)
        {
            return one is not null
                ? new ErrorCollection(one)
                : Empty;
        }
        public string BuildMessage()
        {
            if (this.Count <= 0)
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine,
                this
                    .Where(x => !string.IsNullOrEmpty(x.Message))
                        .Select(x => x.Message));
        }
    }
}
