using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fact.Extensions.Validation
{
    public static class ControlExtensions
    {
        public static Task<object> BeginInvokeAsync(this Control control, Action action) =>
            Task.Factory.FromAsync(control.BeginInvoke(action), a => control.EndInvoke(a));

        public static object Invoke(this Control control, Action action) =>
            control.Invoke(action);
    }
}
