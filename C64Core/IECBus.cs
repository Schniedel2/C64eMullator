using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64Emulator
{
    public struct IECBusSignalState
    {
        public int SRQ;    //  IN (device -> c64)
        public int ATN;    //  OUT (c64 -> device)
        public int CLK;    //  IN/OUT
        public int DATA;    //  IN/OUT
        public int RESET;    //  OUT        

        public void toInactive()
        {
            SRQ = 0;
            ATN = 0;
            CLK = 0;
            DATA = 0;
            RESET = 0;
        }

        public void CopyFrom(IECBusSignalState _from)
        {
            SRQ = _from.SRQ;
            ATN = _from.ATN;
            CLK = _from.CLK;
            DATA = _from.DATA;
            RESET = _from.RESET;
        }
    }

    public class IECBus
    {
        public IECBusSignalState xSignal;
        public IECBusSignalState Signal;

        /*
        public void Process(C64 _c64)
        {
            xSignal.CopyFrom(Signal);
            Signal.toInactive();

            //  collision?
            
            //  CIA2: let host signal
            byte PRA = _c64.CIA2.PRA;
            if ((PRA & (1 << 3)) > 0)
                Signal.ATN += 1;

            if ((PRA & (1 << 4)) > 0)
                Signal.CLK += 1;

            if ((PRA & (1 << 5)) > 0)
                Signal.DATA += 1;

            //  floppy output
            C1541 floppy = _c64.Floppy;
            if (floppy.DATA_OUT)
                Signal.DATA += 2;

            if (floppy.CLOCK_OUT)
                Signal.CLK += 2;
            
            //  collision?
            if ((Signal.ATN == 3) ||
                (Signal.CLK == 3) ||
                (Signal.DATA == 3))
            {
                Console.Out.WriteLine("IEC: collision");
            }

            //
            //  inputs
            //

            //  floppy input
            floppy.ATN_IN = (Signal.ATN > 0);
            floppy.CLOCK_IN = (Signal.CLK > 0);
            floppy.DATA_IN = (Signal.DATA > 0);
            
            //  CIA2 input
            _c64.CIA2.PRA &= 0x3f;
            if (Signal.CLK == 0)
                _c64.CIA2.PRA |= (1 << 6);
            else
                _c64.CIA2.PRA &= 0xbf;

            if (Signal.DATA == 0)
                _c64.CIA2.PRA |= (1 << 7);
            else
                _c64.CIA2.PRA &= 0x7f;

            //
            //  Trace
            //
            if ((Signal.ATN != xSignal.ATN) ||
                (Signal.DATA != xSignal.DATA) ||
                (Signal.CLK != xSignal.CLK))
            {
                string str = "";

                str += string.Format("ATN:{0}  ", Signal.ATN);
                str += string.Format("CLK:{0}  ", Signal.CLK);
                str += string.Format("DATA:{0}  ", Signal.DATA);

                Console.Out.WriteLine(str);
            }
        }
        */
    }
}
