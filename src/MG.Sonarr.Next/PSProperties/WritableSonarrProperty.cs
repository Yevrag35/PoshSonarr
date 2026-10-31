using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Models;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents a writable note property that holds a nested Sonarr object, which the user can replace before the
/// parent object is sent, such as a manual import's <c>Series</c> and <c>Quality</c>.
/// </summary>
/// <remarks>
/// Setting <see cref="WritableProperty{T}.Value"/> accepts only a <typeparamref name="T"/>.
/// </remarks>
/// <typeparam name="T">The type of the nested object.</typeparam>
public sealed class WritableSonarrProperty<T> : WritableProperty<T> where T : SonarrObject
{
	/// <summary>
	/// Gets or sets the nested object that the property holds.
	/// </summary>
	/// <value>The property's value, or <see langword="null"/> when it holds no object.</value>
	public T? SonarrObject { get; set; }
	/// <summary>
	/// Gets the name of the property's value type.
	/// </summary>
	/// <value>
	/// The first of the nested object's PowerShell type names, or the full name of <typeparamref name="T"/> when the
	/// property holds no object.
	/// </value>
	public override string TypeNameOfValue => GetTypeNameOfValueOrDefault(this.SonarrObject);
	/// <summary>
	/// Gets or sets the property's value as a <typeparamref name="T"/>.
	/// </summary>
	/// <value>The value of <see cref="SonarrObject"/>.</value>
	protected override T? ValueAsT
	{
		get => this.SonarrObject;
		set => this.SonarrObject = value;
	}

	/// <summary>
	/// Initializes a new <see cref="WritableSonarrProperty{T}"/> instance with the specified name that holds no object.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public WritableSonarrProperty(string propertyName)
	{
		ArgumentException.ThrowIfNullOrEmpty(propertyName);
		this.SetMemberName(propertyName);
	}
	/// <summary>
	/// Initializes a new <see cref="WritableSonarrProperty{T}"/> instance with the specified name and nested object.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The nested object that the property holds, or <see langword="null"/> for none.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public WritableSonarrProperty(string propertyName, T? value)
		: this(propertyName)
	{
		this.SonarrObject = value;
	}

	/// <summary>
	/// Creates a writable property that holds a copy of the nested object under the specified name.
	/// </summary>
	/// <remarks>
	/// When the nested object's <see cref="PSObject.Copy"/> doesn't return a <typeparamref name="T"/>, the new
	/// property holds the original object instead.
	/// </remarks>
	/// <param name="name">The name of the new property.</param>
	/// <returns>A new <see cref="WritableSonarrProperty{T}"/> that holds the copy, or no object when this one holds none.</returns>
	protected override PSPropertyInfo CopyIntoNew(string name)
	{
		if (this.SonarrObject?.Copy() is not T tObj)
		{
			tObj = this.SonarrObject!;
		}

		return new WritableSonarrProperty<T>(name, tObj);
	}

	//BUG: Setting null throws SetValueException, though the property starts with no object, as a manual import's Quality and Series do, and ValueIsProper returns true for null.
	/// <summary>
	/// Returns a value that is being set on the property as a <typeparamref name="T"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns><paramref name="value"/> as a <typeparamref name="T"/>.</returns>
	/// <exception cref="SetValueException">Thrown when <paramref name="value"/> isn't a <typeparamref name="T"/>.</exception>
	protected override T? ConvertFromObject(object? value)
	{
		return value is T tObj ? tObj : this.ThrowNotType<T>();
	}

	/// <summary>
	/// Gets the name that <see cref="TypeNameOfValue"/> returns for a nested object.
	/// </summary>
	/// <param name="value">The nested object, or <see langword="null"/>.</param>
	/// <returns>
	/// The first of <paramref name="value"/>'s PowerShell type names, or the full name of <typeparamref name="T"/> when
	/// <paramref name="value"/> is <see langword="null"/>.
	/// </returns>
	private static string GetTypeNameOfValueOrDefault(T? value)
	{
		if (value is null)
		{
			return typeof(T).GetName();
		}

		return value.TypeNames[0];
	}

	/// <summary>
	/// Creates a read-only property with the property's name and nested object.
	/// </summary>
	/// <returns>A new <see cref="ReadOnlyPSObjectProperty{T}"/> that holds the same nested object.</returns>
	protected override ReadOnlyProperty<T> CopyToReadOnly()
	{
		return new ReadOnlyPSObjectProperty<T>(this.Name, this.SonarrObject!);
	}
}

