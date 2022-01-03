using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    using FluentAssertions;
    using System.Threading.Tasks;

    public class ExperimentalTests
    {
        [Fact]
        public void Test1()
        {
            var mp = new ModuleProvider<Optional<string>, Synthetic.User, FluentBinder<string>>(
                new Optional<string>(),
                new Synthetic.User(),
                new FluentBinder<string>("test1", () => "test1val"));

            string assigner;

            mp.Test1("value");
            mp.Get(out assigner);
        }




        [Fact]
        public async Task InputProcessorTest()
        {
            var inputProcessor = new InputProcessor();
            object result;
            string initialValue = "hi2u";
            string item2value = "hello world";

            var item1 = inputProcessor.Add((item, v) => initialValue);
            var item2 = inputProcessor.Add((item, v) =>
            {
                if (item.HasChanged)
                {
                    return "TBD";
                }
                else if (item.Previous.HasChanged)
                {
                    return "TBD";
                }
                else
                    return item.LastValue;
            });

            result = await inputProcessor.Process(null);
        }
    }
}
