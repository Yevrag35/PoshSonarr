namespace MG.Sonarr.Next.Shell.Attributes
{
    /// <summary>
    /// Indicates what action to take when a passed parameter is <see langword="null"/> prior to range validation.
    /// </summary>
    public enum InputNullBehavior
    {
        /// <summary>
        /// If the passed validation argument is <see langword="null"/>, the <see cref="ValidateArgumentsAttribute"/>
        /// attribute will throw a <see cref="ValidationMetadataException"/> - akin to adding a
        /// <see cref="ValidateNotNullAttribute"/> attribute.
        /// </summary>
        EnforceNotNull = 0,
        /// <summary>
        /// If the passed validation argument is <see langword="null"/>, the 
        /// <see cref="ValidateArgumentsAttribute"/> attribute will skip validation entirely or if 
        /// <see cref="ValidateEnumeratedArgumentsAttribute"/> will skip validation for that individually element.
        /// </summary>
        Ignore = 1,
        /// <summary>
        /// If the passed validation argument is <see langword="null"/>, the <see cref="ValidateArgumentsAttribute"/> 
        /// will proceed as if the <see cref="int"/> value was 0.
        /// </summary>
        /// <remarks>
        ///     This effectively allows <see langword="null"/> values to pass 
        ///     <see cref="ValidateRangeKind.NonNegative"/> and <see cref="ValidateRangeKind.NonPositive"/> ranges
        ///     validations.
        /// </remarks>
        PassAsZero = 2,
        /// <summary>
        /// If the passed validation argument is <see langword="null"/>, the <see cref="ValidateArgumentsAttribute"/> 
        /// will proceed as if the <see cref="int"/> value was 1.
        /// </summary>
        /// <remarks>
        ///     This effectively allows <see langword="null"/> values to pass <see cref="ValidateRangeKind.Positive"/>
        ///     range validations.
        /// </remarks>
        PassAsOne = 3,
        /// <summary>
        /// If the passed validation argument is <see langword="null"/>, the <see cref="ValidateArgumentsAttribute"/> 
        /// will proceed as if the <see cref="int"/> value was -1.
        /// </summary>
        /// <remarks>
        ///     This effectively allows <see langword="null"/> values to pass <see cref="ValidateRangeKind.Negative"/>
        ///     range validations.
        /// </remarks>
        PassAsNegativeOne = 4,
        /// <summary>
        /// If the passed validation argument is NOT <see langword="null"/>, the <see cref="ValidateArgumentsAttribute"/>
        /// attribute will throw a <see cref="ValidationMetadataException"/>.  This is a rare case.
        /// </summary>
        EnforceNull = 5,
    }
}

