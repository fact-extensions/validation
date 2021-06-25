using Fact.Extensions.Validation.Experimental;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    
    public class FluentBinderTests : IClassFixture<Fixture>
    {
        readonly IServiceProvider services;

        public FluentBinderTests(Fixture fixture)
        {
            services = fixture.Services;
        }

        [Fact]
        public async Task NonTypedFluentBinder()
        {
            var field = new FieldStatus("test", null);
            var fb = field.BindNonTyped(() => "hi2u");

            var fb2 = fb.Required().
                IsNotEqualTo("hi2u").
                As<string>();

            fb2.StartsWith("bye");

            await fb.Binder.Process();

            var statuses = fb.Field.Statuses.ToArray();
            statuses.Should().HaveCount(2);
        }

        [Fact]
        public async Task EpochConversionTest()
        {
            var ag = new AggregatedBinder(new FieldStatus("test", null), services);

            var fb = ag.AddField("epoch", () => long.MinValue).
                FromEpochToDateTimeOffset();

            await fb.Binder.Process();

            var statues = fb.Binder.Field.Statuses.ToArray();
            statues.Should().HaveCount(1);

            var fb2 = ag.AddField("epoch2", () => 0);
            var fb3 = fb2.AsEpoch();
            var fb4 = fb3.ToDateTimeOffset();

            await fb2.Binder.Process();

            statues = fb2.Binder.Field.Statuses.ToArray();
            statues.Should().HaveCount(0);
            fb4.InitialValue.Should().Be(DateTimeOffset.UnixEpoch);
        }


        [Fact]
        public async Task ChainTest()
        {
            var field = new FieldStatus("testSource", null);
            var fb = field.Bind(() => "hi2u");
            var field2 = new FieldStatus("testChained", null);
            var binder = new Binder2(field2, () => field2.Value);
            var fb2 = fb.Chain(binder, v => field2.Value = v);

            //fb.InitialValue.Should().Be("hi2u");
            //fb2.InitialValue.Should().BeNull();

            await fb.Binder.Process();

            // FIX: Technically we want InitialValue set here, but so far it actually only gets set during
            // convert operations
            //fb.InitialValue.Should().Be("hi2u");
            fb2.InitialValue.Should().Be("hi2u");
        }


        [Fact]
        public async Task GroupValidateTest()
        {
            var ab = new AggregatedBinder(new FieldStatus("synthetic", null));
            var fb1 = ab.AddField("field1", () => "one");
            var fb2 = ab.AddField("field2", () => "two");

            fb1.GroupValidate(fb2, (c, field1, field2) =>
            {
                if(field1.Value != field2.Value)
                {
                    field1.Error("Must match field2");
                    field2.Error("Must match field1");
                }
                return new ValueTask();
            });

            await fb1.Binder.Process();

            var statuses1 = fb1.Binder.Field.Statuses.ToArray();
            var statuses2 = fb2.Binder.Field.Statuses.ToArray();
        }


        [Fact]
        public async Task GroupValidate2Test()
        {
            var ab = new AggregatedBinder(new FieldStatus("synthetic", null));
            var fb1 = ab.AddField("field1", () => "one");
            var fb2 = ab.AddField("field2", () => "two");

            fb1.IsMatch(fb2);

            await fb2.Binder.Process();

            var statuses1 = fb1.Binder.Field.Statuses.ToArray();
            var statuses2 = fb2.Binder.Field.Statuses.ToArray();
        }
    }
}
