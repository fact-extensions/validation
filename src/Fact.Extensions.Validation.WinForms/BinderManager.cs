using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fact.Extensions.Validation.WinForms
{
    using Fact.Extensions.Validation.Experimental;

    public class BinderManager
    {
        public IServiceProvider Services { get; }

        internal class Item
        {
            internal IBinder binder;
            internal event Action Initialize;
            internal Control control;

            internal void DoInitialize() => Initialize?.Invoke();
        }

        // 1:1 Field binders
        List<Item> binders = new List<Item>();

        // Other binders which don't have a 1:1 field relationship
        List<IBinder> _binders = new List<IBinder>();

        Button okButton;

        public BinderManager(IServiceProvider services)
        {
            Services = services;
        }

        void EvaluateOkButton(bool hasStatus)
        {
            if (!hasStatus)
                hasStatus = binders.SelectMany(x => x.binder.Field.Statuses).Any();

            okButton.Enabled = !hasStatus;
        }


        void OnEvaluate(Item item, bool hasStatus)
        {
            EvaluateOkButton(hasStatus);
            if(tooltip != null)
            {
                if(hasStatus)
                {
                    tooltip.SetToolTip(item.control, "Has Status");
                }
            }
        }

        public void BindOkButton(Button control)
        {
            okButton = control;
        }


        ToolTip tooltip;

        public void SetupTooltip(Form form)
        {
            // Does not work, since Container is null
            //tooltip = new ToolTip(form.Container);
        }


        /// <summary>
        /// Call after all other binds and validation setup, but before moving on to other non validation code
        /// </summary>
        public void Prep()
        {
            foreach (Item item in binders)
                item.DoInitialize();

            // DEBT: Force-feeding false not a long term solution here
            EvaluateOkButton(false);
        }


        public Binder<T> BindText<TControl, T>(TControl control, string name)
            where TControl: Control
        {
            var field = new FieldStatus(name, control.Text);
            var binder = new Binder<T>(field, () => control.Text);
            //var c = new Experimental.Context();
            string initialText = control.Text;
            string lastText = control.Text;
            bool touched = false;
            bool modified = false;

            Color modifiedAlertColor = Color.LightYellow;
            Color inputAlertColor = Color.Pink;

            var item = new Item { binder = binder, control = control };

            control.TextChanged += (s, e) =>
            {
                binder.Evaluate();

                bool hasStatus = binder.Field.Statuses.Any();

                modified = !initialText.Equals(control.Text);
                touched = true;

                OnEvaluate(item, hasStatus);

                control.BackColor = hasStatus ? 
                    (modified ? Color.Pink : modifiedAlertColor) : 
                    Color.White;
            };

            control.GotFocus += (s, e) =>
            {
                bool hasStatus = binder.Field.Statuses.Any();

                control.BackColor = hasStatus ? 
                    (modified ? Color.Pink : modifiedAlertColor) : 
                    Color.White;
            };


            control.LostFocus += (s, e) =>
            {
                bool hasStatus = binder.Field.Statuses.Any();

                control.BackColor = hasStatus ? Color.Red : Color.White;
            };

            item.Initialize += () =>
            {
                // DEBT: Consolidate this with other code
                // Initial state
                binder.Evaluate();

                bool hasStatus = binder.Field.Statuses.Any();

                // NOTE: Never 'modified' here yet, just keeping like this to make code
                // consolidation easier
                control.BackColor = hasStatus ?
                    (modified ? Color.Pink : modifiedAlertColor) :
                    Color.White;
            };

            binders.Add(item);
            return binder;
        }
    }
}
