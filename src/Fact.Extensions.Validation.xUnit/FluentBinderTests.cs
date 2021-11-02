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
        public async Task BinderV3Test()
        {
            var fb = new FluentBinder3<int>("test1", () => 123);

            var fbConverted = fb.Convert3((IField<int> f, out string s) =>
            {
                s = f.Value.ToString();
                return true;
            });

            await fb.Binder.Process();

            fbConverted.Field.Value.Should().Be("123");
        }

        [Fact]
        public async Task NonTypedFluentBinder()
        {
            var field = new FieldStatus("test");
            var fb = field.BindNonTyped(() => "hi2u");
            
            var fb2 = fb.IsNotNull().
                IsNotEqualTo("hi2u").
                As<string>();

            fb2.StartsWith("bye");

            await fb.Binder.Process();

            var statuses = field.Statuses.ToArray();
            statuses.Should().HaveCount(2);
        }

        [Fact]
        public async Task EpochConversionTest()
        {
            var ag = new AggregatedBinder3(services);

            var fb = ag.AddField3("epoch", () => long.MinValue).
                FromEpochToDateTimeOffset();

            await fb.Binder.Process();

            var statues = fb.Binder.Field.Statuses.ToArray();
            statues.Should().HaveCount(1);

            var fb2 = ag.AddField3("epoch2", () => 0);
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
            var field = new FieldStatus("testSource");
            var fb = field.Bind(() => "hi2u");
            var field2 = new FieldStatus("testChained");
            var binder = new FieldBinder<object>(field2, () => field2.Value);
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
            var ab = new AggregatedBinder3();
            var fb1 = ab.AddField3("field1", () => "one");
            var fb2 = ab.AddField3("field2", () => "two");

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
            statuses1.Should().HaveCount(1);
            statuses2.Should().HaveCount(1);
        }


        [Fact]
        public async Task GroupValidate2Test()
        {
            var ab = new AggregatedBinder3();
            var fb1 = ab.AddField3("field1", () => "one");
            var fb2 = ab.AddField3("field2", () => "two");

            fb1.IsMatch(fb2);

            await fb2.Binder.Process();

            var statuses1 = fb1.Binder.Field.Statuses.ToArray();
            var statuses2 = fb2.Binder.Field.Statuses.ToArray();
            statuses1.Should().HaveCount(1);
            statuses2.Should().HaveCount(1);
        }


        [Fact]
        public async Task InitializerTest()
        {
            var ab = new AggregatedBinder3();
            string value1 = null;
            string value2 = null;
            string committed1 = null;
            string committed2 = null;
            var fb1 = ab.AddField3("field1", () => value1);
            var fb2 = ab.AddField3("field2", () => value2);

            fb1.Setter(v => value1 = v, () => "one");
            fb2.Setter(v => value2 = v, () => "two");

            value1.Should().Be("one");
            value2.Should().Be("two");

            fb1.Field.Value.Should().Be("one");

            fb1.Commit(v => committed1 = v);
            fb2.Commit(v => committed2 = v);

            await ab.Process();

            await ab.Committer.DoCommit();

            committed1.Should().Be("one");
            committed2.Should().Be("two");
        }
        
                
        
        [Fact]
        public async Task StringRequiredTest()
        {
            string value = null;
            var fb = new FluentBinder3<string>("field1", () => value);
            
            await fb.Binder.Process();

            var statuses = fb.Binder.Field.Statuses.ToArray();
            statuses.Should().BeEmpty();

            fb.Required3();

            await fb.Binder.Process();

            statuses = fb.Binder.Field.Statuses.ToArray();
            statuses.Should().HaveCount(1);
            
            value = "hi2u";
            
            await fb.Binder.Process();

            statuses = fb.Binder.Field.Statuses.ToArray();
            statuses.Should().BeEmpty();
        }


        [Fact]
        public async Task StringOptionalTest()
        {
            string value = null;
            var fb = new FluentBinder3<string>("field1", () => value).
                Optional().
                IsEqualTo("hi2u");

            await fb.Binder.Process();

            var statuses = fb.Binder.Field.Statuses.ToArray();
            statuses.Should().BeEmpty();
        }
    }
}
