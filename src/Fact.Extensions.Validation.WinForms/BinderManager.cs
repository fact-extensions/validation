using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fact.Extensions.Validation.WinForms
{
    public class BinderManagerBase
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
    }
}
