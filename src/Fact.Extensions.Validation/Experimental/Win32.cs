using System;
using Microsoft.Win32;

namespace Fact.Extensions.Validation.Experimental
{
    public class TestReg1
    {
        public static void TryMe()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software");
        }
    }
}