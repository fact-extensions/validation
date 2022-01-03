using Fact.Extensions.Validation.Experimental;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    public class LossyQueueTests
    {
        [Fact]
        public async Task LossyQueueTest()
        {
            var lq = new LossyQueue();
            var tcs = new TaskCompletionSource<int>();
            var tcs2 = new TaskCompletionSource<int>();
            int val = 0;
            int dropCount = 0;

            lq.Dropped += () => ++dropCount;

            lq.Add(async () =>
            {
                val = await tcs.Task;
            });

            // this middle one should get lost, since the first one is waiting for tcs
            lq.Add(() =>
            {
                val += 1;
                tcs2.SetResult(val);
                return new ValueTask();
            });

            // this middle one should get lost, since the first one is waiting for tcs
            lq.Add(() =>
            {
                val += 2;
                tcs2.SetResult(val);
                return new ValueTask();
            });

            // this one will run, since it's the last entry before tcs.SetResult
            lq.Add(() =>
            {
                val += 100;
                tcs2.SetResult(val);
                return new ValueTask();
            });

            tcs.SetResult(5);

            var val3 = await tcs2.Task;

            val3.Should().Be(105);
            dropCount.Should().Be(2);
        }
    }
}
