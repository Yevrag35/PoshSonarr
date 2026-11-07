using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Releases;
using MG.Sonarr.Next.Models.Series;

namespace MG.Sonarr.Next.Tests.Models;

/// <summary>
/// Tests that a model that a request sends looks the same afterward as it did before.
/// </summary>
public sealed class RequestRoundTripTests
{
	private const string SERIES = """
		{
			"id": 1,
			"title": "Sandbox Continuing Show",
			"sortTitle": "sandbox continuing show",
			"status": "continuing",
			"overview": "A show that keeps going.",
			"network": "Sandbox",
			"seasons": [{ "seasonNumber": 1, "monitored": true }],
			"year": 2021,
			"path": "/tv/Sandbox Continuing Show",
			"qualityProfileId": 1,
			"languageProfileId": 1,
			"seasonFolder": true,
			"monitored": true,
			"tvdbId": 9000002,
			"seriesType": "standard",
			"tags": [1],
			"added": "2024-01-15T10:30:00Z"
		}
		""";

	private const string SERIES_WITH_FIRST_AIRED = """
		{
			"id": 2,
			"title": "Sandbox Ended Show",
			"status": "ended",
			"overview": "A show that ended.",
			"firstAired": "2020-01-01",
			"seasonFolder": true,
			"monitored": true,
			"tvdbId": 9000001,
			"seriesType": "standard",
			"tags": []
		}
		""";

	private const string EPISODE_WITHOUT_AIR_DATE = """
		{
			"id": 13,
			"seriesId": 1,
			"episodeFileId": 0,
			"seasonNumber": 1,
			"episodeNumber": 3,
			"title": "TBA",
			"hasFile": false,
			"monitored": true
		}
		""";

	private const string EPISODE_WITH_SERIES = $$"""
		{
			"id": 12,
			"seriesId": 2,
			"episodeFileId": 0,
			"seasonNumber": 1,
			"episodeNumber": 2,
			"absoluteEpisodeNumber": 2,
			"title": "The Second",
			"airDate": "2020-01-08",
			"airDateUtc": "2020-01-08T01:00:00Z",
			"hasFile": false,
			"monitored": true,
			"series": {{SERIES_WITH_FIRST_AIRED}}
		}
		""";

	private const string RELEASE = """
		{
			"guid": "sandbox-release-1",
			"indexerId": 1,
			"indexer": "Sandbox",
			"title": "Sandbox.Ended.Show.S01E02.1080p",
			"size": 1234567890,
			"age": 1,
			"ageHours": 30.5,
			"ageMinutes": 1830.25,
			"qualityWeight": 101,
			"languageWeight": 0,
			"preferredWordScore": 0,
			"releaseWeight": 0,
			"protocol": "torrent",
			"seeders": 10
		}
		""";

	/// <summary>
	/// The models whose request bodies hold values that their deserialization took out or changed, with the fields that
	/// the body has to carry and the fields that it mustn't.
	/// </summary>
	private static readonly Dictionary<string, (Type Model, string Json, string[] Sent, string[] NotSent)> s_models =
		new(StringComparer.Ordinal)
		{
			["series without firstAired"] = (typeof(SeriesObject), SERIES, ["title", "seasonFolder"], ["firstAired"]),
			["series with firstAired"] = (typeof(SeriesObject), SERIES_WITH_FIRST_AIRED, ["firstAired"], []),
			["episode without airDate"] = (typeof(EpisodeObject), EPISODE_WITHOUT_AIR_DATE, ["episodeNumber"], ["airDate"]),
			["episode with airDate and its series"] = (typeof(EpisodeObject), EPISODE_WITH_SERIES, ["airDate", "series.firstAired"], []),
			["release"] = (typeof(ReleaseObject), RELEASE, ["guid", "ageHours", "ageMinutes", "qualityWeight"], ["releaseUrl"]),
		};

	/// <summary>
	/// B88, B89, O57, B99: a model looks the same after a request that sends it as before, whether the request succeeds,
	/// after which the cmdlet commits the model, or fails, after which it resets it. <c>OnSerializing</c> puts back what
	/// deserialization took out, and only what Sonarr sent, and <c>Commit</c> and <c>Reset</c> take it out again, from a
	/// series that Sonarr nested in an episode too.
	/// </summary>
	/// <param name="model">The model, by its key in <see cref="s_models"/>.</param>
	/// <param name="succeeded">
	/// <see langword="true"/> to commit the model, as after a request that succeeds; <see langword="false"/> to reset it,
	/// as after one that fails.
	/// </param>
	[Theory]
	[MemberData(nameof(Models))]
	public void B88_AModelLooksTheSameAfterARequestThatSendsIt(string model, bool succeeded)
	{
		(Type type, string json, string[] sent, string[] notSent) = s_models[model];

		SonarrObject read = Assert.IsAssignableFrom<SonarrObject>(
			JsonSerializer.Deserialize(json, type, ModuleServices.Json.ForDeserializing));
		read.SetTag(ModuleServices.Resolver);
		string before = ModelDescription.Describe(read);

		string body = JsonSerializer.Serialize(read, read.GetType(), ModuleServices.Json.ForSerializing);
		if (succeeded)
		{
			read.Commit();
		}
		else
		{
			read.Reset();
		}

		Assert.Equal(before, ModelDescription.Describe(read));

		using JsonDocument document = JsonDocument.Parse(body);
		Assert.Multiple(
		[
			.. sent.Select(path => (Action)(() => Assert.True(Has(document.RootElement, path), $"The body lacks '{path}': {body}"))),
			.. notSent.Select(path => (Action)(() => Assert.False(Has(document.RootElement, path), $"The body has '{path}': {body}"))),
		]);
	}

	public static TheoryData<string, bool> Models
	{
		get
		{
			TheoryData<string, bool> data = [];
			foreach (string model in s_models.Keys)
			{
				data.Add(model, true);
				data.Add(model, false);
			}

			return data;
		}
	}

	/// <summary>
	/// Determines whether a JSON object has a property at a dotted path, such as <c>series.firstAired</c>.
	/// </summary>
	private static bool Has(JsonElement element, string path)
	{
		foreach (string name in path.Split('.'))
		{
			if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out element))
			{
				return false;
			}
		}

		return true;
	}
}
