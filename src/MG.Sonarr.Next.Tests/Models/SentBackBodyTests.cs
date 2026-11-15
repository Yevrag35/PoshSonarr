using System.Text.Json.Nodes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Models.Config;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Media;
using MG.Sonarr.Next.Models.Notifications;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Models.Renames;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Models.System;
using JsonObject = System.Text.Json.Nodes.JsonObject;

namespace MG.Sonarr.Next.Tests.Models;

/// <summary>
/// Tests the body of each request that sends Sonarr a model, or a body that the library builds from models, against
/// what Sonarr sent for those models.
/// </summary>
/// <remarks>
/// The fixtures are the responses that the sandbox's Sonarr, version 4.0.20.3014, gave to a GET of each model, so they
/// hold every field that Sonarr sends and none that it doesn't. The download client, indexer, notification, and release
/// profile were created in the sandbox under the name <c>sandbox-temp-fixture</c> to be read, and deleted afterward.
/// </remarks>
public sealed class SentBackBodyTests
{
	/// <summary>
	/// The values that a series capitalizes, writes with seconds, and writes as a date, at the specified path of a
	/// series in Sonarr's JSON.
	/// </summary>
	private static readonly Func<string, Normalization[]> s_seriesNormalizations = at =>
	[
		new Capitalized($"{at}seriesType", $"{at}status"),
		new WithSeconds($"{at}airTime"),
		new AsDate($"{at}firstAired"),
	];

	/// <summary>
	/// The models that a request sends back, each with its fixture, the change that a caller makes before sending it,
	/// and the changes that the model makes to what Sonarr sent.
	/// </summary>
	private static readonly Dictionary<string, SentBack> s_cases = new(StringComparer.Ordinal)
	{
		["series"] = new("series-ended.json", typeof(SeriesObject), new AddTag(2), s_seriesNormalizations(string.Empty)),
		["series with no tags"] = new("series-continuing.json", typeof(SeriesObject), new AddTag(2), s_seriesNormalizations(string.Empty)),
		["series from a lookup"] = new(
			"series-lookup.json",
			typeof(AddSeriesObject),
			new AddFromLookup(),
			[.. s_seriesNormalizations(string.Empty), new Without("added")]),
		["episode with its series"] = new(
			"episode.json",
			typeof(EpisodeObject),
			new SetProperty("IsMonitored", false, "monitored"),
			s_seriesNormalizations("series.")),
		["host config"] = new(
			"host-config.json",
			typeof(HostObject),
			new SetProperty("InstanceName", "2024", "instanceName"),
			[new Capitalized("authenticationMethod", "certificateValidation", "logLevel", "proxyType", "updateMechanism")]),
		["host config without its secrets"] = new(
			"host-config.json",
			typeof(NoKeyHostObject),
			new SetProperty("InstanceName", "2024", "instanceName"),
			[new Capitalized("authenticationMethod", "certificateValidation", "logLevel", "proxyType", "updateMechanism")]),
		["naming config"] = new(
			"naming-config.json",
			typeof(NamingConfigObject),
			new SetProperty("StandardEpisodeFormat", "{Series Title} - S{season:00}E{episode:00}", "standardEpisodeFormat"),
			[]),
		["media management"] = new("media-management.json", typeof(MediaManagementObject), new SetProperty("RecycleBinCleanupDays", 14, "recycleBinCleanupDays"), []),
		["delay profile"] = new(
			"delay-profile.json",
			typeof(DelayProfileObject),
			new SetProperty("UsenetDelayInMins", 60, "usenetDelay"),
			[new Capitalized("preferredProtocol")]),
		["download client"] = new("download-client.json", typeof(DownloadClientObject), new AddTag(2), [new EmptySelectOptions()]),
		["download client template"] = new(
			"download-client-schema.json",
			typeof(DownloadClientSchemaObject),
			new SetProperty("Name", "007", "name"),
			[new EmptySelectOptions()]),
		["indexer"] = new("indexer.json", typeof(IndexerObject), new SetProperty("Priority", 10, "priority"), [new EmptySelectOptions()]),
		["notification"] = new(
			"notification.json",
			typeof(NotificationObject),
			new AddTag(2),
			[new EmptySelectOptions(), new With("isEnabled", false)]),
		["notification template"] = new(
			"notification-schema.json",
			typeof(NotificationSchemaObject),
			new SetProperty("Name", "2024", "name"),
			[new EmptySelectOptions()]),
		["release profile"] = new("release-profile.json", typeof(ReleaseProfileObject), new AddTag(2), []),
	};

	/// <summary>
	/// A model that a request sends back carries every field that Sonarr sent for it, with the JSON type and value that
	/// Sonarr sent, a date and time as the same instant, plus the change that the caller made. The only exceptions are
	/// the changes that the model makes to what Sonarr sent, which <see cref="s_cases"/> names and Sonarr reads back
	/// the same or doesn't take: a value capitalized for display, a time of day with seconds, a first air date without its
	/// time, the date that a lookup result was added on, which a new series doesn't have, left out, an empty list of
	/// choices for a provider's field that has none, and a notification's computed <c>isEnabled</c>. After the request,
	/// the model looks as it did after the caller's change, whether the
	/// request succeeded and the cmdlet committed it or failed and the cmdlet reset it, except that a reset puts back the
	/// tags that the caller changed.
	/// </summary>
	/// <param name="name">The model, by its key in <see cref="s_cases"/>.</param>
	/// <param name="succeeded">
	/// <see langword="true"/> to commit the model, as after a request that succeeds; <see langword="false"/> to reset it,
	/// as after one that fails.
	/// </param>
	[Theory]
	[MemberData(nameof(Cases))]
	public void SendsBackWhatSonarrSentWithTheCallersChange(string name, bool succeeded)
	{
		SentBack sent = s_cases[name];
		string fixture = ReadFixture(sent.File);

		SonarrObject model = Assert.IsAssignableFrom<SonarrObject>(JsonSerializer.Deserialize(fixture, sent.Model, ModuleServices.Json.ForDeserializing));
		model.SetTag(ModuleServices.Resolver);
		string before = ModelDescription.Describe(model);

		sent.Change.Apply(model);
		string changed = ModelDescription.Describe(model);

		string body = JsonSerializer.Serialize(model, model.GetType(), ModuleServices.Json.ForSerializing);
		if (succeeded)
		{
			model.Commit();
		}
		else
		{
			model.Reset();
		}

		Assert.Equal(!succeeded && sent.Change.IsUndoneByReset ? before : changed, ModelDescription.Describe(model));

		JsonNode expected = JsonNode.Parse(fixture)!;
		foreach (Normalization normalization in sent.Normalizations)
		{
			normalization.ApplyTo(expected);
		}

		sent.Change.ApplyTo(expected);
		using JsonDocument expectedDocument = JsonDocument.Parse(expected.ToJsonString());
		using JsonDocument bodyDocument = JsonDocument.Parse(body);

		List<string> differences = [];
		Compare(expectedDocument.RootElement, bodyDocument.RootElement, "$", differences);
		Assert.True(
			differences.Count == 0,
			string.Join(Environment.NewLine, ["The body differs from what Sonarr sent in:", .. differences.Order(StringComparer.Ordinal)]));
	}

	public static TheoryData<string, bool> Cases
	{
		get
		{
			TheoryData<string, bool> data = [];
			foreach (string name in s_cases.Keys)
			{
				data.Add(name, true);
				data.Add(name, false);
			}

			return data;
		}
	}

	/// <summary>
	/// <see cref="PostRename.FromRenameObjects(IEnumerable{IRenameFilePipeable})"/> builds one rename command for each
	/// series, in order of series ID, each with the series' ID and the IDs of its files, once each and in order, under
	/// the command's name.
	/// </summary>
	[Fact]
	public void BuildsOneRenameCommandForEachSeries()
	{
		(int SeriesId, int FileId)[] files = [(2, 21), (1, 12), (1, 11), (2, 21), (1, 12)];
		RenameObject[] renames =
		[
			.. files.Select(file => Read<RenameObject>($$"""
				{
					"seriesId": {{file.SeriesId}},
					"seasonNumber": 1,
					"episodeNumbers": [1],
					"episodeFileId": {{file.FileId}},
					"existingPath": "Season 1/Sandbox Ended Show - S01E01.mkv",
					"newPath": "Season 1/Sandbox Ended Show - S01E01 - Pilot.mkv"
				}
				""")),
		];

		string[] bodies = [.. PostRename.FromRenameObjects(renames).Select(body => JsonSerializer.Serialize(body, ModuleServices.Json.ForSerializing))];

		AssertJson(
			[
				"""{"files":[11,12],"name":"RenameFiles","seriesId":1}""",
				"""{"files":[21],"name":"RenameFiles","seriesId":2}""",
			],
			bodies);
	}

	/// <summary>
	/// <see cref="ManualImportPostMapper.ToManualImportFiles(MetadataList{ManualImportObject})"/> turns each file that
	/// Sonarr found into the body that imports it: its path, its series' ID, its episodes' IDs, its quality and its
	/// languages as Sonarr sent them, its release group, its indexer flags, and its release type.
	/// </summary>
	[Fact]
	public void BuildsAManualImportBodyFromWhatSonarrFound()
	{
		string series = ReadFixture("series-ended.json");
		ManualImportObject found = Read<ManualImportObject>($$"""
			{
				"id": 1,
				"path": "/downloads/Sandbox.Ended.Show.S01E01.1080p.WEB-DL-126811/Sandbox.Ended.Show.S01E01.1080p.WEB-DL-126811.mkv",
				"relativePath": "Sandbox.Ended.Show.S01E01.1080p.WEB-DL-126811.mkv",
				"folderName": "Sandbox.Ended.Show.S01E01.1080p.WEB-DL-126811",
				"name": "Sandbox.Ended.Show.S01E01.1080p.WEB-DL-126811",
				"size": 1234567890,
				"series": {{series}},
				"seasonNumber": 1,
				"episodes": [
					{ "seriesId": 1, "episodeFileId": 0, "seasonNumber": 1, "episodeNumber": 2, "title": "The Second", "airDate": "2020-01-13", "hasFile": false, "monitored": true, "id": 102 },
					{ "seriesId": 1, "episodeFileId": 0, "seasonNumber": 1, "episodeNumber": 1, "title": "Pilot", "airDate": "2020-01-06", "hasFile": false, "monitored": true, "id": 101 }
				],
				"releaseGroup": "126811",
				"quality": {
					"quality": { "id": 3, "name": "WEBDL-1080p", "source": "web", "resolution": 1080 },
					"revision": { "version": 1, "real": 0, "isRepack": false }
				},
				"languages": [{ "id": 1, "name": "English" }],
				"qualityWeight": 1,
				"customFormats": [],
				"customFormatScore": 0,
				"indexerFlags": 8,
				"releaseType": "singleEpisode",
				"rejections": []
			}
			""");

		string[] bodies = [.. new MetadataList<ManualImportObject>([found]).ToManualImportFiles()
			.Select(body => JsonSerializer.Serialize(body, ModuleServices.Json.ForSerializing))];

		AssertJson(
			[
				"""
				{
					"path": "/downloads/Sandbox.Ended.Show.S01E01.1080p.WEB-DL-126811/Sandbox.Ended.Show.S01E01.1080p.WEB-DL-126811.mkv",
					"seriesId": 1,
					"episodeIds": [101, 102],
					"quality": {
						"quality": { "id": 3, "name": "WEBDL-1080p", "source": "web", "resolution": 1080 },
						"revision": { "version": 1, "real": 0, "isRepack": false }
					},
					"languages": [{ "id": 1, "name": "English" }],
					"releaseGroup": "126811",
					"indexerFlags": 8,
					"releaseType": "singleEpisode"
				}
				""",
			],
			bodies);
	}

	/// <summary>
	/// A command's body names the command and its display name, asks Sonarr to send updates to the client, gives whether
	/// to update the command's scheduled task, and gives a manual trigger and the command's priority.
	/// </summary>
	[Fact]
	public void WritesACommandAsSonarrReadsIt()
	{
		PostCommand command = new()
		{
			CommandName = "Refresh Series",
			Name = "RefreshSeries",
			Priority = CommandPriority.High,
			UpdateScheduledTask = false,
		};

		AssertJson(
			["""{"name":"RefreshSeries","commandName":"Refresh Series","sendUpdatesToClient":true,"updateScheduledTask":false,"trigger":"manual","priority":"High"}"""],
			[JsonSerializer.Serialize(command, ModuleServices.Json.ForSerializing)]);
	}

	/// <summary>
	/// Asserts that each body is the JSON that's expected of it, with its properties in any order.
	/// </summary>
	private static void AssertJson(string[] expected, string[] bodies)
	{
		Assert.Equal(expected.Length, bodies.Length);
		for (int i = 0; i < expected.Length; i++)
		{
			Assert.True(
				JsonNode.DeepEquals(JsonNode.Parse(expected[i]), JsonNode.Parse(bodies[i])),
				$"Body {i} is {bodies[i]}, where {JsonNode.Parse(expected[i])!.ToJsonString()} was expected.");
		}
	}

	/// <summary>
	/// Adds a difference for each place where a body doesn't carry what Sonarr sent: a property that's missing or that
	/// Sonarr didn't send, an array of another length, or a value of another type or another value. A string that's a
	/// date and time matches one that gives the same instant, since a model reads one as a <see cref="DateTimeOffset"/>.
	/// </summary>
	private static void Compare(JsonElement sent, JsonElement written, string path, List<string> differences)
	{
		switch (sent.ValueKind)
		{
			case JsonValueKind.Object when written.ValueKind == JsonValueKind.Object:
				foreach (JsonProperty property in sent.EnumerateObject())
				{
					if (written.TryGetProperty(property.Name, out JsonElement value))
					{
						Compare(property.Value, value, $"{path}.{property.Name}", differences);
					}
					else
					{
						differences.Add($"{path}.{property.Name} is missing");
					}
				}

				foreach (JsonProperty property in written.EnumerateObject())
				{
					if (!sent.TryGetProperty(property.Name, out _))
					{
						differences.Add($"{path}.{property.Name} is added as {property.Value.GetRawText()}");
					}
				}

				break;

			case JsonValueKind.Array when written.ValueKind == JsonValueKind.Array && sent.GetArrayLength() == written.GetArrayLength():
				for (int i = 0; i < sent.GetArrayLength(); i++)
				{
					Compare(sent[i], written[i], string.Create(CultureInfo.InvariantCulture, $"{path}[{i}]"), differences);
				}

				break;

			case JsonValueKind.String when written.ValueKind == JsonValueKind.String
				&& (sent.GetString() == written.GetString() || IsSameInstant(sent.GetString()!, written.GetString()!)):
			case JsonValueKind.Number when written.ValueKind == JsonValueKind.Number && sent.GetRawText() == written.GetRawText():
			case JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null when written.ValueKind == sent.ValueKind:
				break;

			default:
				differences.Add($"{path} is {written.GetRawText()} where Sonarr sent {sent.GetRawText()}");
				break;
		}
	}

	/// <summary>
	/// Determines whether two strings are each a date and time, and give the same instant.
	/// </summary>
	private static bool IsSameInstant(string sent, string written)
	{
		return sent.Contains('T', StringComparison.Ordinal)
			&& written.Contains('T', StringComparison.Ordinal)
			&& DateTimeOffset.TryParse(sent, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset sentInstant)
			&& DateTimeOffset.TryParse(written, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset writtenInstant)
			&& sentInstant == writtenInstant;
	}

	/// <summary>
	/// Reads a model from JSON as a response does, and tags it.
	/// </summary>
	private static T Read<T>(string json)
		where T : SonarrObject
	{
		T model = JsonSerializer.Deserialize<T>(json, ModuleServices.Json.ForDeserializing)
			?? throw new InvalidOperationException("The JSON read as null.");
		model.SetTag(ModuleServices.Resolver);
		return model;
	}

	/// <summary>
	/// Reads a fixture from the folder beside the test assembly.
	/// </summary>
	private static string ReadFixture(string file)
	{
		return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", file));
	}

	/// <summary>
	/// A model that a request sends back.
	/// </summary>
	/// <param name="File">The fixture that holds what Sonarr sent for the model.</param>
	/// <param name="Model">The model's type.</param>
	/// <param name="Change">The change that a caller makes before sending it.</param>
	/// <param name="Normalizations">The changes that the model makes to what Sonarr sent.</param>
	private sealed record SentBack(string File, Type Model, Change Change, Normalization[] Normalizations);

	/// <summary>
	/// A change that a model makes to what Sonarr sent, which Sonarr reads back the same or doesn't take from a request,
	/// as the sandbox's Sonarr showed.
	/// </summary>
	private abstract record Normalization
	{
		/// <summary>
		/// Makes the change to Sonarr's JSON for the model, as the body should carry it.
		/// </summary>
		internal abstract void ApplyTo(JsonNode json);

		/// <summary>
		/// Finds the object that holds the value at a dotted path, such as <c>series.status</c>.
		/// </summary>
		protected static JsonObject Parent(JsonNode json, string path, out string name)
		{
			string[] names = path.Split('.');
			JsonNode parent = json;
			foreach (string step in names[..^1])
			{
				parent = parent[step]!;
			}

			name = names[^1];
			return parent.AsObject();
		}
	}

	/// <summary>
	/// Values that the model gives an uppercase first letter for display, such as an enum's name, which Sonarr reads in
	/// any case.
	/// </summary>
	private sealed record Capitalized(params string[] Paths) : Normalization
	{
		internal override void ApplyTo(JsonNode json)
		{
			foreach (string path in this.Paths)
			{
				JsonObject parent = Parent(json, path, out string name);
				string value = parent[name]!.GetValue<string>();
				parent[name] = string.Concat(value[..1].ToUpperInvariant(), value[1..]);
			}
		}
	}

	/// <summary>
	/// A time of day, which the model reads as a <see cref="TimeOnly"/> and writes with seconds. Sonarr doesn't take a
	/// series' air time from a request.
	/// </summary>
	private sealed record WithSeconds(string Path) : Normalization
	{
		internal override void ApplyTo(JsonNode json)
		{
			JsonObject parent = Parent(json, this.Path, out string name);
			TimeOnly time = TimeOnly.Parse(parent[name]!.GetValue<string>(), CultureInfo.InvariantCulture);
			parent[name] = time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
		}
	}

	/// <summary>
	/// A first air date, which the model holds as a <see cref="DateOnly"/> and writes without its time. Sonarr doesn't
	/// take a series' first air date from a request.
	/// </summary>
	private sealed record AsDate(string Path) : Normalization
	{
		internal override void ApplyTo(JsonNode json)
		{
			JsonObject parent = Parent(json, this.Path, out string name);
			DateTimeOffset aired = DateTimeOffset.Parse(parent[name]!.GetValue<string>(), CultureInfo.InvariantCulture);
			parent[name] = DateOnly.FromDateTime(aired.UtcDateTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}

	/// <summary>
	/// A value that the model leaves out.
	/// </summary>
	private sealed record Without(string Path) : Normalization
	{
		internal override void ApplyTo(JsonNode json)
		{
			_ = Parent(json, this.Path, out string name).Remove(name);
		}
	}

	/// <summary>
	/// A value that the model adds, which Sonarr doesn't read.
	/// </summary>
	private sealed record With(string Path, object Value) : Normalization
	{
		internal override void ApplyTo(JsonNode json)
		{
			Parent(json, this.Path, out string name)[name] = JsonSerializer.SerializeToNode(this.Value);
		}
	}

	/// <summary>
	/// The empty list of choices that the model gives each provider field that Sonarr sent none for, which Sonarr doesn't
	/// read from a request.
	/// </summary>
	private sealed record EmptySelectOptions : Normalization
	{
		internal override void ApplyTo(JsonNode json)
		{
			foreach (JsonNode? field in json["fields"]!.AsArray())
			{
				JsonObject properties = field!.AsObject();
				if (!properties.ContainsKey("selectOptions"))
				{
					properties["selectOptions"] = new JsonArray();
				}
			}
		}
	}

	/// <summary>
	/// A change that a caller makes to a model before a request sends it.
	/// </summary>
	private abstract record Change
	{
		/// <summary>
		/// Gets a value that indicates whether a reset after a failed request takes the change out again.
		/// </summary>
		/// <value><see langword="true"/> for a change to a model's tags; otherwise, <see langword="false"/>.</value>
		internal abstract bool IsUndoneByReset { get; }

		/// <summary>
		/// Makes the change to a model.
		/// </summary>
		internal abstract void Apply(SonarrObject model);

		/// <summary>
		/// Makes the change to Sonarr's JSON for the model, as the body should carry it.
		/// </summary>
		internal abstract void ApplyTo(JsonNode json);
	}

	/// <summary>
	/// Adds a tag to a model, as <c>Add-SonarrTag</c> does.
	/// </summary>
	private sealed record AddTag(int Id) : Change
	{
		internal override bool IsUndoneByReset => true;

		internal override void Apply(SonarrObject model)
		{
			_ = ((ITagPipeable)model).Tags.Add(this.Id);
		}

		internal override void ApplyTo(JsonNode json)
		{
			int[] tags = [.. json["tags"]!.AsArray().Select(tag => tag!.GetValue<int>()).Append(this.Id).Distinct().Order()];
			json["tags"] = new JsonArray([.. tags.Select(tag => (JsonNode)tag)]);
		}
	}

	/// <summary>
	/// Sets a writable property of a model, as a user does in PowerShell.
	/// </summary>
	private sealed record SetProperty(string Property, object Value, string Field) : Change
	{
		internal override bool IsUndoneByReset => false;

		internal override void Apply(SonarrObject model)
		{
			model.Properties[this.Property].Value = this.Value;
		}

		internal override void ApplyTo(JsonNode json)
		{
			json[this.Field] = JsonSerializer.SerializeToNode(this.Value);
		}
	}

	/// <summary>
	/// Readies a lookup result to be added as <c>Add-SonarrSeries -RootFolderPath /tv -QualityProfileId 1</c> does.
	/// </summary>
	private sealed record AddFromLookup : Change
	{
		internal override bool IsUndoneByReset => false;

		internal override void Apply(SonarrObject model)
		{
			AddSeriesObject series = (AddSeriesObject)model;
			series.Path = "/tv";
			series.IsFullPath = false;
			series.AddOptions = SeriesAddOptions.Default;
			series.QualityProfileId = 1;
		}

		internal override void ApplyTo(JsonNode json)
		{
			json["qualityProfileId"] = 1;
			json["rootFolderPath"] = "/tv";
			json["addOptions"] = new JsonObject
			{
				["ignoreEpisodesWithFiles"] = true,
				["ignoreEpisodesWithoutFiles"] = true,
				["searchForMissingEpisodes"] = false,
			};
		}
	}
}
