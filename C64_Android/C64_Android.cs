using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using System.Drawing;
using System.Drawing.Imaging;
using C64Emulator;
using C64Emulator.Devices;
using Android.Support.V7.App;

namespace C64_Android
{
	class C64Android : C64
	{
		byte[] KernelROM;
		byte[] BasicROM;
		byte[] CharEnROM;
		byte[] C1541ROM;
		Keyboard myKeyboard = new Keyboard();
		Bitmap Screen;
		Color[] Palette;

		public C64Android(AppCompatActivity _activity)
		{
			//	load ROMs
			KernelROM = StreamHelpers.LoadBytes(_activity.Assets.Open("KERNAL.ROM"));
			BasicROM = StreamHelpers.LoadBytes(_activity.Assets.Open("BASIC.ROM"));
			CharEnROM = StreamHelpers.LoadBytes(_activity.Assets.Open("CHAR.ROM"));
			C1541ROM = StreamHelpers.LoadBytes(_activity.Assets.Open("C1541.ROM"));

			Init(BasicROM, KernelROM, CharEnROM, C1541ROM, myKeyboard);

			//	init screen
			Palette = new Color[16];
			Palette[0] = Color.FromArgb(0xff, 0x00, 0x00, 0x00);
			Palette[1] = Color.FromArgb(0xff, 0xff, 0xff, 0xff);
			Palette[2] = Color.FromArgb(0xff, 0x68, 0x37, 0x2b);
			Palette[3] = Color.FromArgb(0xff, 0x70, 0xa4, 0xb2);
			Palette[4] = Color.FromArgb(0xff, 0x6F, 0x3D, 0x86);
			Palette[5] = Color.FromArgb(0xff, 0x58, 0x8D, 0x43);
			Palette[6] = Color.FromArgb(0xff, 0x35, 0x28, 0x79);
			Palette[7] = Color.FromArgb(0xff, 0xB8, 0xC7, 0x6F);
			Palette[8] = Color.FromArgb(0xff, 0x6F, 0x4F, 0x25);
			Palette[9] = Color.FromArgb(0xff, 0x43, 0x39, 0x00);
			Palette[10] = Color.FromArgb(0xff, 0x9A, 0x67, 0x59);
			Palette[11] = Color.FromArgb(0xff, 0x44, 0x44, 0x44);
			Palette[12] = Color.FromArgb(0xff, 0x6C, 0x6C, 0x6C);
			Palette[13] = Color.FromArgb(0xff, 0x9A, 0xD2, 0x84);
			Palette[14] = Color.FromArgb(0xff, 0x6C, 0x5E, 0xB5);
			Palette[15] = Color.FromArgb(0xff, 0x95, 0x95, 0x95);

			Screen = new Bitmap(VIC.Settings.PixelsPerScanLine, VIC.Settings.NumScanLines, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

			if (Screen.Palette.Flags > 0)
			{
				ColorPalette pal = Screen.Palette;
				for (int i = 0; i < 16; i++)
					pal.Entries[i] = Palette[i];
				Screen.Palette = pal;
			}

		}

		public Bitmap GetScreen()
		{
			return Screen;
		}

		protected override void OnVerticalRetrace()
		{
			base.OnVerticalRetrace();
		}

		protected override void OnAfterRasterline(int _rasterLine, byte[] _pixels)
		{
			CopyByteLineToBitmap(_pixels, Screen, _rasterLine);
			base.OnAfterRasterline(_rasterLine, _pixels);
		}

		void CopyByteLineToBitmap(byte[] _pixels, Bitmap _screen, int _y)
		{
			BitmapData data = _screen.LockBits(
				new Rectangle(0, _y, _screen.Width, 1),
				ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

			System.Runtime.InteropServices.Marshal.Copy(_pixels, 0, data.Scan0, _screen.Width);
			_screen.UnlockBits(data);
		}

	}
}
