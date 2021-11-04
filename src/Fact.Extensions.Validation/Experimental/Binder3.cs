
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
            where TContext: IContext
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