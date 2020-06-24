using MG.Sonarr.Functionality;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public sealed class ManualImportPost : BaseResult
    {
        public SonarrBodyParameters Body { get; }

        private ManualImportPost(ICollection<ManualImport> importThese) : base()
        {
            this.Body = new SonarrBodyParameters(importThese.Count);


            this.Body.Add("body", new JObject
            {
                new JProperty("completionMessage", "Completed"),
                new JProperty("importMode", "move"),
                new JProperty("name", "ManualImport"),
                new JProperty("sendUpdatesToClient", true),
                new JProperty("trigger", "manual"),
                new JProperty("updateScheduledTask", true),
                new JProperty("files", new JArray
                {

                })
            });
        }

        private IEnumerable<JObject> FormatImport(IEnumerable<ManualImport> importThese)
        {
            foreach (ManualImport im in importThese)
            {
                yield return new JObject
                {
                    new JProperty("episodeIds", JArray.FromObject(im.Episodes.Select(x => x.EpisodeId))),
                    new JProperty("path", im.FullPath),
                    new JProperty("quality", im._quality),
                    new JProperty("seriesId", im._series.TVDBId)
                };
            }
        }
    }
}
