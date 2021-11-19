using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    public class LossyQueue
    {
        Func<ValueTask> queued;
        Func<ValueTask> current;

        async ValueTask Runner()
        {
            await current();
            current = null;
            if(queued != null)
            {
                // to avoid infinite loop
                var temp = queued;
                queued = null;

                Current = temp;
            }
        }

        Func<ValueTask> Current
        {
            get
            {
                return current;
            }
            set
            {
                current = value;
                _ = Runner();
            }
        }

        public void Add(Func<ValueTask> func)
        {
            if(current == null)
            {
                Current = func;
            }
            else
            {
                queued = func;
            }
        }
    }
}
