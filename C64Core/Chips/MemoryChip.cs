using System;
using System.Collections.Generic;
using System.IO;

namespace C64Emulator.Chips
{
    public class MemoryChip : Chip
    {
        public bool IsROM;
        public bool WriteToRam = true;

        public MemoryChip(int _baseAddress, int _maxAddress) : base(_baseAddress, _maxAddress)
        {
            IsROM = false;
        }

        public void SetROM(byte[] _data)
        {
            IsROM = true;
            RAM = _data;
        }
    }

    public class ROMMemoryChip : MemoryChip
    {
        public ROMMemoryChip(int _baseAddress, int _maxAddress) : base(_baseAddress, _maxAddress)
        {
            IsROM = true;
        }
    }

    public class RAMMemoryChip : MemoryChip
    {
        public RAMMemoryChip(int _baseAddress, int _maxAddress) : base(_baseAddress, _maxAddress)
        {
            IsROM = false;
        }
    }

}
