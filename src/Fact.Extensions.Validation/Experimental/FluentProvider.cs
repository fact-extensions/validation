using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation.Experimental
{
    public interface IModuleProvider<TProvided>
    {
        TProvided Provided { get; }
    }


    public class ModuleProvider<TProvided> : 
        IModuleProvider<TProvided>
    {
        readonly TProvided provided1;

        TProvided IModuleProvider<TProvided>.Provided => provided1;

        public ModuleProvider(TProvided provided)
        {
            provided1 = provided;
        }
    }

    public class ModuleProvider<TProvided1, TProvided2> : ModuleProvider<TProvided1>,
        IModuleProvider<TProvided2>
    {
        readonly TProvided2 provided2;

        TProvided2 IModuleProvider<TProvided2>.Provided => provided2;

        public ModuleProvider(TProvided1 provided1, TProvided2 provided2) : base(provided1)
        {
            this.provided2 = provided2;
        }
    }

    /*
    public class ModuleProvider<TProvided1, TProvided2, TProvided3> : ModuleProvider<TProvided1, TProvided2>,
        IModuleProvider<TProvided3>
    {
        TProvided3 IModuleProvider<TProvided3>.Provided { get; }
    } */

    public static class FluentModuleExtensions
    {
        public static TModuleProvider Test1<TModuleProvider, T>(this TModuleProvider test1, T setTo)
            where TModuleProvider: IModuleProvider<Optional<T>>
            //where TOptional: Optional<T>
        {
            test1.Provided.Value = setTo;
            return test1;
        }
    }
}
