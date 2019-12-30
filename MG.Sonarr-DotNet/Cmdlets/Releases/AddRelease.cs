using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets.Releases
{
    /// <summary>
    ///     <para type="synopsis">Adds a release to be sent to a download client.</para>
    ///     <para type="description">
    ///         Takes a given release url and indicates to Sonarr that it should be downloaded.
    ///     </para>
    /// </summary>
    /// <example>
    ///     <para>Add from pipeline input</para>
    ///     <code>Get-SonarrSeries veep | Get-SonarrEpisode -EpisodeIdentifier "s1e7" | Search-SonarrRelease | Add-SonarrRelease</code>
    /// </example>
    [Cmdlet(VerbsCommon.Add, "Release", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(Release))]
    public class AddRelease : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/release";
        private bool _passThru;

        #endregion

        #region PARAMETERS
        /// <summary>
        /// <para type="description">The url for the release to add.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("guid")]
        public Uri ReleaseUrl { get; set; }

        /// <summary>
        /// <para type="description">The id for the indexer that release came from.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public int IndexerId { get; set; }

        /// <summary>
        ///     <para type="description">Passes through the Release object to show the result.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.ShouldProcess(string.Format("Release - {0}", this.ReleaseUrl.ToString()), "Add"))
            {
                SonarrBodyParameters sbp = this.GetBodyParameters();
                Release release = base.SendSonarrPost<Release>(EP, sbp);
                if (_passThru)
                    base.SendToPipeline(release);
            }
        }

        #endregion

        #region METHODS
        private SonarrBodyParameters GetBodyParameters()
        {
            return new SonarrBodyParameters(2)
            {
                { "guid", this.ReleaseUrl },
                { "indexerId", this.IndexerId }
            };
        }

        #endregion
    }
}