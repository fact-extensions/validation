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
        public async Task ConfirmPasswordTest1()
        {
            // NOTE: Sort of a broken test, since GroupBinder wants to run its own Validate
            var entity = new GroupBinder();
            var f1 = new FieldStatus<string>("pass1", null);
            var f2 = new FieldStatus<string>("pass2", null);
            var pass1 = new Binder2<string>(f1, () => f1.Value);
            entity.Add(pass1);
            var pass2 = new Binder2<string>(f2, () => f2.Value);
            entity.Add(pass2);
            var validator = new ProcessingDelegate((f, c) =>
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

            pass1.Processing += validator;
            pass2.Processing += validator;

            f1.Value = "password";
            await pass1.Process();
            //pass1.Evaluate("password");

            var statuses1 = pass1.Field.Statuses.ToArray();
            var statuses2 = pass2.Field?.Statuses.ToArray();

            f2.Value = "nonmatched";
            await pass2.Process();
            f2.Value = "password";
            await pass2.Process();
            //pass2.Evaluate("nonmatched");
            //pass2.Evaluate("password");
        }

        [Fact]
        public void ConfirmPasswordTest2()
        {
            var entity = new GroupBinder();
            var f1 = new FieldStatus<string>("pass1", null);
            var f2 = new FieldStatus<string>("pass2", null);
            var pass1 = new Binder2<string>(f1, () => f1.Value);
            entity.Add(pass1);
            var pass2 = new Binder2<string>(f2, () => f2.Value);
            entity.Add(pass2);
            // Looks like we may not need context anymore...
            var inputContext = new InputContext()
            {
                FieldName = "pass1"
            };
            f1.Value = "password1";
            f2.Value = "password2";

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
            IBinderBase pass1 = entity.Add(f1);
            IBinderBase pass2 = entity.Add(f2);

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

            var _pass1 = (ShimFieldBase)entity["pass1"];
            f1.Statuses.Should().HaveCount(1);
            f1.InternalStatuses.Should().BeEmpty();
            _pass1.statuses.Should().HaveCount(1);
            f2.Statuses.Should().HaveCount(1);
            f2.InternalStatuses.Should().BeEmpty();
            var _pass2 = (ShimFieldBase)entity["pass2"];
            _pass2.statuses.Should().HaveCount(1);
        }


        [Fact]
        // FIX: Doesn't work yet because underlying shimmed group fields (_Item) isn't strongly
        // typed yet
        public void ConfirmPasswordTest4()
        {
            var binder = new GroupBinder();
            var f1 = new FieldStatus<string>("pass1", "password1");
            var f2 = new FieldStatus<string>("pass2", "password2");
            IBinderBase pass1 = binder.Add(f1);
            IBinderBase pass2 = binder.Add(f2);

            binder.DoValidate<string, string>("pass1", "pass2", (c, _pass1, _pass2) =>
            {
                if (!object.Equals(_pass1.Value, _pass2.Value))
                {
                    _pass1.Error("mismatch");
                    _pass2.Error("mismatch");
                }
            });
            binder.Evaluate(null);

            var _pass1 = (ShimFieldBase)binder["pass1"];
            f1.Statuses.Should().HaveCount(1);
            f1.InternalStatuses.Should().BeEmpty();
            _pass1.statuses.Should().HaveCount(1);
            f2.Statuses.Should().HaveCount(1);
            f2.InternalStatuses.Should().BeEmpty();
            var _pass2 = (ShimFieldBase)binder["pass2"];
            _pass2.statuses.Should().HaveCount(1);
        }
    }
}