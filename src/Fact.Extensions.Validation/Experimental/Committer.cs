using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    public class Committer
    {
        public event Func<ValueTask> Committing;

        public async Task DoCommit()
        {
            if(Committing != null)
                await Committing.Invoke();
        }
    }
}
