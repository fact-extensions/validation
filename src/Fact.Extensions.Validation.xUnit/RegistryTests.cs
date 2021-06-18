using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    using FluentAssertions;
    using Microsoft.Win32;
    using System.Linq;
    using System.Threading.Tasks;

    public class RegistryTests
    {
#if OS_WINDOWS
        [Fact]
        public void Test1()
        {
            TestReg1.TryMe();
        }


        [Fact]
        public async Task Test2()
        {
            var field = new FieldStatus("root", null);
            var ab = new AggregatedBinder(field);
            var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");
            var fb = ab.Add<string>(key, "ProductName");
            fb.Contains("Windows");
            await fb.Binder.Process();
            
            var statuses = fb.Field.Statuses.ToArray();
            statuses.Should().HaveCount(0);
        }
#endif
    }
}