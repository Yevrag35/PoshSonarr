namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Represents what an ID validation attribute, such as <see cref="ValidateIdAttribute"/> or
/// <see cref="ValidateIdsAttribute"/>, does with an argument, or an element of one, that gives no ID.
/// </summary>
/// <remarks>
/// An argument gives no ID when it's <see langword="null"/>, when its ID is <see langword="null"/>, or when it's of a
/// type that the attribute doesn't read an ID from, such as a string. An argument that gives an ID is checked against
/// the attribute's range, except under <see cref="EnforceNull"/>.
/// </remarks>
public enum InputNullBehavior
{
	/// <summary>
	/// An argument that gives no ID fails validation, as a <see cref="ValidateNotNullAttribute"/> would fail a
	/// <see langword="null"/> one.
	/// </summary>
	EnforceNotNull = 0,
	/// <summary>
	/// An argument that gives no ID passes without a range check. An attribute that validates each element of a list
	/// skips only that element.
	/// </summary>
	Ignore = 1,
	/// <summary>
	/// An argument that gives no ID is checked as an ID of 0, so it passes a
	/// <see cref="ValidateRangeKind.NonNegative"/> or <see cref="ValidateRangeKind.NonPositive"/> range and fails the
	/// others.
	/// </summary>
	PassAsZero = 2,
	/// <summary>
	/// An argument that gives no ID is checked as an ID of 1, so it passes a <see cref="ValidateRangeKind.Positive"/> or
	/// <see cref="ValidateRangeKind.NonNegative"/> range and fails the others.
	/// </summary>
	PassAsOne = 3,
	/// <summary>
	/// An argument that gives no ID is checked as an ID of -1, so it passes a <see cref="ValidateRangeKind.Negative"/> or
	/// <see cref="ValidateRangeKind.NonPositive"/> range and fails the others.
	/// </summary>
	PassAsNegativeOne = 4,
	/// <summary>
	/// An argument that gives an ID fails validation, and one that gives no ID passes.
	/// </summary>
	EnforceNull = 5,
}

