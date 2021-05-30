using FluentAssertions;
using System;
using System.Linq;
using Xunit;


namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    
    public class BinderTests
    {
        [Fact]
        public void Test1()
        {
            var f = new FieldStatus("field1", "123");
            var b = new Binder2(f);

            var fb = b.As<string>();

            var fbInt = fb.Convert((IField<string> f, out int to) =>
            {
                if (int.TryParse(f.Value, out to)) return true;
                f.Error("Cannot convert to integer");
                return false;
            });

            fbInt.IsTrue(f => f == 123, "Must be 123");
        }
    }
}