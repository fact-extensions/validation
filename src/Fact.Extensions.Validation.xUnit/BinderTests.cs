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
            b.getter = () => f.Value;

            var fb = b.As<string>();

            var fbInt = fb.Convert((IField<string> f, out int to) =>
            {
                if (int.TryParse(f.Value, out to)) return true;
                f.Error("Cannot convert to integer");
                return false;
            });

            fbInt.IsTrue(f => f == 123, "Must be 123");

            b.Process();

            fbInt.IsTrue(v => v == 124, "Must be 124");

            b.Process();

            f.Statuses.Should().HaveCount(1);
        }

        [Fact]
        public void Convert1()
        {
            var f = new FieldStatus("field1", "123");
            var b = new Binder2(f);
            b.getter = () => f.Value;

            var fb = b.As<string>();

            var fb2 = fb.Convert<int>();

            fb2.Field.Value.Should().NotBe(123);

            // DEBT
            //fb2.Field.Value.Should().BeOfType<int>();

            b.Process();
            
            fb2.Field.Value.Should().Be(123);
        }
        
        [Fact]
        public void Compare1()
        {
            var f = new FieldStatus("field1", "123");
            var b = new Binder2(f);
            b.getter = () => f.Value;

            var fb = b.As<string>();

            var fb2 = fb.Convert<int>();
                
            fb2.GreaterThan(100);

            b.Process();

            f.Statuses.Should().BeEmpty();

            fb2.GreaterThan(124);

            b.Process();

            f.Statuses.Should().HaveCount(1);

            //fb2.LessThan(122);
        }
    }
}