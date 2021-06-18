using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    public class RegistryTests
    {
#if OS_WINDOWS
        [Fact]
        public void Test1()
        {
            Experimental.TestReg1.TryMe();
        }
#endif
    }
}