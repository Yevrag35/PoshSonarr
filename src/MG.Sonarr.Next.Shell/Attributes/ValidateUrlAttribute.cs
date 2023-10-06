using MG.Sonarr.Next.Services.Extensions;

namespace MG.Sonarr.Next.Shell.Attributes
{
    public sealed class ValidateUrlAttribute : ValidateArgumentsAttribute
    {
        public UriKind MustBeKind { get; }

        public ValidateUrlAttribute(UriKind mustBeKind)
        {
            this.MustBeKind = mustBeKind;
        }

        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments is Uri goodUrl)
            {
                ValidateUrl(goodUrl, this.MustBeKind);
                return;
            }
            else if (arguments is string possibleUrl && Uri.TryCreate(possibleUrl, UriKind.RelativeOrAbsolute, out Uri? result))
            {
                ValidateUrl(result, this.MustBeKind);
                return;
            }

            ThrowIncorrectType(arguments);
        }

        private static void ValidateUrl(Uri parsedUri, UriKind uriKind)
        {
            bool correctType;
            switch (uriKind)
            {
                case UriKind.RelativeOrAbsolute:
                    correctType = true;
                    break;

                case UriKind.Absolute:
                    correctType = parsedUri.IsAbsoluteUri;
                    break;

                case UriKind.Relative:
                    correctType = !parsedUri.IsAbsoluteUri;
                    break;

                default:
                    goto case UriKind.RelativeOrAbsolute;
            }

            if (!correctType)
            {
                ThrowIncorrectUrlKind(in uriKind);
            }
        }

        [DoesNotReturn]
        private static void ThrowIncorrectUrlKind(in UriKind specifiedKind)
        {
            throw new ValidationMetadataException($"An invalid type of URI was passed as an argument. It must be {specifiedKind}.");
        }

        [DoesNotReturn]
        private static void ThrowIncorrectType(object arguments)
        {
            string typeName = arguments?.GetType().GetTypeName() ?? "null";
            throw new ValidationMetadataException($"The parameter argument is NOT of 1 of 2 types: 'string' or 'System.Uri'. Instead, we got '{typeName}'.");
        }

        /// <summary>
        /// Returns a string similar to this attribute's declaration.
        /// </summary>
        /// <remarks>
        ///     An example:
        ///     <code>[ValidateUrl(UriKind.Absolute)]</code>
        /// </remarks>
        /// <returns></returns>
        public override string ToString()
        {
            ReadOnlySpan<char> name = nameof(ValidateUrlAttribute);
            ReadOnlySpan<char> uriKind = nameof(UriKind);
            ReadOnlySpan<char> value = this.MustBeKind.ToString();
            int index = name.LastIndexOf(nameof(Attribute), StringComparison.InvariantCulture);
            name = name.Slice(0, index);

            // 5 = the number of constant characters beside the names and value.
            // i.e. - '[', '(', '.', ')', ']'
            Span<char> span = stackalloc char[name.Length + uriKind.Length + value.Length + 5];
            int position = 0;
            span[position++] = '[';

            name.CopyToSlice(span, ref position);
            span[position++] = '(';
            uriKind.CopyToSlice(span, ref position);

            span[position++] = '.';
            value.CopyToSlice(span, ref position);
            (stackalloc char[2] { ')', ']' }).CopyToSlice(span, ref position);

            return new string(span.Slice(0, position));
        }
    }
}
