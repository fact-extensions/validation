using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    using FluentAssertions;
    using System.Linq;
    using System.Threading.Tasks;

    public class AggregatedBinderTests
    {
        [Fact]
        public async Task Test1()
        {
            var ab = new AggregatedBinder3();

            var fb = ab.AddField("field1", () => 123);

            await ab.Process();

            var statuses = fb.Binder.Field.Statuses.ToArray();
            statuses.Should().HaveCount(0);

            fb.Required((int v) => v == 0).
                GreaterThan(124);

            await ab.Process();

            statuses = fb.Binder.Field.Statuses.ToArray();
            statuses.Should().HaveCount(1);
        }


        [Fact]
        public async Task Test2()
        {
            var ab = new AggregatedBinder3();
            string value1 = "";
            string value2 = "4";

            var fb = ab.AddField("field1", () => value1);
            var fb2 = ab.AddField("field2", () => value2);

            // FIX: Not going to FluentBinder3 Required, so "null" check is weak

            fb.
                Required().
                Convert<int>().GreaterThan(10);

            fb2.
                Required().
                Convert<int>().LessThan(5);

            await fb.Binder.Process();
            await fb2.Binder.Process();

            var statuses = ab.Fields().SelectMany(x => x.Statuses).ToArray();

            statuses.Should().HaveCount(1);

            value1 = "11";

            await fb.Binder.Process();
            await fb2.Binder.Process();

            statuses = ab.Fields().SelectMany(x => x.Statuses).ToArray();

            statuses.Should().HaveCount(0);
        }
    }
}
