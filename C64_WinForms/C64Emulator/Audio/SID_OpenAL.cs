using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Audio.OpenAL;
using System.Runtime.InteropServices;

namespace C64_WinForms.C64Emulator.Audio
{
    public class SID_OpenAL : SID
    {
        const int Freq = 44020;

        IntPtr ALDevice;
        ContextHandle context;

        public SID_OpenAL() : base()
        {
            ALDevice = Alc.OpenDevice(null);
            int[] attribs = null;
            context = Alc.CreateContext(ALDevice, attribs);
            Alc.MakeContextCurrent(context);
        }

        public override SIDVoice CreateVoice(int _voiceNo)
        {
            int registerStart = 7 * (_voiceNo - 1);
            return new SIDVoice_OpenAL(_voiceNo, this, Freq, registerStart);
        }
    }

    //
    //
    //

    public class SIDVoice_OpenAL : SIDVoice
    {
        SID SIDChip;
        int OutputFreq;
        int RegisterOffset;

        int ALBufferHandle;
        int ALSourceHandle;
        
        bool WaveBufferDirty = false;
        bool LoopingEnabled = false;

        
        Random Random = new Random();

        ~SIDVoice_OpenAL()
        {
            AL.DeleteBuffer(ALBufferHandle);
            AL.DeleteSource(ALSourceHandle);
        }

        public SIDVoice_OpenAL(int _voiceNo, SID _SIDChip, int _freq, int _regOffset)
        {
            SIDChip = _SIDChip;
            VoiceNo = _voiceNo;

            OutputFreq = _freq;
            RegisterOffset = _regOffset;

            ALBufferHandle = AL.GenBuffer();
            ALSourceHandle = AL.GenSource();

            AL.Source(ALSourceHandle, ALSourcei.Buffer, 0);
            SetVolume(0);
            SetLooping(true);
            AL.SourcePlay(ALSourceHandle);
        }

        public void SetBuffer(int _buffer, short[] _buff, int _freq)
        {
            GCHandle handle = GCHandle.Alloc(_buff, GCHandleType.Pinned);
            IntPtr ptr = handle.AddrOfPinnedObject();

            AL.BufferData(_buffer, ALFormat.Mono16, ptr, _buff.Length * 2, _freq);
            handle.Free();
        }

        public override void SetGain(float _gain)
        {
            currGain = _gain;

            float volume = 0;

            if (!Mute)
                volume = myVolume * _gain;

            AL.Source(ALSourceHandle, ALSourcef.Gain, volume);
        }

        void UpdateWaveBuffer()
        {
            Settings = SIDChip.GetVoiceSettings(VoiceNo);
            
            short[] buffer = null;

            if (Settings.Waveform_Triangle)
                buffer = WaveHelper.CreateWave_Triangle(OutputFreq, Settings.Freq);

            if (Settings.Waveform_Sawtooth)
                buffer = WaveHelper.CreateWave_Sawtooth(OutputFreq, Settings.Freq);

            if (Settings.Waveform_Rectangle)
                buffer = WaveHelper.CreateWave_Rectangle(OutputFreq, Settings.Freq, Settings.PWMRatio);

            if (Settings.Waveform_Noise)
                buffer = WaveHelper.CreateWave_Noise(OutputFreq, Settings.Freq);

            //  aplly filters
            if (Settings.FilterEnabled)
            {
                SIDChip_FilterSettings filter = SIDChip.FilterSettings;

                if (filter.LowpassEnabled)
                    buffer = WaveHelper.LowpassFilter(buffer);

                if (filter.HighpassEnabled)
                {
                    //
                }

                if (filter.BandpassEnabled)
                {

                }
            }

            if (buffer != null)
            {
                WaveBufferDirty = false;

                AL.SourceStop(ALSourceHandle);
                AL.Source(ALSourceHandle, ALSourcei.Buffer, 0);
                SetBuffer(ALBufferHandle, buffer, OutputFreq);
                AL.Source(ALSourceHandle, ALSourcei.Buffer, ALBufferHandle);
                SetLooping(true);
                AL.SourcePlay(ALSourceHandle);
            }
        }

        public void NotifyWaveBufferDirty()
        {
            WaveBufferDirty = true;
        }

        public override void OnGateChanged(bool _toGate)
        {
            if (!_toGate)
            {
                //  release
                SetPhase(WavePhase.Release);
                return;
            }

            if (_toGate)
            {
                UpdateWaveBuffer();
                SetPhase(WavePhase.Attack);
            }
        }

        public override void Write(int _SIDChipAddress, byte _val)
        {
            //if (_xval == _val)
            //   return;

            byte StatusRegister = SIDChip.RAM[RegisterOffset + 4];

            if (_SIDChipAddress == RegisterOffset + 4) // Status_Register
            {
                StatusRegister = SIDChip.RAM[RegisterOffset + 4];
                SetGate((StatusRegister & BIT.B0) > 0);
            }
            else
            {
                
                NotifyWaveBufferDirty();
                if (Settings.Gate)
                {
                    UpdateWaveBuffer();
                }
                
            }
        }

        public void SetLooping(bool _loopEnabled)
        {
            LoopingEnabled = _loopEnabled;
            AL.Source(ALSourceHandle, ALSourceb.Looping, _loopEnabled);
        }

    }

}
