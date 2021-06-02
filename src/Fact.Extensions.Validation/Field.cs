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
        IFieldStatusCollector2,
        IFieldStatusProvider2
    {
        object Value { get; }
    }


    public interface IField<T> : IField
    {
        new T Value { get; }
    }


    public class FieldBase : IFieldBase
    {
        readonly string name;

        public string Name => name;


        public FieldBase(string name)
        {
            this.name = name;
        }
    }
}
