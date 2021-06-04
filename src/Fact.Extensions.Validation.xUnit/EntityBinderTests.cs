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
            var eb = new EntityBinder();

            eb.Value = new SyntheticEntity1();

            eb.Bind(typeof(SyntheticEntity1));
            await eb.Process();

            var fields = eb.Fields.ToArray();
            var statuses = fields.SelectMany(f => f.Statuses).ToArray();
            statuses.Should().HaveCount(2);
        }
    }
}
