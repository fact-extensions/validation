using System;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    public class FieldStatusTests
    {
        [Fact]
        public void FieldStatusCollectionTest()
        {
            var fsc = new Experimental.FieldStatusCollection();
        }


        [Fact]
        public void BinderTest()
        {
            var b = new Experimental.Binder("field1");

            b.Validate += f =>
            {
                f.Add(FieldStatus.Code.Error, "You didn't do it right!");
            };
        }
    }
}
