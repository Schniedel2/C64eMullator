using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace C64_WinForms.C64Emulator
{
    public class C1541 : C64Peripherie
    {
        public CPU_6510 CPU;
        public FloppyPLA PLA;

        public C1541()
        {
            PLA = new FloppyPLA();
            CPU = new CPU_6510("C1541", PLA);

            Reset();
        }

        public Form GetControlForm()
        {
            C1541_UI f = new C1541_UI();
            f.myC1541 = this;

            return f;
        }

        public bool DATA_OUT
        {
            get
            {
                return PLA.VIA1_SerialBusAccess.Get_DATA_OUT();
            }
        }

        public bool CLOCK_OUT
        {
            get
            {
                return PLA.VIA1_SerialBusAccess.Get_CLOCK_OUT();
            }
        }

        public bool ATN_OUT
        {
            get
            {
                return PLA.VIA1_SerialBusAccess.Get_ATN_OUT();
            }            
        }

        public bool ATN_IN
        {
            set
            {
                PLA.VIA1_SerialBusAccess.Set_ATN_IN(value);
            }
        }

        public bool CLOCK_IN
        {
            set
            {
                PLA.VIA1_SerialBusAccess.Set_CLOCK_IN(value);
            }
        }

        public bool DATA_IN
        {
            set
            {
                PLA.VIA1_SerialBusAccess.Set_DATA_IN(value);
            }
        }

        public bool LED
        {
            get
            {
                //  LED: Bit 3 
                return ((PLA.VIA2_DriveControl.IRB & 0x08) > 0);
            }
        }

        public override void Reset()
        {
            CPU.Reset();

            base.Reset();
        }

        public override void Process(int _ticks)
        {
            int ticks = 0;
            int totalTicks = 0;
            while (totalTicks < _ticks)
            {
                ticks = CPU.Process();
                PLA.VIA1_SerialBusAccess.Process(ticks);
                PLA.VIA2_DriveControl.Process(ticks);

                //  interrupts
                bool IRQ1 = PLA.VIA1_SerialBusAccess.Has_IRQ();
                bool IRQ2 = PLA.VIA2_DriveControl.Has_IRQ();

                CPU.IRQ = (IRQ1 || IRQ2);

                totalTicks += ticks;

                //  update LED's                
                byte data = PLA.VIA2_DriveControl.Read(0x1c00, true); // PRB
                byte Stempotor = (byte)(data & 0x03);
                bool MotorOn = ((data & 0x04) > 0);
                bool LED = ((data & 0x08) > 0);
                bool WriteProtection = ((data & 0x10) > 0);
                byte Speed = (byte)((data >> 5) & 0x03);
                bool ReadSyncDetected = ((data & 0x80) > 0);

                //  LED: Bit 3 
                bool led = ((PLA.VIA2_DriveControl.IRB & 0x08) > 0);

                if (led)
                {

                }
                if (data != 0x60)
                {

                }
                if (MotorOn)
                {

                }
            }
        }
    }

    //
    //  VIA
    //

    public class VIA : Chip
    {
        internal byte IRA = 0;
        internal byte IRB = 0;
        internal byte ORA = 0;
        internal byte ORB = 0;
        internal byte DDRA = 0;
        internal byte DDRB = 0;

        internal int Timer = 0;
        internal byte Timer_LatchLO = 0;
        internal byte Timer_LatchHI = 0;
        internal bool Timer1_IRQEnabled = false;

        internal byte AUX = 0; //  AuxControlReg/TimerControlReg
        internal byte PCR = 0; //  PeripheralControlReg
        internal byte IER = 0;
        internal byte IFR = 0;

        public VIA(int _baseAddress, int _maxAddress) : base(_baseAddress, _maxAddress)
        {
            Reset();
        }

        void ProcessTimer(int _ticks)
        {
            Timer -= _ticks;
            if (Timer > 0)
                return;

            //  timer underrun
            // only call IRQ, if not already set!
            if (Timer1_IRQEnabled)
                IFR |= 64;

            if ((AUX & 0x80) > 0)
            {
                //  free run
                Timer = Timer_LatchLO | (Timer_LatchHI << 8);
            }
            else
            {
                //  one shot
            }

            if ((AUX & 64) > 0)
            {
                // toggle PB7
                bool pb7 = ((IRB & 0x80) > 0);
                ORB_SetBit(7, !pb7);
            }


            Timer1_IRQEnabled = false;
        }
        
        public bool Has_IRQ()
        {
            return ((IFR & IER) > 0);
        }

        public override void Process(int _ticks)
        {
            ProcessTimer(_ticks);
            base.Process(_ticks);
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            int adr = _fullAddress - BaseAddress;

            switch (adr)
            {
                case (0x00):
                    {
                        return IRB;
                    }

                case (0x01):
                    {
                        if (!_internal)
                        {
                            IFR = 0;
                        }
                        return IRA;
                    }

                case (0x02):
                    {
                        return DDRB;
                    }

                case (0x03):
                    {
                        return DDRA;
                    }

                case 0x04:
                    {
                        byte val = (byte)(Timer & 0xff);
                        if (!_internal)
                        {
                            IFR &= (byte)(0xbf); // 1011 1111
                            Timer1_IRQEnabled = true;
                        }
                        return val;
                    }

                case 0x05:
                    {
                        byte val = (byte)((Timer >> 8) & 0xff);
                        return val;
                    }

                case 0x0b:
                    {
                        return AUX;
                    }

                case 0x0c:
                    {
                        return PCR;
                    }

                case 0x0d:
                    {
                        byte val = IFR;
                        if (Has_IRQ())
                            val |= 0x80;
                        return val;
                    }

                case 0x0e:
                    {
                        return (byte)(0x80 | IER);
                    }
            }

            return base.Read(_fullAddress, _internal);
        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {
            int adr = _fullAddress - BaseAddress;
            // byte xVal = Read(_fullAddress, true);

            switch (adr)
            {
                //  PortB
                case 0x00:
                    {
                        if (_val != 0)
                        { }

                        IRB = _val;
                        ORB = _val;
                        ORB &= DDRB;
                        break;
                    }

                //  PortA
                case 0x01:
                    {
                        IRA = _val;
                        ORA = _val;
                        ORA &= DDRA;
                        break;
                    }

                //  DDRB
                case 0x02:
                    {
                        DDRB = _val;
                        break;
                    }

                //  DDRA
                case 0x03:
                    {
                        DDRA = _val;
                        break;
                    }

                //  Timer LO Counter T1C LO
                case 0x04:
                    {
                        Timer_LatchLO = _val;
                        break;
                    }

                //  Timer HI
                case 0x05:
                    {
                        //                         
                        Timer_LatchHI = _val;
                        Timer = Timer_LatchLO | (Timer_LatchHI << 8);
                        IFR &= (byte)(0xbf); // 1011 1111
                        Timer1_IRQEnabled = true;
                        break;
                    }

                //  Timer Latch LO
                case 0x06:
                    {
                        Timer_LatchLO = _val;
                        break;
                    }

                //  Timer Latch HI
                case 0x07:
                    {
                        Timer_LatchHI = _val;
                        break;
                    }

                //  AUX/Timer Control Register
                case 0x0b:
                    {
                        //  bit #0: CA1 (ATN IN) trigger on positive edge
                        //  bit #6 + #7: T1 Timer Control 
                        //  bit #5: T2 Timer Control
                        //  bit #1: latch PB
                        //  bit #0: latch PA
                        AUX = _val;

                        byte timerMode = (byte)(AUX >> 6);

                        break;
                    }

                case 0x0c:
                    {
                        PCR = _val;

                        if ((PCR & 0x0e) == 0x0e)
                        {
                            //  clear IRQ flag from timer
                            IFR &= 0xbf; // 10111111
                        }
                        break;
                    }


                //  IFR - Interrupt Flags
                case 0x0d:
                    {
                        if (_internal)
                            IFR = _val;
                        else
                        {
                            IFR &= 0x7f; // clear bit 7
                            bool clearIRQ = ((_val & 0x80) > 0);

                            byte mask = 1;
                            for (int i = 0; i < 7; i++)
                            {
                                if ((_val & mask) > 0)
                                {
                                    IFR &= (byte)(~mask);
                                    if (!clearIRQ)
                                        IFR |= mask;
                                }
                                mask <<= 1;
                            }
                        }
                        _val = IFR;
                        break;
                    }

                //  IER - Interrupt Enable
                case 0x0e:
                    {
                        if (_internal)
                            IER = _val;
                        else
                        {
                            IER &= 0x7f; // clear bit 7
                            bool enableBits = ((_val & 0x80) > 0);

                            byte mask = 1;
                            for (int i = 0; i < 7; i++)
                            {
                                if ((_val & mask) > 0)
                                {
                                    IER &= (byte)(~mask);
                                    if (enableBits)
                                        IER |= mask;
                                }
                                mask <<= 1;
                            }
                        }
                        _val = IER;
                        break;
                    }

            }

            base.Write(_fullAddress, _val, _internal);
        }
        
        internal void ORB_SetBit(int _bitNum, bool _bitEnable)
        {
            int bitMask = (1 << _bitNum);

            byte xIRB = IRB;

            if (!_bitEnable)
            {
                ORB &= (byte)(~bitMask);
                // if ((DDRB & bitMask) > 0)
                    IRB &= (byte)(~bitMask);                
            }
            else
            {
                ORB |= (byte)(bitMask);
                // if ((DDRB & bitMask) > 0)
                    IRB |= (byte)(bitMask);
            }

            ORB &= DDRB;

            int CA_ACtion = ((PCR >> 1) & 0x03);

            if ((IRB & 0x80) != (xIRB & 0x80))
            {
                if ((PCR & 1) == 0) // trigger on high -> low
                {
                    if ((IRB & 0x80) == 0)
                        IFR |= 2;
                }
                else // trigger on low -> high
                {
                    if ((IRB & 0x80) == 1)
                        IFR |= 2;
                }

            }
        }

    }

    //  serial bus access
    public class VIA1 : VIA 
    {
        //  serial signals
        internal bool ATN_IN = false;
        internal byte DeviceID = 8; // 8 = standard-floppy

        public VIA1() : base(0x1800, 0x180f)
        {
        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {
            int adr = _fullAddress - BaseAddress;

            switch (adr)
            {
                case 0x00:  // PRB
                    {
                        break;
                    }
            }

            base.Write(_fullAddress, _val, _internal);
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            int adr = _fullAddress - BaseAddress;

            switch (adr)
            {
                case (0x00):
                    {
                        //  PRB
                        /*
                           | Bit  7   |   ATN IN                                          |
                           | Bits 6-5 |   Device address preset switches:                 |  01100000b = $60
                           |          |     00 = #8, 01 = #9, 10 = #10, 11 = #11          |
                           | Bit  4   |   ATN acknowledge OUT                             |
                           | Bit  3   |   CLOCK OUT                                       |
                           | Bit  2   |   CLOCK IN                                        |
                           | Bit  1   |   DATA OUT                                        |
                           | Bit  0   |   DATA IN   
                        */
                        break;
                    }

                case 0x01:
                    {
                        //  PRA - erase IRQ-Flag
                        break;
                    }

                case 0x0d:
                    {
                        break;
                    }
            }
            return base.Read(_fullAddress, _internal);
        }

        public override void Reset()
        {
            DDRB = 0x1a;
            DDRA = 0xff;
            IRB = (byte)(((DeviceID - 8) & 0x03) << 5);
            base.Reset();
        }

        public void Set_ATN_IN(bool _enable)
        {
            ORB_SetBit(7, _enable);
        }

        public void Set_DATA_IN(bool _enable)
        {
            ORB_SetBit(0, _enable);
        }

        public void Set_CLOCK_IN(bool _enable)
        {
            ORB_SetBit(2, _enable);
        }

        public bool Get_DATA_OUT()
        {
            return ((ORB & 0x02) > 0);
        }

        public bool Get_CLOCK_OUT()
        {
            return ((ORB & 0x08) > 0);
        }

        public bool Get_ATN_OUT()
        {
            return ((ORB & 0x10) > 0);
        }

    }

    //  drive control
    public class VIA2 : VIA
    {
        public VIA2() : base(0x1c00, 0x1c0f)
        {
        }

        public override void Reset()
        {
            DDRB = 0x6f;
            DDRA = 0xff;

            base.Reset();
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            return base.Read(_fullAddress, _internal);
        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {
            int adr = _fullAddress - BaseAddress;

            switch (adr)
            {
                case 0x00:
                    {
                        //  PRB
                        break;
                    }

                case 0x01:
                    {
                        //  PRA
                        break;
                    }

                case (0x02):
                    {
                        // DDRB;
                        break;
                    }

                case (0x03):
                    {
                        // DDRA;
                        break;
                    }
            }
            
            base.Write(_fullAddress, _val, _internal);
        }
    }

    public class FloppyPLA : MemoryBus
    {
        public MemoryBank RAM;
        public MemoryBank ROM;
        public VIA1 VIA1_SerialBusAccess;
        public VIA2 VIA2_DriveControl;

        public FloppyPLA()
        {

        }

        public override void Reset()
        {
            RAM = new RAMMemoryBank(0x000, 0x800);
            ROM = new ROMMemoryBank(0xc000, 0x4000);
            VIA1_SerialBusAccess = new VIA1();
            VIA2_DriveControl = new VIA2();

            ROM.LoadROM("ROMS\\C1541.ROM");
        }

        public override byte Read(int _address, bool _internal)
        {
            if ((_address >= VIA1_SerialBusAccess.BaseAddress) && (_address <= VIA1_SerialBusAccess.MaxAddress))
            {
                return VIA1_SerialBusAccess.Read(_address, _internal);
            }

            if ((_address >= VIA2_DriveControl.BaseAddress) && (_address <= VIA2_DriveControl.MaxAddress))
            {
                return VIA2_DriveControl.Read(_address, _internal);
            }

            if (_address < 0x2000)
            {
                return RAM.Read(_address, _internal);
            }

            return ROM.Read(_address, _internal);
        }

        public override void Write(int _address, byte _data, bool _internal)
        {
            if ((_address >= VIA1_SerialBusAccess.BaseAddress) && (_address <= VIA1_SerialBusAccess.MaxAddress))
            {
                VIA1_SerialBusAccess.Write(_address, _data, _internal);
                return;
            }

            if ((_address >= VIA2_DriveControl.BaseAddress) && (_address <= VIA2_DriveControl.MaxAddress))
            {
                VIA2_DriveControl.Write(_address, _data, _internal);
                return;
            }

            if (_address < 0x2000)
            {
                RAM.Write(_address, _data, _internal);
                return;
            }

            ROM.Write(_address, _data, _internal);
        }

        public override void StreamFrom(Stream _stream)
        {
            RAM.StreamFrom(_stream);
            ROM.StreamFrom(_stream);
            VIA1_SerialBusAccess.StreamFrom(_stream);
            VIA2_DriveControl.StreamFrom(_stream);
        }

        public override void StreamTo(Stream _stream)
        {
            RAM.StreamTo(_stream);
            ROM.StreamTo(_stream);
            VIA1_SerialBusAccess.StreamTo(_stream);
            VIA2_DriveControl.StreamTo(_stream);
        }

    }
}
