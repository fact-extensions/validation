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
            var inputEntity = new SyntheticEntity1();
            var outputEntity = new SyntheticEntity1();

            inputEntity.UserName = "fred";

            eb.BindInstance(inputEntity);
            Committer c = eb.BindOutput(outputEntity);
            await eb.Process();

            var fields = eb.Fields.ToArray();
            var statuses = fields.SelectMany(f => f.Statuses).ToArray();
            statuses.Should().HaveCount(2);

            outputEntity.UserName.Should().BeNull();

            await c.DoCommit();

            outputEntity.UserName.Should().Be(inputEntity.UserName);
        }
    }
}
