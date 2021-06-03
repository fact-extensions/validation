using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    using System.Threading.Tasks;

    public class FieldStatusTests : IClassFixture<Fixture>
    {
        readonly IServiceProvider services;

        public FieldStatusTests(Fixture fixture)
        {
            services = fixture.Services;
        }

        [Fact]
        public void FieldStatusCollectionTest()
        {
            var fsc = new Experimental.FieldStatusCollection();
        }


        [Fact]
        public void BinderTest()
        {
            var f = new FieldStatus("field1", null);
            var b = new Binder<int>(f);

            b.Validate += f =>
            {
                if(f.Value == 1)
                    f.Error("You didn't do it right");
            };

            b.Evaluate(0);
            b.Evaluate(1);

            f.Statuses.Should().HaveCount(1).And.
                Subject.First().Level.Should().Be(Status.Code.Error);
        }


        [Fact]
        public void ConfirmPasswordTest1()
        {
            var entity = new GroupBinder();
            var f1 = new FieldStatus("pass1", null);
            var f2 = new FieldStatus("pass2", null);
            var pass1 = new Binder<string>(f1);
            entity.Add(pass1);
            var pass2 = new Binder<string>(f2);
            entity.Add(pass2);
            var validator = new Action<IField>(f =>
            {
                var pass1str = (string)pass1.Field.Value;
                var pass2str = (string)pass2.Field.Value;

                if (!object.Equals(pass2str, pass1str))
                {
                    string otherName = f.Name == "pass1" ? "pass2" : "pass1";

                    IField binder = entity[f.Name];
                    IField otherBinder = entity[otherName];
                    f.Error("Password does not match");
                    // DEBT: If field not yet present, that means it hasn't been input yet.
                    // Don't bother adding to it then.  However, really, we want a kind of
                    // "hollow" field ready to go
                    //otherBinder.Field?.AddIfNotPresent(binder);
                    binder.Add(Status.Code.Conflict, "Test conflict");
                }
            });

            pass1.Validate += validator;
            pass2.Validate += validator;

            pass1.Evaluate("password");

            var statuses1 = pass1.Field.Statuses.ToArray();
            var statuses2 = pass2.Field?.Statuses.ToArray();

            pass2.Evaluate("nonmatched");
            pass2.Evaluate("password");
        }

        [Fact]
        public void ConfirmPasswordTest2()
        {
            var entity = new GroupBinder();
            var f1 = new FieldStatus("pass1", null);
            var f2 = new FieldStatus("pass2", null);
            var pass1 = new Binder<string>(f1);
            entity.Add(pass1);
            var pass2 = new Binder<string>(f2);
            entity.Add(pass2);
            // Looks like we may not need context anymore...
            var inputContext = new InputContext()
            {
                FieldName = "pass1"
            };
            pass1.getter = () => "password1";
            pass2.getter = () => "password2";

            entity.Validate += (b, context) =>
            {
                var _pass1 = b["pass1"];
                var _pass2 = b["pass2"];

                if (!object.Equals(_pass1.Value, _pass2.Value))
                {
                    _pass1.Error("mismatch");
                    _pass2.Error("mismatch");
                    //b.Error("pass1", "mismatch");
                }

                return new ValueTask();
            };
            entity.Evaluate(inputContext);

            var _pass1 = (GroupBinder._Item)entity["pass1"];
            f1.InternalStatuses.Should().BeEmpty();
            _pass1.statuses.Should().HaveCount(1);
            f2.InternalStatuses.Should().BeEmpty();
            var _pass2 = (GroupBinder._Item)entity["pass2"];
            _pass2.statuses.Should().HaveCount(1);
        }


        [Fact]
        public void ConfirmPasswordTest3()
        {
            var entity = new GroupBinder();
            var f1 = new FieldStatus<string>("pass1", "password1");
            var f2 = new FieldStatus<string>("pass2", "password2");
            BinderBase pass1 = entity.Add(f1);
            BinderBase pass2 = entity.Add(f2);

            entity.Validate += (b, context) =>
            {
                var _pass1 = b["pass1"];
                var _pass2 = b["pass2"];

                if (!object.Equals(_pass1.Value, _pass2.Value))
                {
                    _pass1.Error("mismatch");
                    _pass2.Error("mismatch");
                }

                return new ValueTask();
            };
            entity.Evaluate(null);

            var _pass1 = (GroupBinder._Item)entity["pass1"];
            f1.Statuses.Should().HaveCount(1);
            f1.InternalStatuses.Should().BeEmpty();
            _pass1.statuses.Should().HaveCount(1);
            f2.Statuses.Should().HaveCount(1);
            f2.InternalStatuses.Should().BeEmpty();
            var _pass2 = (GroupBinder._Item)entity["pass2"];
            _pass2.statuses.Should().HaveCount(1);
        }


        //[Fact]
        // FIX: Doesn't work yet because underlying shimmed group fields (_Item) isn't strongly
        // typed yet
        public void ConfirmPasswordTest4()
        {
            var binder = new GroupBinder();
            var f1 = new FieldStatus<string>("pass1", "password1");
            var f2 = new FieldStatus<string>("pass2", "password2");
            BinderBase pass1 = binder.Add(f1);
            BinderBase pass2 = binder.Add(f2);

            binder.DoValidate<string, string>("pass1", "pass2", (c, _pass1, _pass2) =>
            {
                if (!object.Equals(_pass1.Value, _pass2.Value))
                {
                    _pass1.Error("mismatch");
                    _pass2.Error("mismatch");
                }
            });
            binder.Evaluate(null);

            var _pass1 = (GroupBinder._Item)binder["pass1"];
            f1.Statuses.Should().HaveCount(1);
            f1.InternalStatuses.Should().BeEmpty();
            _pass1.statuses.Should().HaveCount(1);
            f2.Statuses.Should().HaveCount(1);
            f2.InternalStatuses.Should().BeEmpty();
            var _pass2 = (GroupBinder._Item)binder["pass2"];
            _pass2.statuses.Should().HaveCount(1);
        }

        [Fact]
        public void ConversionTest()
        {
            var f = new FieldStatus<string>("field1", null);
            var b = new Binder<string, int>(f);
            string val = "123";
            object output;
            b.getter = () => val;
            b.Finalize += v => output = v;
            

            b.Convert += (f, cea) =>
            {
                if (int.TryParse(f.Value, out int result))
                    cea.Value = result;
                else
                    f.Error("Unable to convert to integer");
            };

            b.Evaluate("123");

            f.Statuses.Should().BeEmpty();

            b.Evaluate("xyz");

            f.Statuses.Should().HaveCount(1);
        }


        [Fact]
        public void FluentTest1()
        {
            var f = new FieldStatus<string>("field1", null);
            var b = new Binder<string>(f);

            b.Assert().IsTrue(x => x == "hi", "Must be 'hi'");

            b.Evaluate("howdy");

            f.Statuses.Should().HaveCount(1);
        }


        [Fact]
        public void FluentTest2()
        {
            var f = new FieldStatus<int>("field1", default(int));
            var b = new Binder<int>(f);

            b.Assert().
                LessThan(10).
                GreaterThan(6);

            b.Evaluate(5);

            f.Statuses.Should().HaveCount(1);
        }


        [Fact]
        public void FluentTest3()
        {
            var f = new FieldStatus<string>("field1", null);
            var b = new Binder<string>(f);

            b.Assert().Required();

            // DEBT: string cast needed so compiler can figure out which extension to use
            // clearly not ideal
            b.Evaluate((string)null);

            f.Statuses.Should().HaveCount(1);
        }


        [Fact]
        public void ConversionTest2()
        {
            var field = new FieldStatus<string>("field1", "123");
            var ctx = new ConvertEventArgs<int>();

            // Supposed to be for convenience, but just makes things more complicated
            field.TryConvert((v, ctx) =>
            {
                if (int.TryParse(v, out var temp))
                {
                    ctx.Value = temp;
                    return true;
                }

                return false;
            }, ctx, "Cannot convert to integer");
        }
        
        [Fact]
        public void ConversionTest3()
        {
            var f = new FieldStatus<string>("field1", null);
            var b = new Binder<string, int>(f);
            string val = "123";
            object output;
            b.getter = () => val;
            b.Finalize += v => output = v;
            

            b.Convert += (f, cea) =>
            {
                if (int.TryParse(f.Value, out int result))
                    cea.Value = result;
                else
                    f.Error("Unable to convert to integer");
            };

            b.Evaluate("123");

            f.Statuses.Should().BeEmpty();

            b.Evaluate("xyz");

            f.Statuses.Should().HaveCount(1);
        }
    }
}