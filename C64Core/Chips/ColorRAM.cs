using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64Emulator.Chips
{
    public class ColorRAM : Chip
    {
        public ColorRAM() : base(0xd800, 0xdbff)
        {

        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {
            base.Write(_fullAddress, _val, _internal);
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            byte data = base.Read(_fullAddress, _internal);
            return data;
        }
    }
}
