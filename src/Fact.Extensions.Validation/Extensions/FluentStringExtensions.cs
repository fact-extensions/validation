using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    public static class FluentStringExtensions
    {
        public static TFluentBinder StartsWith<TFluentBinder>(this TFluentBinder fb, string mustStartWith)
            where TFluentBinder : IFluentBinder<string>
        {
            fb.Binder.Processor.ProcessingAsync += (_, context) =>
            {
                if (!fb.Field.Value.StartsWith(mustStartWith))
                    fb.Field.Error(FieldStatus.ComparisonCode.Unspecified, mustStartWith,
                        $"Must start with: {mustStartWith}");
                return new ValueTask();
            };
            return fb;
        }


        public static TFluentBinder Contains<TFluentBinder>(this TFluentBinder fb, string mustContain)
            where TFluentBinder : IFluentBinder<string>
        {
            fb.Binder.Processor.ProcessingAsync += (_, context) =>
            {
                if (!fb.Field.Value.Contains(mustContain))
                    fb.Field.Error(FieldStatus.ComparisonCode.Unspecified, mustContain,
                        $"Must start with: {mustContain}");
                return new ValueTask();
            };
            return fb;
        }
    }
}
