using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// EXPERIMENTAL, not used
namespace Fact.Extensions.Validation.Experimental
{
    public delegate ValueTask ProcessingDelegateAsync<TContext>(object sender, TContext context);

    public interface IProcessor<TContext>
    {
        Task ProcessAsync(TContext context, CancellationToken cancellationToken = default);
    }
    
    public class Processor<TContext>
        where TContext: IContext
    {
        public event ProcessingDelegateAsync<TContext> StartingAsync;
        public event ProcessingDelegateAsync<TContext> ProcessingAsync;
        public event ProcessingDelegateAsync<TContext> ProcessedAsync;
        public event Action Aborting;
        
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