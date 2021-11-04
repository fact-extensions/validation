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

    public interface IField : IFieldBase,
        IFieldStatusCollector,
        IFieldStatusProvider2
    {
        object Value { get; }
    }


    public interface IField<out T> : IField
    {
        new T Value { get; }
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
