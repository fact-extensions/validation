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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TBinderProvider"></typeparam>
        /// <param name="binder"></param>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        /// <param name="providerFactory"></param>
        /// <returns></returns>
        /// <remarks>
        /// DEBT: Consolidate with above and also match up return types
        /// </remarks>
        public static FluentBinder<T> AddFieldX<T, TBinderProvider>(this ICollector<TBinderProvider> binder, string name, Func<T> getter,
            Func<FluentBinder<T>, TBinderProvider> providerFactory)
            where TBinderProvider : IBinderProvider
        {
            // default(T) because early init is not the same as runtime init
            // early init is when system is setting up the rules
            // runtime init is at the start of when pipeline processing actually occurs
            var f = new FieldStatus<T>(name);
            var b = new FieldBinder<T>(f, getter);
            var fb = new FluentBinder<T>(b, true);
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
        public static FluentBinder<T> AddField<T>(this IAggregatedBinderCollector binder, string name, Func<T> getter) =>
            binder.AddField(name, getter, fb => new BinderManagerBase.ItemBase<T>(fb.Binder, fb)).FluentBinder;


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
