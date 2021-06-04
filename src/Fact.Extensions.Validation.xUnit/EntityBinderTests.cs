using System;
using System.Collections.Generic;
using System.Text;

using Xunit;
using FluentAssertions;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
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
        }
    }
}
