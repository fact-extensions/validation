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
            var b = new Experimental.Binder("field1");

            b.Validate += f =>
            {
                f.Error("You didn't do it right");
            };
        }


        [Fact]
        public void ConfirmPasswordTest()
        {
            var entity = new Experimental.EntityBinder();
            var pass1 = entity.Add("pass1");
            var pass2 = entity.Add("pass2");
            var validator = new Action<FieldStatus>(f =>
            {
                var pass1str = (string)pass1.Value;
                var pass2str = (string)pass2.Value;

                if (!object.Equals(pass2str, pass1str))
                {
                    string otherName = f.Name == "pass1" ? "pass2" : "pass1";

                    Experimental.Binder binder = entity[f.Name];
                    Experimental.Binder otherBinder = entity[otherName];
                    f.Error("Password does not match");
                    // DEBT: If field not yet present, that means it hasn't been input yet.
                    // Don't bother adding to it then.  However, really, we want a kind of
                    // "hollow" field ready to go
                    otherBinder.Field?.AddIfNotPresent(binder);
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
    }
}
