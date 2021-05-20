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
                f.Error("You didn't do it right");
            };
        }


        [Fact]
        public void ConfirmPasswordTest()
        {
            var pass1 = new Experimental.Binder("pass1");
            var pass2 = new Experimental.Binder("pass2");
            var validator = new Action<FieldStatus>(f =>
            {
                var pass1str = (string)pass1.Value;
                var pass2str = (string)pass2.Value;

                if (!object.Equals(pass2str, pass1str))
                {
                    f.Error("Password does not match");
                }
            });

            pass1.Validate += validator;
            pass2.Validate += validator;

            pass1.Evaluate("password");
            pass2.Evaluate("nonmatched");
            pass2.Evaluate("password");
        }
    }
}
