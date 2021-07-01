using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    using System.Threading.Tasks;

    public class FieldStatusTests : IClassFixture<Fixture>
    {
        readonly IServiceProvider services;

        public FieldStatusTests(Fixture fixture)
        {
            services = fixture.Services;
        }
    }
}