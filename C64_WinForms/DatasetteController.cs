using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace C64_WinForms
{
    public partial class DatasetteController : Form
    {
        public C64Emulator.C64Datasette myDatasette = null;

        public DatasetteController()
        {
            InitializeComponent();

            timer1.Interval = 1000;
            timer1.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            myDatasette.PressPlay();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            myDatasette.PressRecord();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            myDatasette.PressStop();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            myDatasette.PressFastRewind();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            myDatasette.PressFastForward();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.textBox1.Text = string.Format("{0} / {1}", myDatasette.GetTapeCounter(), myDatasette.GetTapeMaxCounter());

        }

        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = "tap";
            dlg.InitialDirectory = "c:\\C64Files";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                myDatasette.PressStop();
                myDatasette.InsertTape(dlg.FileName);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            myDatasette.Eject();
        }
    }
}
