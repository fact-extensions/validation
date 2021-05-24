using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation.xUnit
{
    public class Fixture
    {
        public IServiceProvider Services { get; }

        public Fixture()
        {
            var sc = new ServiceCollection();

            Services = sc.BuildServiceProvider();
        }
    }
}
