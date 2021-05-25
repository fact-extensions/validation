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

        List<IBinder> binders = new List<IBinder>();

        Button okButton;

        public BinderManager(IServiceProvider services)
        {
            Services = services;
        }

        void EvaluateOkButton(bool hasStatus)
        {
            if (!hasStatus)
                hasStatus = binders.SelectMany(x => x.Field.Statuses).Any();

            okButton.Enabled = !hasStatus;
        }

        public void BindOkButton(Button control)
        {
            okButton = control;
        }


        public Binder<T> BindText<TControl, T>(TControl control, string name)
            where TControl: Control
        {
            var field = new FieldStatus(name, control.Text);
            var binder = new Binder<T>(field, () => control.Text);
            //var c = new Experimental.Context();

            control.TextChanged += (s, e) =>
            {
                binder.Evaluate();

                bool hasStatus = binder.Field.Statuses.Any();

                EvaluateOkButton(hasStatus);

                control.BackColor = hasStatus ? Color.Pink : Color.White;
            };

            control.GotFocus += (s, e) =>
            {
                bool hasStatus = binder.Field.Statuses.Any();

                // TODO: Keep track of 'touched' and 'modified' and if so change to pink
                // rather than yellow
                control.BackColor = hasStatus ? Color.Yellow : Color.White;
            };


            control.LostFocus += (s, e) =>
            {
                bool hasStatus = binder.Field.Statuses.Any();

                control.BackColor = hasStatus ? Color.Red : Color.White;
            };

            binders.Add(binder);
            return binder;
        }
    }
}
