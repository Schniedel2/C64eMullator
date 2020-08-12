using System;

namespace C64Emulator
{
    public static class WaveHelper
    {
        public static short[] CreateWave_Triangle(int _outputFreq, double _hz)
        {
            if (_hz == 0)
                return null;

            int iwaveLen = (int)(_outputFreq / _hz);
            short[] data = new short[iwaveLen];

            int lenH = iwaveLen / 2;

            //  triangle
            for (int i = 0; i < lenH; i++)
            {
                int h = (i * 65536 / lenH);
                data[i] = (short)(-32767 + h);
                data[iwaveLen - 1 - i] = (short)(-32767 + h);
            }

            return data;
        }

        public static short[] CreateWave_Sawtooth(int _outputFreq, double _hz)
        {
            if (_hz == 0)
                return null;

            int len = (int)(_outputFreq / _hz);
            short[] data = new short[len];

            //  sawtooth
            for (int i = 0; i < len; i++)
            {
                int h = (i * 65536 / len);
                data[i] = (short)(-32767 + h);
            }

            return data;
        }

        public static short[] CreateWave_Rectangle(int _outputFreq, double _hz, float _pwmRatio)
        {
            if (_hz == 0)
                return null;

            int len = (int)(_outputFreq / _hz);
            short[] data = new short[len];

            //  rectangle
            int change = (int)(_pwmRatio * (float)data.Length);

            for (int i = 0; i < change; i++)
                data[i] = -32767;

            for (int i = change; i < data.Length; i++)
                data[i] = 32767;

            return data;
        }

        public static short[] CreateWave_Noise(int _outputFreq, double _hz)
        {
            if (_hz == 0)
                return null;

            int len = 512;
            short[] data = new short[len];
            Random rnd = new Random();

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (short)((rnd.Next() % 65536) - 32768);
            }

            return data;
        }

        public static short[] LowpassFilter(short[] _wave)
        {
            if (_wave == null)
                return null;

            //return _wave;
            short[] output = new short[_wave.Length];
            float alpha = 0.5f;

            output[0] = _wave[0];
            for (int i = 1; i < _wave.Length; i++)
            {
                short om1 = output[i - 1];
                output[i] = (short)(om1 + alpha * (_wave[i] - om1));
            }

            return output;
        }

        /*
        // https://en.wikipedia.org/wiki/High-pass_filter
        public static short[] HighpassFilter(short[] _wave)
        {


 // Return RC high-pass filter output samples, given input samples,
 // time interval dt, and time constant RC
 function highpass(real[0..n] x, real dt, real RC)
   var real[0..n] y
   var real α := RC / (RC + dt)
   y[0] := x[0]
   for i from 1 to n
     y[i] := α * y[i-1] + α * (x[i] - x[i-1])
   return y
The loop which calculates each of the 
n {\displaystyle n} 
 outputs can be refactored into the equivalent:
   for i from 1 to n
     y[i] := α * (y[i-1] + x[i] - x[i-1])
            if (_wave == null)
                return null;







            //return _wave;
            short[] output = new short[_wave.Length];
            float alpha = 0.5f;

            output[0] = _wave[0];
            for (int i = 1; i < _wave.Length; i++)
            {
                short om1 = output[i - 1];
                output[i] = (short)(om1 + alpha * (_wave[i] - om1));
            }

            return output;

        }
        */
    }
}
