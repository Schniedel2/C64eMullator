using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace C64_WinForms.C64Emulator
{

    // http://codebase64.org/doku.php?id=base:memmanage

    //  kernel references
    //    http://codebase64.org/doku.php?id=base:kernalreference

    public class MemoryBank
    {
        public int Offset;
        public int Len;
        public byte[] Data;
        public bool IsROM;
        public bool WriteToRam = true;

        public MemoryBank(int _offset, int _len)
        {
            Offset = _offset;            
            Len = _len;
            Data = new byte[_len];
            IsROM = false;
        }

        public virtual void StreamTo(Stream _stream)
        {
            for (int i = 0; i < Len; i++)
            {
                byte d = Read(Offset + i, true);
                _stream.WriteByte(d);
            }
        }

        public virtual void StreamFrom(Stream _stream)
        {
            for (int i = 0; i < Len; i++)
            {
                byte d = (byte)_stream.ReadByte();
                Write(Offset + i, d, true);
            }
        }

        public virtual byte Read(int _fullAdress, bool _internal)
        {
            int adr = _fullAdress - Offset;
            return Data[adr];
        }

        public virtual void Write(int _fullAdress, byte _data, bool _internal)
        {
            int adr = _fullAdress - Offset;
            Data[adr] = _data;
        }

        public void SetRom(byte[] _data)
        {
            IsROM = true;
            Data = _data;
        }

        public void LoadROM(string _filename)
        {
            FileStream f = File.OpenRead(_filename);
            f.Read(Data, 0, Len);
            f.Close();
        }
    }

    public class ROMMemoryBank : MemoryBank
    {
        public ROMMemoryBank(int _offset, int _len) : base(_offset, _len)
        {
            IsROM = true;
        }
    }

    public class RAMMemoryBank : MemoryBank
    {
        //CPU_6510 CPU;
        public RAMMemoryBank(int _offset, int _len) : base(_offset, _len)
        {
            //CPU = _c64.CPU;
            IsROM = false;
        }

        public override void Write(int _fullAdress, byte _data, bool _internal)
        {
            int adr = _fullAdress - Offset;
            Data[adr] = _data;
        }

        public override byte Read(int _fullAdress, bool _internal)
        {
            int adr = _fullAdress - Offset;
            return Data[adr];
        }

    }

    public class IOMemoryBank : MemoryBank
    {
        C64 C64;

        public IOMemoryBank(C64 _c64, int _offset, int _len) : base(_offset, _len)
        {
            C64 = _c64;
        }

        public override void Write(int _fullAdress, byte _data, bool _internal)
        {            
            if (C64.CIA.IsInAddressSpace(_fullAdress))
            {
                C64.CIA.Write(_fullAdress, _data, _internal);
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
                int adr = _fullAdress - Offset;
                Data[adr] = _data;
            }
        }

        public override byte Read(int _fullAdress, bool _internal)
        {
            if (C64.CIA.IsInAddressSpace(_fullAdress))
            {
                return (C64.CIA.Read(_fullAdress, _internal));
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
                int adr = _fullAdress - Offset;
                return Data[adr];
            }
        }

        public override void StreamTo(Stream _stream)
        {
            long pos = _stream.Position;

            C64.CIA.StreamTo(_stream);
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

            C64.CIA.StreamFrom(_stream);
            C64.CIA2.StreamFrom(_stream);
            C64.VIC.StreamFrom(_stream);
            C64.IO1.StreamFrom(_stream);
            C64.IO2_Disk.StreamFrom(_stream);
            C64.SID.StreamFrom(_stream);
            C64.ColorRAM.StreamFrom(_stream);
        }

    }

    public abstract class MemoryBus
    {
        public abstract void Write(int _address, byte _data, bool _internal);
        public abstract byte Read(int _address, bool _internal);
        public abstract void Reset();
        public abstract void StreamFrom(Stream _stream);
        public abstract void StreamTo(Stream _stream);
    }

    public class PLA : MemoryBus
    {
        //  RAM
        public MemoryBank RAM;
        public MemoryBank Kernel;
        public MemoryBank ROM_HI_A000;
        public MemoryBank ROM_HI_E000;
        public MemoryBank ROM_LO;
        public MemoryBank BASIC_ROM;
        public MemoryBank CHAREN_ROM;
        public MemoryBank IO_MAP;

        //  external pins from expansion-port
        public bool EXROM = true;  // extern pullup-resister
        public bool GAME = true;   // extern pullup-resister

        public MemoryBank[] BankCache = new MemoryBank[16];

        C64 C64;

        public byte PR;
        byte DDR;

        public PLA(C64 _c64)
        {
            C64 = _c64;
            Reset();
        }

        public override void StreamTo(Stream _stream)
        {
            _stream.WriteByte(PR);
            _stream.WriteByte(DDR);
            //_stream.WriteByte(EXROM);
            //_stream.WriteByte(GAME);

            long pos = _stream.Position;

            RAM.StreamTo(_stream);
            IO_MAP.StreamTo(_stream);
        }

        public override void StreamFrom(Stream _stream)
        {
            PR = (byte)_stream.ReadByte();
            DDR = (byte)_stream.ReadByte();
            //_stream.WriteByte(EXROM);
            //_stream.WriteByte(GAME);

            long pos = _stream.Position;

            RAM.StreamFrom(_stream);
            IO_MAP.StreamFrom(_stream);
        }

        public override void Reset()
        {
            PR = 0x37;
            DDR = 0x2f;

            RAM = new RAMMemoryBank(0x0000, 0x10000);

            Kernel = new ROMMemoryBank(0xe000, 0x2000);
            Kernel.LoadROM("ROMS\\KERNAL.ROM");

            BASIC_ROM = new ROMMemoryBank(0xa000, 0x2000);
            BASIC_ROM.LoadROM("ROMS\\BASIC.ROM");

            IO_MAP = new IOMemoryBank(C64, 0xd000, 0x2000);
            CHAREN_ROM = new ROMMemoryBank(0xd000, 0x1000);
            CHAREN_ROM.LoadROM("ROMS\\CHAR.ROM");

            ROM_HI_A000 = new ROMMemoryBank(0xa000, 0x2000);
            ROM_HI_E000 = new ROMMemoryBank(0xe000, 0x2000);
            ROM_LO = new ROMMemoryBank(0x8000, 0x2000);

            // ROM_HI_A000.WriteToRam = false;
            ROM_HI_E000.WriteToRam = false;
            // ROM_LO.WriteToRam = false;

            PrepareBankCache(0x37);
        }

        MemoryBank SelectBank(int _address)
        {
            int bank = (_address >> 12);
            return (BankCache[bank]);
        }

        void PrepareBankCache(byte _pr)
        {

            for (int i=0; i<16; i++)
                BankCache[i] = GetMemoryBank(_pr, i * 0x1000);
        }


        MemoryBank GetMemoryBank(byte _pr, int _address)
        {
            if (_address < 8000)
                return RAM;

            bool LORAM = true;
            bool HIRAM = true;
            bool CHAREN = true;

            LORAM = (_pr & 1) > 0;
            HIRAM = (_pr & 2) > 0;
            CHAREN = (_pr & 4) > 0;

            //  extern ROM (LO)
            if ((_address >= 0x8000) && (_address <= 0x9fff))
            {
                if ((LORAM) && (HIRAM) && (GAME) && (!EXROM))    return ROM_LO;
                if ((LORAM) && (HIRAM) && (!GAME) && (!EXROM))    return ROM_LO;
                if ((!GAME) && (EXROM))    return ROM_LO;
            }

            //  extern ROM (HI)
            if ((_address >= 0xA000) && (_address <= 0xBFFF))
            {
                if ((!LORAM) && (HIRAM) && (!GAME) && (!EXROM)) return ROM_HI_A000;
                if ((LORAM) && (HIRAM) && (!GAME) && (!EXROM)) return ROM_HI_A000;
            }

            //  extern ROM (HI)
            if ((_address >= 0xE000) && (_address <= 0xFFFF))
            {
                if ((!GAME) && (EXROM)) return ROM_HI_E000;
            }

            //  BASIC
            if ((_address >= 0xA000) && (_address <= 0xBFFF))
            {
                if ((LORAM) && (HIRAM) && (GAME) && (!EXROM)) return BASIC_ROM;
                if ((LORAM) && (HIRAM) && (GAME) && (EXROM)) return BASIC_ROM;
            }

            //  Kernel
            if ((_address >= 0xE000) && (_address <= 0xFFFF))
            {
                if ((HIRAM) && (GAME)) return Kernel;
                if ((HIRAM) && (!GAME) && (!EXROM)) return Kernel;
            }

            //  CHAREN / IO
            if ((_address >= 0xD000) && (_address <= 0xDFFF))
            {
                //  I/O
                if ((!GAME) && (EXROM))
                    return IO_MAP;

                //  RAM
                if ((!LORAM) && (!HIRAM) && (EXROM)) return RAM;
                if ((LORAM) && (!HIRAM) && (!GAME) && (!EXROM) && (CHAREN)) return RAM;
                
                // CHAREN
                if (!CHAREN)
                    return CHAREN_ROM;

                return IO_MAP;
            }

            return RAM;
        }

        MemoryBank GetRAM()
        {
            return RAM;
        }

        public override byte Read(int _address, bool _internal)
        {
            if (_address == 0x00)
            {
                //  DDR
                return DDR;
            }
            if (_address == 0x01)
            {
                // PR

                //  datasette
                // bit 4: sense == 0 if any key pressed
                PR &= 0xef; // clear bit 4
                if (!C64.Datasette.Get_F6_SENSE())
                    PR |= 0x10;

                return PR;
            }

            MemoryBank mem = SelectBank(_address);
            byte data = mem.Read(_address, _internal);

            return data;
        }

        public override void Write(int _address, byte _data, bool _internal)
        {
            if (_address == 0x00)
            {
                //  DDR
                DDR = _data;
            }
            if (_address == 0x01)
            {
                // PR
                if ((_data & 0x07) != (PR & 0x07))
                    PrepareBankCache(_data);

                PR = _data;

                //  datasette
                // bit 3: write
                C64.Datasette.Set_E5_WRITE((PR & 0x08) == 0);
                // bit 5: Motor
                C64.Datasette.Set_C3_MOTOR((PR & 0x20) == 0);
            }

            MemoryBank mem = SelectBank(_address);
            if (mem.IsROM)
            {
                //  write through?
                if (!mem.WriteToRam)
                    return;

                mem = GetRAM();
            }

            mem.Write(_address, _data, _internal);
        }

    }
}
