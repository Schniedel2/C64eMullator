using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64_WinForms.C64Emulator
{
    public class OpenIOSlot : Chip
    {
        public OpenIOSlot(int _baseAddress, int _maxAddress) : base(_baseAddress, _maxAddress)
        {
        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {
            base.Write(_fullAddress, _val, _internal);
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            return base.Read(_fullAddress, _internal);
        }
    }


    public class OpenIOSlot_DiskIO : OpenIOSlot
    {
        public OpenIOSlot_DiskIO(int _baseAddress, int _maxAddress) : base(_baseAddress, _maxAddress)
        {
        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {
            base.Write(_fullAddress, _val, _internal);
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            return base.Read(_fullAddress, _internal);
        }
    }
}
