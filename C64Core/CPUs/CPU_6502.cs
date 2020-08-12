using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using C64Emulator.Chips;

namespace C64Emulator.CPUs
{
    /*
    http://www.pagetable.com/c64rom/c64rom_de.html
    http://www.ffd2.com/fridge/docs/c64-diss.html

    http://www.oxyron.de/html/opcodes02.html
    http://tnd64.unikat.sk/assemble_it2.html

    http://codebase64.org/doku.php?id=base:6510_instruction_timing
    http://codebase64.org/doku.php?id=magazines:chacking1#opcodes_and_quasi-opcodes

    IO:
    https://www.c64-wiki.com/wiki/Page_208-211

    OPCODES + Beschreibung
    http://www.retro-programming.de/programming/assembler/asm-grundlagen/die-akku-ladebefehle/
    http://codebase64.org/doku.php?id=magazines:chacking1#opcodes_and_quasi-opcodes

    //  kernal.rountines + startup-sequence
    https://www.c64-wiki.de/wiki/KERNAL
    */

    public enum AddressMode
    {
        Implied,        // -
        Immediate,      // #$00
        ZeroPage,       // $00
        ZeroPageX,      // $00 + X
        ZeroPageY,      // $00 + Y
        IndZeroPageX,   // ($00 + X)
        IndZeroPageY,   // ($00) + Y
        Absolute,   // $0000
        AbsoluteX,  // $0000 + X
        AbsoluteY,  // $0000 + Y
        Indirect,   // ($0000)
        Relative,   // $0000 + PC
        other
    }

    public class OpcodeDescription
    {
        public bool IsLegal;
        public byte Opcode;
        public int Length;
        public string Name;
        public string Desc;
        public int Ticks;
        public AddressMode AdressMode;
        // public Type Opcode;

        public OpcodeDescription(byte _opcode, string _name, string _desc, AddressMode _mode, int _length, int _ticks)
        {
            IsLegal = true;
            Opcode = _opcode;
            Name = _name;
            Desc = _desc;
            Ticks = _ticks;
            AdressMode = _mode;
            Length = _length;
        }
    }

    public class DecodedInstruction
    {
        public OpcodeDescription OpcodeDesc;

        public ushort PC;

        public byte Opcode;
        public byte p8;
        public byte p8_2;
        public ushort p16;
        public ushort adr16;

        public bool PageOverrun;

        public override string ToString()
        {
            string[] ParamPrintMask = new string[(int)AddressMode.other];
            ParamPrintMask[(int)AddressMode.Implied] = "{0}";
            ParamPrintMask[(int)AddressMode.Immediate] = "{0} #${1,2:X2}";
            ParamPrintMask[(int)AddressMode.ZeroPage] = "{0} ${1,2:X2}";
            ParamPrintMask[(int)AddressMode.ZeroPageX] = "{0} ${1,2:X2},X";
            ParamPrintMask[(int)AddressMode.ZeroPageY] = "{0} ${1,2:X2},Y";
            ParamPrintMask[(int)AddressMode.IndZeroPageX] = "{0} (${1,2:X2},X)";
            ParamPrintMask[(int)AddressMode.IndZeroPageY] = "{0} (${1,2:X2}),Y";
            ParamPrintMask[(int)AddressMode.Absolute] = "{0} ${2,2:X2}{1,2:X2}";
            ParamPrintMask[(int)AddressMode.AbsoluteX] = "{0} ${2,2:X2}{1,2:X2},X";
            ParamPrintMask[(int)AddressMode.AbsoluteY] = "{0} ${2,2:X2}{1,2:X2},Y";
            ParamPrintMask[(int)AddressMode.Indirect] = "{0} (${2,2:X2}{1,2:X2})";
            ParamPrintMask[(int)AddressMode.Relative] = "{0} ${3,4:X4}  ->  +${1,2:X2}";

            string PCptr = string.Format("${0:X4}:  ", PC);

            //  memory bytes
            PCptr += string.Format("{0,2:X2} ", Opcode);

            if (OpcodeDesc.Length >= 2)
                PCptr += string.Format("{0,2:X2} ", p8);
            else
                PCptr += "   ";

            if (OpcodeDesc.Length >= 3)
                PCptr += string.Format("{0,2:X2} ", p8_2);
            else
                PCptr += "   ";

            //  splitter
            if (OpcodeDesc.IsLegal)
                PCptr += "   ";
            else
                PCptr += " ! ";  //  note for illegal opcodes

            ushort relativeToPC = (ushort)(PC + OpcodeDesc.Length);
            if (OpcodeDesc.AdressMode == AddressMode.Relative)
            {
                relativeToPC += p8;

                if (p8 >= 128)
                    relativeToPC -= 256;
            }

            string disasm = string.Format(
                ParamPrintMask[(int)OpcodeDesc.AdressMode],
                OpcodeDesc.Name,
                p8,
                p8_2,
                relativeToPC);

            return PCptr + disasm; ;
        }

        public void Decode(CPU_6502 _cpu, ushort _adr)
        {
            PC = _adr;
            Opcode = _cpu.ReadMemory(_adr);
            p8 = 0;
            p8_2 = 0;
            
            adr16 = 0;
            PageOverrun = false;

            OpcodeDesc = CPU_6502.Opcodes[Opcode];

            byte imm8 = 0;
            byte pLO = 0;
            byte pHI = 0;
            ushort rel16 = 0;

            if (OpcodeDesc.Length == 2)
            {
                p8 = _cpu.ReadMemory(_adr + 1);
            }

            if (OpcodeDesc.Length == 3)
            {
                p8 = _cpu.ReadMemory(_adr + 1);
                p8_2 = _cpu.ReadMemory(_adr + 2);
                pLO = p8;
                pHI = p8_2;
                p16 = (ushort)(pLO | (pHI << 8));
            }

            //  decode pointer for memory access
            switch (OpcodeDesc.AdressMode)
            {
                case AddressMode.Implied:
                    {
                        break;
                    }

                case AddressMode.Immediate:
                    {
                        imm8 = p8;
                        break;
                    }

                case AddressMode.Relative:
                    {
                        rel16 = (ushort)(_cpu.PC + OpcodeDesc.Length);
                        rel16 += p8;
                        if (p8 >= 128)
                            rel16 -= 256;
                        adr16 = rel16;
                        break;
                    }

                case AddressMode.ZeroPage:
                    {
                        adr16 = p8;
                        break;
                    }

                case AddressMode.ZeroPageX:
                    {
                        adr16 = (ushort)((p8 + _cpu.X) & 0xff);
                        break;
                    }

                case AddressMode.ZeroPageY:
                    {
                        adr16 = (ushort)((p8 + _cpu.Y) & 0xff);
                        break;
                    }

                case AddressMode.IndZeroPageX:
                    {
                        // 
                        byte adr8 = p8;
                        adr8 += _cpu.X;

                        pLO = _cpu.ReadMemory(adr8);
                        adr8++;
                        pHI = _cpu.ReadMemory(adr8);

                        int page1 = adr16 >> 8;

                        adr16 = (ushort)(pLO | (pHI << 8));

                        int page2 = adr16 >> 8;

                        if (page1 != page2)
                        {
                            //  todo: add tick on page-overrun
                            int falseAdr = (page1 << 8) | (adr16 & 0xff);
                            _cpu.ReadMemory(falseAdr);
                            PageOverrun = true;
                        }


                        break;
                    }

                case AddressMode.IndZeroPageY:
                    {
                        // 
                        byte adr8 = p8;

                        pLO = _cpu.ReadMemory(adr8);
                        adr8++;
                        pHI = _cpu.ReadMemory(adr8);

                        adr16 = (ushort)(pLO | (pHI << 8));
                        int page1 = adr16 >> 8;
                        adr16 += _cpu.Y;
                        int page2 = adr16 >> 8;

                        if (page1 != page2)
                        {
                            //  handle page-overrun
                            int falseAdr = (page1 << 8) | (adr16 & 0xff);
                            _cpu.ReadMemory(falseAdr);
                            PageOverrun = true;
                        }
                        break;
                    }

                case AddressMode.Absolute:
                    {
                        adr16 = (ushort)(pLO | (pHI << 8));
                        break;
                    }

                case AddressMode.AbsoluteX:
                    {
                        adr16 = (ushort)(pLO | (pHI << 8));

                        int page1 = adr16 >> 8;
                        adr16 += _cpu.X;
                        int page2 = adr16 >> 8;

                        if (page1 != page2)
                        {
                            int falseAdr = (page1 << 8) | (adr16 & 0xff);
                            _cpu.ReadMemory(falseAdr);
                            PageOverrun = true;
                        }
                        break;
                    }

                case AddressMode.AbsoluteY:
                    {
                        adr16 = (ushort)(pLO | (pHI << 8));

                        int page1 = adr16 >> 8;
                        adr16 += _cpu.Y;
                        int page2 = adr16 >> 8;

                        if (page1 != page2)
                        {
                            int falseAdr = (page1 << 8) | (adr16 & 0xff);
                            _cpu.ReadMemory(falseAdr);
                            PageOverrun = true;
                        }

                        break;
                    }

                case AddressMode.Indirect:
                    {
                        byte LOAdr = pLO;
                        byte HIAdr = pHI;

                        byte indLO = _cpu.ReadMemory(LOAdr | (HIAdr << 8));
                        LOAdr++;    //  <- simulate cpu-error for indirect accessing
                        byte indHI = _cpu.ReadMemory(LOAdr | (HIAdr << 8));

                        adr16 = (ushort)(indLO | (indHI << 8));
                        break;
                    }

            }
        }
    }

    public class CPUState
    {
        public byte A;
        public byte X;
        public byte Y;

        public byte Flags;

        public byte SP;
        public ushort PC;

        public override bool Equals(object obj)
        {
            CPUState e = obj as CPUState;
            if (e == null)
                return false;

            if (e.A != A) return false;
            if (e.X != X) return false;
            if (e.Y != Y) return false;
            if (e.Flags != Flags) return false;
            if (e.SP != SP) return false;
            if (e.PC != PC) return false;

            return base.Equals(obj);
        }
	}

    public class CPU_6502
    {
        //  Memory
        public MemoryBus MPU;

        public static OpcodeDescription[] Opcodes = new OpcodeDescription[256];

        public string Desc = "";
        public int ClockTicks = 0;
        public bool Trace = false;

        //  CPU Registers        
        byte A = 0;
        public byte X = 0;
        public byte Y = 0;

        public byte SP = 0;     //  stack pointer
        public ushort PC = 0;

        bool Flag_N = false; // negative flag(1 when result is negative)
        bool Flag_V = false; //  = overflow flag(1 on signed overflow)
        // bool # = unused (always 1)
        bool Flag_B = false; //  = break flag(1 when interupt was caused by a BRK)
        bool Flag_D = false; //  = decimal flag(1 when CPU in BCD mode)
        bool Flag_I = false; //  = IRQ flag(when 1, no interupts will occur (exceptions are IRQs forced by BRK and NMIs))
        bool Flag_Z = false; //  = zero flag(1 when all bits of a result are 0)
        bool Flag_C = false; //  = carry flag(1 on unsigned overflow)

        public bool IRQ = false;
        bool NMI = false;
        bool NMITriggered = false;

        public int OPCodesCounter = 0;
        public bool Stopped = false;

        //public int Ticks = 0;

        int[] OpcodeLength;

        int[] numOpCodeCalls = new int[256];

        public CPU_6502(string _desc, MemoryBus _pla)
        {
            Desc = _desc;
            MPU = _pla;

            //  
            OpcodeLength = new int[(int)AddressMode.other];
            OpcodeLength[(int)AddressMode.Implied] = 1;
            OpcodeLength[(int)AddressMode.Immediate] = 2;
            OpcodeLength[(int)AddressMode.ZeroPage] = 2;
            OpcodeLength[(int)AddressMode.ZeroPageX] = 2;
            OpcodeLength[(int)AddressMode.ZeroPageY] = 2;
            OpcodeLength[(int)AddressMode.IndZeroPageX] = 2;
            OpcodeLength[(int)AddressMode.IndZeroPageY] = 2;
            OpcodeLength[(int)AddressMode.Relative] = 2;
            OpcodeLength[(int)AddressMode.Absolute] = 3;
            OpcodeLength[(int)AddressMode.AbsoluteX] = 3;
            OpcodeLength[(int)AddressMode.AbsoluteY] = 3;
            OpcodeLength[(int)AddressMode.Indirect] = 3;

            //  opcodes
            AddLegal("BRK", 0x00, "Stack <- PC, PC <- ($fffe)", AddressMode.Implied, 7);
            AddLegal("ORA", 0x01, "A < -(A) V M", AddressMode.IndZeroPageX, 2);
            Add_NV__("JAM", 0x02, "locks up machine", AddressMode.Implied, 1);
            Add_NV__("SLO", 0x03, "M < -(M >> 1) + A + C", AddressMode.IndZeroPageX, 1);
            Add_NV__("NOP", 0x04, "no operation", AddressMode.ZeroPage, 1);
            AddLegal("ORA", 0x05, "A < -(A)V M", AddressMode.ZeroPage, 3);
            AddLegal("ASL", 0x06, "C < -A7, A < -(A) << 1", AddressMode.ZeroPage, 5);
            Add_NV__("SLO", 0x07, "M <- (M >> 1) + A + C", AddressMode.ZeroPage, 5);
            AddLegal("PHP", 0x08, "Stack < -(P)", AddressMode.Implied, 2);
            AddLegal("ORA", 0x09, "A <- (A) V M", AddressMode.Immediate, 2);
            AddLegal("ASL", 0x0A, "C <- A7, A <- (A) << 1", AddressMode.Implied, 2);
            Add_NV__("ANC", 0x0B, "A <- A & M, C=~A7", AddressMode.Immediate, 2);
            Add_NV__("NOP", 0x0C, "no operation", AddressMode.Absolute, 4);
            AddLegal("ORA", 0x0D, "A <- (A) V M", AddressMode.Absolute, 4);
            AddLegal("ASL", 0x0E, "C <- A7, A <- (A) << 1", AddressMode.Absolute, 6);
            AddLegal("SLO", 0x0F, "M <- (M >> 1) + A + C", AddressMode.Absolute, 6);
            AddLegal("BPL", 0x10, "if N=0, PC = PC + offset", AddressMode.Relative, 2);
            AddLegal("ORA", 0x11, "A <- (A) V M", AddressMode.IndZeroPageY, 5);
            Add_NV__("JAM", 0x12, "[locks up machine]", AddressMode.Implied, 1);
            Add_NV__("SLO", 0x13, "M <- (M >. 1) + A + C", AddressMode.IndZeroPageY, 8);
            Add_NV__("NOP", 0x14, "[no operation]", AddressMode.ZeroPageX, 4);
            AddLegal("ORA", 0x15, "A <- (A) V M", AddressMode.ZeroPageX, 4);
            AddLegal("ASL", 0x16, "C <- A7, A <- (A) << 1", AddressMode.ZeroPageX, 6);
            Add_NV__("SLO", 0x17, "M <- (M >> 1) + A + C", AddressMode.ZeroPageX, 6);
            AddLegal("CLC", 0x18, "C <- 0", AddressMode.Implied, 2);
            AddLegal("ORA", 0x19, "A <- (A) V M", AddressMode.AbsoluteY, 4);
            Add_NV__("NOP", 0x1a, "[no operation]", AddressMode.Implied, 2);
            Add_NV__("SLO", 0x1b, "M <- (M >> 1) + A + C", AddressMode.AbsoluteY, 7);
            Add_NV__("NOP", 0x1c, "[no operation]", AddressMode.AbsoluteX, 4);
            AddLegal("ORA", 0x1d, "A <- (A) V M", AddressMode.AbsoluteX, 4);
            AddLegal("ASL", 0x1e, "C <- A7, A <- (A) << 1", AddressMode.AbsoluteX, 7);
            Add_NV__("SLO", 0x1f, "M <- (M >> 1) + A + C", AddressMode.AbsoluteX, 7);
            AddLegal("JSR", 0x20, "Stack <- PC, PC <- Address", AddressMode.Absolute, 6);
            AddLegal("AND", 0x21, "A <- (A) & M", AddressMode.IndZeroPageX, 6);
            Add_NV__("JAM", 0x22, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("RLA", 0x23, "M <- (M << 1) & (A)      ", AddressMode.IndZeroPageX, 8);
            AddLegal("BIT", 0x24, "Z <- ~(A & M) N<-M7 V<-M6", AddressMode.ZeroPage, 3);
            AddLegal("AND", 0x25, "A <- (A) & M             ", AddressMode.ZeroPage, 3);
            AddLegal("ROL", 0x26, "C <- A7 & A <- A << 1 + C ", AddressMode.ZeroPage, 5);
            Add_NV__("RLA", 0x27, "M <- (M << 1) & (A)      ", AddressMode.ZeroPage, 5);
            AddLegal("PLP", 0x28, "A <- (Stack)              ", AddressMode.Implied, 4);
            AddLegal("AND", 0x29, "A <- (A) & M             ", AddressMode.Immediate, 2);
            AddLegal("ROL", 0x2A, "C <- A7 & A <- A << 1 + C ", AddressMode.Implied, 2);
            Add_NV__("ANC", 0x2B, "A <- A & M, C <- ~A7     ", AddressMode.Immediate, 2);
            AddLegal("BIT", 0x2C, "Z <- ~(A & M) N<-M7 V<-M6", AddressMode.Absolute, 4);
            AddLegal("AND", 0x2D, "A <- (A) & M             ", AddressMode.Absolute, 4);
            AddLegal("ROL", 0x2E, "C <- A7 & A <- A << 1 + C ", AddressMode.Absolute, 6);
            Add_NV__("RLA", 0x2F, "M <- (M << 1) & (A)      ", AddressMode.Absolute, 6);
            AddLegal("BMI", 0x30, "if N=1, PC = PC + offset  ", AddressMode.Relative, 2);
            AddLegal("AND", 0x31, "A <- (A) & M             ", AddressMode.IndZeroPageY, 5);
            Add_NV__("JAM", 0x32, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("RLA", 0x33, "M <- (M << 1) & (A)      ", AddressMode.IndZeroPageY, 8);
            Add_NV__("NOP", 0x34, "[no operation]            ", AddressMode.ZeroPageX, 4);
            AddLegal("AND", 0x35, "A <- (A) & M             ", AddressMode.ZeroPageX, 4);
            AddLegal("ROL", 0x36, "C <- A7 & A <- A << 1 + C ", AddressMode.ZeroPageX, 6);
            Add_NV__("RLA", 0x37, "M <- (M << 1) & (A)      ", AddressMode.ZeroPageX, 6);
            AddLegal("SEC", 0x38, "C <- 1                    ", AddressMode.Implied, 2);
            AddLegal("AND", 0x39, "A <- (A) & M             ", AddressMode.AbsoluteY, 4);
            Add_NV__("NOP", 0x3A, "[no operation]            ", AddressMode.Implied, 2);
            Add_NV__("RLA", 0x3B, "M <- (M << 1) & (A)      ", AddressMode.AbsoluteY, 7);
            Add_NV__("NOP", 0x3C, "[no operation]            ", AddressMode.AbsoluteX, 4);
            AddLegal("AND", 0x3D, "A <- (A) & M             ", AddressMode.AbsoluteX, 4);
            AddLegal("ROL", 0x3E, "C <- A7 & A <- A << 1 + C ", AddressMode.AbsoluteX, 7);
            Add_NV__("RLA", 0x3F, "M <- (M << 1) & (A)      ", AddressMode.AbsoluteX, 7);
            AddLegal("RTI", 0x40, "P <- (Stack), PC <-(Stack)", AddressMode.Implied, 6);
            AddLegal("EOR", 0x41, "A <- (A) XOR M            ", AddressMode.IndZeroPageX, 6);
            Add_NV__("JAM", 0x42, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("SRE", 0x43, "M <- (M >> 1) XOR A       ", AddressMode.IndZeroPageX, 8);
            Add_NV__("NOP", 0x44, "[no operation]            ", AddressMode.ZeroPage, 3);
            AddLegal("EOR", 0x45, "A <- (A) XOR M            ", AddressMode.ZeroPage, 3);
            AddLegal("LSR", 0x46, "C <- A0, A <- (A) >> 1    ", AddressMode.ZeroPage, 7); // According to AR Monitor and Grahams Table this should be LSR ZP instead of LSR $ffff,x
            Add_NV__("SRE", 0x47, "M <- (M >> 1) XOR A       ", AddressMode.ZeroPage, 5);
            AddLegal("PHA", 0x48, "Stack <- (A)              ", AddressMode.Implied, 3);
            AddLegal("EOR", 0x49, "A <- (A) XOR M            ", AddressMode.Immediate, 2);
            AddLegal("LSR", 0x4A, "C <- A0, A <- (A) >> 1    ", AddressMode.Implied, 2);
            Add_NV__("ASR", 0x4B, "A <- [(A & M) >> 1]      ", AddressMode.Immediate, 2);
            AddLegal("JMP", 0x4C, "PC <- Address             ", AddressMode.Absolute, 3);
            AddLegal("EOR", 0x4D, "A <- (A) XOR M            ", AddressMode.Absolute, 4);
            AddLegal("LSR", 0x4E, "C <- A0, A <- (A) >> 1    ", AddressMode.Absolute, 6);
            Add_NV__("SRE", 0x4F, "M <- (M >> 1) XOR A       ", AddressMode.Absolute, 6);
            AddLegal("BVC", 0x50, "if V=0, PC = PC + offset  ", AddressMode.Relative, 2);
            AddLegal("EOR", 0x51, "A <- (A) XOR M            ", AddressMode.IndZeroPageY, 5);
            Add_NV__("JAM", 0x52, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("SRE", 0x53, "M <- (M >> 1) XOR A       ", AddressMode.IndZeroPageY, 8);
            Add_NV__("NOP", 0x54, "[no operation]            ", AddressMode.ZeroPageX, 4);
            AddLegal("EOR", 0x55, "A <- (A) XOR M            ", AddressMode.ZeroPageX, 4);
            AddLegal("LSR", 0x56, "C <- A0, A <- (A) >> 1    ", AddressMode.ZeroPageX, 6);
            Add_NV__("SRE", 0x57, "M <- (M >> 1) XOR A       ", AddressMode.ZeroPageX, 6);
            AddLegal("CLI", 0x58, "I <- 0                    ", AddressMode.Implied, 2);
            AddLegal("EOR", 0x59, "A <- (A) XOR M            ", AddressMode.AbsoluteY, 4);
            Add_NV__("NOP", 0x5A, "[no operation]            ", AddressMode.Implied, 2);
            Add_NV__("SRE", 0x5B, "M <- (M >> 1) XOR A       ", AddressMode.AbsoluteY, 7);
            Add_NV__("NOP", 0x5C, "[no operation]            ", AddressMode.AbsoluteX, 4);
            AddLegal("EOR", 0x5D, "A <- (A) XOR M            ", AddressMode.AbsoluteX, 4);
            Add_NV__("SRE", 0x5F, "M <- (M >> 1) XOR A       ", AddressMode.AbsoluteX, 7);
            AddLegal("RTS", 0x60, "PC <- (Stack)             ", AddressMode.Implied, 6);
            AddLegal("ADC", 0x61, "A <- (A) + M + C          ", AddressMode.IndZeroPageX, 6);
            Add_NV__("JAM", 0x62, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("RRA", 0x63, "M <- (M >> 1) + (A) + C   ", AddressMode.IndZeroPageX, 8);
            Add_NV__("NOP", 0x64, "[no operation]            ", AddressMode.ZeroPage, 3);
            AddLegal("ADC", 0x65, "A <- (A) + M + C          ", AddressMode.ZeroPage, 3);
            AddLegal("ROR", 0x66, "C<-A0 & A<- (A7=C + A>>1) ", AddressMode.ZeroPage, 5);
            Add_NV__("RRA", 0x67, "M <- (M >> 1) + (A) + C   ", AddressMode.ZeroPage, 5);
            AddLegal("PLA", 0x68, "A <- (Stack)              ", AddressMode.Implied, 4);
            AddLegal("ADC", 0x69, "A <- (A) + M + C          ", AddressMode.Immediate, 2);
            AddLegal("ROR", 0x6A, "C<-A0 & A<- (A7=C + A>>1) ", AddressMode.Implied, 2);
            Add_NV__("ARR", 0x6B, "A <- [(A & M) >> 1]       ", AddressMode.Immediate, 2);
            AddLegal("JMP", 0x6C, "PC <- Address             ", AddressMode.Indirect, 5);
            AddLegal("ADC", 0x6D, "A <- (A) + M + C          ", AddressMode.Absolute, 4);
            AddLegal("ROR", 0x6E, "C<-A0 & A<- (A7=C + A>>1) ", AddressMode.Absolute, 6);
            Add_NV__("RRA", 0x6F, "M <- (M >> 1) + (A) + C   ", AddressMode.Absolute, 6);
            AddLegal("BVS", 0x70, "if V=1, PC = PC + offset  ", AddressMode.Relative, 2);
            AddLegal("ADC", 0x71, "A <- (A) + M + C          ", AddressMode.IndZeroPageY, 5);
            Add_NV__("JAM", 0x72, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("RRA", 0x73, "M <- (M >> 1) + (A) + C   ", AddressMode.IndZeroPageY, 8);
            Add_NV__("NOP", 0x74, "[no operation]            ", AddressMode.ZeroPageX, 4);
            AddLegal("ADC", 0x75, "A <- (A) + M + C          ", AddressMode.ZeroPageX, 4);
            AddLegal("ROR", 0x76, "C<-A0 & A<- (A7=C + A>>1) ", AddressMode.ZeroPageX, 6);
            Add_NV__("RRA", 0x77, "M <- (M >> 1) + (A) + C   ", AddressMode.ZeroPageX, 6);
            AddLegal("SEI", 0x78, "I <- 1                    ", AddressMode.Implied, 2);
            AddLegal("ADC", 0x79, "A <- (A) + M + C          ", AddressMode.AbsoluteY, 4);
            Add_NV__("NOP", 0x7A, "[no operation]            ", AddressMode.Implied, 2);
            Add_NV__("RRA", 0x7B, "M <- (M >> 1) + (A) + C   ", AddressMode.AbsoluteY, 7);
            Add_NV__("NOP", 0x7C, "[no operation]            ", AddressMode.AbsoluteX, 4);
            AddLegal("ADC", 0x7D, "A <- (A) + M + C          ", AddressMode.AbsoluteX, 4);
            AddLegal("ROR", 0x7E, "C<-A0 & A<- (A7=C + A>>1) ", AddressMode.AbsoluteX, 7);
            Add_NV__("RRA", 0x7F, "M <- (M >> 1) + (A) + C   ", AddressMode.AbsoluteX, 7);
            Add_NV__("NOP", 0x80, "[no operation]            ", AddressMode.Immediate, 2);
            AddLegal("STA", 0x81, "M <- (A)                  ", AddressMode.IndZeroPageX, 6);
            Add_NV__("NOP", 0x82, "[no operation]            ", AddressMode.Immediate, 2);
            Add_NV__("SAX", 0x83, "M <- (A) & (X)           ", AddressMode.IndZeroPageX, 6);
            AddLegal("STY", 0x84, "M <- (Y)                  ", AddressMode.ZeroPage, 3);
            AddLegal("STA", 0x85, "M <- (A)                  ", AddressMode.ZeroPage, 3);
            AddLegal("STX", 0x86, "M <- (X)                  ", AddressMode.ZeroPage, 3);
            Add_NV__("SAX", 0x87, "M <- (A) & (X)           ", AddressMode.ZeroPage, 3);
            AddLegal("DEY", 0x88, "Y <- (Y) - 1              ", AddressMode.Implied, 2);
            Add_NV__("NOP", 0x89, "[no operation]            ", AddressMode.Immediate, 2);
            AddLegal("TXA", 0x8A, "A <- (X)                  ", AddressMode.Implied, 2);
            Add_NV__("ANE", 0x8B, "M <-[(A) OR $EE] & (X)&(M)", AddressMode.Immediate, 2);
            AddLegal("STY", 0x8C, "M <- (Y)                  ", AddressMode.Absolute, 4);
            AddLegal("STA", 0x8D, "M <- (A)                  ", AddressMode.Absolute, 4);
            AddLegal("STX", 0x8E, "M <- (X)                  ", AddressMode.Absolute, 4);
            Add_NV__("SAX", 0x8F, "M <- (A) & (X)           ", AddressMode.Absolute, 4);
            AddLegal("BCC", 0x90, "if C=0, PC = PC + offset  ", AddressMode.Relative, 2);
            AddLegal("STA", 0x91, "M <- (A)                  ", AddressMode.IndZeroPageY, 6);
            Add_NV__("JAM", 0x92, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("SHA", 0x93, "M <- (A) & (X) & (PCH+1)", AddressMode.AbsoluteX, 6);
            AddLegal("STY", 0x94, "M <- (Y)                  ", AddressMode.ZeroPageX, 4);
            AddLegal("STA", 0x95, "M <- (A)                  ", AddressMode.ZeroPageX, 4);
            Add_NV__("SAX", 0x97, "M <- (A) & (X)           ", AddressMode.ZeroPageY, 4);
            AddLegal("STX", 0x96, "M <- (X)                  ", AddressMode.ZeroPageY, 4);
            AddLegal("TYA", 0x98, "A <- (Y)                  ", AddressMode.Implied, 2);
            AddLegal("STA", 0x99, "M <- (A)                  ", AddressMode.AbsoluteY, 5);
            AddLegal("TXS", 0x9A, "S <- (X)                  ", AddressMode.Implied, 2);
            Add_NV__("TAS", 0x9B, "X <- (A) & (X), S <- (X) ", AddressMode.AbsoluteY, 5);
            Add_NV__("SHY", 0x9C, "M <- (Y) & (PCH+1)       ", AddressMode.AbsoluteY, 5);
            AddLegal("STA", 0x9D, "M <- (A)                  ", AddressMode.AbsoluteX, 5);
            Add_NV__("SHX", 0x9E, "M <- (X) & (PCH+1)       ", AddressMode.AbsoluteX, 5);
            Add_NV__("SHA", 0x9F, "M <- (A) & (X) & (PCH+1)", AddressMode.AbsoluteY, 5);
            AddLegal("LDY", 0xA0, "Y <- M                    ", AddressMode.Immediate, 2);
            AddLegal("LDA", 0xA1, "A <- M                    ", AddressMode.IndZeroPageX, 6);
            AddLegal("LDX", 0xA2, "X <- M                    ", AddressMode.Immediate, 2);
            Add_NV__("LAX", 0xA3, "A <- M, X <- M            ", AddressMode.IndZeroPageX, 6);
            AddLegal("LDY", 0xA4, "Y <- M                    ", AddressMode.ZeroPage, 3);
            AddLegal("LDA", 0xA5, "A <- M                    ", AddressMode.ZeroPage, 3);
            AddLegal("LDX", 0xA6, "X <- M                    ", AddressMode.ZeroPage, 3);
            Add_NV__("LAX", 0xA7, "A <- M, X <- M            ", AddressMode.ZeroPage, 3);
            AddLegal("TAY", 0xA8, "Y <- (A)                  ", AddressMode.Implied, 2);
            AddLegal("LDA", 0xA9, "A <- M                    ", AddressMode.Immediate, 2);
            AddLegal("TAX", 0xAA, "X <- (A)                  ", AddressMode.Implied, 2);
            Add_NV__("LXA", 0xAB, "X04 <- (X04) & M04       ", AddressMode.Immediate, 2);
            AddLegal("LDY", 0xAC, "Y <- M                    ", AddressMode.Absolute, 4);
            AddLegal("LDA", 0xAD, "A <- M                    ", AddressMode.Absolute, 4);
            AddLegal("LDX", 0xAE, "X <- M                    ", AddressMode.Absolute, 4);
            Add_NV__("LAX", 0xAF, "A <- M, X <- M            ", AddressMode.Absolute, 4);
            AddLegal("BCS", 0xB0, "if C=1, PC = PC + offset  ", AddressMode.Relative, 2);
            AddLegal("LDA", 0xB1, "A <- M                    ", AddressMode.IndZeroPageY, 5);
            Add_NV__("JAM", 0xB2, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("LAX", 0xB3, "A <- M, X <- M            ", AddressMode.IndZeroPageY, 5);
            AddLegal("LDY", 0xB4, "Y <- M                    ", AddressMode.ZeroPageX, 4);
            AddLegal("LDA", 0xB5, "A <- M                    ", AddressMode.ZeroPageX, 4);
            AddLegal("LDX", 0xB6, "X <- M                    ", AddressMode.ZeroPageY, 4);
            Add_NV__("LAX", 0xB7, "A <- M, X <- M            ", AddressMode.ZeroPageY, 4);
            AddLegal("CLV", 0xB8, "V <- 0                    ", AddressMode.Implied, 2);
            AddLegal("LDA", 0xB9, "A <- M                    ", AddressMode.AbsoluteY, 4);
            AddLegal("TSX", 0xBA, "X <- (S)                  ", AddressMode.Implied, 2);
            Add_NV__("LAE", 0xBB, "X,S,A <- (S & M)         ", AddressMode.AbsoluteY, 4);
            AddLegal("LDY", 0xBC, "Y <- M                    ", AddressMode.AbsoluteX, 4);
            AddLegal("LDA", 0xBD, "A <- M                    ", AddressMode.AbsoluteX, 4);
            AddLegal("LDX", 0xBE, "X <- M                    ", AddressMode.AbsoluteY, 4);
            Add_NV__("LAX", 0xBF, "A <- M, X <- M            ", AddressMode.AbsoluteY, 4);
            AddLegal("CPY", 0xC0, "(Y - M) -> NZC            ", AddressMode.Immediate, 2);
            AddLegal("CMP", 0xC1, "(A - M) -> NZC            ", AddressMode.IndZeroPageX, 6);
            Add_NV__("NOP", 0xC2, "[no operation]            ", AddressMode.Immediate, 2);
            Add_NV__("DCP", 0xC3, "M <- (M)-1, (A-M) -> NZC  ", AddressMode.IndZeroPageX, 8);
            AddLegal("CPY", 0xC4, "(Y - M) -> NZC            ", AddressMode.ZeroPage, 3);
            AddLegal("CMP", 0xC5, "(A - M) -> NZC            ", AddressMode.ZeroPage, 3);
            AddLegal("DEC", 0xC6, "M <- (M) - 1              ", AddressMode.ZeroPage, 5);
            Add_NV__("DCP", 0xC7, "M <- (M)-1, (A-M) -> NZC  ", AddressMode.ZeroPage, 5);
            AddLegal("INY", 0xC8, "Y <- (Y) + 1              ", AddressMode.Implied, 2);
            AddLegal("CMP", 0xC9, "(A - M) -> NZC            ", AddressMode.Immediate, 2);
            AddLegal("DEX", 0xCA, "X <- (X) - 1              ", AddressMode.Implied, 2);
            Add_NV__("SBX", 0xCB, "X <- (X)&(A) - M         ", AddressMode.Immediate, 2);
            AddLegal("CPY", 0xCC, "(Y - M) -> NZC            ", AddressMode.Absolute, 4);
            AddLegal("CMP", 0xCD, "(A - M) -> NZC            ", AddressMode.Absolute, 4);
            AddLegal("DEC", 0xCE, "M <- (M) - 1              ", AddressMode.Absolute, 6);
            Add_NV__("DCP", 0xCF, "M <- (M)-1, (A-M) -> NZC  ", AddressMode.Absolute, 6);
            AddLegal("BNE", 0xD0, "if Z=0, PC = PC + offset  ", AddressMode.Relative, 2);
            AddLegal("CMP", 0xD1, "(A - M) -> NZC            ", AddressMode.IndZeroPageY, 5);
            Add_NV__("JAM", 0xD2, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("DCP", 0xD3, "M <- (M)-1, (A-M) -> NZC  ", AddressMode.IndZeroPageY, 8);
            Add_NV__("NOP", 0xD4, "[no operation]            ", AddressMode.ZeroPageX, 4);
            AddLegal("CMP", 0xD5, "(A - M) -> NZC            ", AddressMode.ZeroPageX, 4);
            AddLegal("DEC", 0xD6, "M <- (M) - 1              ", AddressMode.ZeroPageX, 6);
            Add_NV__("DCP", 0xD7, "M <- (M)-1, (A-M) -> NZC  ", AddressMode.ZeroPageX, 6);
            AddLegal("CLD", 0xD8, "D <- 0                    ", AddressMode.Implied, 2);
            AddLegal("CMP", 0xD9, "(A - M) -> NZC            ", AddressMode.AbsoluteY, 4);
            Add_NV__("NOP", 0xDA, "[no operation]            ", AddressMode.Implied, 2);
            Add_NV__("DCP", 0xDB, "M <- (M)-1, (A-M) -> NZC  ", AddressMode.AbsoluteY, 7);
            Add_NV__("NOP", 0xDC, "[no operation]            ", AddressMode.AbsoluteX, 4);
            AddLegal("CMP", 0xDD, "(A - M) -> NZC            ", AddressMode.AbsoluteX, 4);
            AddLegal("DEC", 0xDE, "M <- (M) - 1              ", AddressMode.AbsoluteX, 7);
            Add_NV__("DCP", 0xDF, "M <- (M)-1, (A-M) -> NZC  ", AddressMode.AbsoluteX, 7);
            AddLegal("CPX", 0xE0, "(X - M) -> NZC            ", AddressMode.Immediate, 2);
            AddLegal("SBC", 0xE1, "A <- (A) - M - ~C         ", AddressMode.IndZeroPageX, 6);
            Add_NV__("NOP", 0xE2, "[no operation]            ", AddressMode.Immediate, 2);
            Add_NV__("ISC", 0xE3, "M <- (M) - 1,A <- (A)-M-~C", AddressMode.IndZeroPageX, 8);
            AddLegal("CPX", 0xE4, "(X - M) -> NZC            ", AddressMode.ZeroPage, 3);
            AddLegal("SBC", 0xE5, "A <- (A) - M - ~C         ", AddressMode.ZeroPage, 3);
            AddLegal("INC", 0xE6, "M <- (M) + 1              ", AddressMode.ZeroPage, 5);
            Add_NV__("ISC", 0xE7, "M <- (M) - 1,A <- (A)-M-~C", AddressMode.ZeroPage, 5);
            AddLegal("INX", 0xE8, "X <- (X) +1               ", AddressMode.Implied, 2);
            AddLegal("SBC", 0xE9, "A <- (A) - M - ~C         ", AddressMode.Immediate, 2);
            AddLegal("NOP", 0xEA, "[no operation]            ", AddressMode.Implied, 2);
            Add_NV__("SBC", 0xEB, "A <- (A) - M - ~C         ", AddressMode.Immediate, 2);
            AddLegal("SBC", 0xED, "A <- (A) - M - ~C         ", AddressMode.Absolute, 4);
            AddLegal("CPX", 0xEC, "(X - M) -> NZC            ", AddressMode.Absolute, 4);
            AddLegal("INC", 0xEE, "M <- (M) + 1              ", AddressMode.Absolute, 6);
            Add_NV__("ISC", 0xEF, "M <- (M) - 1,A <- (A)-M-~C", AddressMode.Absolute, 6);
            AddLegal("BEQ", 0xF0, "if Z=1, PC = PC + offset  ", AddressMode.Relative, 2);
            AddLegal("SBC", 0xF1, "A <- (A) - M - ~C         ", AddressMode.IndZeroPageY, 5);
            Add_NV__("JAM", 0xF2, "[locks up machine]        ", AddressMode.Implied, 1);
            Add_NV__("ISC", 0xF3, "M <- (M) - 1,A <- (A)-M-~C", AddressMode.IndZeroPageY, 8);
            Add_NV__("NOP", 0xF4, "[no operation]            ", AddressMode.ZeroPageX, 4);
            AddLegal("SBC", 0xF5, "A <- (A) - M - ~C         ", AddressMode.ZeroPageX, 4);
            AddLegal("INC", 0xF6, "M <- (M) + 1              ", AddressMode.ZeroPageX, 6);
            Add_NV__("ISC", 0xF7, "M <- (M) - 1,A <- (A)-M-~C", AddressMode.ZeroPageX, 6);
            AddLegal("SED", 0xF8, "D <- 1                    ", AddressMode.Implied, 2);
            AddLegal("SBC", 0xF9, "A <- (A) - M - ~C         ", AddressMode.AbsoluteY, 4);
            Add_NV__("NOP", 0xFA, "[no operation]            ", AddressMode.Implied, 2);
            Add_NV__("ISC", 0xFB, "M <- (M) - 1,A <- (A)-M-~C", AddressMode.AbsoluteY, 7);
            Add_NV__("NOP", 0xFC, "[no operation]            ", AddressMode.AbsoluteX, 4);
            AddLegal("SBC", 0xFD, "A <- (A) - M - ~C         ", AddressMode.AbsoluteX, 4);
            AddLegal("INC", 0xFE, "M <- (M) + 1              ", AddressMode.AbsoluteX, 7);
            Add_NV__("ISC", 0xFF, "M <- (M) - 1,A <- (A)-M-~C", AddressMode.AbsoluteX, 7);
        }

        void AddLegal(string _name, byte _opcode, string _desc, AddressMode _mode, int _ticks)
        {
            int length = OpcodeLength[(int)_mode];
            OpcodeDescription op = new OpcodeDescription(_opcode, _name, _desc, _mode, length, _ticks);
            Opcodes[op.Opcode] = op;
        }

        void Add_NV__(string _name, byte _opcode, string _desc, AddressMode _mode, int _ticks)
        {
            int length = OpcodeLength[(int)_mode];
            OpcodeDescription op = new OpcodeDescription(_opcode, _name, _desc, _mode, length, _ticks);
            op.IsLegal = false;
            Opcodes[op.Opcode] = op;
        }

        public OpcodeDescription Decode(byte _opcode)
        {
            return Opcodes[_opcode];
        }

        public DecodedInstruction DecodeInstruction(ushort _adr)
        {
            DecodedInstruction ex = new DecodedInstruction();
            ex.Decode(this, _adr);
            return ex;
        }

        public CPUState GetState()
        {
            CPUState state = new CPUState();

            state.A = A;
            state.X = X;
            state.Y = Y;
            state.Flags = GetFlags();
            state.PC = PC;
            state.SP = SP;

            return state;
        }

        public void Reset()
        {
            Stopped = false;

            SetFlags(0);
            SP = 0xff;

            ClockTicks = 0;
            OPCodesCounter = 0;

            //  do a RESET
            MPU.Reset();

            byte PCL = ReadMemory(Vector_RESET);
            byte PCH = ReadMemory(Vector_RESET+1);
            PC = (ushort)((PCH << 8) | (PCL));
        }

        public byte GetFlags()
        {
            byte p = 0;

            if (Flag_C) p += 1;
            if (Flag_Z) p += 2;
            if (Flag_I) p += 4;
            if (Flag_D) p += 8;
            if (Flag_B) p += 16;
            p += 32;
            if (Flag_V) p += 64;
            if (Flag_N) p += 128;

            return p;
        }

        void SetFlags(byte p)
        {
            Flag_C = ((p & 1) > 0);
            Flag_Z = ((p & 2) > 0);
            Flag_I = ((p & 4) > 0);
            Flag_D = ((p & 8) > 0);
            Flag_B = ((p & 16) > 0);
            Flag_V = ((p & 64) > 0);
            Flag_N = ((p & 128) > 0);
        }

        //  hardware vectors
        ushort Vector_NMI = 0xFFFA;     //  = NMI vector(NMI= not maskable interupts)
        ushort Vector_RESET = 0xFFFC;   //  = Reset vector

        public byte ReadMemory(int _address)
        {
            byte data8 = MPU.Read(_address, false);
            return data8;
        }

        public byte PeekMemory(int _address)
        {
            byte data8 = MPU.Read(_address, true);
            return data8;
        }

        public void WriteMemory(int _address, byte _data)
        {
            byte old = MPU.Read(_address, true);
            MPU.Write(_address, _data, false);
        }

        byte readPC()
        {
            byte data = ReadMemory(PC);
            PC++;
            return data;
        }

        byte HI(ushort p16)
        {
            return ((byte)(p16 >> 8));
        }

        byte LO(ushort p16)
        {
            return ((byte)(p16 & 0xff));
        }

        void Push16(ushort p16)
        {
            Push(HI(p16));
            Push(LO(p16));
        }

        ushort Pop16()
        {
            byte lo = Pop();
            byte hi = Pop();
            ushort data = (ushort)((hi << 8) | lo);

            return data;
        }

        void Push(byte _byte)
        {
            WriteMemory(0x0100 + SP, _byte);
            SP--;
        }

        byte Pop()
        {
            SP++;
            return ReadMemory(0x0100 + SP);
        }
        
        public void StreamTo(Stream _stream)
        {
            StreamHelpers.WriteInt16(_stream, PC);
            _stream.WriteByte(SP);
            _stream.WriteByte(A);
            _stream.WriteByte(X);
            _stream.WriteByte(Y);
            _stream.WriteByte(GetFlags());

            byte b0 = (byte)(ClockTicks & 0xff);
            _stream.WriteByte(b0);
            byte b1 = (byte)((ClockTicks >> 8) & 0xff);
            _stream.WriteByte(b1);
            byte b2 = (byte)((ClockTicks >> 16) & 0xff);
            _stream.WriteByte(b2);
            byte b3 = (byte)((ClockTicks >> 24) & 0xff);
            _stream.WriteByte(b3);

            MPU.StreamTo(_stream);
        }

        public void StreamFrom(Stream _stream)
        {
            PC = (ushort)StreamHelpers.ReadInt16(_stream);
            
            SP = (byte)_stream.ReadByte();
            A = (byte)_stream.ReadByte();
            X = (byte)_stream.ReadByte();
            Y = (byte)_stream.ReadByte();
            SetFlags((byte)_stream.ReadByte());
            ClockTicks = (_stream.ReadByte()) | (_stream.ReadByte() << 8) | (_stream.ReadByte() << 16) | (_stream.ReadByte() << 24);

            MPU.StreamFrom(_stream);
        }

        public void SetNMI(bool _state)
        {
            //  NMI's only trigger an edge
            if (_state = NMI)
                return;
            NMI = _state;

            if (NMI == true)
                NMITriggered = true;
        }

        public int Process()
        {
            if (Stopped)
                return 1;

            ushort xPC = PC;

            if (NMITriggered)
            {
                NMITriggered = false;

                Push16(PC);

                byte flags = GetFlags();
                Push(flags);

                // Flag_I = true;
                
                byte PLO = ReadMemory(Vector_NMI);
                byte PHI = ReadMemory(Vector_NMI + 1);

                PC = (ushort)(PLO | (PHI << 8));
            }

            if ((!Flag_I) & (IRQ))
            {
                Push16(PC);

                byte flags = GetFlags();
                Push(flags);

                Flag_I = true;

                byte PLO = ReadMemory(0xfffe);
                byte PHI = ReadMemory(0xffff);
                PC = (ushort)(PLO | (PHI << 8));
            }

            //  decode opcode
            DecodedInstruction ex = DecodeInstruction(PC);
            OPCodesCounter++;

            int ticks = ex.OpcodeDesc.Ticks;
            if (ex.PageOverrun) ticks++;

            PC += (ushort)(ex.OpcodeDesc.Length);

            ushort adr16 = ex.adr16;
            byte imm8 = ex.p8;

            switch (ex.Opcode)
            {
                case 0x00:  //  BRK
                    {
                        PC += 2;              
                        Push16(PC);
                        
                        byte PCL = ReadMemory(0xfffe);
                        byte PCH = ReadMemory(0xffff);

                        Flag_B = true;
                        Flag_I = true;

                        PC = PCH;
                        PC <<= 8;
                        PC |= PCL;

                        // PC = (ushort)(PCL | (PCH << 8));
                        break;
                    }

                #region LDX

                //
                //  LDX
                //

                case 0xa2:  // LDX immediate
                    {
                        X = ex.p8;

                        Flag_Z = (X == 0);
                        Flag_N = ((X & 0x80) > 0);

                        break;
                    }

                case 0xa6:  // LDX (Z-Page)
                case 0xae:  // LDX (Absolute)
                case 0xb6:  // LDX (Z - Page,Y)
                case 0xbe:  // LDX (Absolute, Y)
                    {
                        X = ReadMemory(adr16);

                        Flag_Z = (X == 0);
                        Flag_N = ((X & 0x80) > 0);

                        break;
                    }

                #endregion

                #region STX

                case 0x86:  //  STX
                case 0x96:
                case 0x8e:
                    {
                        WriteMemory(adr16, X);                            
                        break;
                    }

                #endregion

                #region STY

                case 0x84:  //  STY
                case 0x94:
                case 0x8c:
                    {
                        WriteMemory(adr16, Y);
                        break;
                    }

                #endregion

                #region LDY

                //
                //  LDY
                //

                case 0xa0:  // LDY immediate
                    {
                        Y = imm8;

                        Flag_Z = (Y == 0);
                        Flag_N = ((Y & 0x80) > 0);

                        break;
                    }

                case 0xa4:
                case 0xb4:
                case 0xac:
                case 0xbc:
                    {
                        Y = ReadMemory(adr16);

                        Flag_Z = (Y == 0);
                        Flag_N = ((Y & 0x80) > 0);

                        break;
                    }

                #endregion

                #region LDA

                //
                //  LDA
                //

                case 0xa9:  // LDA immediate
                    {
                        A = imm8;

                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);

                        break;
                    }

                case 0xa1:
                case 0xa5:
                case 0xb1:
                case 0xb5:
                case 0xad:
                case 0xbd:
                case 0xb9:
                    {
                        A = ReadMemory(adr16);

                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);

                        break;
                    }

                #endregion

                #region STA

                //
                //  STA
                //

                case 0x85:  //
                case 0x95:  //
                case 0x81:  //
                case 0x91:  //
                case 0x8d:  //  e.g. STA $D020
                case 0x9d:  //
                case 0x99:  //
                    {
                        WriteMemory(adr16, A);
                        break;
                    }

                #endregion

                //
                //  BIT
                //

                //  checked: 15.04.2017
                case 0x24:  //  BIT zp / zero page
                //  checked: 15.04.2017
                case 0x2c:  //  BIT abs                             
                    {
                        byte data = ReadMemory(adr16);

                        Flag_N = ((data & BIT.B7) > 0);
                        Flag_V = ((data & BIT.B6) > 0);
                        Flag_Z = ((A & data) == 0);

                        break;
                    }

                case 0x60:  // RTS
                    {
                        PC = Pop16();
                        PC++;
                        break;
                    }

                #region JMP

                //
                // JMP
                //

                case 0x4c:  //  JMP (Absolute)
                case 0x6c:  //  JMP ((Indirect))
                    {
                        // PC = p16; // <- may be wrong
                        PC = adr16;
                        break;
                    }

                #endregion

                #region FLAGS SET/CLEAR

                case 0x18:  //  CLC
                    {
                        Flag_C = false;
                        break;
                    }

                case 0x38:  // SEC
                    {
                        Flag_C = true;
                        break;
                    }

                case 0xD8:  // CLD
                    {
                        Flag_D = false;
                        break;
                    }

                case 0xF8:  // SED
                    {
                        Flag_D = true;
                        break;
                    }

                case 0x58:  // CLI
                    {
                        Flag_I = false;
                        break;
                    }

                case 0x78:  // SEI
                    {                        
                        Flag_I = true;
                        break;
                    }

                case 0xB8:  // CLV
                    {
                        Flag_V = false;
                        break;
                    }

                #endregion

                case 0xEA:  // NOP
                case 0x7A:  // NOP (illegal!)
                case 0xDC:  // NOP (illegal!)
                case 0xFC:  // NOP (illegal!)
                    {
                        break;
                    }

                case 0x20:  // JSR
                    {                        
                        Push16((ushort)(xPC+2));
                        PC = adr16;
                        break;
                    }

                #region BRANCH

                //  checked: 15.04.2017
                case 0xd0:  // BNE: Brach on Not Equal
                    {
                        bool branch = (Flag_Z == false);

                        if (branch)
                        {
                            ticks++;
                            PC = adr16;
                        }

                        break;
                    }

                //  checked: 15.04.2017
                case 0x10: // BPL: Brank on Plus
                    {
                        bool branch = (Flag_N == false);

                        if (branch)
                        {
                            ticks++;
                            PC = adr16;
                        }

                        break;
                    }

                //  checked: 15.04.2017
                case 0x30: // BMI: Branch on MInus 
                    {
                        bool branch = (Flag_N == true);

                        if (branch)
                        {
                            ticks++;
                            PC = adr16;
                        }

                        break;
                    }

                //  checked: 15.04.2017
                case 0x50: // BVC
                    {
                        bool branch = (Flag_V == false);

                        if (branch)
                        {
                            ticks++;
                            PC = adr16;
                        }

                        break;
                    }

                //  checked: 15.04.2017
                case 0x70: // BVS
                    {
                        bool branch = (Flag_V == true);

                        if (branch)
                        {
                            ticks++;
                            PC = adr16;
                        }

                        break;
                    }

                //  checked: 15.04.2017
                case 0x90: // BCC: Bracnh on carry clear
                    {
                        bool branch = (Flag_C == false);

                        if (branch)
                        {
                            ticks++;
                            PC = adr16;
                        }
                        
                        break;
                    }

                //  checked: 15.04.2017
                case 0xB0: // BCS: Branch on Carry Set 
                    {
                        bool branch = (Flag_C == true);

                        if (branch)
                        {
                            ticks++;
                            PC = adr16;
                        }

                        break;
                    }

                //  checked: 15.04.2017
                case 0xf0:  // BEQ: Branch on EQual 
                    {
                        bool branch = (Flag_Z == true);

                        if (branch)
                        {
                            ticks++;
                            PC = adr16;
                        }

                        break;
                    }

                #endregion

                #region CMP

                //
                //  CMP
                //

                case 0xc9:  // CMP (Immediate)
                    {
                        int result = A - imm8;

                        Flag_C = (result >= 0);
                        Flag_Z = (result == 0);
                        Flag_N = (result < 0);
                        break;
                    }

                case 0xc1:  // CMP (Ind,X)
                case 0xc5:  // CMP (Z-Page)
                case 0xcd:  // CMP (absolute)
                case 0xd1:  // CMP ((Ind),Y)
                case 0xd5:  // CMP (Z-Page,X)
                case 0xd9:  // CMP (absolute,Y)
                case 0xdd:  // CMP (absolute,X)
                    {
                        int result = A - ReadMemory(adr16);

                        Flag_C = (result >= 0);
                        Flag_Z = (result == 0);
                        Flag_N = (result < 0);
                        break;
                    }

                #endregion

                #region CPX

                //
                //  CPX
                //

                case 0xe0:  // CPX (Immediate)
                    {
                        int result = X - imm8;

                        Flag_C = (result >= 0);
                        Flag_Z = (result == 0);
                        Flag_N = (result < 0);
                        break;
                    }

                case 0xe4:  // CPX (Ind,X)
                case 0xec:  // CPX (Z-Page)
                    {
                        int result = X - ReadMemory(adr16);

                        Flag_C = (result >= 0);
                        Flag_Z = (result == 0);
                        Flag_N = (result < 0);
                        break;
                    }

                #endregion

                #region CPY

                //
                //  CPY
                //

                case 0xc0:  // CPY (Immediate)
                    {
                        int result = Y - imm8;

                        Flag_C = (result >= 0);
                        Flag_Z = (result == 0);
                        Flag_N = (result < 0);
                        break;
                    }

                case 0xc4:  // CPY (Ind,X)
                case 0xcc:  // CPY (Z-Page)
                    {
                        int result = Y - ReadMemory(adr16);

                        Flag_C = (result >= 0);
                        Flag_Z = (result == 0);
                        Flag_N = (result < 0);
                        break;
                    }

                #endregion

                #region AND

                //
                // AND  Flags: N, Z
                //

                case 0x29: //   AND immediate
                    {
                        A &= imm8;
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                case 0x21:
                case 0x25:
                case 0x2d:
                case 0x31:
                case 0x35:
                case 0x39:
                case 0x3d:
                    {
                        A &= ReadMemory(adr16);
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                #endregion

                #region ADC

                //
                // ADC  Flags: N, V, Z, C
                //

                case 0x69: //   ADC immediate
                    {
                        byte oldA = A;
                        byte C = 0;
                        if (Flag_C) C = 1;

                        A += imm8;
                        A += C;

                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        Flag_C = (oldA + imm8 + C > 0x00ff);
                        Flag_V = ((A & 0x80) > 0) && ((oldA & 0x80) == 0);
                        break;
                    }

                case 0x65:
                case 0x75:
                case 0x61:
                case 0x71:
                case 0x6d:
                case 0x7d:
                case 0x79:
                    {
                        byte oldA = A;
                        byte C = 0;
                        if (Flag_C) C = 1;

                        byte p8 = ReadMemory(adr16);
                        A += p8;
                        A += C;

                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        Flag_C = (oldA + p8 + C > 0x00ff);
                        Flag_V = ((A & 0x80) > 0) && ((oldA & 0x80) == 0);
                        break;
                    }

                #endregion

                #region SBC

                //
                // SBC  Flags: N, V, Z, C
                //

                case 0xe9: //   SBC immediate
                    {
                        if (Flag_D)
                        {

                        }

                        byte oldA = A;

                        int iC = 0;
                        if (!Flag_C) iC = 1;

                        int iR = A - imm8 - iC;

                        A = (byte)iR;

                        Flag_C = (iR >= 0);
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        Flag_V = ((A & 0x80) == 0) && ((oldA & 0x80) > 0);

                        /*
                        ushort a16 = A;
                        ushort p = imm8;

                        a16 -= p;
                        if (!Flag_C)
                            a16--;

                        A = (byte)a16;
                        Flag_C = (a16 <= 0x00ff); // inverted borrow!
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        Flag_V = ((A & 0x80) == 0) && ((oldA & 0x80) > 0);
                        */
                        break;
                    }

                case 0xe5:
                case 0xf5:
                case 0xe1:
                case 0xf1:
                case 0xed:
                case 0xfd:
                case 0xf9:
                    {
                        if (Flag_D)
                        {
                        }

                        byte oldA = A;
                        byte param = ReadMemory(adr16);

                        int iC = 0;
                        if (!Flag_C) iC = 1;

                        int iR = A - param - iC;

                        A = (byte)iR;

                        Flag_C = (iR >= 0);
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        Flag_V = ((A & 0x80) == 0) && ((oldA & 0x80) > 0);

                        /*
                        byte oldA = A;
                        ushort a16 = A;
                        ushort p = ReadMemory(adr16);

                        a16 -= p;
                        if (!Flag_C)
                            a16--;

                        A = (byte)a16;
                        Flag_C = (a16 <= 0x00ff) ; // inverted borrow!
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        Flag_V = ((A & 0x80) == 0) && ((oldA & 0x80) > 0);
                        */
                        break;
                    }

                #endregion

                #region ORA

                case 0x09: // ORA immediate
                    {
                        A |= imm8;
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                case 0x05:
                case 0x15:
                case 0x01:
                case 0x11:
                case 0x0d:
                case 0x1d:
                case 0x19:
                    {
                        A |= ReadMemory(adr16);
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }
                #endregion

                #region EOR / XOR   

                //
                // EOR  Flags: N, Z
                //

                case 0x49: //   EOR immediate
                    {
                        A ^= imm8;
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                case 0x41:
                case 0x45:
                case 0x4d:
                case 0x51:
                case 0x55:
                case 0x59:
                case 0x5d:
                    {
                        A ^= ReadMemory(adr16);
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                #endregion

                #region INC

                //
                // INC  Flags: N, Z
                //

                case 0xe6:
                case 0xf6:
                case 0xee:
                case 0xfe:
                    {
                        byte mem = ReadMemory(adr16);
                        mem++;
                        WriteMemory(adr16, mem);

                        Flag_Z = (mem == 0);
                        Flag_N = ((mem & 0x80) > 0);
                        break;
                    }

                #endregion

                #region DEC

                //
                // DEC  Flags: N, Z
                //

                case 0xc6:
                case 0xd6:
                case 0xce:
                case 0xde:
                    {
                        byte mem = ReadMemory(adr16);
                        mem--;
                        WriteMemory(adr16, mem);

                        Flag_Z = (mem == 0);
                        Flag_N = ((mem & 0x80) > 0);
                        break;
                    }

                #endregion

                case 0xE8:  //  INX
                    {
                        X++;
                        Flag_Z = (X == 0);
                        Flag_N = ((X & 0x80) > 0);
                        break;
                    }

                case 0xc8:  //  INY
                    {
                        Y++;
                        Flag_Z = (Y == 0);
                        Flag_N = ((Y & 0x80) > 0);
                        break;
                    }

                case 0x88:  //DEY
                    {
                        Y--;
                        Flag_Z = (Y == 0);
                        Flag_N = ((Y & 0x80) > 0);
                        break;
                    }

                case 0xCA:  //DEX
                    {
                        X--;
                        Flag_Z = (X == 0);
                        Flag_N = ((X & 0x80) > 0);
                        break;
                    }



                case 0x40:  // RTI
                    {                        
                        byte flags = Pop();
                        SetFlags(flags);

                        PC = Pop16();

                        break;
                    }

                case 0x8a: // TXA
                    {
                        A = X;
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                case 0x98: // TYA
                    {
                        A = Y;
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                case 0xa8: // TAY
                    {
                        Y = A;
                        Flag_Z = (Y == 0);
                        Flag_N = ((Y & 0x80) > 0);
                        break;
                    }

                case 0xaa: // TAX
                    {
                        X = A;
                        Flag_Z = (X == 0);
                        Flag_N = ((X & 0x80) > 0);
                        break;
                    }

                case 0xBA: // TSX
                    {
                        X = SP;
                        Flag_Z = (X == 0);
                        Flag_N = ((X & 0x80) > 0);
                        break;
                    }

                case 0x9a:  //  TXS
                    {
                        SP = X;
                        break;
                    }

                case 0x08:  //  PHP      $08       Stack < -(P)(Implied)    1 / 3
                    {
                        Push(GetFlags());
                        break;
                    }

                case 0x28:  //  PLP
                    {
                        byte b = Pop();
                        SetFlags(b);
                        break;
                    }

                case 0x48:  //  PHA      $48       Stack <- (A)               (Implied)        1/3
                    {
                        Push(A);
                        break;
                    }

                case 0x68:  //  PLA
                    {
                        A = Pop();
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                //  ASL
                case 0x0a: // ASL implied
                    {
                        Flag_C = (A & 0x80) > 0;
                        A <<= 1;
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                case 0x06: // ASL 
                case 0x16:
                case 0x0e:
                case 0x1e:
                    {
                        byte mem = ReadMemory(adr16);

                        Flag_C = (mem & 0x80) > 0;
                        mem <<= 1;
                        Flag_Z = (mem == 0);
                        Flag_N = ((mem & 0x80) > 0);

                        WriteMemory(adr16, mem);
                        break;
                    }

                //  LSR
                case 0x4a: // LSR implied
                    {
                        Flag_C = ((A & 0x01) > 0);
                        A >>= 1;
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                case 0x46: // LSR
                case 0x56:
                case 0x4e:
                case 0x5e:
                    {
                        byte mem = ReadMemory(adr16);

                        Flag_C = ((mem & 0x01) > 0);
                        mem >>= 1;
                        Flag_Z = (mem == 0);
                        Flag_N = ((mem & 0x80) > 0);

                        WriteMemory(adr16, mem);
                        break;
                    }

                //  ROL
                case 0x2a: // ROL implied
                    {
                        bool oldC = Flag_C;
                        Flag_C = ((A & 0x80) > 0);

                        A <<= 1;
                        if (oldC) A |= 0x01;

                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                case 0x26: // ROL 
                case 0x36:
                case 0x2e:
                case 0x3e:
                    {
                        byte mem = ReadMemory(adr16);

                        bool oldC = Flag_C;
                        Flag_C = ((mem & 0x80) > 0);

                        mem <<= 1;
                        if (oldC) mem |= 0x01;

                        Flag_Z = (mem == 0);
                        Flag_N = ((mem & 0x80) > 0);

                        WriteMemory(adr16, mem);
                        break;
                    }

                //  ROR
                case 0x6a: // checked
                    {
                        bool oldC = Flag_C;
                        Flag_C = ((A & 0x01) > 0);
                        
                        A >>= 1;
                        if (oldC) A |= 0x80;

                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                case 0x66:  //  checked
                case 0x76:  //  checked                
                case 0x6e:  //  checked
                case 0x7e:  //  checked
                    {
                        byte mem = ReadMemory(adr16);

                        bool oldC = Flag_C;
                        Flag_C = ((mem & 0x01) > 0);

                        mem >>= 1;
                        if (oldC) mem |= 0x80;

                        Flag_Z = (mem == 0);
                        Flag_N = ((mem & 0x80) > 0);

                        WriteMemory(adr16, mem);
                        break;
                    }

                // illegal opcodes

                //  DCP
                case 0xCF:
                case 0xDF:
                case 0xDB:
                case 0xC7:
                case 0xD7:
                case 0xC3:
                case 0xD3:
                    {
                        byte mem8 = ReadMemory(adr16);
                        mem8--;
                        WriteMemory(adr16, mem8);

                        int result = A - mem8;

                        Flag_C = (result >= 0);
                        Flag_Z = (result == 0);
                        Flag_N = (result < 0);
                        break;
                    }

                //  LAX
                case 0xaf:
                case 0xbf:
                case 0xb7:
                case 0xa7:
                case 0xb3:
                case 0xa3:
                    {
                        A = ReadMemory(adr16);
                        X = A;

                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);

                        break;
                    }

                //  SAX
                case 0x83:
                case 0x87:
                case 0x8f:
                case 0x97:
                    {
                        byte val = (byte)(A & X);
                        WriteMemory(adr16, val);
                        break;
                    }

                //  ISB / ISC
                case 0xe7:
                case 0xf7:
                case 0xe3:
                case 0xf3:
                case 0xef:
                case 0xff:
                case 0xfb:
                    {
                        byte mem = ReadMemory(adr16);
                        mem++;
                        WriteMemory(adr16, mem);

                        byte oldA = A;

                        int iC = 0;
                        if (!Flag_C) iC = 1;

                        int iR = A - mem - iC;

                        A = (byte)iR;

                        Flag_C = (iR >= 0);
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        Flag_V = ((A & 0x80) == 0) && ((oldA & 0x80) > 0);

                        break;
                    }

                //  TAS
                case 0x9b:
                    {
                        byte AandX = A;
                        AandX &= X;

                        SP = AandX;
                        AandX &= (byte)(ex.p8_2 + 1);
                        WriteMemory(adr16, AandX);

                        break;
                    }

                //  XAA: TXA+AND
                case 0x8b:
                    {
                        A = X;

                        A &= imm8;
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                //  ARR: AND+ROR
                case 0x6b:
                    {
                        // AND
                        A &= imm8;

                        //  ROR
                        bool oldC = Flag_C;
                        Flag_C = ((A & 0x80) > 0);

                        A <<= 1;
                        if (oldC) A |= 0x01;

                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                //  SLO/ASO: ASL + ORA
                case 0x0f:
                case 0x1f:
                case 0x1b:
                case 0x07:
                case 0x17:
                case 0x03:
                case 0x13:
                    {
                        byte mem = ReadMemory(adr16);

                        Flag_C = (mem & 0x80) > 0;
                        mem <<= 1;

                        A |= mem;
                        Flag_Z = (A == 0);
                        Flag_N = ((A & 0x80) > 0);
                        break;
                    }

                //  JAM / HLT
                case 0x02:
                case 0x12:
                case 0x22:
                case 0x32:
                case 0x42:
                case 0x52:
                case 0x62:
                case 0x72:
                case 0x92:
                case 0xb2:
                case 0xd2:
                case 0xf2:
                    {
                        Stopped = true;
                        break;
                    }

                default:
                    {
                        //  invalid/unknown opcode
                        // TraceLine(string.Format("${0:X}", OPCode));
                        break;
                    }
            }

            ClockTicks += ticks;
            return ticks;
        }

		public Dictionary<string, object> GetStateDict()
		{
			Dictionary<string, object> dict = new Dictionary<string, object>();

			dict["A"] = A;
			dict["X"] = X;
			dict["Y"] = Y;
			dict["SP"] = SP;
			dict["PC"] = PC;
			dict["Flag.B"] = Flag_B;
			dict["Flag.C"] = Flag_C;
			dict["Flag.D"] = Flag_D;
			dict["Flag.I"] = Flag_I;
			dict["Flag.N"] = Flag_N;
			dict["Flag.V"] = Flag_V;
			dict["Flag.Z"] = Flag_Z;

			dict["IRQ"] = IRQ;
			dict["NMI"] = NMI;
			dict["NMI.Triggered"] = NMITriggered;

			return dict;
		}

	}
}
