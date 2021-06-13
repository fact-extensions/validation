using System;
using Xunit;

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

        async Task DoAssert1(SyntheticEntity1 se1)
        {
            var va = asserter.From(se1);

            va.Where(x => x.Password1).Required();

            await va.AssertAsync();
        }

        [Fact]
        public async Task Test1()
        {
            var se1 = new SyntheticEntity1
            {
                Password1 = "hi2u"
            };
            await DoAssert1(se1);
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
