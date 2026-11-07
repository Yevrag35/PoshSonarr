using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Json.Converters;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Errors;
using MG.Sonarr.Next.Models.Fields;
using MG.Sonarr.Next.Models.Qualities;

namespace MG.Sonarr.Next.Tests.Models;

/// <summary>
/// Tests what every model needs to be read, written, shown, and piped: a converter of its own, its type name, and its
/// metadata tag. A model that misses one fails nowhere at startup, so nothing else finds it.
/// </summary>
public sealed class ModelWiringTests
{
	/// <summary>
	/// The type name that every model carries after its own.
	/// </summary>
	private const string SONARR_OBJECT = "MG.Sonarr.Next.Models.SonarrObject";

	/// <summary>
	/// The models that stand for no endpoint, with whether tagging one gives it a <c>MetadataTag</c> property that holds
	/// <see cref="MetadataTag.Empty"/>, as a server error's does, or none, as the three that a parent holds get.
	/// </summary>
	private static readonly Dictionary<Type, bool> s_untagged = new()
	{
		[typeof(FieldObject)] = false,
		[typeof(SelectOptionObject)] = false,
		[typeof(QualityRevisionObject)] = false,
		[typeof(SonarrServerError)] = true,
	};

	/// <summary>
	/// Every concrete model class is public and marked <c>[SonarrObject]</c>, so the connection's JSON options read and
	/// write it with a <see cref="SonarrObjectConverter{T}"/> of its own rather than through its C# properties. Reading
	/// one puts its full type name first in its <see cref="PSObject.TypeNames"/>, which the format and type files select
	/// it by, ahead of <c>SonarrObject</c>'s and without the name of a model that it derives from. Tagging it gives it a
	/// tag that the connection registers, unless it stands for no endpoint, as a provider's field, a select field's
	/// choice, a quality with its revision, and a server error do.
	/// </summary>
	/// <param name="model">The model's full type name.</param>
	[Theory]
	[MemberData(nameof(Models))]
	public void GivesEveryModelItsConverterTypeNameAndTag(string model)
	{
		Type type = typeof(SonarrObject).Assembly.GetType(model, throwOnError: true)!;
		Type converter = typeof(SonarrObjectConverter<>).MakeGenericType(type);

		Assert.True(type.IsPublic, $"{type.Name} isn't public, so the scan for models passes over it.");
		Assert.True(type.IsDefined(typeof(SonarrObjectAttribute), inherit: false), $"{type.Name} isn't marked [SonarrObject].");
		Assert.IsType(converter, ModuleServices.Json.ForDeserializing.GetConverter(type));
		Assert.IsType(converter, ModuleServices.Json.ForSerializing.GetConverter(type));

		SonarrObject read = Assert.IsAssignableFrom<SonarrObject>(
			JsonSerializer.Deserialize("""{"id":1}""", type, ModuleServices.Json.ForDeserializing));

		Assert.IsType(type, read);
		Assert.Equal(type.FullName, read.TypeNames[0]);
		Assert.Contains(SONARR_OBJECT, read.TypeNames);
		for (Type? baseType = type.BaseType; baseType is not null && baseType != typeof(SonarrObject); baseType = baseType.BaseType)
		{
			if (!baseType.IsAbstract)
			{
				Assert.DoesNotContain(baseType.FullName, read.TypeNames);
			}
		}

		read.SetTag(ModuleServices.Resolver);
		if (!s_untagged.TryGetValue(type, out bool keepsEmptyTag))
		{
			string tag = read.MetadataTag.Value;
			Assert.True(ModuleServices.Resolver.ContainsKey(tag), $"{type.Name} has the tag '{tag}', which the connection doesn't register.");
		}
		else if (keepsEmptyTag)
		{
			Assert.Same(MetadataTag.Empty, read.MetadataTag);
		}
		else
		{
			Assert.Null(read.Properties[nameof(SonarrObject.MetadataTag)]);
		}
	}

	public static TheoryData<string> Models => [.. ModelTypes().Select(type => type.FullName!)];

	/// <summary>
	/// Gives every concrete class in the services library that derives from <see cref="SonarrObject"/>, public or not.
	/// </summary>
	private static IEnumerable<Type> ModelTypes()
	{
		return typeof(SonarrObject).Assembly.GetTypes()
			.Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(SonarrObject)))
			.OrderBy(type => type.FullName, StringComparer.Ordinal);
	}
}
