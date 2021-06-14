using System;
using Xunit;

using FluentAssertions;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;

    public class VAssertTests : IClassFixture<Fixture>
    {
        readonly IServiceProvider services;
        readonly Asserter asserter = new Asserter();

        public VAssertTests(Fixture fixture)
        {
            this.services = fixture.Services;
        }

        async Task DoAssert1(SyntheticEntity1 se1, int param1)
        {
            var va = asserter.From(se1);

            va.Where(x => x.Password1).StartsWith("hello");

            // FIX: Problematic
            //va.Where(() => param1).GreaterThan(5);

            await va.AssertAsync();
        }

        [Fact]
        public async Task Test1()
        {
            var se1 = new SyntheticEntity1
            {
                Password1 = "hi2u"
            };

            var ae = await Assert.ThrowsAsync<AssertException>(() => DoAssert1(se1, 4));

            var fields = ae.Fields.ToArray();
            //fields.Should().HaveCount(2);
        }
    }

    public class Asserter
    {

    }


    public static class AsserterExtensions
    {
        public static VAssert<T> From<T>(this Asserter asserter, T entity)
        {
            var eb = new EntityBinder<T>();
            //b.BindInput(typeof(T), true, eb);
            return new VAssert<T>(entity);
        }


        public static IFluentBinder<T> Where<T>(this VAssert assert, Expression<Func<T>> fieldLambda)
        {
            var name = fieldLambda.Name;
            //System.Linq.Expressions.Fi
            //var body = fieldLambda.Body as FieldExpression;
            var member = fieldLambda.Body as MemberExpression;
            var memberInfo = member.Member;
            var f = memberInfo as FieldInfo;
            //object value = f.GetValue(null);

            //var ex2 = Expression.Field(body, fieldLambda.Name);
            //switch(body.me)

            return null;
        }


        public static IFluentBinder<T> Where<TEntity, T>(this VAssert<TEntity> assert,  
            Expression<Func<TEntity, T>> propertyLambda)
        {
            var name = propertyLambda.Name;
            var member = propertyLambda.Body as MemberExpression;

            switch(member.Member)
            {
                case PropertyInfo propertyInfo:
                    var p = assert.Binder.Binders.Single(x => x.Property == propertyInfo);
                    return ((PropertyBinderProvider<T>)p).FluentBinder;

                case FieldInfo fieldInfo:
                    // FIX: May need EntityBinder's value here
                    return assert.AggregatedBinder.AddField(fieldInfo.Name, () =>
                    {
                        object v = fieldInfo.GetValue(null);
                        return (T)v;
                    });
                    //var fv = ;
                    //break;

                default:
                    return null;
            }
        }
    }
}
