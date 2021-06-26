using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    public static class AggregateBinderExtensions
    {
        /// <summary>
        /// For testing "v3" binder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TBinderProvider"></typeparam>
        /// <param name="binder"></param>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        /// <param name="providerFactory"></param>
        /// <returns></returns>
        public static FluentBinder2<T> AddField3<T, TBinderProvider>(this ICollector<TBinderProvider> binder, string name, Func<T> getter,
            Func<IFluentBinder<T>, TBinderProvider> providerFactory)
            where TBinderProvider : IBinderProvider
        {
            var b = new FieldBinder<T>(name, getter);
            var fb = new FluentBinder2<T>(b);
            binder.Add(providerFactory(fb));
            return fb;
        }


        /// <summary>
        /// For testing "v3" binder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binder"></param>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        public static FluentBinder2<T> AddField3<T>(this IAggregatedBinderCollector binder, string name, Func<T> getter) =>
            binder.AddField3(name, getter, fb => new BinderManagerBase.ItemBase(fb.Binder, fb));


        public static async Task Process<TAggregatedBinder>(this TAggregatedBinder aggregatedBinder,
            InputContext inputContext = null,
            CancellationToken cancellationToken = default)
            where TAggregatedBinder : IBinder3Base, IAggregatedBinderBase
        {
            // DEBT: AggregatedBinder3 won't have field or initialvalue
            var context = new Context2(null, null, cancellationToken);
            // DEBT: Pretty sloppy presumptions if InputContext not specified.  But, will do for now
            context.InputContext = inputContext ?? new InputContext
            {
                InitiatingEvent = InitiatingEvents.Load,
                InteractionLevel = Interaction.Low
            };

            await aggregatedBinder.Processor.ProcessAsync(context, cancellationToken);
        }
    }
}
