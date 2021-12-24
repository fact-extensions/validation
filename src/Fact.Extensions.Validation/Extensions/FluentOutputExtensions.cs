using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation
{
    using Committer = Experimental.Committer;

    public static class FluentOutputExtensions
    {
        static bool FilterStatus(Status s)
            => s.Level != Status.Code.OK;

        /// <summary>
        /// Emit value in its current state of processing
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fb"></param>
        /// <param name="emitter"></param>
        /// <param name="whenStatus"></param>
        /// <param name="bypassFilter"></param>
        /// <returns></returns>
        public static IFluentBinder<T> Emit<T>(this IFluentBinder<T> fb, Action<T> emitter,
            Func<Status, bool> whenStatus = null, bool bypassFilter = false)
        {
            if (whenStatus == null) whenStatus = FilterStatus;

            fb.Binder.Processor.ProcessingAsync += (_, context) =>
            {
                IField field = context.Field;
                IField<T> f = fb.Field;

                if (bypassFilter || !field.Statuses.Any(whenStatus))
                    emitter(f.Value);

                return new ValueTask();
            };
            return fb;
        }


        /// <summary>
        /// Configures optional setter to write back to validating source
        /// </summary>
        /// <typeparam name="TFluentBinder"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="fluentBinder"></param>
        /// <param name="setter"></param>
        /// <param name="initialGetter"></param>
        /// <returns></returns>
        /// <remarks>
        /// Be careful - no compile time enforcement of T
        /// Mainly so that we can experiment with Win32 Registry setters
        /// Otherwise this could be IFluentBinder<typeparamref name="T"/> constrained
        /// </remarks>
        public static TFluentBinder Setter<TFluentBinder, T>(this TFluentBinder fluentBinder, Action<T> setter,
            Func<T> initialGetter = null)
            where TFluentBinder : IFluentBinder
        {
            var binder = (Experimental.IBinderBase<T>)fluentBinder.Binder;
            binder.setter = setter;
            if (initialGetter != null)
                setter(initialGetter());
            return fluentBinder;
        }


        public static TFluentBinder Commit<T, TFluentBinder>(this TFluentBinder fluentBinder, Committer committer,
            Action<T> commit)
            where TFluentBinder: IFluentBinder<T>
        {
            committer.Committing += () =>
            {
                //commit(fb.InitialValue);
                // DEBT: We may prefer to use InitialValue, though fb.Field.Value is smart enough to get us the
                // right thing
                IField<T> f = fluentBinder.Field;
                commit(f.Value);
                return new ValueTask();
            };
            return fluentBinder;
        }


        /// <summary>
        /// Take a particular action during commit phase of this binder
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fluentBinder"></param>
        /// <param name="commit"></param>
        /// <returns></returns>
        public static IFluentBinder<T> Commit<T>(this IFluentBinder<T> fluentBinder, Action<T> commit) =>
            fluentBinder.Commit(fluentBinder.Binder.Committer, commit);
    }
}
