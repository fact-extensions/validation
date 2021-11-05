using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    public interface IFieldBase
    {
        // DEBT: Use Fact.Collections version of this
        string Name { get; }
    }


    public interface IValueProvider<out T>
    {
        T Value { get; }
    }


    public interface IFieldStatus : IFieldBase,
        IFieldStatusCollector,
        IFieldStatusProvider2
    {
    }

    public interface IField : IFieldStatus,
        IValueProvider<object>
    {
    }


    public interface IField<out T> : IFieldStatus,
        IValueProvider<T>
    {
    }


    public class FieldBase : IFieldBase
    {
        public string Name { get; }


        public FieldBase(string name)
        {
            Name = name;
        }
    }
}
