using MG.Sonarr.Next.Services.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
