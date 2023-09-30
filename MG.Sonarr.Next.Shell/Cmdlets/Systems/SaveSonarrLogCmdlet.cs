﻿using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Http;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Models.System;
using MG.Sonarr.Next.Shell.Attributes;
using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems
{
    [Cmdlet(VerbsData.Save, "SonarrLog", DefaultParameterSetName = "ByExplicitUrl")]
    public sealed class SaveSonarrLogCmdlet : SonarrCmdletBase, IApiCmdlet
    {
        bool _noFileName;
        ISonarrDownloadClient Downloader { get; }
        Queue<IApiCmdlet> Queue { get; }

        public SaveSonarrLogCmdlet()
            : base()
        {
            this.Downloader = this.Services.GetRequiredService<ISonarrDownloadClient>();
            this.Queue = this.Services.GetRequiredService<Queue<IApiCmdlet>>();
        }

        [Parameter(Mandatory = true, ParameterSetName = "ByExplicitUrl")]
        [ValidateUrl(UriKind.Relative)]
        public string LogUri { get; set; } = string.Empty;

        [Parameter]
        public SwitchParameter Force { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ParameterSetName = "ByPipelineInput", ValueFromPipeline = true)]
        public LogFileObject InputObject
        {
            get => null!;
            set => this.LogUri = value?.ContentsUrl ?? string.Empty;
        }

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [MaybeNull]
        [Parameter(Mandatory = false)]
        public PSCredential Credential
        {
            get => null;
            set => _creds = value.GetNetworkCredential();
        }
        NetworkCredential? _creds;

        protected override void Begin()
        {
            ReadOnlySpan<char> originalPath = this.Path.AsSpan();
            this.Path = this.GetAbsolutePath(this.Path);
            if (!originalPath.Equals(this.Path, StringComparison.InvariantCulture))
            {
                this.WriteDebug($"{nameof(this.Path)} resolved to -> {this.Path}");
            }

            if (IsFileExtensionNotTxtOrLog(this.Path, out bool isEmpty))
            {
                this.StopCmdlet(
                    new ArgumentException("If a target file extension is provided, it must be '.txt' or '.log'.")
                        .ToRecord(ErrorCategory.InvalidArgument, this.Path));

                return;
            }
            else if (isEmpty)
            {
                this.WriteDebug($"No file name detected in provided path. Will add from {nameof(this.LogUri)} or incoming {nameof(this.InputObject)}");
                _noFileName = true;
            }
        }
        protected override void Process()
        {
            this.Queue.Enqueue(this);
            string downloadPath = this.MakeFilePath(this.Path, this.LogUri, _noFileName);
            if (!this.Force && IOFile.Exists(downloadPath))
            {
                this.WriteError(new IOException($"The file '{downloadPath}' already exists.")
                    .ToRecord(ErrorCategory.WriteError, downloadPath));

                return;
            }

            this.WriteVerbose($"Writing response to -> {downloadPath}");
            var response = this.Downloader.DownloadToPath(this.LogUri, downloadPath);

            if (response.IsError)
            {
                this.WriteError(response.Error);
                return;
            }

            FileInfo fi = new(downloadPath);
            this.WriteObject(fi);
        }

        private string GetAbsolutePath(string providedPath)
        {
            string path = this.GetUnresolvedProviderPathFromPSPath(providedPath);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The provided path is invalid.", new DirectoryNotFoundException($"Unable to resolve the path -> {providedPath}"));
            }

            return path;
        }
        private static bool IsFileExtensionNotTxtOrLog(ReadOnlySpan<char> path, out bool isEmpty)
        {
            ReadOnlySpan<char> fileExt = IOPath.GetExtension(path);
            isEmpty = fileExt.IsEmpty;
            return !isEmpty
                   &&
                   !fileExt.Equals(stackalloc char[] { '.', 't', 'x', 't' },
                        StringComparison.InvariantCultureIgnoreCase)
                   &&
                   !fileExt.Equals(stackalloc char[] { '.', 'l', 'o', 'g' },
                        StringComparison.InvariantCultureIgnoreCase);
        }
        private string MakeFilePath(string dirPath, string logUrl, bool noFileExtension)
        {
            if (!noFileExtension)
            {
                return dirPath;
            }

            string fileName = IOPath.GetFileName(logUrl);
            this.WriteDebug($"Appending file name to {nameof(this.Path)} -> {fileName}");
            return IOPath.Combine(dirPath, fileName);
        }
        public void WriteVerbose(HttpRequestMessage request)
        {
            this.WriteVerbose($"Sending {request.Method.Method} request -> {request.RequestUri?.ToString()}");
        }

        bool _disposed;
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                this.Queue.Clear();
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
