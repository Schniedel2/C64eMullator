using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using C64_WinForms.C64Emulator;

using System.IO;

namespace C64_WinForms
{
    public partial class Form1 : Form
    {
        public C64 myC64 = new C64();
        public bool ExecEnabled = true;
        public float FramesPerTick = 1;
        float FramesCounter = 0;
         
        public Form1()
        {
            DatasetteController dc = new DatasetteController();
            dc.myDatasette = myC64.Datasette;
            dc.Show(this);

            InitializeComponent();
            this.KeyPreview = true;

            this.pictureBox1.Image = myC64.VIC.GetScreen();

            timer1.Interval = 19;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (ExecEnabled)
            {
                double sumMS = 0;
                double ms = 0;

                FramesCounter += FramesPerTick;

                while (FramesCounter > 0)
                {
                    FramesCounter -= 1;
                    DateTime d0 = DateTime.Now;
                    myC64.ProcessFrames(1);

                    TimeSpan dur = DateTime.Now - d0;
                    ms = dur.TotalMilliseconds;
                    sumMS += ms;
                }                
            }

            timer1.Interval = 16;
            this.pictureBox1.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {            
            base.OnPaint(e);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            int i = e.KeyValue;
            Keys k = (Keys)e.KeyValue;

            myC64.OnKeyUp(k);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            int i = e.KeyValue;
            Keys k = (Keys)e.KeyValue;

            myC64.OnKeyDown(k);
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void Form1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {

        }

        private void saveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FileStream s = System.IO.File.Open("c:\\c64files\\state_000.dat", FileMode.Create);
            myC64.StreamTo(s);
            s.Close();
        }

        private void restoreStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = "dat";
            dlg.InitialDirectory = "c:\\C64Files";
            dlg.Filter = "*.dat|State";
            dlg.CheckFileExists = true;
            dlg.FilterIndex = 0;
            dlg.FileName = "*.dat";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                FileStream s = System.IO.File.Open(dlg.FileName, FileMode.Open);

                myC64.StreamFrom(s);
                s.Close();
            }
            /*
            FileStream s = System.IO.File.Open("c:\\c64files\\state_000.dat", FileMode.Open);

            myC64.StreamFrom(s);
            s.Close();
            */
        }

        private void resetHardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myC64.Reset();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            FramesPerTick = 1;
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            FramesPerTick = 2;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            FramesPerTick = 4;
        }

        private void maxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FramesPerTick = 0;
        }

        private void swapJoysticksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myC64.SwapJoysticks();
        }

        private void toggle1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myC64.SID.ToggleChannel(1);
        }

        private void toggle2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myC64.SID.ToggleChannel(2);
        }

        private void toggle3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myC64.SID.ToggleChannel(3);
        }

        private void loadPRGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = "prg";
            dlg.InitialDirectory = "c:\\C64Files\\PRG";
            if (dlg.ShowDialog() == DialogResult.OK)
                myC64.LoadPRG(dlg.FileName);
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            FramesPerTick = 0.1f;
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            FramesPerTick = 0.01f;
        }
    }
}
