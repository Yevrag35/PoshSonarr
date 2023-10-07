using MG.Sonarr.Next.Models;
using System.Collections;

namespace MG.Sonarr.Next.Collections
{
    public interface IErrorCollection : IReadOnlyList<SonarrServerError>
    {
        string GetMessage();
    }

    file sealed class EmptyReadOnlyErrorCollection : IErrorCollection
    {
        public SonarrServerError this[int index] => null!;

        public int Count => 0;

        public IEnumerator<SonarrServerError> GetEnumerator()
        {
            yield break;
        }

        public string GetMessage()
        {
            return string.Empty;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public sealed class ErrorCollection : List<SonarrServerError>, IErrorCollection
    {
        public ErrorCollection()
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

        public static readonly IErrorCollection Empty = new EmptyReadOnlyErrorCollection();
        public static IErrorCollection FromOne(SonarrServerError? one)
        {
            return one is not null
                ? new ErrorCollection(one)
                : Empty;
        }
        public string GetMessage()
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
