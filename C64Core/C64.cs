using System;
using System.IO;

using C64Emulator.Chips;
using C64Emulator.Devices;
using C64Emulator.CPUs;

namespace C64Emulator
{
    public class C64
    {
        public CPU_6502 CPU;
        public PLA MPU;
        public IECBus IEC;        
        public VIC_II VIC;
        public OpenIOSlot IO1 = new OpenIOSlot(0xde00, 0xdeff);
        public OpenIOSlot IO2_Disk = new OpenIOSlot_DiskIO(0xdf00, 0xdfff);
        public SID SID;
        public ColorRAM ColorRAM = new ColorRAM();
        public CIA1 CIA1;
        public CIA2 CIA2;

        public Datasette Datasette;
        public Joystick Joystick1;
        public Joystick Joystick2;

        public readonly int MonitorWidth = 403;
        public readonly int MonitorHeight = 312;

        // public C1541 Floppy;
        // public PCKeyboard Keyboard = new PCKeyboard();

        bool ClockActive = false;

        public C64()
        {

        }

        public void Init(byte[] _BASICROM, byte[] _KERNALROM, byte[] _CHARENROM, byte[] _C1541ROM, Keyboard _keyboard)
        {
            MPU = new PLA(this, _BASICROM, _KERNALROM, _CHARENROM);
            CPU = new CPU_6502("C64", MPU);            
            SID = new SID_NullDevice();     //
            IEC = new IECBus();
            VIC = new VIC_II(this);
            CIA1 = new CIA1(_keyboard);
            CIA2 = new CIA2();

            CIA1.SetJoystick(1, Joystick1);
            CIA1.SetJoystick(2, Joystick2);

            Datasette = new DatasetteTAP();
            // Floppy = new C1541();

            Reset();

            ClockActive = false;
        }

        public void SetKeyboard(Keyboard _keyboard)
        {
            CIA1.SetKeyboard(_keyboard);
        }

        public Keyboard GetKeyboard()
        {
            return CIA1.GetKeyboard();
        }

        public void SetJoystick1(Joystick _joy)
        {
            CIA1.SetJoystick(1, _joy);
        }

        public void SetJoystick2(Joystick _joy)
        {
            CIA1.SetJoystick(2, _joy);
        }

        public void StartClock()
        {
            ClockActive = true;
        }

        public void StopClock()
        {
            ClockActive = false;
        }

        public void SwapJoysticks()
        {
            Joystick j = Joystick1;
            Joystick1 = Joystick2;
            Joystick2 = j;

            CIA1.SetJoystick(1, Joystick1);
            CIA1.SetJoystick(2, Joystick2);
        }
        /*
        public void OnKeyDown(Keys _key)
        {
            Keyboard.OnKeyDown(_key);
            if (Joystick1 != null)
                Joystick1.OnKeyDown(_key);
            if (Joystick2 != null)
                Joystick2.OnKeyDown(_key);
        }

        public void OnKeyUp(Keys _key)
        {
            Keyboard.OnKeyUp(_key);
            if (Joystick1 != null)
                Joystick1.OnKeyUp(_key);
            if (Joystick2 != null)
                Joystick2.OnKeyUp(_key);
        }
        */

        public void Reset()
        {
            CPU.Reset();
            MPU.Reset();
            CIA1.Reset();
            CIA2.Reset();
            SID.Reset();
            VIC.Reset();
        }

        public int ProcessCPUCycle()
        {
            int ticks = CPU.Process();

            SID.Process(ticks);
            CIA1.Process(ticks);
            CIA2.Process(ticks);

            Datasette.Process(ticks);

            /*
            //  process floppy 
            Floppy.Process(ticks);

            //  build signals on IEC-Bus
            IEC.Process(this);
            */

            // CIA2.SetFlagPIN(...)

            //
            //  inter-chip-handlings
            //

            //  datasette -> CPU
            CIA1.SetFlagPIN(Datasette.Get_D4_READ());

            //  check for IRQ's
            bool IRQ_CIA = CIA1.HasIRQ();
            bool IRQ_VIC = VIC.HasIRQ();

            CPU.IRQ = IRQ_CIA || IRQ_VIC;

            //  check for NMI's
            CPU.SetNMI(CIA2.HasIRQ());
            
            return ticks;
        }

        public void StreamTo(Stream _stream)
        {
            CPU.StreamTo(_stream);

            Datasette.StreamTo(_stream);
            // Floppy.StreamTo(_stream);
        }

        public void StreamFrom(Stream _stream)
        {
            CPU.StreamFrom(_stream);

            Datasette.StreamFrom(_stream);
            // Floppy.StreamFrom(_stream);
        }

        public void LoadPRG(Stream _stream)
        {
            int memLO = _stream.ReadByte();
            int memHI = _stream.ReadByte();

            int adr = memLO | (memHI << 8);

            while (_stream.Position < _stream.Length)
            {
                byte b = (byte)_stream.ReadByte();
                MPU.Write(adr, b, true);
                adr++;
            }
        }

        public void LoadPRG(byte[] _raw)
        {
            int memLO = _raw[0];
            int memHI = _raw[1];

            int adr = memLO | (memHI << 8);

            for (int i = 2; i < _raw.Length; i++)
            {
                byte b = _raw[i];
                MPU.Write(adr, b, true);
                adr++;
            }
        }

        public void ProcessClockMS(double _ms)
        {
            if (!ClockActive)
                return;

            int totalTicks = (int)(_ms * 950.0); // ca.
            ProcessClockTicks(totalTicks);
        }

        public void ProcessClockTicks(int _totalTicks)
        {
            int ticksLeft = _totalTicks;

            while (ticksLeft > 0)
            {
                byte[] pixels;

                int ticks = VIC.ProcessRasterLine(out pixels);

                if (VIC.currentRasterLine == 0)
                    OnVerticalRetrace();

                OnAfterRasterline(VIC.currentRasterLine, pixels);

                while (ticks <= 63)
                {
                    ticks += ProcessCPUCycle();
                }

                ticksLeft -= ticks;
            }
        }

        public void ProcessFrames(int _frames)
        {
            ProcessClockTicks(312 * 63 * _frames);
        }



#if NDEBUG
        public void OnAfter_JSR(DecodedInstruction _instr, int _newPC)
        {
            Debugger.OnAfter_JSR(CPU, _instr, _newPC);
        }

        public void OnReadMem(int _fullAdress)
        {
            Debugger.OnReadMem(_fullAdress);

        }

        public void OnAfter_RTS(DecodedInstruction _instr)
        {
            Debugger.OnAfter_RTS(CPU, _instr);
        }

        public void OnWriteMem(int _fullAdress, byte _data)
        {
            Debugger.OnWriteMem(_fullAdress, _data);
        }

        public void OnMemoryChanged(int _fullAdress, byte _oldValue, byte _newValue)
        {
            Debugger.OnMemoryChanged(_fullAdress, _oldValue, _newValue);
        }

        public void OnNextOpcode(DecodedInstruction _instr)
        {
            Debugger.OnNextOpcode(CPU, _instr);
        }
#else
        public void OnAfter_JSR(DecodedInstruction _instr, int _newPC) { }
        
        public void OnReadMem(int _fullAdress) { }

        public void OnAfter_RTS(DecodedInstruction _instr) { }

        public void OnWriteMem(int _fullAdress, byte _data) { }

        public void OnMemoryChanged(int _fullAdress, byte _oldValue, byte _newValue) { }

        public void OnNextOpcode(DecodedInstruction _instr) { }
#endif

        //
        //  platform/device dependend interface
        //  

        protected virtual void OnAfterRasterline(int _rasterLine, byte[] _pixels)
        {

        }
        protected virtual void OnVerticalRetrace()
        {

        }

    }
}
