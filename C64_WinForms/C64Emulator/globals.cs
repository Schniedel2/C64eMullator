using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64_WinForms.C64Emulator
{
    public static class BIT
    {
        public static byte B0 = 0x01;
        public static byte B1 = 0x02;
        public static byte B2 = 0x04;
        public static byte B3 = 0x08;
        public static byte B4 = 0x10;
        public static byte B5 = 0x20;
        public static byte B6 = 0x40;
        public static byte B7 = 0x80;

        public static byte NOT0 = 0xfe;
        public static byte NOT1 = 0xfd;
        public static byte NOT2 = 0xfb;
        public static byte NOT3 = 0xf7;
        public static byte NOT4 = 0xef;
        public static byte NOT5 = 0xdf;
        public static byte NOT6 = 0xbf;
        public static byte NOT7 = 0x7f;
    }

    public static class DebugOptions
    {
        public static bool RenderSpriteBorder = true;
        public static bool RenderIRQRasterLine = true;
    }

}
