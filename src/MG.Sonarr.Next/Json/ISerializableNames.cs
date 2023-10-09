using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Models;

namespace MG.Sonarr.Next.Json
{
    public interface ISerializableNames<T> where T : SonarrObject
    {
        static virtual IReadOnlyDictionary<string, string> GetDeserializedNames()
        {
            return EmptyNameDictionary.Default;
        }

        static virtual IReadOnlyDictionary<string, string> GetSerializedNames()
        {
            return EmptyNameDictionary.Default;
        }
    }
}
