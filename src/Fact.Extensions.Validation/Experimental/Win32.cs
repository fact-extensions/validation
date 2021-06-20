using System;
using System.Collections.Generic;
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


    public interface IRegistryBinder : IAggregatedBinderBase
    {
        RegistryKey Root { get; }
    }


    public class RegistryBinder : IRegistryBinder
    {
        public class Provider : AggregatedBinderBase.ItemBase
        {
            public Provider(IFluentBinder fb) : base(fb.Binder, fb)
            {

            }
        }

        readonly RegistryKey root;

        public RegistryKey Root => root;

        public RegistryBinder(RegistryKey key)
        {
            root = key;
        }


        public RegistryBinder(RegistryHive hive, string path)
        {
            var view = RegistryView.Default;
            root = RegistryKey.OpenBaseKey(hive, view);
            root = root.OpenSubKey(path);
        }


        protected readonly List<Provider> providers = new List<Provider>();

        public IEnumerable<IBinderProvider> Providers => providers;

        public void Add(IBinderProvider collected)
        {
            // DEBT: Problematic cast
            providers.Add((Provider)collected);
        }
    }


    public static class RegistryExtensions
    {
        public static FluentBinder2<T> Add<T>(this IRegistryBinder binder, string name)
        {
            Func<T> getter = () => (T)binder.Root.GetValue(name);
            var field = new FieldStatus<T>(name, default(T));
            var b = new Binder2<T>(field, getter);
            var fluentBinder = new FluentBinder2<T>(b);
            binder.Add(new RegistryBinder.Provider(fluentBinder));
            return fluentBinder;
        }

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