using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using C64Emulator.Devices;

namespace C64Emulator.Chips
{
    public class CIA1 : CIA_6526
    {
        Keyboard Keyboard = null;
        Joystick[] Joystick = new Joystick[2];

        public CIA1(Keyboard _keyboard) : base(0xdc00)
        {
            SetKeyboard(_keyboard);

            SetJoystick(1, new NoJoystick());
            SetJoystick(2, new NoJoystick());
        }

        public void SetJoystick(int _port1or2, Joystick _joystick)
        {
            Joystick[_port1or2 - 1] = _joystick;
        }

        public Joystick GetJoystick(int _port1or2)
        {
            return Joystick[_port1or2 - 1];
        }

        public Keyboard GetKeyboard()
        {
            return Keyboard;
        }

        public void SetKeyboard(Keyboard _keyboard)
        {
            Keyboard = _keyboard;
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            int adr = _fullAddress - BaseAddress;
            adr %= 16;

            switch (adr)
            {
                case 0:
                    {
                        byte data = 0;

                        if (Keyboard != null)
                            data |= Keyboard.ReadCol((byte)(~PRB));

                        Joystick joy = GetJoystick(2);
                        if (joy != null)
                            data |= joy.ReadInput();
                        return (byte)(~data);
                    }

                case 1:
                    {
                        byte data = 0;
                        if (Keyboard != null)
                            data |= Keyboard.ReadRow((byte)(~PRA));

                        Joystick joy = GetJoystick(1);
                        if (joy != null)
                            data |= joy.ReadInput();

                        return (byte)(~data);
                    }

            }

            return base.Read(_fullAddress, _internal);
        }
    }
}
