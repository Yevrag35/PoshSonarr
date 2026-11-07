using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.PSProperties;

namespace MG.Sonarr.Next.Tests.PSProperties;

/// <summary>
/// Tests the writable note properties of a model, which a user sets from PowerShell before the model goes back to
/// Sonarr.
/// </summary>
public sealed class WritablePropertyTests
{
	/// <summary>
	/// The kinds of writable property that a model's fields are, each with a value to start from.
	/// </summary>
	private static readonly (string Kind, Func<WritableProperty> Create)[] s_kinds =
	[
		("string", () => new StringNoteProperty("Value", "start")),
		("int", () => new NumberNoteProperty<int>("Value", 1)),
		("long", () => new NumberNoteProperty<long>("Value", 1)),
		("double", () => new NumberNoteProperty<double>("Value", 1)),
		("decimal", () => new NumberNoteProperty<decimal>("Value", 1)),
		("bool", () => new StructNoteProperty<bool>("Value", false)),
		("Guid", () => new StructNoteProperty<Guid>("Value", Guid.Empty)),
		("DateOnly", () => new StructNoteProperty<DateOnly>("Value", default)),
		("DateTimeOffset", () => new StructNoteProperty<DateTimeOffset>("Value", default)),
		("TimeSpan", () => new StructNoteProperty<TimeSpan>("Value", default)),
		("series", () => new WritableSonarrProperty<SeriesObject>("Value", null)),
	];

	/// <summary>
	/// B80, O42: a writable property takes a value that PowerShell passes wrapped in a <see cref="PSObject"/>, as it
	/// passes the output of a command, as the value that it wraps, through any number of wrappers, and stops at a model,
	/// which is a <see cref="PSObject"/> itself. Its <see cref="WritableProperty.ValueIsProper(object?)"/> returns
	/// <see langword="true"/> for exactly the values that its setter takes, wrapped or not.
	/// </summary>
	/// <remarks>
	/// The test first sets properties in a runspace from the output of commands, as a user does, and then holds every
	/// kind of property to its own setter, for a value of each type that a user might set, bare and in one and two
	/// wrappers.
	/// </remarks>
	[Fact]
	public void B80_TakesAWrappedValueAsTheValueThatItWraps()
	{
		SeriesObject series = new();
		PSObject model = new();
		model.Properties.Add(new NumberNoteProperty<int>("QualityProfileId", 1));
		model.Properties.Add(new StructNoteProperty<bool>("IsMonitored", false));
		model.Properties.Add(new StringNoteProperty("Title", "Old"));
		model.Properties.Add(new WritableSonarrProperty<SeriesObject>("Series", null));

		using PowerShell powerShell = PowerShellHost.Create();
		_ = powerShell.AddScript("""
			param($Model, $Series)
			$Model.QualityProfileId = [pscustomobject]@{ Id = 7 } | Select-Object -ExpandProperty Id
			$Model.IsMonitored = Write-Output $true
			$Model.Title = Write-Output 'New'
			$Model.Series = Write-Output $Series
			""").AddArgument(model).AddArgument(series).Invoke();

		Assert.Empty(powerShell.Streams.Error);
		Assert.Equal(7, Assert.IsType<int>(model.Properties["QualityProfileId"].Value));
		Assert.True(Assert.IsType<bool>(model.Properties["IsMonitored"].Value));
		Assert.Equal("New", model.Properties["Title"].Value);
		Assert.Same(series, model.Properties["Series"].Value);

		// The values on which the two disagreed before.
		NumberNoteProperty<int> number = new("Value", 1);
		Assert.Multiple(
			() => Assert.False(number.ValueIsProper("abc")),
			() => Assert.False(number.ValueIsProper(true)),
			() => Assert.False(number.ValueIsProper(1.5)),
			() => Assert.True(number.ValueIsProper((Int128)5)),
			() => Assert.False(new WritableSonarrProperty<SeriesObject>("Value", null).ValueIsProper(null)));

		object?[] values = Values();
		Assert.Multiple(
		[
			.. s_kinds.SelectMany(kind => values.Select(value => (Action)(() => AssertAgreesWithSetter(kind.Kind, kind.Create, value)))),
		]);
	}

	/// <summary>
	/// Creates a value of each type that a user might set: strings that do and don't parse, numbers of several types,
	/// structs, an enum, objects, and models.
	/// </summary>
	private static object?[] Values()
	{
		return
		[
			null, "", "5", " 5 ", "abc", "1.5", "True", "2024-01-15", "0f8fad5b-d9cb-469f-a165-70867728950e",
			5, -5, 5L, 5.0, 1.5, 5m, (Int128)5, (byte)5, long.MaxValue, double.NaN,
			true, new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), new DateOnly(2024, 1, 15),
			new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero), TimeSpan.FromMinutes(5), ActionPreference.Continue,
			new object(), new PSObject(), new SeriesObject(), new EpisodeObject(),
		];
	}

	/// <summary>
	/// Asserts that a new property's <see cref="WritableProperty.ValueIsProper(object?)"/> agrees with its setter on a
	/// value, and that the setter takes the value in one and two wrappers as it takes the value itself.
	/// </summary>
	private static void AssertAgreesWithSetter(string kind, Func<WritableProperty> create, object? value)
	{
		string bare = Set(create(), value);
		Assert.True(
			bare.StartsWith("took", StringComparison.Ordinal) == create().ValueIsProper(value),
			$"The {kind} property's ValueIsProper disagreed with its setter, which {bare}, for {Describe(value)}.");

		if (value is null)
		{
			return;
		}

		foreach (PSObject wrapped in (PSObject[])[new PSObject(value), new PSObject(new PSObject(value))])
		{
			string set = Set(create(), wrapped);
			Assert.True(
				bare == set,
				$"The {kind} property's setter {set} for {Describe(value)} wrapped, and {bare} for it bare.");
			Assert.True(
				set.StartsWith("took", StringComparison.Ordinal) == create().ValueIsProper(wrapped),
				$"The {kind} property's ValueIsProper disagreed with its setter, which {set}, for {Describe(value)} wrapped.");
		}
	}

	/// <summary>
	/// Sets a value on a property, and tells what the setter did with it.
	/// </summary>
	/// <returns>
	/// <c>took it as</c> and the value that the property then holds, with its type, or <c>turned it away</c> when the
	/// setter threw <see cref="SetValueException"/>.
	/// </returns>
	private static string Set(WritableProperty property, object? value)
	{
		try
		{
			property.Value = value;
		}
		catch (SetValueException)
		{
			return "turned it away";
		}

		return "took it as " + Describe(property.Value);
	}

	/// <summary>
	/// Describes a value by its type and its text, or a model by its identity, since two models' text can be equal.
	/// </summary>
	private static string Describe(object? value)
	{
		return value switch
		{
			null => "null",
			PSObject pso when pso.ImmediateBaseObject is PSCustomObject => string.Concat(
				pso.GetType().Name, " #", RuntimeHelpers.GetHashCode(pso).ToString(CultureInfo.InvariantCulture)),
			IFormattable formattable => string.Concat(
				value.GetType().Name, " '", formattable.ToString(null, CultureInfo.InvariantCulture), "'"),
			_ => string.Concat(value.GetType().Name, " '", value.ToString(), "'"),
		};
	}
}
