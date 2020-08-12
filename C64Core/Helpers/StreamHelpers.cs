using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace C64Emulator
{
    public static class StreamHelpers
    {
        public static void WriteInt16(Stream _stream, int _val)
        {
            byte b0 = (byte)(_val & 0xff);
            byte b1 = (byte)((_val >> 8) & 0xff);

            _stream.WriteByte(b0);
            _stream.WriteByte(b1);
        }

        public static int ReadInt16(Stream _stream)
        {
            byte b0 = (byte)_stream.ReadByte();
            byte b1 = (byte)_stream.ReadByte();

            return (b0 | (b1 << 8));
        }

        public static void WriteBool(Stream _stream, bool _val)
        {
            byte b0 = 0;
            if (_val)
                b0 = 1;

            _stream.WriteByte(b0);
        }

        public static bool ReadBool(Stream _stream)
        {
            byte b0 = (byte)_stream.ReadByte();

            return (b0 == 1);
        }

        public static byte[] LoadBytes(string _filename)
        {            
            FileStream f = File.OpenRead(_filename);
            return LoadBytes(f);
            /*
            long len = f.Length;
            byte[] data = new byte[len];            
            f.Read(data, 0, (int)len);
            f.Close();

            return data;
            */
        }

        public static byte[] LoadBytes(Stream _stream)
        {
            long len = _stream.Length;
            byte[] data = new byte[len];
            _stream.Read(data, 0, (int)len);
            return data;
        }
    }
}
