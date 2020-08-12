using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Windows.Forms;
using System.IO;

using C64_WinForms.C64Emulator.Audio;

namespace C64_WinForms.C64Emulator
{
    public class C64
    {
        public CPU_6510 CPU;
        public PLA MPU;
        public IECBus IEC;        
        public VIC_II VIC;
        Debugger Debugger = new Debugger();
        public CIA2 CIA2;
        public OpenIOSlot IO1 = new OpenIOSlot(0xde00, 0xdeff);
        public OpenIOSlot IO2_Disk = new OpenIOSlot_DiskIO(0xdf00, 0xdfff);
        public SID SID;
        public ColorRAM ColorRAM = new ColorRAM();
        public C64Datasette Datasette;
        public C1541 Floppy;
        public PCKeyboard Keyboard = new PCKeyboard();
        public C64Joystick Joystick1;
        public C64Joystick Joystick2;
        public CIA CIA;        

        public bool CPUActive = true;

        public C64()
        {
            MPU = new PLA(this);
            CPU = new CPU_6510("C64", MPU);            
            SID = new SID_OpenAL();
            IEC = new IECBus();
            VIC = new VIC_II(this);
            CIA = new CIA(Keyboard);
            CIA2 = new CIA2();

            //Joystick1 = new NoJoystick();
            //Joystick1 = new JoystickKeypad(Keys.W, Keys.S, Keys.A, Keys.D, Keys.LControlKey);
            Joystick2 = new JoystickKeypad(Keys.NumPad8, Keys.NumPad5, Keys.NumPad4, Keys.NumPad6, Keys.ControlKey);
            // Joystick2 = new JoystickKeypad(Keys.Up, Keys.Down, Keys.Left, Keys.Right, Keys.ControlKey);

            CIA.SetJoystick(1, Joystick1);
            CIA.SetJoystick(2, Joystick2);

            Datasette = new DatasetteTAP();
            Floppy = new C1541();

            // Form f = Floppy.GetControlForm();
            // f.Show();

            Reset();
        }

        public void SwapJoysticks()
        {
            C64Joystick j = Joystick1;
            Joystick1 = Joystick2;
            Joystick2 = j;

            CIA.SetJoystick(1, Joystick1);
            CIA.SetJoystick(2, Joystick2);
        }

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

        public void Reset()
        {
            CPU.Reset();
            MPU.Reset();
            CIA.Reset();
            CIA2.Reset();
            SID.Reset();
            VIC.Reset();

            CPUActive = true;
        }

        public void ProcessFrames(int _frames)
        {
            for (int i=0; i < _frames; i++)
            {
                ProcessSingleFrame(i+1 == _frames);
            }            
        }

        public void ProcessFrame(bool _renderEnabled)
        {
            ProcessSingleFrame(_renderEnabled);
        }
        
        public int ProcessCPUCycle()
        {
            int ticks = CPU.Process();

            SID.Process(ticks);
            CIA.Process(ticks);
            CIA2.Process(ticks);

            Datasette.Process(ticks);
            CIA.SetFlagPIN(Datasette.Get_D4_READ());

            /*
            //  process floppy 
            Floppy.Process(ticks);

            //  build signals on IEC-Bus
            IEC.Process(this);
            */

            // CIA2.SetFlagPIN(...)

            //  check for IRQ's
            bool IRQ_CIA = CIA.HasIRQ();
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
            Floppy.StreamTo(_stream);
        }

        public void StreamFrom(Stream _stream)
        {
            CPUActive = false;

            CPU.StreamFrom(_stream);

            Datasette.StreamFrom(_stream);
            Floppy.StreamFrom(_stream);

            CPUActive = true;
        }

        public void LoadPRG(string _filename)
        {
            FileStream f = File.OpenRead(_filename);

            int memLO = f.ReadByte();
            int memHI = f.ReadByte();

            int adr = memLO | (memHI << 8);

            while (f.Position < f.Length)
            {
                byte b = (byte)f.ReadByte();
                MPU.Write(adr, b, true);
                adr++;
            }
        }

        public int ProcessSingleFrame(bool _outputEnabled)
        {
            if (!CPUActive)
                return 0;

            //  performs a complete screen-refresch cycle
            // https://www.c64-wiki.com/wiki/raster_time

            int totalTicks = 0;

            for (int rasterLine = 0; rasterLine < VIC.Settings.NumScanLines; rasterLine++)
            {
               
                //
                //  let the CPU run for 63 cycles
                //
                int ticks = VIC.DrawLine(rasterLine, _outputEnabled);

                while (ticks <= 63)
                {
                    ticks += ProcessCPUCycle();
                }

                totalTicks += ticks;
            }
            
            VIC.VerticalRetrace();

            return totalTicks;
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

        public string InspectCPUStates(CPUState oldState, CPUState newState)
        {
            string str = "";

            if (oldState.A != newState.A)
                str += string.Format("\t\t    A: ${0,2:X2} -> ${1,2:X2}\n", oldState.A, newState.A);
            if (oldState.X != newState.X)
                str += string.Format("\t\t    X: ${0,2:X2} -> ${1,2:X2}\n", oldState.X, newState.X);
            if (oldState.Y != newState.Y)
                str += string.Format("\t\t    Y: ${0,2:X2} -> ${1,2:X2}\n", oldState.Y, newState.Y);
            // if (oldState.PC != newState.PC)
            //     str += string.Format("\t\t   PC: ${0,2:X2} -> ${1,2:X2}\n", oldState.PC, newState.PC);
            if (oldState.SP != newState.SP)
                str += string.Format("\t\t   SP: ${0,2:X2} -> ${1,2:X2}\n", oldState.SP, newState.SP);
            if (oldState.Flags != newState.Flags)
                str += string.Format("\t\tFlags: ${0,2:X2} -> ${1,2:X2}\n", oldState.Flags, newState.Flags);

            return str;
        }


    }
}
