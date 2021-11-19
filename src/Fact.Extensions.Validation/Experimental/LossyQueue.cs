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

        async ValueTask Runner()
        {
            await current();
            await mutex.WaitAsync();
            current = null;
            if (queued != null)
            {
                current = queued;
                queued = null;
                mutex.Release();

                _ = Runner();
            }
            else
                mutex.Release();
        }

        /// <summary>
        /// Fired when a queued func displaces an unexecuted queued func
        /// </summary>
        public event Action Dropped;

        public void Add(Func<ValueTask> func)
        {
            mutex.Wait();
            if (current == null)
            {
                current = func;
                mutex.Release();
                _ = Runner();
            }
            else
            {
                bool dropping = queued != null;

                queued = func;
                mutex.Release();

                if (dropping && Dropped != null) Dropped();
            }
        }
    }
}
