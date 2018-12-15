using MG.Api;
using Sonarr.Api.Results;
using System.Collections.Generic;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    public abstract class BaseCmdlet : PSCmdlet
    {
        internal ApiCaller Api
        {
            get => SonarrServiceContext.TheCaller;
            set => SonarrServiceContext.TheCaller = value;
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (!SonarrServiceContext.IsSet)
                throw new SonarrContextNotSetException("  Run the 'Connect-Sonarr' cmdlet first.");
        }

        //public void PipeBack<T>(T result, params string[] filters) where T : SonarrResult
        //{
        //    for (int i = 0; i < result.Count; i++)
        //    {
        //        var r = (dynamic)result[i];
        //        WriteObject((T)r);
        //    }
        //}

        //public T[] ResultWithNoOutput<T>(ApiResult[] results) where T : SonarrResult
        //{
        //    var tArr = new List<T>();
        //    for (int i = 0; i < results.Length; i++)
        //    {
        //        var ar = results[i];
        //        for (int r = 0; r < ar.Count; r++)
        //        {
        //            var res = (dynamic)ar[r];
        //            tArr.Add((T)res);
        //        }
        //    }
        //    return tArr.ToArray();
        //}
    }
}
