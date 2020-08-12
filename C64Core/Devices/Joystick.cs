using System;
using System.IO;

namespace C64Emulator.Devices
{

    public abstract class Joystick : Device
    {
        public abstract byte ReadInput(); // Bit 0..3 Richtung(Bit 0: hoch, Bit 1: runter, Bit 2: links, Bit 3: rechts), Bit 4 Feuerknopf. 0 = aktiviert.
    }


    public class NoJoystick : Joystick
    {
        public override byte ReadInput()
        {
            return (0);
        }
    }

    /*
    public class JoystickKeypad : C64Joystick
    {
        bool Up = false;
        bool Down = false;
        bool Left = false;
        bool Right = false;
        bool Fire = false;

        Keys KeyUp;
        Keys KeyDown;
        Keys KeyLeft;
        Keys KeyRight;
        Keys KeyFire;

        public JoystickKeypad()
        {
            KeyUp = Keys.NumPad8;
            KeyDown = Keys.NumPad2;
            KeyLeft = Keys.NumPad4;
            KeyRight = Keys.NumPad6;
            KeyFire = Keys.NumPad5;
        }

        public JoystickKeypad(Keys _up, Keys _down, Keys _left, Keys _right, Keys _fire)
        {
            KeyUp = _up;
            KeyDown = _down;
            KeyLeft = _left;
            KeyRight = _right;
            KeyFire = _fire;
        }

        public override byte ReadInput()
        {
            byte data = 0;

            if (Up) data |= 0x01;
            if (Down) data |= 0x02;
            if (Left) data |= 0x04;
            if (Right) data |= 0x08;
            if (Fire) data |= 0x10;

            return (data);
        }

        public override void OnKeyDown(Keys _key)
        {
            if (_key == KeyLeft)
                Left = true;

            if (_key == KeyRight)
                Right = true;

            if (_key == KeyUp)
                Up = true;

            if (_key == KeyDown)
                Down = true;

            if (_key == KeyFire)
                Fire = true;
        }

        public override void OnKeyUp(Keys _key)
        {
            if (_key == KeyLeft)
                Left = false;

            if (_key == KeyRight)
                Right = false;

            if (_key == KeyUp)
                Up = false;

            if (_key == KeyDown)
                Down = false;

            if (_key == KeyFire)
                Fire = false;
        }
    }
    */
}
