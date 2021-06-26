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
            var ab = new AggregatedBinderBase3<IBinderProvider>();

            var fb = ab.AddField3("field1", () => 123);

            await ab.Process();

            var statuses = fb.Binder.Field.Statuses.ToArray();
            statuses.Should().HaveCount(0);

            fb.GreaterThan(124);

            await ab.Process();

            statuses = fb.Binder.Field.Statuses.ToArray();
            statuses.Should().HaveCount(1);
        }
    }
}
