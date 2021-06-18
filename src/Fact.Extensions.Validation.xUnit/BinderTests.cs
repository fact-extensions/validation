using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;


namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;

    public class BinderTests
    {
        [Fact]
        public void Test1()
        {
            var f = new FieldStatus<string>("field1", "123");
            var b = new Binder2<string>(f);

            var fb = b.As();

            var fbInt = fb.Convert((string v, out int to) =>
                int.TryParse(v, out to), "Cannot convert to integer");

            fbInt.IsTrue(v => v == 123, "Must be 123");

            b.Process();

            fbInt.IsTrue(v => v == 124, "Must be 124");

            b.Process();

            f.Statuses.Should().HaveCount(1);
        }

        [Fact]
        public void Convert1()
        {
            var f = new FieldStatus<string>("field1", "123");
            var b = new Binder2<string>(f);

            var fb = b.As();

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
            var f = new FieldStatus<string>("field1", "123");
            var b = new Binder2<string>(f);

            var fb = b.As();

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
            var f = new FieldStatus<string>("field1", "123a");
            var b = new Binder2<string>(f);

            var fb = b.As();

            var fb2 = fb.Convert<int>();

            b.Process();

            var statuses = f.Statuses.ToArray();
            statuses.Should().HaveCount(1);
            //statuses[1].Description.Should().Be("Must be less than 122");
        }

        [Fact]
        public void Emit1()
        {
            var f = new FieldStatus("field1", "123a");
            var b = new Binder2(f);
            b.getter2 = () => f.Value;
            int value = 0;
            string _value = null;

            var fb = b.As<string>();

            fb.Emit(v => _value = v);

            var fb2 = fb.Convert<int>().Emit(v => value = v);

            b.Process();

            var statuses = f.Statuses.ToArray();
            statuses.Should().HaveCount(1);
            //statuses[1].Description.Should().Be("Must be less than 122");

            _value.Should().Be((string)f.Value);
            value.Should().Be(0);
        }

        [Fact]
        public async Task AwaitedProcessor()
        {
            var f = new FieldStatus("field1", "123a");
            var b = new Binder2(f);
            b.getter2 = () => f.Value;
            var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<int>();
            var gotHere = new HashSet<int>();

            b.Processing += (field, context) =>
            {
                gotHere.Add(0);
                tcs.SetResult(0);
            };

            b.ProcessingAsync += async (field, context) =>
            {
                gotHere.Add(1);
                context.Sequential = false;
                await Task.Delay(5000, context.CancellationToken);
            };

            b.ProcessingAsync += async (field, context) =>
            {
                gotHere.Add(2);
                await Task.Delay(5000, context.CancellationToken);
            };

            b.ProcessingAsync += (field, context) =>
            {
                gotHere.Add(3);
                throw new XunitException("Should never reach here");
            };

            Task t2 = tcs.Task.ContinueWith(t => cts.CancelAfter(TimeSpan.FromSeconds(0.5)),
                cts.Token);

            var oce = await Assert.ThrowsAsync<TaskCanceledException>(async delegate
            {
                await b.Process(ct: cts.Token);
            });

            gotHere.Should().HaveCount(3);
        }

        [Fact]
        public async Task EpochConversionTest()
        {
            var ag = new AggregatedBinder(new FieldStatus("test", null));

            var fb = ag.AddField("epoch", () => long.MinValue).
                FromEpochToDateTimeOffset();

            await fb.Binder.Process();

            var statues = fb.Binder.Field.Statuses.ToArray();
            statues.Should().HaveCount(1);

            var fb2 = ag.AddField("epoch2", () => 0);
            var fb3 = fb2.AsEpoch();
            var fb4 = fb3.ToDateTimeOffset();

            await fb2.Binder.Process();

            statues = fb2.Binder.Field.Statuses.ToArray();
            statues.Should().HaveCount(0);
            fb4.InitialValue.Should().Be(DateTimeOffset.UnixEpoch);
        }
    }
}