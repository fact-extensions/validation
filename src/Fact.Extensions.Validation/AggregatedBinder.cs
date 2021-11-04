﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    // DEBT: For .Process() Extension and InitiatingEvents both of which work, but are sloppy in name and function
    using Experimental;

    public delegate void BindersProcessedDelegate<TBinderProvider>(IEnumerable<TBinderProvider> binders, Context2 context);

    public class AggregatedBinderBase3<TBinderProvider> : Experimental.Binder3Base,
        IAggregatedBinderBase<TBinderProvider>
        where TBinderProvider : IBinderProvider
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

            binderProvider.Binder.Processor.ProcessedAsync += (_, context) =>
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
                foreach (TBinderProvider provider in providers)
                {
                    // DEBT: when running through providers sometimes they abort, but we overall want to keep
                    // going.  So brute forcing abort to false always -- this feels like the wrong place to do
                    // this though
                    context.Abort = false;

                    await provider.Binder.Processor.ProcessAsync(context, context.CancellationToken);
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


    public interface IAggregatedBinderBase :
        IAggregatedBinderProvider,
        IAggregatedBinderCollector
    {
    }

    public interface IAggregatedBinderBase<TBinderProvider> :
        IAggregatedBinderProvider,
        ICollector<TBinderProvider>
        where TBinderProvider : IBinderProvider
    {
        event BindersProcessedDelegate<TBinderProvider> BindersProcessed;
    }


    public interface IAggregatedBinder3 :
        IAggregatedBinderBase, IServiceProviderProvider, IBinder3Base
    {
    }

    /// <summary>
    /// "v3" Aggregated Binder
    /// </summary>
    public class AggregatedBinder : AggregatedBinderBase3<IBinderProvider>,
        IAggregatedBinder3
    {
        public IServiceProvider Services { get; }

        public AggregatedBinder(IServiceProvider services = null)
        {
            Services = services;
        }
    }
}
