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


    public static class RegistryExtensions
    {
        public static FluentBinder2<T> Add<T>(this IAggregatedBinder binder, RegistryKey key, string name)
        {
            Func<T> getter = () => (T)key.GetValue(name);
            return binder.AddField(name, getter);
            //var binder = new Binder2<T>()
            //var fb = new FluentBinder2<T>()
        }
    }
}