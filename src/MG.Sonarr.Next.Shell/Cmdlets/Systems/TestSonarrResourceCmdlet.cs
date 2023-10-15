﻿using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Testing;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems
{
    [Cmdlet(VerbsDiagnostic.Test, "SonarrResource")]
    public sealed class TestSonarrResourceCmdlet : TimedCmdlet
    {
        const string TEST = "/test";
        const string TEST_ALL = "/testall";
        ITestingService _tester = null!;
        Queue<IApiCmdlet> _queue = null!;

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNull]
        public ITestPipeable[] InputObject { get; set; } = Array.Empty<ITestPipeable>();

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _queue = provider.GetRequiredService<Queue<IApiCmdlet>>();
            _tester = provider.GetRequiredService<ITestingService>();
        }

        protected override void Process(IServiceProvider provider)
        {
            foreach (ITestPipeable testable in InputObject)
            {
                _queue.Enqueue(this);
                StartTimer();
                var response = SendSingleTest(testable);
                var obj = new
                {
                    testable.Id,
                    IsSuccess = !response.IsError,
                    Type = testable.MetadataTag.Value,
                    Error = response.Error?.Exception,
                };

                WriteObject(obj);
            }
        }

        private SonarrResponse SendSingleTest<T>(T sonarrObj, CancellationToken token = default)
            where T : ITestPipeable
        {
            string url = GetUrl(TEST, sonarrObj.MetadataTag);
            return _tester.SendTest(url, sonarrObj, token);
        }

        private static string GetUrl([ConstantExpected] string segmentToAdd, MetadataTag tag)
        {
            ArgumentException.ThrowIfNullOrEmpty(segmentToAdd);
            ArgumentNullException.ThrowIfNull(tag);

            return string.Create(segmentToAdd.Length + tag.UrlBase.Length, (segmentToAdd, tag),
                (chars, state) =>
                {
                    state.tag.UrlBase.CopyTo(chars);
                    state.segmentToAdd.CopyTo(chars.Slice(state.tag.UrlBase.Length));
                });
        }
    }
}