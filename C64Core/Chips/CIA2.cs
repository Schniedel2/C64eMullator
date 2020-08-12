using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64Emulator.Chips
{

    public class CIA2 : CIA_6526
    {
        public CIA2() : base(0xdd00)
        {
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            int adr = _fullAddress - BaseAddress;
            adr %= 16;

            if (adr == 0)
            {

            }

            return base.Read(_fullAddress, _internal);
        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {
            int adr = _fullAddress - BaseAddress;
            adr %= 16;

            byte xVal = base.Read(_fullAddress, true);
            base.Write(_fullAddress, _val, _internal);

            if (adr == 0)
            {

            }

        }
    }

}
