using System;
using System.Threading.Tasks;
using Fact.Extensions.Validation.Experimental;
using FluentAssertions;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    public class FluentBinderExtensionsTests
    {
        [Fact]
        public void StartsWithTest()
        {
            var binder = new AggregatedBinder();
            
            var fb = binder.AddField("test1", () => "hi2u");

            fb.StartsWith("hi");
        }


        [Fact]
        public async Task ToUriTest()
        {
            var binder = new AggregatedBinder();
            Uri emitted = null;
            
            var fb = binder.AddField("test1", () => Constants.Strings.Uris.Google);

            fb.ToUri().Emit(v => emitted = v);

            await binder.Process();

            emitted.Should().NotBeNull();
            emitted.OriginalString.Should().Be(Constants.Strings.Uris.Google);
        }
    }
}