using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace C64_Win
{
	public partial class Form1 : Form
	{
        C64Win myC64 = new C64Win();
        public float FramesPerTick = 1;
        float FramesCounter = 0;

        public Form1()
		{
			InitializeComponent();
			this.KeyPreview = true;
			timer1.Interval = 19;
			timer1.Start();

            this.pictureBox1.Image = myC64.GetScreen();
        }

		private void timer1_Tick(object sender, EventArgs e)
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

            timer1.Interval = 16;
            this.pictureBox1.Invalidate();
        }

        private void Form1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {

        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            myC64.GetKeyboard().ResetKeyState();
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            myC64.GetKeyboard().PressKey(e.KeyCode.ToString());
        }
    }
}
