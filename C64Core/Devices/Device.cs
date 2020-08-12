using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace C64Emulator.Devices
{
    public class Device
    {

        public virtual void Process(int _ticks)
        {

        }

        public virtual void StreamTo(Stream _stream)
        {
            //  todo: stream current state: TAP-File + file-pointer etc.
        }

        public virtual void StreamFrom(Stream _stream)
        {
            //  todo: stream current state: TAP-File + file-pointer etc.
        }

        public virtual void Reset()
        {

        }
    }
}
