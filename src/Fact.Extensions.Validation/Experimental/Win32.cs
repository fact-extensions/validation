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
            Func<T> getter = () =>
            {
                object v = key.GetValue(name);
                // watch out for
                // https://stackoverflow.com/questions/61417462/unable-to-cast-object-of-type-system-int32-to-type-system-int64
                // here and other places we may require an exact cast (can't specify int64 when it's an int32)
                T _v = (T)v;
                return _v;
            };
            return binder.AddField(name, getter);
            //var binder = new Binder2<T>()
            //var fb = new FluentBinder2<T>()
        }
    }
}