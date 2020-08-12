using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace C64Emulator.Devices
{
    public class Datasette : Device
    {
        public virtual double GetTapeCounter()
        {
            return 0;
        }

        public virtual double GetTapeMaxCounter()
        {
            return 0;
        }

        public virtual void InsertTape(Stream _stream)
        {

        }

        public virtual bool Get_D4_READ()
        {
            return true;
        }

        public virtual bool Get_F6_SENSE()
        {
            return false;
        }

        public virtual void Set_E5_WRITE(bool _bitValue)
        {
        }

        public virtual void Set_C3_MOTOR(bool _on)
        {
        }

        public virtual void PressStop()
        {

        }

        public virtual void PressPlay()
        {

        }

        public virtual void PressRecord()
        {

        }

        public virtual void PressFastForward()
        {

        }

        public virtual void PressFastRewind()
        {   }

        public virtual void Eject()
        {   }
    }

    public class TapeFile
    {

    }

    public class TapeFileTAP : TapeFile
    {

    }

    public class TapeFileT64 : TapeFile
    {

    }

    public class DatasetteTAP : Datasette
    {
        // https://www.c64-wiki.de/wiki/Kassettenport

        //  keys / buttons
        bool PlayPressed = false;
        bool RecordPressed = false;
        bool FastForwardPressed = false;
        bool FastRewindPressed = false;

        //  tape data
        bool InputSignal = false;
        int inputPulseDuration = 0;
        int inputPulseLowUntil = 0;

        bool OutputSignal = false;
        int outputPuleDuration = 0;
        int lastOutputPulseDuration = 0;
        int MotorOnDelay = 0;

        bool MotorOn = false;

        List<int> TapeRAWData = null;
        List<int> TapeHeader = null;
        int FilePos = 0;
        int FileLen = 0;

        long Tick = 0;
        string Filename;
        bool DataModified = false;

        public DatasetteTAP()
        {
            //InsertTapeTAP("c:\\c64files\\Ghosts_n_Goblins.tap");
            //InsertTapeTAP("d:\\c64files\\Super Dogfight.tap");

            OutputSignal = false;
            InputSignal = true;

            PressStop();
        }

        public override void PressStop()
        {
            PlayPressed = false;
            RecordPressed = false;
            FastForwardPressed = false;
            FastRewindPressed = false;

            lastOutputPulseDuration = 0;
            outputPuleDuration = 0;
            inputPulseDuration = 0;
        }

        public override void PressPlay()
        {
            PressStop();
            PlayPressed = true;
        }

        public override void PressRecord()
        {
            PressStop();
            RecordPressed = true;
            PlayPressed = true;
        }

        public override void InsertTape(Stream _stream)
        {
            Eject();
            InsertTapeTAP(_stream);
        }

        public void InsertTapeTAP(Stream _stream)
        {
            DataModified = false;

            TapeHeader = new List<int>();
            TapeRAWData = new List<int>();

            for (int i=0; i<0x14; i++)
            {
                int b = _stream.ReadByte();
                TapeHeader.Add(b);
            }

            while (_stream.Position < _stream.Length)
            {
                int b = _stream.ReadByte();
                TapeRAWData.Add(b);
            }

            FilePos = 0;
            FileLen = (TapeHeader[0x10]) | (TapeHeader[0x11] << 8) | (TapeHeader[0x12] << 16) | (TapeHeader[0x13] << 24);

            inputPulseDuration = 0;
        }

        public override void Eject()
        {
            if (DataModified)
            {
                // SaveAsTAP(Filename + ".sav");
                DataModified = false;
            }
        }

        /*
        public void SaveAsTAP(string _filename)
        {
            if (File.Exists(_filename))
                File.Delete(_filename);

            FileLen = TapeRAWData.Count();

            TapeHeader[0x10] = (FileLen & 0xff);
            TapeHeader[0x11] = ((FileLen >> 8) & 0xff);
            TapeHeader[0x12] = ((FileLen >> 16) & 0xff);
            TapeHeader[0x13] = ((FileLen >> 24) & 0xff);

            FileStream f = File.OpenWrite(_filename);

            for (int i = 0; i < TapeHeader.Count; i++)
                f.WriteByte((byte)TapeHeader[i]);

            for (int i = 0; i < TapeRAWData.Count; i++)
                f.WriteByte((byte)TapeRAWData[i]);

        }
        */

        public override void Set_E5_WRITE(bool _bitValue)
        {
            if (OutputSignal != _bitValue)
            {
                lastOutputPulseDuration = outputPuleDuration;
                outputPuleDuration = 0;
            }
            OutputSignal = _bitValue;
        }

        public override void Set_C3_MOTOR(bool _on)
        {
            if (MotorOn != _on)
            {
                MotorOn = _on;

                if (_on)
                    MotorOnDelay = 0x0ffff;

                if (!_on)
                {
                    //  finished loading (?)
                }
            }
        }

        public override bool Get_F6_SENSE()
        {
            return (
                (PlayPressed ||
                RecordPressed ||
                FastForwardPressed ||
                FastRewindPressed));
        }

        public override bool Get_D4_READ()
        {
            return InputSignal;
        }

        void WriteTape(int _ticks)
        {
            bool bit = OutputSignal;

            if (outputPuleDuration == 0)
            {
                //  new value
                if (lastOutputPulseDuration > 0)
                {
                    int len = lastOutputPulseDuration / 8;

                    FilePos++;
                    while (FilePos >= TapeRAWData.Count) TapeRAWData.Add(0);
                    FileLen = TapeRAWData.Count;
                    TapeRAWData[FilePos] =  len;

                    DataModified = true;
                }
            }
            outputPuleDuration += _ticks;
        }

        void ReadTape(int _ticks)
        {
            // InputSignal = true;
            
            //  simulate square wave
            bool xSignal = InputSignal;

            if (inputPulseDuration <= inputPulseLowUntil)
                InputSignal = true;
            else
                InputSignal = false;

            if (xSignal != InputSignal)
            {

            }
            
            inputPulseDuration -= _ticks;

            if (inputPulseDuration <= 0)
            {
                //  skip first flux
                //if (FilePos > 0)
                //    InputSignal = false;

                if (FileLen > FilePos)
                {
                    // pulse length (in seconds) = (8 * data byte) / (clock cycles)
                    int len = TapeRAWData[FilePos] * 8;
                    if (len == 0)
                    {
                        FilePos++;
                        byte b0 = (byte)TapeRAWData[FilePos];
                        FilePos++;
                        byte b1 = (byte)TapeRAWData[FilePos];
                        FilePos++;
                        byte b2 = (byte)TapeRAWData[FilePos];

                        len = b0 | (b1 << 8) | (b2 << 16);
                    }
                    inputPulseDuration = len;
                    inputPulseLowUntil = len / 2;
                    //Console.Out.WriteLine(inputPulseDuration.ToString());
                    FilePos++;
                }
                else
                {
                    //  tape finished!
                    PressStop();
                }

            }
        }

        public override double GetTapeCounter()
        {
            return FilePos / 6785;
        }

        public override double GetTapeMaxCounter()
        {
            return FileLen / 6785;
        }

        public override void Process(int _ticks)            
        {
            Tick += _ticks;

            if (MotorOn)
            {
                if (MotorOnDelay > 0)
                    MotorOnDelay -= _ticks;

                if (MotorOnDelay <= 0)
                {
                    if (RecordPressed) WriteTape(_ticks);
                    else if (PlayPressed) ReadTape(_ticks);
                }
            }
            else
            {
                InputSignal = true;
            }

            if (FastForwardPressed)
            {
                FilePos += 1;
                if (FilePos >= FileLen)
                {
                    FilePos = FileLen - 1;
                    PressStop();
                }
            }
            else if (FastRewindPressed)
            {
                FilePos -= 1;
                if (FilePos < 0)
                {
                    FilePos = 0;
                    PressStop();
                }
            }

        }

        public override void PressFastForward()
        {
            PressStop();
            FastForwardPressed = true;
        }

        public override void PressFastRewind()
        {
            PressStop();
            FastRewindPressed = true;
        }

    }
}
