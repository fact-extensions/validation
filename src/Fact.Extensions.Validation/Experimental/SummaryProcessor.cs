using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fact.Extensions.Validation.Experimental
{
    public class SummaryProcessor
    {
        internal Dictionary<IBinderProvider, Item> items = new Dictionary<IBinderProvider, Item>();

        public class Item
        {
            public int Warnings { get; set; }
            public int Errors { get; set; }

            public void Update(IEnumerable<Status> statuses)
            {
                // TODO: Fire off events when these change so that Statuses down below has to do less work

                Warnings = statuses.Count(x => x.Level == Status.Code.Warning);
                Errors = statuses.Count(x => x.Level == Status.Code.Error);
            }
        }

        public IEnumerable<Status> Statuses
        {
            get
            {
                int errors = items.Values.Sum(x => x.Errors);
                int warnings = items.Values.Sum(x => x.Warnings);

                if (errors > 0) yield return new Status(Status.Code.Error, $"Encountered {errors} errors");
                if (warnings > 0) yield return new Status(Status.Code.Error, $"Encountered {warnings} warnings");
            }
        }
    }


    public static class SummaryProcessorExtensions
    {
        public static void AddSummaryProcessor<TBinderProvider>(this IAggregatedBinderBase<TBinderProvider> aggregatedBinder,
            FieldStatus summaryField)
            where TBinderProvider : IBinderProvider
        {
            var sp = new SummaryProcessor();

            summaryField.Add(sp.Statuses);

            foreach (var provider in aggregatedBinder.Providers)
            {
                var item = new SummaryProcessor.Item();
                item.Update(provider.Binder.Field.Statuses);
                sp.items.Add(provider, item);
            }

            aggregatedBinder.BindersProcessed += (providers, context) =>
            {
                foreach (var provider in providers)
                {
                    sp.items[provider].Update(provider.Binder.Field.Statuses);
                }
            };
        }
    }
}
