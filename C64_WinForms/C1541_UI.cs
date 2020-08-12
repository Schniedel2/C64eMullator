using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace C64_WinForms.C64Emulator
{
    public partial class C1541_UI : Form
    {
        public C1541 myC1541;

        public C1541_UI()
        {            
            InitializeComponent();
        }

        private void C1541_UI_Load(object sender, EventArgs e)
        {

        }
    }
}
