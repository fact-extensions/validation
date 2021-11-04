
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// AKA Binder3, mostly same as Binder2 but uses Processor for a has-a vs is-a relationship.  This lets us
// re-use processor for all kinds of things and also opens up easier possibility of AggregateBinder which
// itself has no field
// DEBT: At this point, only "Experimental" because of clumsy naming
namespace Fact.Extensions.Validation.Experimental
{
    public interface IBinder3Base : IProcessorProvider<Context2>
    {
    }


    /// <summary>
    /// Binder base class agnostic to whether we're binding against a field specifically or some
    /// other unspecified source
    /// </summary>
    public class Binder3Base : IBinder3Base
    {
        public Processor<Context2> Processor { get; } = new Processor<Context2>();

        /*
        public Task ProcessAsync(InputContext inputContext = null, CancellationToken cancellationToken = null)
        {
            // 
        } */
        
        public Committer Committer { get; } = new Committer();
    }
    



    public class AggregatedBinderBase3<TBinderProvider> : Binder3Base,
        IAggregatedBinderBase<TBinderProvider>
        where TBinderProvider: IBinderProvider
    {
        readonly List<TBinderProvider> providers = new List<TBinderProvider>();

        public IEnumerable<IBinderProvider> Providers => providers.Cast<IBinderProvider>();

        /// <summary>
        /// Occurs after interactive/discrete binder processing, whether it generated new status or not
        /// </summary>
        public event BindersProcessedDelegate<TBinderProvider> BindersProcessed;

        protected void FireFieldsProcessed(IEnumerable<TBinderProvider> fields, Context2 context) =>
            BindersProcessed?.Invoke(fields, context);

        public void Add(TBinderProvider binderProvider)
        {
            providers.Add(binderProvider);
            Committer.Committing += binderProvider.Binder.Committer.DoCommit;

            ((IFieldBinder)binderProvider.Binder).Processor.ProcessedAsync += (_, context) =>
            {
                // Filter out overall load/aggregated Process
                if (context.InputContext?.InitiatingEvent != InitiatingEvents.Load)
                    FireFieldsProcessed(new[] { binderProvider }, context);

                return new ValueTask();
            };
        }

        public AggregatedBinderBase3()
        {
            Processor.ProcessingAsync += async (sender, context) =>
            {
                foreach(TBinderProvider provider in providers)
                {
                    // DEBT: when running through providers sometimes they abort, but we overall want to keep
                    // going.  So brute forcing abort to false always -- this feels like the wrong place to do
                    // this though
                    context.Abort = false;

                    await ((IFieldBinder)provider.Binder).Process(context, context.CancellationToken);
                }
            };

            Processor.ProcessedAsync += (_, c) =>
            {
                // Filter out overall load/aggregated Process
                if (c.InputContext?.InitiatingEvent == InitiatingEvents.Load)
                    FireFieldsProcessed(providers, c);

                return new ValueTask();
            };
        }
    }


    public interface IAggregatedBinder3 : 
        IAggregatedBinderBase, IServiceProviderProvider, IBinder3Base
    {
    }

    /// <summary>
    /// "v3" Aggregated Binder
    /// </summary>
    public class AggregatedBinder3 : AggregatedBinderBase3<IBinderProvider>,
        IAggregatedBinder3
    {
        public IServiceProvider Services { get; }

        public AggregatedBinder3(IServiceProvider services = null)
        {
            Services = services;
        }
    }



    public static class ProcessorProviderExtensions
    {
        /// <summary>
        /// Convenience method, mainly for compatibility with Binder v2
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="provider"></param>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task Process<TContext>(this IProcessorProvider<TContext> provider, TContext context, 
            CancellationToken cancellationToken = default)
            where TContext: class, IContext
        {
            return provider.Processor.ProcessAsync(context, cancellationToken);
        }
    }


    public static class Binder3Extensions
    {
        /// <summary>
        /// Convenience method, mainly for compatibility with Binder v2
        /// </summary>
        public static Task Process(this IFieldBinder binder, CancellationToken cancellationToken = default)
        {
            var context = new Context2(null, binder.Field, cancellationToken);
            return binder.Processor.ProcessAsync(context, cancellationToken);
        }

        public static FluentBinder<T> As<T>(this IFieldBinder<T> binder)
        {
            return new FluentBinder<T>(binder, true);
        }


        /// <summary>
        /// Creates a FluentBinder assuming that <paramref name="binder"/> can be safely cast to T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binder"></param>
        /// <returns></returns>
        public static FluentBinder<T> As<T>(this IFieldBinder binder)
        {
            return new FluentBinder<T>(binder);
        }
    }
}