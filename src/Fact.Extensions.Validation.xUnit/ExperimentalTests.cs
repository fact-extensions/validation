using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;

    public class ExperimentalTests
    {
        [Fact]
        public void Test1()
        {
            var mp = new ModuleProvider<Optional<string>, Synthetic.SyntheticEntity1>(
                new Optional<string>(),
                new Synthetic.SyntheticEntity1());

            mp.Test1("value");
        }
    }
}
