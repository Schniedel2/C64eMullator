using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace C64Emulator.Chips
{
    public class Chip
    {
        internal int BaseAddress;
        internal int MaxAddress;

        internal byte[] RAM;

        public Chip(int _baseAddress, int _maxAddress)
        {
            BaseAddress = _baseAddress;
            MaxAddress = _maxAddress;

            RAM = new byte[MaxAddress - BaseAddress + 1];
        }

        public virtual void Reset()
        {

        }

        public virtual bool IsInAddressSpace(int _address)
        {
            if ((_address >= BaseAddress) && (_address <= MaxAddress))
            {
                return true;
            }
            return false;
        }

        public virtual void Write(int _fullAddress, byte _val, bool _internal)
        {
            RAM[_fullAddress - BaseAddress] = _val;
        }

        public virtual byte Read(int _fullAddress, bool _internal)
        {
            return RAM[_fullAddress - BaseAddress];
        }

        public virtual void Process(int _ticks)
        {
        }

        public virtual void StreamTo(Stream _stream)
        {
            for (int i = BaseAddress; i <= MaxAddress; i++)
            {
                byte d = Read(i, true);
                _stream.WriteByte(d);
            }
        }

        public virtual void StreamFrom(Stream _stream)
        {
            for (int i = BaseAddress; i <= MaxAddress; i++)
            {
                byte d = (byte)_stream.ReadByte();
                Write(i, d, true);
            }
        }

        public int Length()
        {
            return (MaxAddress - BaseAddress - 1);
        }
    }
}
