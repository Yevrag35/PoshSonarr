---
paths:
- "src/MG.Sonarr.Next/Models/**/*.cs"
- "src/MG.Sonarr.Next/Attributes/SonarrObject*.cs"
- "src/MG.Sonarr.Next/Json/SonarrJsonOptions.cs"
- "src/MG.Sonarr.Next/Json/ISerializableNames.cs"
- "src/MG.Sonarr.Next/Json/IJsonSonarrMetadata.cs"
- "src/MG.Sonarr.Next/Json/Converters/SonarrObjectConverter.cs"
- "src/MG.Sonarr.Next/Json/Converters/ObjectConverter.cs"
- "src/MG.Sonarr.Next/Json/Converters/ImmutableArrayConverter.cs"
- "src/MG.Sonarr.Next/Metadata/IPipeables.cs"
- "src/MG.Sonarr.Next/Metadata/MetadataResolverDI.cs"
- "src/MG.Sonarr.Next.Shell/PSFormats/*.ps1xml"
- "src/MG.Sonarr.Next.Shell/PSTypes/*.ps1xml"
---

# PSObject models in `MG.Sonarr.Next.Models`

The models are the classes under `src/MG.Sonarr.Next/Models/` that derive from `SonarrObject`, which derives from `PSObject`. Each one holds a Sonarr API resource as a PowerShell object. The JSON converter creates a model as an empty custom object, through the `PSObject(int capacity)` constructor, and adds a note property for each field of the resource's JSON object, so a model shows every field that Sonarr sends, including fields that Sonarr added after the model was written.

## What PowerShell sees of a model

- PowerShell reads a model's note properties and type data, not its C# members. In script, a C# property reads as `$null` and a C# method isn't found, while `$model.psobject.<Member>` reaches both (measured 2026-10-25 on PowerShell 7.6.8). Whatever a user, a format view, or type data should see is a note property or type data.
- A typed C# property exists only where C# code needs the value, such as an ID to pipe, a value to sort by, or a value that a cmdlet checks. It either reads and writes the note property or keeps a copy of it.
	- `this.GetValue<T>()`, `this.GetStringOrEmpty()`, and `this.SetValue(value)` default the note property's name to the calling member's name, through `[CallerMemberName]`. Leave the name out only in the accessor of the C# property that has the note property's name, and pass it everywhere else, as in `this.GetValue<string>(nameof(this.Name))`, since another member's name matches no property.
	- A copy, which the model sets while it's deserialized, doesn't follow a change that PowerShell makes to the note property afterward. Code that has to honor a user's edit reads the note property.
- The format and type files in `src/MG.Sonarr.Next.Shell/PSFormats/` and `src/MG.Sonarr.Next.Shell/PSTypes/` select a model by a type name that its `SetPSTypeName()` inserts, so renaming a model class or changing its namespace means updating them.

## Base classes

The three bases are abstract and sit in `src/MG.Sonarr.Next/Models/Bases/`, in the `MG.Sonarr.Next.Models` namespace.

### `SonarrObject : PSObject`

It implements `IJsonSonarrMetadata`, and through it `IJsonMetadataTaggable`, System.Text.Json's `IJsonOnDeserialized`, and `IResettable`.

- **Tag.** `MetadataTag` reads the `MetadataTag` note property, a read-only `MetadataProperty`, and is `MetadataTag.Empty` until the object is tagged. `SetTag(IMetadataResolver)` asks the abstract `GetTag(resolver, existing)` for the tag, passing `MetadataTag.Empty` as `existing`, and adds or replaces the property. A model returns `resolver[Meta.<NAME>]` for its endpoint, or `existing` when it stands for no endpoint, and a model whose `DisregardMetadataTag` returns `true` gets no `MetadataTag` property at all.
- **Deserialization.** The converter calls the public virtual `OnDeserialized()` once per object. It calls the protected virtual `OnDeserialized(bool alreadyCalled)`, empty here, and then, the first time only, `SetPSTypeName()`. A model that overrides `OnDeserialized()` itself calls `base.OnDeserialized()`; most models override `OnDeserialized(bool)` instead.
- **Type names.** `SetPSTypeName()` inserts `MG.Sonarr.Next.Models.SonarrObject` at index 0 of `TypeNames`. A model's override calls the base and then inserts the model's own full type name at index 0, which gives it the type names `<model>`, `MG.Sonarr.Next.Models.SonarrObject`, `System.Management.Automation.PSCustomObject`, and `System.Object` (measured 2026-10-25 on PowerShell 7.6.9). A model derived from another model replaces index 0 instead, so it doesn't carry the base model's name or match its format views. `SeriesObject` also inserts `SeriesBase` at index 1, which `AddSeriesObject` keeps.
- **Commit and reset.** The public virtual `Commit()` and `Reset()` are empty here. A cmdlet calls `Commit()` after a request that sends the model succeeds and `Reset()` after one fails, through `SonarrApiCmdletBase.TryCommitFromResponse` or directly. A model overrides them to take out what `OnSerializing()` put in and to bring its copies and note properties back in step. `IResettable.TryReset()` calls `Reset()`.
- **Nesting.** The internal virtual `ShouldBeReadOnly(propertyName, parentType)` returns `true`, and a model overrides it to become a writable property of a particular parent.

### `IdSonarrObject<TSelf> : SonarrObject`, where `TSelf : IdSonarrObject<TSelf>`

The base for a resource with a numeric ID. It adds `int Id`, with a private setter, `IHasId`, and `IComparable<TSelf>` through a virtual `CompareTo` that compares IDs and that models override with their own order. Its `OnDeserialized()` is `sealed`: it copies the `Id` note property, which the converter made read-only, into `Id` and then calls the base, so a derived model hooks deserialization only through `OnDeserialized(bool)`.

### `TagUpdateObject<TSelf> : IdSonarrObject<TSelf>`, where `TSelf : IdSonarrObject<TSelf>`

The base for a resource that carries tag IDs. It implements `ITagPipeable`, the input of `Add-SonarrTag`, `Clear-SonarrTag`, and `Get-SonarrTag`.

- `Tags` is a `SortedSet<int>`. `OnDeserialized(bool)` takes the set that the read-only `Tags` note property, a `ReadOnlyTagsProperty`, holds, so a change made to `Tags` in C# shows in PowerShell and in the next request body, while PowerShell can't replace the set. Setting `Tags` also takes a snapshot of its IDs.
- `Commit()` and `Reset()` are `sealed`. `Commit()` calls `CommitTags()`, which makes the current tags the snapshot, and then `OnCommit()`. `Reset()` puts the snapshot back into `Tags` and then calls `OnReset()`. A derived model overrides `OnCommit()` and `OnReset()`.
- `MustUpdateViaApi` tells a tag cmdlet whether to PUT the object after changing its tags: `true` for an object that exists on the server, and `false` for one that doesn't exist yet, such as a lookup result or a schema, whose tags go out with its own POST. `OnDeserialized(bool)` sets it to `true`, so a model for objects that don't exist on the server sets it to `false` after calling the base.
- A derived model's `OnDeserialized(bool)` calls `base.OnDeserialized(alreadyCalled)` first, and `IPipeable<ITagPipeable>.GetId()` returns `Id`.

### Models derived from models

`AddSeriesObject` derives from `SeriesObject`, and `NoKeyHostObject` from `HostObject`, so those two bases are unsealed and expose what the derived classes need: `SeriesObject`'s `protected SeriesObject(int capacity)` constructor and `private protected virtual int? GetSeriesId()`, `HostObject`'s `private protected Conditionals`, and a `public virtual OnSerializing()` on both. Seal a model unless another model derives from it.

## Interfaces

### `ISerializableNames<T>`, where `T : SonarrObject`

Declared in `src/MG.Sonarr.Next/Json/ISerializableNames.cs`. Every model implements it, with `T` as the class itself, because `SonarrObjectConverter<T>` requires it. Each converter reads its three static virtual methods once, and their defaults are empty:

- `GetDeserializedNames()` maps a JSON field's name, once its first letter is uppercased, to the note property's name, as `BackupObject` maps `Path` to `BackupUri`.
- `GetSerializedNames()` maps a note property's name back to the field's name for a request body.
- `GetPropertiesToCapitalize()` names the string properties whose values get an uppercase first letter, such as `CommandObject`'s `Status`.

A model keeps them in static fields: a `Lazy<JsonNameHolder>` from `JsonNameHolder.FromSingleNamePair` or `FromDeserializationNamePairs`, which builds both maps from the deserialization pairs, and a `HashSet<string>` or `OneStringSet` of the properties to capitalize. A model's own maps win over the renames that apply to every model, such as `Monitored` to `IsMonitored`, which `EnumerateGlobalReplaceNames` in `src/MG.Sonarr.Next/Json/SonarrJsonOptions.cs` lists.

A model derived from another declares the interface again for itself, as in `AddSeriesObject : SeriesObject, ISerializableNames<AddSeriesObject>`. The compiler then forwards each member to the base class's public static method of the same name, so `AddSeriesObject` uses `SeriesObject`'s maps, and a member that the base class doesn't declare keeps the empty default, as `NoKeyHostObject`'s two name maps do (measured 2026-10-25 with `Type.GetInterfaceMap`, on a build from the .NET SDK 10.0.403).

### Tagging, serialization, identity, and order

- `IJsonSonarrMetadata` and `IJsonMetadataTaggable`, in `src/MG.Sonarr.Next/Json/IJsonSonarrMetadata.cs`, declare `MetadataTag` and `SetTag`. `SonarrHttpClient` calls `SetTag` on what a GET returns, and on what a POST with a body returns, when it's `IJsonMetadataTaggable`: a model, a `MetadataList<T>`, which tags each item, or a `RecordResult<T>`.
- System.Text.Json's `IJsonOnSerializing` marks a model that changes its properties before it's written. `SonarrObjectConverter<T>.Write` calls `OnSerializing()` itself, as `Read` calls `OnDeserialized()`.
- `IHasId` comes with `IdSonarrObject<TSelf>`, and `NamingConfigObject`, `HostObject`, and `TagObject` implement it themselves. `IHasName` is on `TagObject`, as its `Label`.
- `MetadataList<T>` requires `IComparable<T>` of its items, and `SonarrHttpClient` sorts what a GET returns when it's `ISortable` and holds more than one item, so a model's `CompareTo` sets the order that a cmdlet writes.

### Pipeable interfaces

Cmdlets take pipeline input through the interfaces in `src/MG.Sonarr.Next/Metadata/IPipeables.cs`, not through model classes. Each `I…Pipeable` extends `IPipeable<I…Pipeable>` and exposes the ID that a parameter of its type acts on, and a model's `IPipeable<I…Pipeable>.GetId()` returns that same ID, because `[ValidateIds(ValidateRangeKind.Positive, typeof(I…Pipeable))]` checks it before the cmdlet sees the object, and by default turns away a `null`. A model that implements several returns a different ID from each, so it implements `GetId()` explicitly for each interface.

| Interface | Exposes | Implemented by |
| --- | --- | --- |
| `ISeriesPipeable` | `SeriesId` | `SeriesObject`, `EpisodeObject`, `EpisodeFileObject`, `CalendarObject`, `RenameObject` |
| `IEpisodeBySeriesPipeable`, `IEpisodeFileBySeriesPipeable`, `IReleasePipeableBySeries` | `SeriesId`, and `Title` on the first | `SeriesObject` |
| `IEpisodePipeable` | `EpisodeId` | `CalendarObject` |
| `IReleasePipeableByEpisode` | `EpisodeId` | `EpisodeObject` |
| `IEpisodeFilePipeable` | `EpisodeFileId` | `EpisodeFileObject`, `EpisodeObject`, `CalendarObject` |
| `IRenameFilePipeable` | `EpisodeFileId`, which `GetId()` returns, and `SeriesId` | `RenameObject`, `EpisodeFileObject`, `EpisodeObject`, `CalendarObject` |
| `ILanguageProfilePipeable`, `IQualityProfilePipeable` | `LanguageProfileId`, `QualityProfileId` | `SeriesObject` |
| `ITagPipeable` | `Id`, `Tags`, `MustUpdateViaApi` | every `TagUpdateObject<TSelf>` model |
| `ITestPipeable` | `Id` | `DownloadClientObject`, `IndexerObject`, `NotificationObject`, for `Test-SonarrResource` |

A model whose own `Id` is the exposed ID returns `Id`, as `SeriesObject` does for `SeriesId` and `EpisodeFileObject` for `EpisodeFileId`. A cmdlet's `[MetadataCanPipe(Tag = …)]` attributes only add its name to those tags' `CanPipeTo` lists, which are for display; PowerShell binds input by the parameter's interface.

### Other interfaces

- `ISchemaObject` (`IsTaggable`) marks a schema model: `NotificationSchemaObject`.
- `ITagResolvable<T>` declares a static `GetTag(IMetadataResolver)`, which gets a model's tag without an instance: `CalendarObject`.
- `ICommand` is what the command history keeps and `Get-SonarrCommand` takes: a `CommandObject`, or `CommandObject.Empty`, an internal `EmptyCommand`.

## Registering a converter with `[SonarrObject]`

`[SonarrObject]`, in `src/MG.Sonarr.Next/Attributes/SonarrObjectAttribute.cs`, is internal and not inherited, and it gives a model class a `SonarrObjectConverter<T>` of its own:

1. `src/MG.Sonarr.Next/MG.Sonarr.Next.csproj` puts the public `[assembly: SonarrObjectConverterAssembly]` on the services library through an `<AssemblyAttribute>` item.
2. `SonarrJsonDependencyInjection.AddSonarrJsonOptions`, in `src/MG.Sonarr.Next/Json/SonarrJsonOptions.cs`, registers `ISonarrJsonOptions` as a singleton whose factory builds the options when it's first resolved. `Connect-SonarrInstance` resolves it as soon as it builds a connection, through the `ISonarrClient` that it tests the connection with, and each connection builds its own.
3. `GetSonarrObjectTypes` goes through the assemblies loaded in the AppDomain, skipping dynamic ones and ones without `SonarrObjectConverterAssemblyAttribute`, and keeps each exported type that is a class, isn't abstract, has `[SonarrObject]` itself, and derives from `SonarrObject`.
4. `ConstructSonarrObjectConverters` closes `SonarrObjectConverter<T>`, where `T : SonarrObject, ISerializableNames<T>, new()`, over each type with `MakeGenericType`, and creates it around the shared `ObjectConverter`. The converters go into `ForDeserializing`, which `ForSerializing` and `ForDebugging` copy.
5. `ThrowIfMissingConverters` throws a `ModuleStartupException` when no converter results.

What follows from it:

- Give every concrete model class its own `[SonarrObject]`, a model derived from another model and one only ever read as a nested object included. A converter claims only its exact type (`SonarrObjectConverter<SeriesObject>.CanConvert(typeof(AddSeriesObject))` is `false`, measured 2026-10-25 on .NET 10.0.14), and a request body is serialized by its runtime type, so an `AddSeriesObject` that the code holds as a `SeriesObject` still needs its own.
- A model without a converter fails nowhere at startup. The scan skips a class without the attribute, an internal or abstract one, and one in an assembly without the assembly attribute. Reading or writing that model as its own type, as when a nested model inside a parent's request body is written, then falls to System.Text.Json's default object handling, which uses the class's public C# properties instead of its note properties.
- A marked class that doesn't implement `ISerializableNames<itself>`, or has no public parameterless constructor, makes `MakeGenericType` throw `ArgumentException` ("violates the constraint of type 'T'"), so every `Connect-SonarrInstance` fails, not just the cmdlets that use the model.

### Nested models and arrays

Converters don't read nested objects. `ObjectConverter.ReadObject` picks a model for a nested JSON object by the property's name and the parent's type, and makes any other nested object a plain `PSObject`:

- `Series`, `Episode`, and `EpisodeFile`, in any parent: a `SeriesObject`, an `EpisodeObject`, and an `EpisodeFileObject`.
- `Data` in a `HistoryObject`: a `ReleaseObject`.
- `Quality` in a `QualityRevisionObject` or a `QualityDefinitionObject`: a `QualityObject`, and in a `ManualImportObject`: a `QualityRevisionObject`.
- `Revision` in a `QualityRevisionObject`: a `RevisionObject`.

Each of these runs `OnDeserialized()` and `SetTag()` as soon as it's read. Arrays go by property name, through `EnumerateConverterProperties` in `SonarrJsonOptions.cs`: `Episodes` becomes a `SortedSet<EpisodeObject>`, `Fields` an `ImmutableArray<FieldObject>`, `SelectOptions` an `ImmutableArray<SelectOptionObject>`, and `Tags` a `SortedSet<int>`. The model's converter reads their items, through an `ImmutableArrayConverter<T>` for an immutable array, and the items get no tag. To nest another model, add a case to `ReadObject`, or an entry to `EnumerateConverterProperties` and, for an immutable array of models, an `ImmutableArrayConverter<T>` in `AddAllConverters`.

## Reading a response

`SonarrObjectConverter<T>.Read` calls `ObjectConverter.ConvertToObject<T>`, which creates the model with `new T()` and adds a property for each JSON field. It names the property after the field, with the first letter uppercased and then renamed by the model's map or else the global one, and picks the property's kind from the value:

- A numeric `id` becomes a `ReadOnlyNumberProperty<int>`, so PowerShell can't change a model's ID.
- A string, number, or boolean becomes a writable `StringNoteProperty`, `NumberNoteProperty<T>`, or `StructNoteProperty<T>`. A string that parses as a number, `Guid`, `DateOnly`, `DateTimeOffset`, or `Version`, tried in that order, becomes one, unless a span converter that `AddAllConverters` registers for the property's name claims it, such as `AirDate` as a `DateOnly` or `Title` always as a string. A `DateTimeOffset` turns into local time unless the property's name ends in `Utc`.
- A nested object becomes a read-only property. A nested model whose `ShouldBeReadOnly(propertyName, parentType)` returns `false` becomes a writable `WritableSonarrProperty<T>` instead, as `SeriesObject` and `QualityRevisionObject` do for a `ManualImportObject`'s `Series` and `Quality`, which the user sets before the import is posted.
- An array becomes the type that `EnumerateConverterProperties` gives its name, held in a `ReadOnlyTagsProperty` for `Tags`, a `ReadOnlyCollectionProperty<T>` for an immutable array, and a `ReadOnlySetProperty<T>` for a string set, or else an `object[]` in a `PSNoteProperty`.

The converter then calls `OnDeserialized()`, and `SonarrHttpClient` tags the result and sorts a list. While it's deserialized, a model copies the values that its C# properties keep, removes the properties that PowerShell shouldn't show or change, keeping the value of each one that a request body needs for `OnSerializing()`, swaps writable properties for read-only ones where Sonarr owns the value, and adds computed and alias properties. Each model's entry below says which.

## Writing a request

`SonarrObjectConverter<T>.Write` calls `OnSerializing()` when the model implements `IJsonOnSerializing`, and then writes each note property that it can get, except `MetadataTag` and `ShortOverview`, under its serialized name and in camel case. Alias and script properties aren't note properties, so they're left out, and a nested model is written by its own converter. `OnSerializing()` puts back what deserialization took out and checks or fills in values, and `Commit()` or `Reset()` takes it out again afterward, so the object looks the same after the request as before it.

## The models

Each entry gives the model's base and the `Meta` tag that its `GetTag` returns, with "no ID" for a tag that `MetadataResolverDI.AddMetadata` registers without ID support, whose `GetUrlForId` throws.

### Series, episodes, calendar, and renames

- `SeriesObject` (`TagUpdateObject`, `#series`): a series in the library. It pipes its `Id` through `GetSeriesId()` as `ISeriesPipeable` and the three `…BySeries` interfaces, and its profile IDs, which read and write their note properties, as the two profile interfaces. `Title` is a copy. It renames `ImdbId` to `IMDbId` and `SeasonFolder` to `UseSeasonFolders` both ways, and capitalizes `SeriesType` and `Status`. It holds `FirstAired` back, writes it in `OnSerializing()`, and takes it out again in `OnCommit()` and `OnReset()`. `ShortOverview` is an alias of `Overview` up to 90 characters, or else a read-only copy cut after a word and ended with `...`. Type data for `SeriesBase` adds a `Name` alias of `Title`, and a format view shows it.
- `AddSeriesObject` (`SeriesObject`, `#add_series`, no ID): a result of `/series/lookup`, which its own POST adds to the library. `GetSeriesId()` returns `null`, so `[ValidateIds]` turns it away where a series ID is expected. It removes `Id` and `Added`, makes `LanguageProfileId` a read-only 1, and sets `UseSeasonFolders` when the lookup lists more than one season. It adds `AddOptions`, a `SeriesAddOptions`, `IsMonitored`, a set-only `ProfileId`, `Path` with `IsFullPath`, `SeriesType`, and `UseSeasonFolders`. `OnSerializing()` writes `AddOptions` and `Title`, then the path, as a `Path` property when `IsFullPath` is set and as `RootFolderPath` otherwise, then the base's `FirstAired`, and `OnReset()` removes that path property and clears `AddOptions`. It sorts by title, its `ShouldBeReadOnly` is always `true`, and its type name replaces `SeriesObject`'s but keeps `SeriesBase`.
- `EpisodeObject` (`IdSonarrObject`, `#episode`): an episode. It copies the IDs that it pipes, and `AbsoluteEpisodeNumber`, with `HasAbsolute`, `EpisodeNumber`, and `SeasonNumber`. Its C# `Series` is the nested series' title, or else the series ID, which `SetSeries(IEpisodeBySeriesPipeable)` can replace, and episodes sort by series, season, episode, and absolute number. `Matches(IEpisodeIdentifier)` tests the episode against a season and episode range, or an absolute range. It holds `AirDate` back and writes it in `OnSerializing()`. Type data adds the aliases `Season`, `Episode`, and `Absolute`, and a format view shows it.
- `EpisodeFileObject` (`IdSonarrObject`, `#episode_file`): a file on disk. Its `Id` is the `EpisodeFileId` of `IEpisodeFilePipeable` and `IRenameFilePipeable`, and it pipes `SeriesId` as `ISeriesPipeable`.
- `CalendarObject` (`IdSonarrObject`, `#calendar`, no ID): an episode airing in a date range, from `/calendar`, whose `Id` is the episode's ID. It removes `AirDate`, keeping `AirDateUtc`, and sorts by `AirDateUtc`, then `Id`.
- `RenameObject` (`SonarrObject`, `#rename_file`, no ID): a file that a rename would change, from `/rename`. It pipes as `IRenameFilePipeable` and `ISeriesPipeable`, sorts by series and then file, and writes both IDs back in `OnSerializing()`. A format view shows it.
- Not models: `EpisodeRange` is an inclusive range of episode numbers, whose `Start` of 0 matches every episode, and `IEpisodeIdentifier` is a season and a range, or an absolute range. `PostRename` is the body of the `RenameFiles` command, and `FromRenameObjects` groups rename input into one body per series. `SeriesAddOptions` and `SeriesImage` are records for a series' add options and images.

### Download clients, indexers, notifications, and fields

- `DownloadClientObject` (`TagUpdateObject`, `#download_client`): a download client, piped as `ITestPipeable`. `Name` reads and writes its note property.
- `DownloadClientSchemaObject` (`TagUpdateObject`, `#download_client_schema`, no ID): a template from `/downloadclient/schema` for a new client. `ConfigContract`, `Implementation`, `ImplementationName`, `InfoLink`, and `Protocol` turn read-only, while `Enable`, `Name`, and `Priority` stay writable. Its `Copy()` builds a new instance with a copy of each property and the model's type names.
- `IndexerObject` (`TagUpdateObject`, `#indexer`): an indexer, piped as `ITestPipeable`. `EnableRss`, `EnableAutomaticSearch`, `EnableInteractiveSearch`, and `Priority` use their note properties, and `Name` and `Protocol` are copies. `OnSerializing()` sets a `Priority` outside 0 to 100 back to 25 and fills a blank `Name` from the copy, and `OnCommit()` refreshes `Name` and `Protocol` from their note properties.
- `NotificationObject` (`TagUpdateObject`, `#notification`): a notification connection, piped as `ITestPipeable`. `Name` reads and writes its note property, and `IsEnabled` is `true` when one of its `On…` triggers is.
- `NotificationSchemaObject` (`TagUpdateObject`, `#notification`): a template from `/notification/schema` for a new notification, and an `ISchemaObject`. It removes `Id` after deserializing and again before serializing, since a new notification has none.
- `FieldObject` (`SonarrObject`, no tag): one setting of a provider, such as an indexer, a download client, or a notification, in its parent's read-only `Fields`. `Advanced`, `HelpText`, `IsFloat`, `Label`, `Name`, `Order`, `Privacy`, and `Type` are read-only, and `Value` stays writable. `SelectOptions` holds a select field's choices and is empty otherwise. It sorts by `Order`.
- `SelectOptionObject` (`SonarrObject`, no tag): one choice of a select field, with a read-only `Name` and `Order`.

### Profiles and qualities

- `DelayProfileObject` (`TagUpdateObject`, `#delay_profile`): renames `TorrentDelay` and `UsenetDelay` to `TorrentDelayInMins` and `UsenetDelayInMins` both ways, and capitalizes `PreferredProtocol`.
- `ReleaseProfileObject` (`TagUpdateObject`, `#release_profile`): `Name` reads and writes its note property, and its `Ignored` and `Required` terms arrive as a `StringSet` and its `Preferred` terms as a `StringKeyValueSet<int>`.
- `LanguageProfileObject` (`IdSonarrObject`, `#language_profile`): `Name` reads and writes its note property.
- `QualityProfileObject` (`IdSonarrObject`, `#quality_profile`): `Name` is a copy.
- `QualityDefinitionObject` (`SonarrObject`, `#quality_definition`): a quality's size limits, from `/qualitydefinition`. It copies `Id` and leaves the property in place, `Title` reads and writes its note property, and its nested `Quality` is a `QualityObject`.
- `QualityObject` (`SonarrObject`, `#quality`, no ID): one quality, with a read-only `Id`, `Name`, `Resolution`, and `Source`. C# can build one from a `QualityRevisionObject`, `QualityObject.Default` is the "Unknown" quality, and `OnDeserialized()` does nothing for a constructed instance.
- `QualityRevisionObject` (`SonarrObject`, no tag): a quality and its revision, as a manual import's writable `Quality`. `Quality` and `Revision` fall back to `QualityObject.Default` and `RevisionObject.Default`, and its constructors build one in C# from a `QualityObject`, with or without the proper and real flags.
- `RevisionObject` (`SonarrObject`, `#quality_revision`, no ID): a release's revision, with `Version`, `Real`, and `IsRepack`. It's read only as the `Revision` of a `QualityRevisionObject`, or made by `Create(isProper, isReal)` or `Default`, which builds a new instance each time.

### Configuration and system

- `NamingConfigObject` (`SonarrObject`, `#naming_config`) and `HostObject` (`SonarrObject`, `#host_config`): the single objects at `/config/naming` and `/config/host`. Each implements `IHasId` itself and keeps `Id` out of PowerShell's view: `OnDeserialized()` copies and removes it, `OnSerializing()` adds it back, and `Commit()` and `Reset()` remove it again. Type data adds `SeparatorLength` to the naming config. `HostObject` capitalizes `AuthenticationMethod`, `CertificateValidation`, `LogLevel`, `ProxyType`, and `UpdateMechanism`.
- `NoKeyHostObject` (`HostObject`, `#host_config`): the host config with `ApiKey`, `Password`, and `ProxyPassword` hidden. `OnDeserialized()` moves them into `Conditionals`, `OnSerializing()` writes them back so that a PUT keeps them, `Commit()` moves them again, and `Reset()` removes them. Its type name replaces `HostObject`'s.
- `DownloadClientConfigObject` (`IdSonarrObject`, `#download_client_config`): the settings at `/config/downloadclient`. `OnSerializing()` rewrites the `Id` property from the C# `Id`.
- `MediaManagementObject` (`SonarrObject`, `#media_management`): the settings at `/config/mediamanagement`. It keeps `Id` out of view like the naming config, renames `RecycleBin` to `RecycleBinPath` both ways and shows it through a `Path` alias, and checks the path with `IsRecycleBinPathProper(out string)`. `OnSerializing()` removes the alias and writes `Id`, and `Commit()` and `Reset()` remove `Id` and add the alias back.
- `RootFolderObject` (`IdSonarrObject`, `#root_folder`): a root folder, with nothing beyond its base.
- `RemotePathObject` (`IdSonarrObject`, `#remote_path_mapping`): a remote path mapping. It renames `Host` to `HostName` both ways and keeps `HostName`, `LocalPath`, and `RemotePath` as copies. An update sends a `RemotePathBody`, `ApplyFromPost(RemotePathBody)` takes the values of a body that carries this object's `Id` and commits them, and `Commit()` and `Reset()` write the copies back to the note properties.
- `BackupObject` (`IdSonarrObject`, `#backup`): a backup, from `/system/backup`. It renames `Path` to `BackupUri` both ways, keeps `Name` and the relative `BackupUri` as copies, and sorts by name and then ID.
- `DiskspaceObject` (`SonarrObject`, `#disk`, no ID): a drive's `Path`, with `FreeSpace` and `TotalSpace` as `Size` values. It adds an empty `Label` when Sonarr sends none and sorts by path. Type data adds `FreeSpaceInMB`, `FreeSpaceInGB`, and `FreeSpaceInTB`, and the same for `TotalSpace`, reading the `Size` values through `$this.psobject`.
- `LogObject` (`SonarrObject`, `#log_item`, no ID): a log entry. It copies `Level` and `Time` and sorts by time.
- `LogFileObject` (`IdSonarrObject`, `#log_file`, no ID): a log file. It removes `Id`, copies `ContentsUrl` and `LastWriteTime`, and sets `Type`, which reads and writes a note property, to `LogFileType.Update` when the URL holds `/file/update/` and to `General` otherwise. It's `IEquatable<LogFileObject>` and sorts by last write time, then URL.
- `SystemStatusObject` (`SonarrObject`, `#system_status`, no ID): the server's status, from `/system/status`. It parses `Authentication` into a `SonarrAuthType` and capitalizes `Authentication`, `Mode`, `PackageUpdateMechanism`, and `RuntimeName`. A format view shows it.
- Not models: `Size` is a byte count with MB, GB, and TB values rounded to two places, which compares and equates with `int` and `long`. `PingResponse` is the body of a ping, and `PingResult` is what a ping reports.

### History, releases, manual imports, and commands

- `HistoryObject` (`IdSonarrObject`, `#history_item`, no ID): a history record. It copies `DownloadId`, `EpisodeId`, and `SeriesId`, and its nested `Data` is a `ReleaseObject`.
- `ReleaseObject` (`SonarrObject`, `#release`, no ID): a search result from `/release`. It copies `IndexerId` and `ReleaseUrl`, from Sonarr's `guid`, shows `Age` as a `TimeSpan` built from `AgeMinutes`, hides `AgeHours` and `AgeMinutes`, and adds `TotalWeight`, the sum of the four weights that the `Weight` struct reads. It sorts by total weight, then age, indexer, and URL, and capitalizes `Protocol`. `OnSerializing()` writes back the day count, the weights, and the age fields, and `Reset()` hides them again. `PostRelease`, with `Guid` and `IndexerId`, is the body that grabs a release.
- `ManualImportObject` (`IdSonarrObject`, `#manual_import`, no ID): a file that Sonarr found for a manual import. It makes `Name` read-only and adds what a post needs when Sonarr leaves it out: an `Episodes` set, a writable `Quality` and `Series`, `ReleaseGroup`, and a `SeasonNumber` script property. `IsReadyToPost()` checks for episodes, a series, and a quality. It isn't posted itself: `ManualImportPostMapper.ToManualImportFiles` turns a `MetadataList<ManualImportObject>` into `ManualImportPost` bodies. Type data adds `IsRejected` and `IsReadyToPost`, and it sorts by name.
- `CommandObject` (`IdSonarrObject`, `#command`): a command queued or run through `/command`, and an `ICommand` with equality operators. It copies `Name`, `Started`, `Ended`, and `Status`, and `IsCompleted` means a `Status` of `completed`. It holds `Body` back, writes it in `OnSerializing()`, and removes it in `Commit()` and `Reset()`, and it capitalizes `Priority`, `Status`, and `Trigger`. Not models: `PostCommand`, with `CommandName`, `Name`, `Priority`, and `UpdateScheduledTask`, is the body that starts a command, written by `PostCommandWriter`, and `CommandPriority` is its priority.

### Tags, errors, and the folder's root

- `TagObject` (`SonarrObject`, `#tag`): a tag, with `Id` and `Label` as copies and `IHasName` giving `Label`. It sorts by ID, and a format view shows it.
- `SonarrServerError` (`SonarrObject`, no tag): one validation error in an error response. `Message` joins the string properties whose names end in `message`, and `PropertyName` names the field. It sorts by property name, then message.
- Not models: `SonarrTag` is the record that `New-SonarrTag` posts, with an `Id` that's left out when it's 0 and a `Label`. `IdModel` is a record that holds only an `Id`. `RecordResult<T>` is one page of `/history`, `/log`, or `/wanted/missing`, with `Page`, `PageSize`, `TotalRecords`, and `Records` as a `MetadataList<T>` that it tags and sorts. `ShortOverview` cuts text after a word and ends it with `...`, and `SonarrStatus` holds the `Authentication` value that the connection test reads.

## Adding or changing a model

1. Put the class in the folder for its Sonarr resource, in `MG.Sonarr.Next.Models.<Folder>`, as `public sealed` unless another model will derive from it.
2. Derive from `TagUpdateObject<TSelf>` for a resource with tags, from `IdSonarrObject<TSelf>` for one with a numeric ID, and from `SonarrObject` otherwise, or when the model keeps its `Id` out of PowerShell's view.
3. Mark the class `[SonarrObject]` and implement `ISerializableNames<TheClass>`, with the static name maps and capitalize set it needs.
4. Give it a public parameterless constructor that passes a `CAPACITY` constant, about the number of fields that Sonarr sends, to the base. The converter adds the properties after the constructor runs, so set up anything that depends on them during deserialization.
5. Return its tag from `GetTag`. For an endpoint without one, add a `Meta` constant and an `AddMetadata` entry with the path from `Constants` and whether the endpoint takes an ID. Return `existing`, or override `DisregardMetadataTag`, for an object that stands for no endpoint.
6. Override `SetPSTypeName()`: call the base, then insert `typeof(TheClass).GetName()`, kept in a static field, at index 0, or replace index 0 when deriving from another model.
7. Implement each pipeable interface that a cmdlet should take it through, with `GetId()` returning the ID that the interface exposes, and `IComparable<TheClass>` when a cmdlet gets it as a list.
8. When deserialization takes out a value that the request body needs, implement `IJsonOnSerializing` to put it back, and override `Commit()` and `Reset()`, or `OnCommit()` and `OnReset()` on a `TagUpdateObject<TSelf>`, to take it out again.
9. For a format view or type data, select the type name from step 6. A new `*.Format.ps1xml` or `*.Type.ps1xml` file needs a `Content` item with `CopyToOutputDirectory` in `src/MG.Sonarr.Next.Shell/MG.Sonarr.Next.Shell.csproj`.
