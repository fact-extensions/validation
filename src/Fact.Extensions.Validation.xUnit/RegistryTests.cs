using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    using FluentAssertions;
    using Microsoft.Win32;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class RegistryTests
    {
#if OS_WINDOWS
        [Fact]
        public void Test1()
        {
            TestReg1.TryMe();
            //var key = Registry.LocalMachine.OpenSubKey(Constants.Registry.Paths.WindowsVersion);
            //key.GetValue
        }


        [Fact]
        public async Task Test2()
        {
            var field = new FieldStatus("root");
            var ab = new AggregatedBinder(field);
            var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");
            var fb = ab.Add<string>(key, "ProductName");
            fb.Contains("Windows");
            await fb.Binder.Process();
            
            var statuses = fb.Field.Statuses.ToArray();
            statuses.Should().HaveCount(0);

            var fb2 = ab.Add<int>(key, "InstallDate");
            // NOTE: This Convert doesn't actually do anything here, the fb2Converted below takes over
            fb2.Convert((IField<int> f, out DateTimeOffset dt) =>
            {
                dt = DateTimeOffset.FromUnixTimeSeconds(f.Value);
                return true;
            });

            DateTimeOffset emittedDto = DateTimeOffset.MinValue;
            DateTimeOffset y2k = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset now = DateTimeOffset.Now;

            var fb2Converted = fb2.FromEpochToDateTimeOffset().
                GreaterThan(y2k).LessThan(now).
                Emit(dto => emittedDto = dto);

            await fb2.Binder.Process();

            emittedDto.Should().BeAfter(y2k);
        }

        // FIX: Sloppiness with FluentBinder2.InitialValue is hurting us here
        [Fact]
        public async Task Test3()
        {
            var field = new FieldStatus("root", null);
            var ab = new AggregatedBinder(field);
            var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");

            var fb2 = ab.Add<int>(key, "InstallDate").AsEpoch().
                ToDateTimeOffset().  // FIX: Doesn't work, look into this
                GreaterThan(DateTimeOffset.UnixEpoch);

            await fb2.Binder.Process();

            var statuses = fb2.Field.Statuses.ToArray();
            statuses.Should().BeEmpty();
        }


        [Fact]
        public async Task Test4()
        {
            var rb = new RegistryBinder(RegistryHive.LocalMachine, Constants.Registry.Paths.WindowsVersion);
            int installDate = 0;

            var fb = rb.Add("InstallDate").As<int>();
            
            fb.Emit(v => installDate = v);

            await fb.Binder.Process();

            installDate.Should().BeGreaterThan(0);
        }
#endif
    }
}