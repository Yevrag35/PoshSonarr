using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Cmdlets.Logging
{
    [Cmdlet(VerbsData.Save, "LogFile", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [Alias("Download-LogFile")]
    public class SaveLogFile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public string DownloadUrl { get; set; }

        [Parameter(Mandatory = true, Position = 0)]
        public string Path { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (!Directory.Exists(this.Path))
                throw new ArgumentException("The output folder specified does not exist.");

            Task task = this.DownloadLogFile(this.Path, this.DownloadUrl);
            task.Wait();
        }

        #endregion

        #region BACKEND METHODS
        private async Task DownloadLogFile(string folderPath, string downloadUrl)
        {
            string fileName = System.IO.Path.GetFileName(downloadUrl);
            string fullPath = folderPath + @"\" + fileName;
            using (var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl))
            {
                using (
                    Stream contentStream = await (await Context.ApiCaller.SendAsync(request)).Content.ReadAsStreamAsync(),
                        stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 16384, true))
                {
                    await contentStream.CopyToAsync(stream);
                }
            }
        }

        #endregion
    }
}