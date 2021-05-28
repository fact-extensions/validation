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

    public class BinderManagerBase<TSource>
    {
        // 1:1 Field binders
        protected List<Item> binders = new List<Item>();

        public class Item
        {
            internal IBinder binder;
            internal event Action Initialize;
            internal TSource control;
            // DEBT: Pretty sure we can deduce this at will based on an initial vs current value
            internal bool modified;

            internal void DoInitialize() => Initialize?.Invoke();
        }

        public class Item<T> : Item
        {
            internal Tracker<T> tracked;
        }

        /// <summary>
        /// A list of all tracked original/canonical fields
        /// </summary>
        public IEnumerable<IField> Fields =>
            binders.Select(x => x.binder.Field);
    }

    public class BinderManagerBase : BinderManagerBase<Control>
    {
        public class ColorOptions
        {
            /// <summary>
            /// When a field has focus/is being input and has some kind of error status
            /// </summary>
            public Color FocusedStatus { get; set; } = Color.Pink;
            /// <summary>
            /// When a field starts out with some kind of status (such as required)
            /// </summary>
            public Color InitialStatus { get; set; } = Color.LightYellow;
            public Color UnfocusedStatus { get; set; } = Color.Red;
            public Color ClearedStatus { get; set; } = Color.White;
        }


        public class Options
        {
            public ColorOptions Color = new ColorOptions();
        }

        protected Options options = new Options();

        public IServiceProvider Services { get; }

        public BinderManagerBase(IServiceProvider services)
        {
            Services = services;
        }
    }

    public class BinderManager : BinderManagerBase
    {
        // Other binders which don't have a 1:1 field relationship
        List<IBinder> _binders = new List<IBinder>();
        List<GroupBinder> groupBinders = new List<GroupBinder>();

        Button okButton;

        public BinderManager(IServiceProvider services) : base(services)
        {
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


        public void Add(GroupBinder binder)
        {
            groupBinders.Add(binder);
        }

        // DEBT: Need to heed still-unfinished 'interactivity measure', since we loosely
        // expect group binders to take more processing time
        IEnumerable<IField> GroupEvaluate()
        {
            foreach (GroupBinder binder in groupBinders)
                binder.Evaluate(null);

            // Return a deduped list of any fields which have an associated status
            return groupBinders.SelectMany(x => x.Fields).
                Distinct().
                Where(x => x.Statuses.Any());
        }


        // EXPERIMENTAL - accounts for group binders
        // Exclude so that we don't mess with work done during TextChanged
        public void Evaluate(IField exclude = null, bool initializing = false)
        {
            GroupEvaluate();

            foreach(Item item in binders)
            {
                if (exclude == item.binder.Field) continue;

                bool hasStatus = item.binder.Field.Statuses.Any();

                // TODO: Need to account for per-field modified/not modified
                item.control.BackColor = hasStatus ?
                    (initializing || !item.modified ? 
                        options.Color.InitialStatus : options.Color.UnfocusedStatus) : 
                    options.Color.ClearedStatus;
            }
        }


        public void DoFinalize()
        {
            foreach(Item item in binders)
            {
                item.binder.DoFinalize();
            }
        }


        internal Binder<T> InternalBindText<T>(Item item, string name)
        {
            // FIX: this T object cast is bad
            var field = new FieldStatus<T>(name, (T)(object)item.control.Text);
            var binder = new Binder<T>(field, () => item.control.Text);

            // DEBT: Clumsy assignment
            item.binder = binder;

            binders.Add(item);

            return binder;
        }

        /// <summary>
        /// Occurs after interactive validation, whether it generated new status or not
        /// </summary>
        public event Action Validated;

        public Binder<T> BindText<TControl, T>(TControl control, string name)
            where TControl: Control
        {
            var item = new Item { control = control };

            Binder<T> binder = InternalBindText<T>(item, name);
            string initialText = control.Text;
            string lastText = control.Text;
            bool touched = false;
            bool modified = false;

            Color initialAlertColor = options.Color.InitialStatus;
            Color inputAlertColor = options.Color.FocusedStatus;
            Color clearColor = options.Color.ClearedStatus;

            control.TextChanged += (s, e) =>
            {
                binder.Evaluate();
                Evaluate(binder.Field);

                Validated?.Invoke();

                bool hasStatus = binder.Field.Statuses.Any();

                modified = !initialText.Equals(control.Text);
                item.modified = modified;
                touched = true;

                OnEvaluate(item, hasStatus);

                control.BackColor = hasStatus ? 
                    (modified ? inputAlertColor : initialAlertColor) : 
                    clearColor;
            };

            control.GotFocus += (s, e) =>
            {
                bool hasStatus = binder.Field.Statuses.Any();

                control.BackColor = hasStatus ? 
                    (modified ? inputAlertColor : initialAlertColor) :
                    clearColor;
            };


            control.LostFocus += (s, e) =>
            {
                bool hasStatus = binder.Field.Statuses.Any();

                control.BackColor = hasStatus ? 
                    options.Color.UnfocusedStatus : 
                    clearColor;
            };

            item.Initialize += () =>
            {
                // DEBT: Consolidate this with other code
                // Initial state
                binder.Evaluate();
                Evaluate(initializing: true);

                bool hasStatus = binder.Field.Statuses.Any();

                // NOTE: Never 'modified' here yet, just keeping like this to make code
                // consolidation easier
                control.BackColor = hasStatus ?
                    (modified ? inputAlertColor : initialAlertColor) :
                    clearColor;
            };

            return binder;
        }
    }
}
