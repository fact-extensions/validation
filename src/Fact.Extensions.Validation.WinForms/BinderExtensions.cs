using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fact.Extensions.Validation
{
    public static class BinderExtensions
    {
        public static void Bind(this Experimental.Binder<string> binder, Control control)
        {
            control.TextChanged += (s, e) =>
            {
                var c = new Experimental.Context();
                binder.Evaluate(control.Text);

                bool hasStatus = binder.Field.Statuses.Any();

                control.BackColor = hasStatus ? Color.Red : Color.White;
            };
        }
    }
}
