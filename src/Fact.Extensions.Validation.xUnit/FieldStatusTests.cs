using FluentAssertions;
using System;
using System.Linq;
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
            var f = new FieldStatus("field1", null);
            var b = new Experimental.Binder(f);

            b.Validate += f =>
            {
                if((int?)f.Value == 1)
                    f.Error("You didn't do it right");
            };

            b.Evaluate(0);
            b.Evaluate(1);

            f.Statuses.Should().HaveCount(1).And.
                Subject.First().Level.Should().Be(FieldStatus.Code.Error);
        }


        [Fact]
        public void ConfirmPasswordTest1()
        {
            var entity = new Experimental.GroupBinder();
            var f1 = new FieldStatus("pass1", null);
            var f2 = new FieldStatus("pass2", null);
            var pass1 = entity.Add(f1);
            var pass2 = entity.Add(f2);
            var validator = new Action<IField>(f =>
            {
                var pass1str = (string)pass1.Value;
                var pass2str = (string)pass2.Value;

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
                    binder.Add(FieldStatus.Code.Conflict, "Test conflict");
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
            var entity = new Experimental.GroupBinder();
            var f1 = new FieldStatus("pass1", null);
            var f2 = new FieldStatus("pass2", null);
            var pass1 = entity.Add(f1);
            var pass2 = entity.Add(f2);
            var inputContext = new Experimental.InputContext()
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
            };
            entity.Evaluate(null, inputContext);

            var _pass1 = (Experimental.GroupBinder._Item)entity["pass1"];
            f1.InternalStatuses.Should().BeEmpty();
            _pass1.statuses.Should().HaveCount(1);
            f2.InternalStatuses.Should().BeEmpty();
            var _pass2 = (Experimental.GroupBinder._Item)entity["pass2"];
            _pass2.statuses.Should().HaveCount(1);
        }
    }
}