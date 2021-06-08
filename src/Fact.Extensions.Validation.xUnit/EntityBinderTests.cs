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

    public class EntityBinderTests
    {
        [Fact]
        public async Task Test1()
        {
            // NOTE: Experimenting with the idea of an entity itself contained in a field
            // Seems reasonable, but definitely more complicated than a regular field
            // Also there are some circumstances where there is no direct 'field' holding a
            // entity (like a form input).  However, if you think about it, form controls also
            // aren't really fields either.  But there is this notion of a completely virtualized
            // entity that only exists as a bunch of getters from those controls.
            var field = new FieldStatus("synthetic", null);

            var ab = new AggregatedBinder(field);
            var inputEntity = new SyntheticEntity1();
            var outputEntity = new SyntheticEntity1();

            inputEntity.UserName = "fred";

            ab.BindInput2(inputEntity, true);
            Committer c = ab.BindOutput(outputEntity);
            await ab.Process();

            var fields = ab.Fields().ToArray();
            var statuses = fields.SelectMany(f => f.Statuses).ToArray();
            statuses.Should().HaveCount(2);

            outputEntity.UserName.Should().BeNull();

            await c.DoCommit();

            outputEntity.UserName.Should().Be(inputEntity.UserName);
        }

        [Fact]
        public async Task Test2()
        {
            var field = new FieldStatus("synthetic", null);

            var eb = new AggregatedBinder(field);

            eb.AddField<string>(nameof(SyntheticEntity1.Password1), () => null);
            eb.AddField<string>(nameof(SyntheticEntity1.Password2), () => null);
            
            eb.BindValidation(typeof(SyntheticEntity1));
            await eb.Process();

            var fields = eb.Fields().ToArray();
            var statuses = fields.SelectMany(f => f.Statuses).ToArray();
            statuses.Should().HaveCount(2);
        }

        [Fact]
        public async Task Test3()
        {
            var field = new FieldStatus("synthetic", null);
            var ab = new AggregatedBinder(field);
            var inputEntity = new SyntheticEntity1();

            var entityBinder = ab.BindInput2(inputEntity, true);

            var passwordBinder1 = entityBinder.Get(x => x.Password1);
            passwordBinder1.FluentBinder.IsTrue(x => x == null, "Must be null");

            await ab.Process();
        }
    }
}
