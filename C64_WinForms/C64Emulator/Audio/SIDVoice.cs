using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64_WinForms.C64Emulator.Audio
{
    public enum WavePhase
    {
        Attack = 0,
        Decay = 1,
        Sustain = 2,
        Release = 3,
        Mute

    }

    public class SIDVoice
    {
        public int VoiceNo;
        bool Gate = false;

        internal SIDVoice_Settings Settings;
        
        internal float myVolume = 1.0f;
        internal float currGain = 0.0f;

        WavePhase currWavePhase = WavePhase.Mute;

        int currPhaseTick = 0;
        int currPhaseTicks = 0;
        float currPhaseFrom = 0;
        float currPhaseTo = 0;

        public void SetGate(bool _newVal)
        {
            if (Gate != _newVal)
            {
                Gate = _newVal;
                OnGateChanged(Gate);
            }
        }

        public virtual void OnGateChanged(bool _toGate)
        {

        }

        public void SetPhase(WavePhase _phase)
        {
            currPhaseTick = 0;
            currWavePhase = _phase;

            switch (_phase)
            {
                case WavePhase.Attack:
                    {
                        currPhaseFrom = 0.0f;
                        currPhaseTo = 1.0f;
                        currPhaseTicks = 1000 * Settings.Attack_MS;
                        break;
                    }

                case WavePhase.Decay:
                    {
                        currPhaseFrom = 1.0f;
                        currPhaseTo = Settings.SustainLevel;
                        currPhaseTicks = 1000 * Settings.Decay_MS;
                        break;
                    }

                case WavePhase.Sustain:
                    {
                        currPhaseFrom = Settings.SustainLevel;
                        currPhaseTo = Settings.SustainLevel;
                        currPhaseTicks = 0;
                        break;
                    }

                case WavePhase.Release:
                    {
                        if (currGain == 0)
                        {
                            SetPhase(WavePhase.Mute);
                            break;
                        }
                        currPhaseFrom = currGain;
                        currPhaseTo = 0;

                        currPhaseTicks = 1000 * Settings.Release_MS;
                        break;
                    }

                case WavePhase.Mute:
                    {
                        currPhaseFrom = 0;
                        currPhaseTo = 0;
                        currPhaseTicks = 0;
                        break;
                    }
            }

            Process(0);
            // SetGain(currPhaseFrom);
        }


        public virtual void Process(int _ticks)
        {
            //  process phase
            if (currWavePhase == WavePhase.Mute)
            {
                SetGain(0);
                return;
            }

            if (currWavePhase == WavePhase.Sustain)
            {
                // evtl. RING-MOD oder SYNC-Effekte machen
                return;
            }

            currPhaseTick += _ticks;

            float ratio = (float)currPhaseTick / (float)currPhaseTicks;
            float vol = currPhaseFrom + ratio * (currPhaseTo - currPhaseFrom);

            SetGain(vol);

            if (currPhaseTick >= currPhaseTicks)
            {
                //  next phase
                SetPhase(currWavePhase + 1);
            }
        }

        public bool Mute
        {
            set
            {
                Settings.IsMuted = value;
                SetVolume(GetVolume());
            }
            get
            {
                return Settings.IsMuted;
            }
        }

        public virtual void SetGain(float _gain)
        {
        }

        public virtual void Reset()
        {
            SetVolume(0);
            SetPhase(WavePhase.Mute);            
        }

        public virtual void SetVolume(float _volume)
        {
            myVolume = _volume;
        }

        public virtual float GetVolume()
        {
            return myVolume;
        }

        public virtual void Write(int _SIDChipAddress, byte _val)
        {

        }

    }

}
