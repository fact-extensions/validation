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
        // Due to change in setter and aggregated binder direction, this test is offline
        //[Fact]
        public async Task Test1()
        {
            // NOTE: Experimenting with the idea of an entity itself contained in a field
            // Seems reasonable, but definitely more complicated than a regular field
            // Also there are some circumstances where there is no direct 'field' holding a
            // entity (like a form input).  However, if you think about it, form controls also
            // aren't really fields either.  But there is this notion of a completely virtualized
            // entity that only exists as a bunch of getters from those controls.
            // NOTE: No longer heading that direction as we can see via AggregatedBinder3
            //var field = new FieldStatus("synthetic");

            var ab = new AggregatedBinder();
            var inputEntity = new Synthetic.User();
            var outputEntity = new Synthetic.User();

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
            var eb = new AggregatedBinder();

            eb.AddField<string>(nameof(Synthetic.User.Password1), () => null);
            eb.AddField<string>(nameof(Synthetic.User.Password2), () => null);
            
            eb.BindValidation(typeof(Synthetic.User));
            await eb.Process();

            var fields = eb.Fields().ToArray();
            var statuses = fields.SelectMany(f => f.Statuses).ToArray();
            statuses.Should().HaveCount(2);
        }

        // Due to change in setter and aggregated binder direction, this test is offline
        //[Fact]
        public async Task Test3()
        {
            var ab = new AggregatedBinder();

            var inputEntity = new Synthetic.User
            {
                Password1 = "bye"
            };

            var entityBinder = ab.BindInput2(inputEntity);

            // FIX: Need to build out Required and AbortOnNull, otherwise we get a null exception
            // when reaching 'StartsWith'

            var passwordBinder1 = entityBinder.Get(x => x.Password1);

            // DEBT: Naughty cast
            var fluentBinder = passwordBinder1.FluentBinder;
            fluentBinder.
                Required().
                StartsWith("hi");

            await ab.Process();

            var fields = ab.Fields().ToArray();
            var statuses = fields.SelectMany(f => f.Statuses).ToArray();
            statuses.Should().HaveCount(1);
        }

        [Fact]
        public async Task Test4()
        {
            var field = new FieldStatus("synthetic");
            var ab = new AggregatedBinder();
            string test1val = "test1 value";

            ab.AddField("test1", () => test1val).StartsWith("test2");

            ab.AddSummaryProcessor(field);

            await ab.Process();

            var statuses = ab.Fields().SelectMany(f => f.Statuses).ToArray();
            statuses.Should().HaveCount(1);
        }


        // Due to change in setter and aggregated binder direction, this test is offline
        //[Fact]
        public async Task Test5()
        {
            var ab = new AggregatedBinder();
            var dummy = new Synthetic.User();
            dummy.Password1 = "hi2u";
            var eb = ab.BindInput2(dummy);

            eb.GroupValidate(e => e.Password1, e => e.Password2,
                (c, pass1, pass2) =>
            {
                if(pass1.Value != pass2.Value)
                {
                    // NOTE: Abusing the scalar flavor so that we can observe the input values more easily below
                    pass1.Error(FieldStatus.ComparisonCode.Unspecified, pass1.Value, "Passwords do not match");
                    pass2.Error(FieldStatus.ComparisonCode.Unspecified, pass2.Value, "Passwords do not match");
                }
            });

            await ab.Process();

            var statuses = ab.Fields().SelectMany(f => f.Statuses).ToArray();
            statuses.Should().HaveCount(2);

            dummy.Password2 = dummy.Password1;

            await ab.Process();

            statuses = ab.Fields().SelectMany(f => f.Statuses).ToArray();
            statuses.Should().HaveCount(0);
        }


        [Fact]
        public void PropertyBinderProviderCreate()
        {
            var obj = new Synthetic.User();
            var prop = obj.GetType().GetProperty(nameof(obj.Password1));
            var pbp = PropertyBinderProvider.Create(prop, () => obj);
            pbp.Should().BeOfType<PropertyBinderProvider<string>>();
        }


        [Fact]
        public async Task BasicEntityBinderTest1()
        {
            var entity = new Synthetic.User();
            var binder = new BasicEntityBinder(typeof(Synthetic.User), () => entity);
            var context = new Context2(null, null, default);
            await binder.Processor.ProcessAsync(context);
            var fields = binder.Fields().ToArray();
            fields.Should().HaveCount(3);
            var statuses = fields[0].Statuses.ToArray();
            statuses.Should().HaveCount(1);
            statuses[0].Level.Should().Be(Status.Code.Error);
        }


        [Fact]
        public async Task Test6()
        {
            var binder = new AggregatedBinder();
            var entity = new Synthetic.User();

            binder.AddField(e => e.Password1, () => entity);
            binder.AddField(e => e.Password2, () => entity);

            entity.Password2 = "hi2u";

            await binder.Process();

            var fields = binder.Fields().ToArray(); 
            var statuses = fields[0].Statuses.ToArray();

            statuses.Should().HaveCount(1);
        }
    }
}
