using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    // DEBT: Experimental only because naming of IFieldBinder still in flux and FluentBinder3 is not final name
    using Experimental;

    public static class FluentDateTimeExtensions
    {
        static FluentBinder<DateTimeOffset> FromEpochToDateTimeOffset<T>(this IFluentBinder<T> fb)
        {
            var fbConverted = fb.Convert((IField<T> f, out DateTimeOffset dt) =>
            {
                // NOTE: We know we can safely do this because only the <int> and <long> overloads
                // are permitted to call this method
                var value = System.Convert.ToInt64(f.Value);

                try
                {
                    dt = DateTimeOffset.FromUnixTimeSeconds(value);
                    return true;
                }
                catch (ArgumentOutOfRangeException aoore)
                {
                    f.Error(FieldStatus.ComparisonCode.Unspecified, f.Value, aoore.Message);
                    return false;
                }
            });
            return fbConverted;
        }

        public static FluentBinder<DateTimeOffset> FromEpochToDateTimeOffset(this IFluentBinder<int> fb) =>
            FromEpochToDateTimeOffset<int>(fb);

        public static FluentBinder<DateTimeOffset> FromEpochToDateTimeOffset(this IFluentBinder<long> fb) =>
            FromEpochToDateTimeOffset<long>(fb);

        public static FluentBinder<DateTimeOffset> ToDateTimeOffset(this IFluentBinder<int, Experimental.Traits.Epoch> fb) =>
            FromEpochToDateTimeOffset<int>(fb);

        public static FluentBinder<DateTimeOffset> ToDateTimeOffset(this IFluentBinder<long, Experimental.Traits.Epoch> fb) =>
            FromEpochToDateTimeOffset<long>(fb);

        /// <summary>
        /// Tags the fluent binder as a UNIX Epoch
        /// </summary>
        /// <param name="fb"></param>
        /// <returns></returns>
        public static FluentBinder<int, Experimental.Traits.Epoch> AsEpoch(this IFluentBinder<int> fb) =>
            fb.WithTrait<int, Experimental.Traits.Epoch>();


    }
}
