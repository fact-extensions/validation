using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    public class FieldStatusTests : IClassFixture<Fixture>
    {
        readonly IServiceProvider services;

        public FieldStatusTests(Fixture fixture)
        {
            services = fixture.Services;
        }


        // Newly added IValueProvider may be finicky, so proving concept here - not so much
        // a unit test - might be useful if casting rules subtly change (doubtful though)
        [Fact]
        public void DowncastTest()
        {
            var f = new FieldStatus<int>("intval", 10);
            IValueProvider<object> f2 = f;
            IField f3 = f;

            f2.Value.Should().BeOfType<int>().And.BeEquivalentTo(10);
            f3.Value.Should().BeOfType<int>().And.BeEquivalentTo(10);
        }
    }
}