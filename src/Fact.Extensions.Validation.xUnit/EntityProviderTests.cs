using System;
using System.Collections.Generic;
using System.Text;
using Fact.Extensions.Validation.Synthetic;
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
            var tracker = new Tracker<string>(entity.Password1);

            tracker.Updated += (v, _) => entity.Password1 = v;
            tracker.Update("newPassword");
            
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
        
        /// <summary>
        /// Experimental test to start to bring Tracker & EntityProvider together
        /// </summary>
        /// <remarks>
        /// FIX: Turn this into a real unit test
        /// </remarks>
        [Fact]
        public async Task Test3()
        {
            var binder = new AggregatedBinder();
            var entity = new Synthetic.SyntheticEntity1();

            var entityProvider = binder.Entity(entity);
            var tracker = new Tracker<string>(entity.Password1);

            tracker.Updated += (v, _) => entity.Password1 = v;
            tracker.Update("newPassword");
            
            var fbPassword1 = entityProvider.AddField(x => x.Password1);

            await binder.Process();
        }
 
        /// <summary>
        /// Raw test just to verify things compile and run, not a good unit test
        /// </summary>
        /// <remarks>
        /// FIX: Turn this into a real unit test
        /// </remarks>
        [Fact]
        public async Task NestedEntityTest()
        {
            var binder = new AggregatedBinder();
            var profile = new Synthetic.Profile();

            profile.Mailing = new UsAddress();

            var profileProvider = binder.Entity(profile);
            var mailingAddressProvider = profileProvider.Entity(x => x.Mailing);
            
            var fbMailingCity = mailingAddressProvider.AddField(x => x.City);

            await binder.Process();

            var providers = binder.Providers.ToArray();
            providers.Should().HaveCount(1);
            var provider = providers[0];
            provider.Binder.Field.Name.Should().Be("Mailing.City");
            var statuses = provider.Binder.Field.Statuses.ToArray();
            statuses.Should().HaveCount(1);
            var status = statuses[0];
            status.Should().BeOfType<ScalarStatus>();
            //binder.Providers.Select(x => x.Binder.Field.Statuses)
        }
    }
}