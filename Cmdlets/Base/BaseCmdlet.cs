using MG.Api;
using Sonarr.Api.Results;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    public abstract class BaseCmdlet : PSCmdlet
    {
        public ApiCaller Api { get; set; }

        public void PipeBack<T>(ApiResult result, params string[] filters)
        {
            for (int i = 0; i < result.Count; i++)
            {
                var r = (dynamic)result[i];
                WriteObject((T)r);
            }
        }
    }
}
