using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64_WinForms.C64Emulator.Audio
{
    public struct SIDChip_FilterSettings
    {
        public float Resonance; // 0..1

        public bool Voice1FilterEnabled;
        public bool Voice2FilterEnabled;
        public bool Voice3FilterEnabled;

        public float Volume; // 0..1
        public bool HighpassEnabled;
        public bool BandpassEnabled;
        public bool LowpassEnabled;

        public bool Voice3Muted;
    }

    public struct SIDVoice_Settings
    {
        public float Freq;
        public float PWMRatio;
        public bool Gate;
        public bool Sync;
        public bool Ring;
        public bool Test;
        public bool Waveform_Triangle;
        public bool Waveform_Sawtooth;
        public bool Waveform_Rectangle;
        public bool Waveform_Noise;
        public int Attack_MS;
        public int Decay_MS;
        public int Release_MS;
        public float SustainLevel;
        public bool FilterEnabled;
        public bool IsMuted;
    }

    public abstract class SID : Chip
    {
        public Random Random;

        public SIDChip_FilterSettings FilterSettings;

        int TicksToWait = 0;

        SIDVoice[] Voice;

        public readonly int[] Attack_MS = { 2, 8, 16, 24, 38, 56, 68, 80, 100, 250, 500, 800, 1000, 3000, 5000, 8000 };
        public readonly int[] Release_MS = { 6, 24, 48, 72, 114, 168, 204, 240, 300, 750, 1500, 2400, 3000, 9000, 15000, 24000 };

        public SID() : base(0xd400, 0xd7ff)
        {
        }

        public abstract SIDVoice CreateVoice(int _voiceNo);


        public override void Reset()
        {
            Voice = new SIDVoice[3];

            Voice[0] = CreateVoice(1);
            Voice[1] = CreateVoice(2);
            Voice[2] = CreateVoice(3);

            Voice[0].SetGain(0);
            Voice[1].SetGain(0);
            Voice[2].SetGain(0);

            Random = new Random((int)DateTime.Now.Ticks);

            for (int i = 0; i < 3; i++)
            {
                SIDVoice v = GetVoice(i);
                if (v != null)
                    v.Reset();
            }
        }

        public virtual void ToggleChannel(int _voiceNo)
        {
        }

        public void SetSIDVolume(float _volume)
        {
            for (int i = 0; i<3; i++)
            {
                SIDVoice v = GetVoice(i);
                v.SetVolume(_volume);
            }
        }

        public SIDVoice GetVoice(int _voiceNo)
        {        
            return Voice[_voiceNo];
        }

        public override void Process(int _ticks)
        {
            TicksToWait -= _ticks;
            if (TicksToWait > 0)
                return;

            TicksToWait += 250; // microseconds

            int ticksElapsed = 250;
            // int ticksElapsed = _ticks;

            for (int i = 0; i < 3; i++)
            {
                SIDVoice v = GetVoice(i);
                if (v != null)
                    v.Process(ticksElapsed);
            }

            base.Process(ticksElapsed);
        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {
            byte xval = Read(_fullAddress, true); 
            base.Write(_fullAddress, _val, _internal);
            
            int ofs = _fullAddress - BaseAddress;
            ofs %= 32;
            _fullAddress = BaseAddress + ofs;

            //  voices
            if ((ofs >= 0) && (ofs < 21))
            {
                int currVoice = 0;
                if (ofs >= 7)
                    currVoice = 1;
                if (ofs >= 14)
                    currVoice = 2;

                SIDVoice v = GetVoice(currVoice);
                v.Write(_fullAddress - BaseAddress, _val);
            }

            switch (_fullAddress)                
            {
                case 0xd417:
                    {
                        FilterSettings = GetFilterSettings();
                        break;
                    }

                case 0xd418:
                    {
                        FilterSettings = GetFilterSettings();

                        SetSIDVolume(FilterSettings.Volume);

                        SIDVoice v = GetVoice(2);
                        v.Mute = FilterSettings.Voice3Muted;

                        break;
                    }
            }
        }

        public SIDChip_FilterSettings GetFilterSettings()
        {
            SIDChip_FilterSettings filter = new SIDChip_FilterSettings();

            filter.Resonance = (Read(0xd417, true) & 0x0f) / 15.0f;

            filter.Voice1FilterEnabled = ((Read(0xd417, true) & BIT.B1) > 0);
            filter.Voice2FilterEnabled = ((Read(0xd417, true) & BIT.B2) > 0);
            filter.Voice3FilterEnabled = ((Read(0xd417, true) & BIT.B3) > 0);            

            filter.Volume = (Read(0xd418, true) & 0x0f) / 15.0f;
            filter.HighpassEnabled = ((Read(0xd418, true) & 64) > 0);
            filter.BandpassEnabled = ((Read(0xd418, true) & 32) > 0);
            filter.LowpassEnabled = ((Read(0xd418, true) & 16) > 0);
            filter.Voice3Muted = ((Read(0xd418, true) & 128) > 0);

            return filter;
        }

        public SIDVoice_Settings GetVoiceSettings(int _voiceNo)
        {
            SIDVoice_Settings settings = new SIDVoice_Settings();

            int baseAdr = 0xd400;
            if (_voiceNo == 2) baseAdr = 0xd407;
            if (_voiceNo == 3) baseAdr = 0xd40e;

            byte StatusRegister = Read(baseAdr + 4, true);
            settings.Gate = ((StatusRegister & BIT.B0) > 0);
            settings.Sync = ((StatusRegister & BIT.B1) > 0);
            settings.Ring = ((StatusRegister & BIT.B2) > 0);
            settings.Test = ((StatusRegister & BIT.B3) > 0);
            settings.Waveform_Triangle = ((StatusRegister & BIT.B4) > 0);
            settings.Waveform_Sawtooth = ((StatusRegister & BIT.B5) > 0);
            settings.Waveform_Rectangle= ((StatusRegister & BIT.B6) > 0);
            settings.Waveform_Noise = ((StatusRegister & BIT.B7) > 0);

            //  freq
            byte FreqLO = Read(baseAdr + 0, true);
            byte FreqHI = Read(baseAdr + 1, true);
            int freqIndex = FreqLO + (FreqHI << 8);
            settings.Freq = (freqIndex * 0.0596f);

            settings.Attack_MS = Attack_MS[Read(baseAdr + 5, true) >> 4];
            settings.Decay_MS = Release_MS[Read(baseAdr + 5, true) & 0x0f];
            settings.SustainLevel = (Read(baseAdr + 6, true) >> 4) / 15.0f;
            settings.Release_MS = Release_MS[Read(baseAdr + 6, true) & 0x0f];

            int PWM = Read(baseAdr + 2, true) + ((Read(baseAdr + 3, true) & 0x0f) << 8);
            settings.PWMRatio = PWM / 4095.0f;

            settings.FilterEnabled = false;

            if (_voiceNo == 1)
                settings.FilterEnabled = ((Read(0xd417, true) & BIT.B0) > 0);

            if (_voiceNo == 2)
                settings.FilterEnabled = ((Read(0xd417, true) & BIT.B1) > 0);

            if (_voiceNo == 3)
                settings.FilterEnabled = ((Read(0xd417, true) & BIT.B2) > 0);

            settings.IsMuted = false;
            if (_voiceNo == 3)
                settings.IsMuted = ((Read(0xd418, true) & BIT.B7) > 0);

            return settings;
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            int ofs = _fullAddress - BaseAddress;

            switch (ofs)
            {
                case 27: //  Oszillator Stimme 3
                    {
                        byte[] b = new byte[1];
                        
                        Random.NextBytes(b);
                        return b[0];
                    }

                case 28: //  gain of voice #3
                    {
                        SIDVoice v = GetVoice(2);
                        byte vol = (byte)(v.currGain * 15.0f);
                        
                        return vol;
                    }
                case 29:
                case 30:
                case 31:
                     {
                        return 0xfF;
                    }
            }

            return base.Read(_fullAddress, _internal);
        }
    }

}
