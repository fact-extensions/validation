using System;

namespace Fact.Extensions.Validation
{
    /// <summary>
    /// Miscellaneous built-in converters and system type validators
    /// </summary>
    public static class FluentSystemExtensions
    {
        public static FluentBinder<Uri> ToUri(this IFluentBinder<string> fb)
        {
            return fb.Convert((IField<string> f, out Uri converted) =>
            {
                // DEBT: A more descriptive reason for failed conversion is needed
                
                return Uri.TryCreate(f.Value, UriKind.RelativeOrAbsolute, out converted);
            }, "Unrecognized URI format");
        }
    }
}