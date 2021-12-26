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

            // DEBT: Make an =default here once we shake out stronger input binding
            
            // TODO: Make AddField not require an instance so that we can bind inputs later
            // from something other than 'entity'
            //var entityProvider = binder.Entity<Synthetic.UsAddress>(null);
            var entity = new Synthetic.UsAddress();
            var entityProvider = binder.Entity(entity);
            
            entityProvider.AddField(x => x.City);
            
            await binder.Process();
        }
    }
}