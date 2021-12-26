using System;
using System.Collections.Generic;
using System.Text;

using Xunit;
using FluentAssertions;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    using System.Linq;
    using System.Threading.Tasks;

    public class EntityProviderTests
    {
        /// <summary>
        /// Raw test just to verify things compile and run, not a good unit test
        /// </summary>
        /// <remarks>
        /// FIX: Turn this into a real unit test
        /// </remarks>
        [Fact]
        public async Task Test1()
        {
            var binder = new AggregatedBinder();
            var entity = new Synthetic.SyntheticEntity1();

            var entityProvider = binder.Entity(entity);
            
            var fbPassword1 = entityProvider.AddField(x => x.Password1);

            await binder.Process();
        }
        
        [Fact]
        public async Task Test2()
        {
            var binder = new AggregatedBinder();
            var entity = new Synthetic.UsAddress();

            var entityProvider = binder.Entity(entity);
            
            await binder.Process();
        }
    }
}