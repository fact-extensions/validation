using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{


    public abstract class ValidationAttribute : Attribute
    {
        public virtual void Configure<T>(IFluentBinder<T> fb)
        {

        }

        public abstract void Validate<T>(IField<T> field, IFieldContext context);
    }

    public class RequiredAttribute : ValidationAttribute
    {
        public override void Validate<T>(IField<T> field, IFieldContext context)
        {
            // DEBT: Need a much more robust "required" assessor than merely checking null
            // thing is, we'll likely need an IServiceProvider with a factory to generate
            // checkers - or perhaps lift it out of context somehow
            if (field.Value == null)
            {
                field.Error(FieldStatus.ComparisonCode.IsNull, null, "Must not be null");
                context.Abort = true;
            }
            else if (field.Value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                field.Error(FieldStatus.ComparisonCode.IsNull, stringValue, "Must not be empty");
                context.Abort = true;
            }
        }
    }

    public interface IGroupValidatorProvider
    {
        IFluentBinder FirstFluentBinderInGroup { get; }
    }


    public class GroupValidatorProvider : IGroupValidatorProvider
    {
        public IFluentBinder FirstFluentBinderInGroup => throw new NotImplementedException();
    }


    /// <summary>
    /// FIX: Needs better name
    /// </summary>
    public interface IGroupValidatorHelper
    {
        string GroupName { get; }
        void ConfigureGroup(IGroupValidatorProvider groupValidatorProvider);
    }


    // NOTE: Not doable yet since we don't have easy access to the entity which this is attached.
    // However, knowing that these attributes MUST be attached to an entity indicates we can and possibly
    // should augment Configure with EntityProvider or similar or perhaps a special IGroup 
    public class MatchAttribute : ValidationAttribute, IGroupValidatorHelper
    {
        IGroupValidatorProvider groupValidatorProvider;

        public string GroupName { get; }

        public void ConfigureGroup(IGroupValidatorProvider groupValidatorProvider)
        {
            this.groupValidatorProvider = groupValidatorProvider;
        }

        public MatchAttribute(string group)
        {
            GroupName = group;
        }

        public override void Validate<T>(IField<T> field, IFieldContext context)
        {
            throw new NotImplementedException();
        }
    }
}