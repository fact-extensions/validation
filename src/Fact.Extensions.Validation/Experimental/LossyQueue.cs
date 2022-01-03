using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    public class LossyQueue
    {
        Func<ValueTask> queued;
        Func<ValueTask> current;

        SemaphoreSlim mutex = new SemaphoreSlim(1);

        /// <summary>
        /// Cancellation token source associated with 'current' running 
        /// NOT ACTIVE OR FULLY TESTED YET
        /// </summary>
        CancellationTokenSource cts;

        // TODO: Add way to abort current() so that queued delegate can immediately run and displace it
        async ValueTask Runner()
        {
            await mutex.WaitAsync();
            while(current != null)
            {
                mutex.Release();
                await current();

                await mutex.WaitAsync();
                cts = new CancellationTokenSource();
                current = queued;
                queued = null;
            }
            mutex.Release();
        }

        /// <summary>
        /// Fired when a queued func displaces an unexecuted queued func
        /// </summary>
        public event Action Dropped;


        /// <summary>
        /// Add semi-scheduled execution to lossy queue
        /// While waiting for current running func, will dump all but the last added func
        /// </summary>
        /// <param name="func"></param>
        public void Add(Func<ValueTask> func)
        {
            mutex.Wait();

            if (current == null)
            {
                // Setting up cancellation token here as well as in Runner loop so that if
                // loop thread doesn't start yet, we can still cancel if another Add gets called
                cts = new CancellationTokenSource();
                current = func;

                mutex.Release();

                _ = Runner();
            }
            else
            {
                // If 'current' is running or about to run, cancel it
                cts.Cancel();

                bool dropping = queued != null;

                // prep next func for after the one we are cancelling
                queued = func;

                // once we release mutex, Runner loop will pick up queued func
                mutex.Release();

                if (dropping && Dropped != null) Dropped();
            }
        }
    }
}
