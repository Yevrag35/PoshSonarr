using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Testing;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems;

/// <summary>
/// Represents the <c>Test-SonarrResource</c> cmdlet, which has Sonarr test providers: download clients, indexers,
/// and notifications.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the providers from the pipeline or from <c>-InputObject</c>, and for each one, posts it to the
/// <c>/test</c> path under its endpoint, such as <c>/indexer/test</c>, where Sonarr checks that the provider's settings
/// work.
/// </para>
/// <para>
/// For each provider, the cmdlet writes an object with the provider's <c>Id</c>, <c>IsSuccess</c>, which says whether
/// the test passed, <c>Type</c>, the provider's metadata tag, such as <c>#indexer</c>, and <c>Error</c>, the exception
/// that describes a failed test, or <see langword="null"/>. A failed test writes no error.
/// </para>
/// </remarks>
[Cmdlet(VerbsDiagnostic.Test, "SonarrResource")]
[MetadataCanPipe(Tag = Meta.DownloadClient)]
[MetadataCanPipe(Tag = Meta.Indexer)]
public sealed class TestSonarrResourceCmdlet : TimedCmdlet
{
	const string TEST = "/test";
	ITestingService _tester = null!;

	/// <summary>
	/// Gets or sets the providers to test.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, such as the objects that <c>Get-SonarrDownloadClient</c>,
	/// <c>Get-SonarrIndexer</c>, or <c>Get-SonarrNotification</c> writes. Each provider's ID has to be greater than zero, and a <see langword="null"/>
	/// array or element is turned away.
	/// </remarks>
	/// <value>The providers to test.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true)]
	[ValidateNotNull]
	[ValidateIds<ITestPipeable>(ValidateRangeKind.Positive)]
	public ITestPipeable[] InputObject { get; set; } = [];

	/// <summary>
	/// Runs the base class's setup for the scope, then resolves the service that sends the tests.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_tester = provider.GetRequiredService<ITestingService>();
	}

	/// <summary>
	/// Tests each provider in <c>-InputObject</c> and writes the outcome of each test to the pipeline.
	/// </summary>
	/// <remarks>
	/// The cmdlet starts its timer before each test, so the verbose line after each response reports that test's time.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		foreach (ITestPipeable testable in this.InputObject)
		{
			this.StartTimer();
			SonarrClientResult response = this.SendSingleTest(testable, this.StoppingToken);
			var obj = new
			{
				testable.Id,
				IsSuccess = !response.IsError,
				Type = testable.MetadataTag.Value,
				Error = response.Error?.Exception,
			};

			this.WriteObject(obj);
		}
	}

	/// <summary>
	/// Posts the specified provider to the <c>/test</c> path under its endpoint.
	/// </summary>
	/// <typeparam name="T">The type of the provider.</typeparam>
	/// <param name="sonarrObj">The provider to test.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The outcome of the test, which is an error when Sonarr finds the provider's settings don't work.</returns>
	private SonarrClientResult SendSingleTest<T>(T sonarrObj, CancellationToken token = default)
		where T : ITestPipeable
	{
		string url = GetUrl(TEST, sonarrObj.MetadataTag);
		return _tester.SendTest(url, sonarrObj, token);
	}

	/// <summary>
	/// Builds a path from the endpoint path of the specified tag followed by the specified segment.
	/// </summary>
	/// <param name="segmentToAdd">The segment to add after the endpoint path, such as <c>/test</c>.</param>
	/// <param name="tag">The metadata tag whose endpoint path the result starts with.</param>
	/// <returns>The endpoint path and the segment, such as <c>/indexer/test</c>.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="segmentToAdd"/> is empty.
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// Thrown when <paramref name="segmentToAdd"/> or <paramref name="tag"/> is null.
	/// </exception>
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
