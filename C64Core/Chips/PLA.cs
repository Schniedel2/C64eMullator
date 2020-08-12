using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace C64Emulator.Chips
{

    // http://codebase64.org/doku.php?id=base:memmanage

    //  kernel references
    //    http://codebase64.org/doku.php?id=base:kernalreference

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
        public MemoryChip RAM;
        public MemoryChip Kernel;
        public MemoryChip ROM_HI_A000;
        public MemoryChip ROM_HI_E000;
        public MemoryChip ROM_LO;
        public MemoryChip BASIC_ROM;
        public MemoryChip CHAREN_ROM;
        public MemoryChip IO_MAP;

        public byte[] ROM_BASIC;
        public byte[] ROM_KERNAL;
        public byte[] ROM_CHAREN;

        //  external pins from expansion-port
        public bool EXROM = true;  // extern pullup-resister
        public bool GAME = true;   // extern pullup-resister

        public MemoryChip[] BankCache = new MemoryChip[16];

        C64 C64;

        public byte PR;
        byte DDR;

        public PLA(C64 _c64, byte[] _BASICROM, byte[] _KERNALROM, byte[] _CHARENROM)
        {
            C64 = _c64;

            ROM_BASIC = _BASICROM;
            ROM_KERNAL = _KERNALROM;
            ROM_CHAREN = _CHARENROM;

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

            RAM = new RAMMemoryChip(0x0000, 0xffff);

            Kernel = new ROMMemoryChip(0xe000, 0xffff);
            Kernel.SetROM(ROM_KERNAL);

            BASIC_ROM = new ROMMemoryChip(0xa000, 0xbfff);
            BASIC_ROM.SetROM(ROM_BASIC);

            IO_MAP = new IOMemoryChip(C64, 0xd000, 0xefff);
            CHAREN_ROM = new ROMMemoryChip(0xd000, 0xdfff);
            CHAREN_ROM.SetROM(ROM_CHAREN);

            ROM_HI_A000 = new ROMMemoryChip(0xa000, 0xbfff);
            ROM_HI_E000 = new ROMMemoryChip(0xe000, 0xffff);
            ROM_LO = new ROMMemoryChip(0x8000, 0x9fff);

            // ROM_HI_A000.WriteToRam = false;
            ROM_HI_E000.WriteToRam = false;
            // ROM_LO.WriteToRam = false;

            PrepareBankCache(0x37);
        }

        MemoryChip SelectBank(int _address)
        {
            int bank = (_address >> 12);
            return (BankCache[bank]);
        }

        void PrepareBankCache(byte _pr)
        {

            for (int i=0; i<16; i++)
                BankCache[i] = GetMemoryChip(_pr, i * 0x1000);
        }


        MemoryChip GetMemoryChip(byte _pr, int _address)
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

        MemoryChip GetRAM()
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

            MemoryChip mem = SelectBank(_address);
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

            MemoryChip mem = SelectBank(_address);
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
