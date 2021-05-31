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

            var fbInt = fb.Convert((string v, out int to) =>
                int.TryParse(v, out to),"Cannot convert to integer");

            fbInt.IsTrue(v => v == 123, "Must be 123");

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

            fb2.LessThan(122);

            b.Process();

            var statuses = f.Statuses.ToArray();
            statuses.Should().HaveCount(2);
            statuses[1].Description.Should().Be("Must be less than 122");
        }


        [Fact]
        public void Convert2()
        {
            var f = new FieldStatus("field1", "123a");
            var b = new Binder2(f);
            b.getter = () => f.Value;

            var fb = b.As<string>();

            var fb2 = fb.Convert<int>();

            b.Process();
            
            var statuses = f.Statuses.ToArray();
            statuses.Should().HaveCount(1);
            //statuses[1].Description.Should().Be("Must be less than 122");
        }
    }
}