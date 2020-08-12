using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace C64Emulator.Chips
{
    public class IOMemoryChip : MemoryChip
    {
        C64 C64;

        public IOMemoryChip(C64 _c64, int _offset, int _len) : base(_offset, _len)
        {
            C64 = _c64;
        }

        public override void Write(int _fullAdress, byte _data, bool _internal)
        {
            if (C64.CIA1.IsInAddressSpace(_fullAdress))
            {
                C64.CIA1.Write(_fullAdress, _data, _internal);
                return;
            }

            if (C64.CIA2.IsInAddressSpace(_fullAdress))
            {
                C64.CIA2.Write(_fullAdress, _data, _internal);
                return;
            }

            if (C64.VIC.IsInAddressSpace(_fullAdress))
            {
                C64.VIC.Write(_fullAdress, _data, _internal);
                return;
            }

            if (C64.IO1.IsInAddressSpace(_fullAdress))
            {
                C64.IO1.Write(_fullAdress, _data, _internal);
                return;
            }

            if (C64.IO2_Disk.IsInAddressSpace(_fullAdress))
            {
                C64.IO2_Disk.Write(_fullAdress, _data, _internal);
                return;
            }

            if (C64.SID.IsInAddressSpace(_fullAdress))
            {
                C64.SID.Write(_fullAdress, _data, _internal);
                return;
            }

            if (C64.ColorRAM.IsInAddressSpace(_fullAdress))
            {
                C64.ColorRAM.Write(_fullAdress, _data, _internal);
                return;
            }

            {
                //  invalid(?)
            }
        }

        public override byte Read(int _fullAdress, bool _internal)
        {
            if (C64.CIA1.IsInAddressSpace(_fullAdress))
            {
                return (C64.CIA1.Read(_fullAdress, _internal));
            }

            if (C64.CIA2.IsInAddressSpace(_fullAdress))
            {
                return (C64.CIA2.Read(_fullAdress, _internal));
            }

            if (C64.VIC.IsInAddressSpace(_fullAdress))
            {
                return (C64.VIC.Read(_fullAdress, _internal));
            }

            if (C64.IO1.IsInAddressSpace(_fullAdress))
            {
                return (C64.IO1.Read(_fullAdress, _internal));
            }

            if (C64.IO2_Disk.IsInAddressSpace(_fullAdress))
            {
                return (C64.IO2_Disk.Read(_fullAdress, _internal));
            }

            if (C64.SID.IsInAddressSpace(_fullAdress))
            {
                return (C64.SID.Read(_fullAdress, _internal));
            }

            if (C64.ColorRAM.IsInAddressSpace(_fullAdress))
            {
                return (C64.ColorRAM.Read(_fullAdress, _internal));
            }

            {
                int adr = _fullAdress - BaseAddress;
                return RAM[adr];
            }
        }

        public override void StreamTo(Stream _stream)
        {
            long pos = _stream.Position;

            C64.CIA1.StreamTo(_stream);
            C64.CIA2.StreamTo(_stream);
            C64.VIC.StreamTo(_stream);
            C64.IO1.StreamTo(_stream);
            C64.IO2_Disk.StreamTo(_stream);
            C64.SID.StreamTo(_stream);
            C64.ColorRAM.StreamTo(_stream);
        }

        public override void StreamFrom(Stream _stream)
        {
            long pos = _stream.Position;

            C64.CIA1.StreamFrom(_stream);
            C64.CIA2.StreamFrom(_stream);
            C64.VIC.StreamFrom(_stream);
            C64.IO1.StreamFrom(_stream);
            C64.IO2_Disk.StreamFrom(_stream);
            C64.SID.StreamFrom(_stream);
            C64.ColorRAM.StreamFrom(_stream);
        }

    }
}
