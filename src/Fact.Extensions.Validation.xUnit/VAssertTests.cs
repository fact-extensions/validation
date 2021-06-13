using System;
using Xunit;

namespace Fact.Extensions.Validation.xUnit
{
    using Experimental;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class VAssertTests : IClassFixture<Fixture>
    {
        readonly IServiceProvider services;
        readonly Asserter asserter = new Asserter();

        public VAssertTests(Fixture fixture)
        {
            this.services = fixture.Services;
        }

        void DoAssert1(SyntheticEntity1 se1)
        {
            var va = asserter.From(se1);

            va.Where(x => x.Password1).Required();
        }

        [Fact]
        public void Test1()
        {

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
            return new VAssert<T>(eb);
        }


        public static IFluentBinder<T> Where<TEntity, T>(this VAssert<TEntity> assert,  
            Expression<Func<TEntity, T>> propertyLambda)
        {
            var name = propertyLambda.Name;
            var member = propertyLambda.Body as MemberExpression;
            var properInfo = member.Member as PropertyInfo;

            var p = assert.Binder.Binders.Single(x => x.Property == properInfo);
            return ((PropertyBinderProvider<T>)p).FluentBinder;

        }
    }
}
