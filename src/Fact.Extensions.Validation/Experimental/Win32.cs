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


    public interface IRegistryBinder : IAggregatedBinderBase<RegistryBinder.Provider>
    {
        RegistryKey Root { get; }
    }


    // DEBT: Wants to attach to a parent binder, since this isn't an aggregated binder of its own
    public class RegistryBinder : 
        AggregatedBinderBase<RegistryBinder.Provider>,
        IRegistryBinder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Could have used ItemBase directly, but breaking it out here in case we want to track extra things
        /// </remarks>
        public class Provider : BinderProviderBase
        {
            public Provider(IFluentBinder fb) : base(fb.Binder, fb)
            {

            }
        }

        public class Provider<T> : Provider
        {
            public new FluentBinder<T> FluentBinder { get; }

            public Provider(FluentBinder<T> fb) : base(fb)
            {
                FluentBinder = fb;
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


        // EXPERIMENTAL
        public RegistryBinder(RegistryHive hive, string path, bool writeable)
        {
            var view = RegistryView.Default;
            root = RegistryKey.OpenBaseKey(hive, view);
            root = root.CreateSubKey(path, writeable);
        }
    }


    public static class RegistryExtensions
    {
        // Keeping around as a "v2" curiousity.  Can delete at any time
#if UNUSED
        public static FluentBinder2<T> Add<T>(this IRegistryBinder binder, string name)
        {
            Func<T> getter = () => (T)binder.Root.GetValue(name);
            var field = new FieldStatus<T>(name, default(T));
            var b = new Binder2<T>(field, getter);
            var fluentBinder = new FluentBinder2<T>(b);
            binder.Add(new RegistryBinder.Provider(fluentBinder));
            return fluentBinder;
        }
#endif


        /// <summary>
        /// Adds field associated with registry key and also auto loads value for it
        /// </summary>
        /// <typeparam name="T">Type expected to be retrieved from registry</typeparam>
        /// <param name="binder"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FluentBinder<T> Add<T>(this IRegistryBinder binder, string name, T defaultValue = default) =>
            binder.AddField(name, 
                () => (T)binder.Root.GetValue(name, defaultValue), 
                fb => new RegistryBinder.Provider<T>(fb)).
            FluentBinder;

        /// <summary>
        /// Adds field associated with registry key and also auto loads value for it
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FluentBinder<object> Add(this IRegistryBinder binder, string name) =>
            binder.Add<object>(name);


        // TODO: Consider consolidating with Fact.Extensions Taxonomy
        public static RegistryBinder Open(this IRegistryBinder binder, string path) =>
            new RegistryBinder(binder.Root.OpenSubKey(path));

        public static FluentBinder<object> Add(this IAggregatedBinderBase binder, RegistryKey key, string name)
        {
            Func<object> getter = () => key.GetValue(name);
            var fb = binder.AddField(name, getter);
            fb.Setter((object v) => key.SetValue(name, v)); // UNUSED
            return fb;
        }


        /// <summary>
        /// Create and add a FluentBinder which attaches to a specified registry key and name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="binder"></param>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <remarks>
        /// This one's trick is that one needn't hang it off an IRegistryBinder
        /// </remarks>
        public static FluentBinder<T> Add<T>(this IAggregatedBinderBase binder, RegistryKey key, string name)
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
            var fb = binder.AddField(name, getter);
            fb = fb.Setter((T v) => key.SetValue(name, v)); // UNUSED
            //var binder = new Binder2<T>()
            //var fb = new FluentBinder2<T>()
            return fb;
        }
    }
}