using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation
{
    public interface IFieldBase :
        IFieldStatusProvider2,
        IFieldStatusCollector2
    {
        // DEBT: Use Fact.Collections version of this
        string Name { get; }
    }

    public interface IField : IFieldBase
    {
        object Value { get; }
    }


    public interface IField<T> : IField
    {
        new T Value { get; }
    }


}
