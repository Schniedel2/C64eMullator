using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace C64Emulator.Chips
{
    /*
    http://codebase64.org/doku.php?id=base:cia

    Wiki: https://www.c64-wiki.de/wiki/CIA#CIA_2
    */

    public class HHMMSSTSClock
    {
        public int HH;
        public int MM;
        public int SS;
        public int TS;

        public HHMMSSTSClock()
        {
            FromInt(0);
        }

        public HHMMSSTSClock(int _tenthSecs)
        {
            FromInt(_tenthSecs);
        }

        public HHMMSSTSClock(HHMMSSTSClock _from)
        {
            HH = _from.HH;
            MM = _from.MM;
            SS = _from.SS;
            TS = _from.TS;
        }        

        public int ToInt()
        {
            return TS + 10 * SS + 600 * MM + 3600 * HH;
        }

        public void FromInt(int _tenthSecs)
        {
            TS = _tenthSecs % 10;
            _tenthSecs /= 10;

            SS = _tenthSecs % 60;
            _tenthSecs /= 60;

            MM = _tenthSecs % 60;
            _tenthSecs /= 60;

            HH = _tenthSecs;
        }
    }


    public class CIA_6526 : Chip
    {

        internal byte PRA = 0;
        internal byte PRB = 0;
        internal byte DDRA = 0;
        internal byte DDRB = 0;
        internal byte SDR = 0;
        internal byte CRA = 0;
        internal byte CRB = 0;

        int TimerA = 0;
        byte TimerA_LatchLO = 0;
        byte TimerA_LatchHI = 0;        

        int TimerB = 0;
        byte TimerB_LatchLO = 0;
        byte TimerB_LatchHI = 0;

        byte INT_DATA = 0;
        byte INT_MASK = 0;

        bool FlagPIN = false;

        HHMMSSTSClock AlarmClock = new HHMMSSTSClock(0);
        HHMMSSTSClock TimeOfDay = new HHMMSSTSClock();

        public CIA_6526(int _baseAddress) : base(_baseAddress, _baseAddress + 0xff)
        {
        }

        public override void Reset()
        {
            PRA = 0;
            PRB = 0;

            INT_DATA = 0;
            INT_MASK = 0;

            DDRA = 0;
            DDRB = 0;
            SDR = 0;
            CRA = 0;
            CRB = 0;

            TimerA = 0;
            TimerA_LatchLO = 0;
            TimerA_LatchHI = 0;

            TimerB = 0;
            TimerB_LatchLO = 0;
            TimerB_LatchHI = 0;
            FlagPIN = false;

            base.Reset();
        }

        public override void StreamTo(Stream _stream)
        {
            _stream.WriteByte(65);
            _stream.WriteByte(26);

            _stream.WriteByte(PRA);
            _stream.WriteByte(PRB);
            _stream.WriteByte(DDRA);
            _stream.WriteByte(DDRB);
            _stream.WriteByte(SDR);
            _stream.WriteByte(CRA);
            _stream.WriteByte(CRB);

            StreamHelpers.WriteInt16(_stream, TimerA);
            _stream.WriteByte(TimerA_LatchLO);
            _stream.WriteByte(TimerA_LatchHI);

            StreamHelpers.WriteInt16(_stream, TimerB);
            _stream.WriteByte(TimerB_LatchLO);
            _stream.WriteByte(TimerB_LatchHI);

            _stream.WriteByte(INT_DATA);
            _stream.WriteByte(INT_MASK);
            StreamHelpers.WriteBool(_stream, FlagPIN);
        }

        public override void StreamFrom(Stream _stream)
        {
            byte b65 = (byte)_stream.ReadByte();
            byte b26 = (byte)_stream.ReadByte();

            PRA = (byte)_stream.ReadByte();
            PRB = (byte)_stream.ReadByte();
            DDRA = (byte)_stream.ReadByte();
            DDRB = (byte)_stream.ReadByte();
            SDR = (byte)_stream.ReadByte();
            CRA = (byte)_stream.ReadByte();
            CRB = (byte)_stream.ReadByte();

            TimerA = StreamHelpers.ReadInt16(_stream);
            TimerA_LatchLO = (byte)_stream.ReadByte();
            TimerA_LatchHI = (byte)_stream.ReadByte();

            TimerB = StreamHelpers.ReadInt16(_stream);
            TimerB_LatchLO = (byte)_stream.ReadByte();
            TimerB_LatchHI = (byte)_stream.ReadByte();

            INT_DATA = (byte)_stream.ReadByte();
            INT_MASK = (byte)_stream.ReadByte();

            FlagPIN = StreamHelpers.ReadBool(_stream);
        }

        public void SetFlagPIN(bool _val)
        {
            if (FlagPIN != _val)
            {
                if (_val == false)
                {
                    INT_DATA |= 16; //  negative flank of Flag-Pin 
                }
            }
            FlagPIN = _val;
        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {            
            int adr = _fullAddress - BaseAddress;
            adr %= 16;

            RAM[adr] = _val;

            switch (adr)
            {
                case 0:
                    {
                        PRA = _val;
                        return;
                    }

                case 1:
                    {
                        PRB = _val;
                        return;
                    }

                case 2:
                    {
                        DDRA = _val;
                        return;
                    }

                case 3:
                    {
                        DDRB = _val;
                        return;
                    }

                case 4:
                    {
                        TimerA_LatchLO = _val;
                        return;
                    }

                case 5:
                    {
                        TimerA_LatchHI = _val;
                        return;
                    }

                case 6:
                    {
                        TimerB_LatchLO = _val;
                        return;
                    }

                case 7:
                    {
                        TimerB_LatchHI = _val;
                        return;
                    }

                case 8: // TimeOfDay 10/sec
                    {
                        if ((CRB & 0x80) > 0)
                            AlarmClock.TS = (_val & 0x0f);
                        else
                            TimeOfDay.TS = (_val & 0x0f);
                        return;
                    }

                case 9: // TimeOfDay sec
                    {
                        int digit1 = (_val & 0x0f);
                        int digit10 = ((_val >> 4) & 0x08);
                        int num = digit1 + 10 * digit10;

                        if ((CRB & 0x80) > 0)
                            AlarmClock.SS = num;
                        else
                            TimeOfDay.SS = num;

                        return;
                    }

                case 10: // TimeOfDay Minutes
                    {
                        int digit1 = (_val & 0x0f);
                        int digit10 = ((_val >> 4) & 0x08);
                        int num = digit1 + 10 * digit10;

                        if ((CRB & 0x80) > 0)
                            AlarmClock.MM = num;
                        else
                            TimeOfDay.MM = num;

                        return;
                    }

                case 11: // TimeOfDay hours
                    {
                        int digit1 = (_val & 0x0f);
                        int digit10 = ((_val >> 4) & 0x08);
                        int num = digit1 + 10 * digit10;

                        if ((_val & 0x80) > 0)
                            num += 12;

                        if ((CRB & 0x80) > 0)
                            AlarmClock.MM = num;
                        else
                            TimeOfDay.MM = num;

                        // todo: Schreiben in dieses Register stoppt TOD, bis Register 8 (TOD 10THS) geschrieben wird.
                        return;
                    }

                case 12:
                    {
                        SDR = _val;
                        return;
                    }

                case 13:    //  ICR
                    {
                        /*
                            Schreiben: (Bit 0..4 = INT MASK, Interruptmaske)
                            Bit 0: 1 = Interruptfreigabe für Timer A Unterlauf.
                            Bit 1: 1 = Interruptfreigabe für Timer B Unterlauf.
                            Bit 2: 1 = Interruptfreigabe für Uhrzeit-Alarmzeit-Übereinstimmung.
                            Bit 3: 1 = Interruptfreigabe für das Ende der Übertragung eines kompletten Bytes über das serielle Schieberegister.
                            Bit 4: 1 = Interruptfreigabe für das Erkennen einer negativen Flanke am FLAG-Pin.
                            Bit 5..6: unbenutzt
                            Bit 7: Quellbit: 0 = jedes gesetzte Bits 0..4 löscht das entsprechende Masken-Bit. 1 = jedes gesetzte Bits 0..4 setzt das entsprechende Masken-Bit. Gelöschte Bits 0..4 lassen die Maske also in jedem Fall unverändert.
                        */

                        if (_internal)
                        {
                            INT_MASK = (byte)(_val & 0x1f);
                            return;
                        }

                        bool qb = ((_val & 0x80) > 0);

                        _val &= 0x1f;

                        if (qb)
                        {
                            // 1 = jedes gesetzte Bits 0..4 setzt das entsprechende Masken-Bit
                            if ((_val & 0x01) > 0) INT_MASK |= 0x01;
                            if ((_val & 0x02) > 0) INT_MASK |= 0x02;
                            if ((_val & 0x04) > 0) INT_MASK |= 0x04;
                            if ((_val & 0x08) > 0) INT_MASK |= 0x08;
                            if ((_val & 0x10) > 0) INT_MASK |= 0x10;
                        }
                        else
                        {
                            // 0 = jedes gesetzte Bits 0..4 löscht das entsprechende Masken-Bit                            

                            if ((_val & 0x01) > 0) INT_MASK &= 0xfe;
                            if ((_val & 0x02) > 0) INT_MASK &= 0xfd;
                            if ((_val & 0x04) > 0) INT_MASK &= 0xfb;
                            if ((_val & 0x08) > 0) INT_MASK &= 0xf7;
                            if ((_val & 0x10) > 0) INT_MASK &= 0xef;
                        }
                        return;
                    }

                case 14:    // CRA
                    {
                        CRA = _val;

                        if ((CRA & BIT.B4) > 0) // Bit 4: load latch into timer
                        {
                            TimerA = (TimerA_LatchLO | (TimerA_LatchLO << 8));
                            // CRA &= (0xef);
                        }

                        //  bit 5:
                        if ((CRA & BIT.B5) > 0)
                        {
                            // todo: special timing!
                        }

                        //  bit 6
                        if ((CRA & BIT.B6) > 0)
                        {
                            // todo: schiebe-register
                        }

                        return;
                    }

                case 15:    // CRB
                    {
                        CRB = _val;

                        if ((CRB & 0x10) > 0) // Bit 4: load latch into timer
                        {
                            TimerB = (TimerB_LatchLO | (TimerB_LatchLO << 8));
                            // CRB &= (0xef);
                        }

                        //  bit 5+6:
                        if (((CRA & BIT.B5) > 0) || ((CRA & BIT.B6) > 0))
                        {
                            // todo: special timing!
                        }

                        return;
                    }
            }
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            int adr = _fullAddress - BaseAddress;
            adr %= 16;

            switch (adr)
            {

                case 0:
                    {
                        return PRA;
                    }

                case 1:
                    {
                        return PRB;
                    }

                case 2:
                    {
                        return DDRA;
                    }

                case 3:
                    {
                        return DDRB;
                    }

                case 4:
                    {
                        return (byte)(TimerA % 256);
                    }

                case 5:
                    {
                        return (byte)(TimerA / 256);
                    }

                case 6:
                    {
                        return (byte)(TimerB % 256);
                    }

                case 7:
                    {
                        return (byte)(TimerB / 256);
                    }

                case 8:
                    {
                        byte data = (byte)(TimeOfDay.TS);
                        return data;
                    }

                case 9:
                    {
                        byte data1 = (byte)(TimeOfDay.SS % 10);
                        byte data10 = (byte)(TimeOfDay.SS / 10);

                        return (byte)(data10 * 10 + data1);
                    }

                case 10:
                    {
                        byte data1 = (byte)(TimeOfDay.MM % 10);
                        byte data10 = (byte)(TimeOfDay.MM / 10);

                        return (byte)(data10 * 10 + data1);
                    }

                case 11:
                    {
                        int HH = TimeOfDay.HH % 12;

                        byte data1 = (byte)(HH % 10);
                        byte data10 = (byte)(HH / 10);

                        byte data = (byte)(data10 * 10 + data1);
                        if (TimeOfDay.HH >= 12)
                            data |= 0x80;

                        return data;
                    }

                case 12:
                    return SDR;

                case 13:    //  ICR
                    {
                        byte data = INT_DATA;
                        if ((INT_DATA & INT_MASK) > 0)
                        {
                            //  Bit 7: 1 = IRQ Ein Interrupt ist aufgetreten
                            data |= 0x80;
                        }

                        if (!_internal)
                            INT_DATA = 0;
                        return data;
                    }

                case 14:
                    return CRA;

                case 15:
                    return CRB;

            }

            return 0;
        }

        public byte ICR
        {
            get
            {
                return Read(BaseAddress + 0x0d, true);
            }
        }

        bool Clear_PRB6_NextTick = false;
        bool Clear_PRB7_NextTick = false;

        public bool ProcessTimerA(int _ticks)
        {
            if (Clear_PRB6_NextTick)
            {
                PRB &= 0xBF;
                Clear_PRB6_NextTick = false;
            }

            //    Bit 0: 0 = Stop Timer; 1 = Start Timer
            if ((CRA & 0x01) == 0)
                return false;

            // todo: check timing/synch
            TimerA -= _ticks;
            if (TimerA >= 0)
                return false;

            //
            //  timer underrun!
            //

            // Bit 1: 1 = Zeigt einen Timer Unterlauf an Port B in Bit 6 an
            if ((CRA & 0x01) > 0)
            {
                    
                if ((CRA & 0x02) == 0)
                {
                    // Bit 2: 0 = Bei Timer Unterlauf wird an Port B das Bit 6 invertiert
                    PRB ^= 0x40;
                }
                    
                if ((CRA & 0x02) > 0)
                {
                    // Bit 2: 1 = Bei Timer - Unterlauf wird an Port B das Bit 6 für einen Systemtaktzyklus High
                    PRB |= 0x40;
                    Clear_PRB6_NextTick = true;
                }

            }

            // Bit 3: 
            if ((CRA & BIT.B3) == 0)
            {
                //  0 = Timer - Neustart nach Unterlauf (Latch wird neu geladen)
                TimerA += (TimerA_LatchLO | (TimerA_LatchHI << 8));
                if (TimerA <= 0)
                    TimerA = (TimerA_LatchLO | (TimerA_LatchHI << 8));
            }
            else
            {
                // 1 = Timer stoppt nach Unterlauf
                CRA &= BIT.NOT0;
                TimerA = 0;
            }

            return true;
        }

        public bool ProcessTimerB(int _ticks, bool underrunTimerA)
        {
            if (Clear_PRB7_NextTick)
            {
                PRB &= 0x7F;
                Clear_PRB7_NextTick = false;
            }

            //    Bit 0: 0 = Stop Timer; 1 = Start Timer
            if ((CRB & 0x01) == 0)
                return false;

            // todo: check timing/synch
            TimerB -= _ticks;
            if (TimerB >= 0)
                return false;

            //
            //  timer underrun!
            //

            // Bit 1: 1 = Zeigt einen Timer Unterlauf an Port B in Bit 7 an
            if ((CRB & 0x01) > 0)
            {

                if ((CRB & 0x02) == 0)
                {
                    // Bit 2: 0 = Bei Timer Unterlauf wird an Port B das Bit 7 invertiert
                    PRB ^= 0x80;
                }

                if ((CRB & 0x02) > 0)
                {
                    // Bit 2: 1 = Bei Timer - Unterlauf wird an Port B das Bit 7 für einen Systemtaktzyklus High
                    PRB |= 0x80;
                    Clear_PRB7_NextTick = true;
                }

            }

            // Bit 3: 
            if ((CRB & 0x08) == 0)
            {
                //  0 = Timer - Neustart nach Unterlauf (Latch wird neu geladen)
                TimerB += (TimerB_LatchLO | (TimerB_LatchHI << 8));
                if (TimerB <= 0)
                    TimerB = (TimerB_LatchLO | (TimerB_LatchHI << 8));
            }
            else
            {
                // 1 = Timer stoppt nach Unterlauf
                CRB &= 0xfe;
                TimerB = 0;
            }

            return true;
        }

        public bool HasIRQ()
        {
            return ((INT_DATA & INT_MASK) > 0);
        }


        public override void Process(int _ticks)
        {
            bool underrunA = ProcessTimerA(_ticks);
            bool underrunB = ProcessTimerB(_ticks, underrunA);

            if (underrunA)
                INT_DATA |= 0x01;

            if (underrunB)
                INT_DATA |= 0x02;

            if ((TimeOfDay.HH == AlarmClock.HH) &&
                (TimeOfDay.MM == AlarmClock.MM) &&
                (TimeOfDay.SS == AlarmClock.SS) &&
                (TimeOfDay.TS == AlarmClock.TS))
            {
                INT_DATA |= 0x04;
            }

        }
    }   

}
