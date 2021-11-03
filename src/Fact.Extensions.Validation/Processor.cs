using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Fact.Extensions.Validation
{
    public delegate ValueTask ProcessingDelegateAsync<TContext>(object sender, TContext context);



    public interface IProcessor<TContext>
    {
        Task ProcessAsync(TContext context, CancellationToken cancellationToken = default);
    }
    
    public class Processor<TContext>
        where TContext: IContext
    {
        /// <summary>
        /// Runs before main Processing chain.  Does not heed Sequential or Abort flags
        /// </summary>
        public event ProcessingDelegateAsync<TContext> StartingAsync;
        /// <summary>
        /// Main processing chain.  If Abort flag is set, processing stops
        /// </summary>
        public event ProcessingDelegateAsync<TContext> ProcessingAsync;
        /// <summary>
        /// Runs after main Processing chain.  Does not heed Sequential or Abort flags
        /// </summary>
        public event ProcessingDelegateAsync<TContext> ProcessedAsync;
        public event Action Aborting;
        

        /// <summary>
        /// Runs full processing chain - Starting, Processing, Processed
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ProcessAsync(TContext context, CancellationToken cancellationToken = default)
        {
            if (StartingAsync != null)
                await StartingAsync(this, context);

            // NOTE: Odd that following line doesn't compile now.
            // Fortunately our scenario that's OK
            //Processing?.Invoke(field, context);
            var delegates = ProcessingAsync?.GetInvocationList() ?? System.Linq.Enumerable.Empty<object>();

            var nonsequential = new LinkedList<Task>();

            foreach (ProcessingDelegateAsync<TContext> d in delegates)
            {
                context.Sequential = true;
                ValueTask task = d(this, context);
                if (context.Abort)
                {
                    // TODO: Consider an alternate tcs here which we immediately abort
                    Aborting?.Invoke();
                    break;
                }
                if (context.Sequential)
                    await task;
                else
                    nonsequential.AddLast(task.AsTask());
            }

            // guidance from
            // https://stackoverflow.com/questions/27238232/how-can-i-cancel-task-whenall
            var tcs = new TaskCompletionSource<bool>(cancellationToken);
            await Task.WhenAny(Task.WhenAll(nonsequential), tcs.Task);

            if(ProcessedAsync != null)
                await ProcessedAsync.Invoke(this, context);            
        }
    }
}