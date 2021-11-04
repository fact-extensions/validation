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
        /// Adds a Field to the aggregator/collector with a new binder underneath it
        /// </summary>
        /// <typeparam name="T">Type associated with field itself</typeparam>
        /// <typeparam name="TBinderProvider">Type of binder provider the factory is creating</typeparam>
        /// <typeparam name="TBinderProviderCollector">
        /// Type of binder provider the aggregator/collector understandings
        /// </typeparam>
        /// <param name="binder"></param>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        /// <param name="providerFactory"></param>
        /// <returns></returns>
        public static TBinderProvider AddField<T, TBinderProviderCollector, TBinderProvider>(
            this ICollector<TBinderProviderCollector> binder, string name, Func<T> getter,
            Func<FluentBinder<T>, TBinderProvider> providerFactory)
            where TBinderProvider : TBinderProviderCollector
            where TBinderProviderCollector : IBinderProvider
        {
            var b = new FieldBinder<T>(name, getter);
            var fb = new FluentBinder<T>(b, true);
            var bp = providerFactory(fb);
            binder.Add(bp);
            return bp;
        }




        /// <summary>
        /// Adds a field with new binders to the aggregator using stock standard BinderManagerBase provider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binder"></param>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        /// <returns></returns>
        public static FluentBinder<T> AddField<T>(this IAggregatedBinderCollector binder, string name, Func<T> getter) =>
            binder.AddField(name, getter, fb => new BinderManagerBase.ItemBase<T>(fb)).FluentBinder;


        /// <summary>
        /// DEBT: This particular Process() helper fiddles with InputContext while others don't
        /// </summary>
        /// <typeparam name="TAggregatedBinder"></typeparam>
        /// <param name="aggregatedBinder"></param>
        /// <param name="inputContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
