using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace C64_WinForms.C64Emulator
{
    public class C64Keyboard : C64Peripherie
    {
        byte[] KeyState = new byte[8];

        public C64Keyboard()
        {
            ResetKeyState();
        }

        public void ResetKeyState()
        {
            for (int i = 0; i < 8; i++)
            {
                KeyState[i] = 0;
            }
        }

        public void SetBit(int col, int row, bool val)
        {
            byte bcol = (byte)(1 << col);

            KeyState[row] &= (byte)(~bcol); //  clear bit

            if (val)
            {
                KeyState[row] |= bcol; //  setbit
            }
        }

        bool GetBit(int col, int row)
        {
            byte b = KeyState[row];
            byte mask = (byte)(1 << col);

            return ((b & mask) > 0);
        }

        byte GetSingleRow(int row)
        {
            return KeyState[row];
        }

        byte GetSingleCol(int col)
        {
            byte data = 0;
            for (int i = 0; i < 8; i++)
            {
                if (GetBit(col, i))
                {
                    data |= (byte)(1 << i);
                }
            }
            return data;
        }

        public byte ReadRow(byte colMask)
        {
            byte data = 0;
            for (int col = 0; col < 8; col++)
            {
                if ((colMask & (1 << col)) > 0)
                {
                    byte r = GetSingleCol(col);
                    data |= r;
                }
            }
            return data;
        }

        public byte ReadCol(byte rowMask)
        {
            byte data = 0;
            for (int row = 0; row < 8; row++)
            {
                if ((rowMask & (1 << row)) > 0)
                {
                    byte r = GetSingleRow(row);
                    data |= r;
                }
            }
            return data;
        }

        static public int MakeCoord(int col, int row)
        {
            return (col | (row << 8));
        }
    }

    public class PCKeyboard : C64Keyboard
    {
        Dictionary<Keys, int> KeyMap = new Dictionary<Keys, int>();
        bool Key_ALT = false;
        bool Key_Control = false;
        bool Key_Shift = false;

        public PCKeyboard()
        {
            KeyMap.Add(Keys.D1, MakeCoord(7, 0));
            KeyMap.Add(Keys.Escape, MakeCoord(7, 1));
            KeyMap.Add(Keys.Control, MakeCoord(7, 2));
            KeyMap.Add(Keys.LControlKey, MakeCoord(7, 2));
            KeyMap.Add(Keys.RControlKey, MakeCoord(7, 2));
            KeyMap.Add(Keys.D2, MakeCoord(7, 3));
            KeyMap.Add(Keys.Space, MakeCoord(7, 4));
            KeyMap.Add(Keys.LWin, MakeCoord(7, 5)); // C= Key
            KeyMap.Add(Keys.Q, MakeCoord(7, 6));
            KeyMap.Add(Keys.CapsLock, MakeCoord(7, 7)); // Run-Stop


            //KeyMap.Add(Keys.Add, MakeCoord(6, 0)); // Pfund
            KeyMap.Add(Keys.Multiply, MakeCoord(6, 1));
            // KeyMap.Add(Keys.OemSemicolon, MakeCoord(6, 2));
            KeyMap.Add(Keys.Oem7, MakeCoord(6, 2)); // ;
            KeyMap.Add(Keys.Home, MakeCoord(6, 3));
            KeyMap.Add(Keys.RShiftKey, MakeCoord(6, 4));
            KeyMap.Add(Keys.Oem2, MakeCoord(6, 5)); // # -> =
            KeyMap.Add(Keys.PageUp, MakeCoord(6, 6)); // ^
            // KeyMap.Add(Keys.OemQuestion, MakeCoord(6, 7)); // => "/"

            KeyMap.Add(Keys.Add, MakeCoord(5, 0));
            KeyMap.Add(Keys.P, MakeCoord(5, 1));
            KeyMap.Add(Keys.L, MakeCoord(5, 2));
            KeyMap.Add(Keys.Subtract, MakeCoord(5, 3));
            KeyMap.Add(Keys.OemPeriod, MakeCoord(5, 4));
            KeyMap.Add(Keys.Oem3, MakeCoord(5, 5)); // :
            // KeyMap.Add(Keys.Decimal, MakeCoord(5, 5));
            // KeyMap.Add(Keys.Divide, MakeCoord(5, 6)); // = @
            KeyMap.Add(Keys.Oemcomma, MakeCoord(5, 7));

            KeyMap.Add(Keys.D9, MakeCoord(4, 0));
            KeyMap.Add(Keys.I, MakeCoord(4, 1));
            KeyMap.Add(Keys.J, MakeCoord(4, 2));
            KeyMap.Add(Keys.D0, MakeCoord(4, 3));
            KeyMap.Add(Keys.M, MakeCoord(4, 4));
            KeyMap.Add(Keys.K, MakeCoord(4, 5));
            KeyMap.Add(Keys.O, MakeCoord(4, 6));
            KeyMap.Add(Keys.N, MakeCoord(4, 7));

            KeyMap.Add(Keys.D7, MakeCoord(3, 0));
            KeyMap.Add(Keys.Y, MakeCoord(3, 1));
            KeyMap.Add(Keys.G, MakeCoord(3, 2));
            KeyMap.Add(Keys.D8, MakeCoord(3, 3));
            KeyMap.Add(Keys.B, MakeCoord(3, 4));
            KeyMap.Add(Keys.H, MakeCoord(3, 5));
            KeyMap.Add(Keys.U, MakeCoord(3, 6));
            KeyMap.Add(Keys.V, MakeCoord(3, 7));

            KeyMap.Add(Keys.D5, MakeCoord(2, 0));
            KeyMap.Add(Keys.R, MakeCoord(2, 1));
            KeyMap.Add(Keys.D, MakeCoord(2, 2));
            KeyMap.Add(Keys.D6, MakeCoord(2, 3));
            KeyMap.Add(Keys.C, MakeCoord(2, 4));
            KeyMap.Add(Keys.F, MakeCoord(2, 5));
            KeyMap.Add(Keys.T, MakeCoord(2, 6));
            KeyMap.Add(Keys.X, MakeCoord(2, 7));

            KeyMap.Add(Keys.D3, MakeCoord(1, 0));
            KeyMap.Add(Keys.W, MakeCoord(1, 1));
            KeyMap.Add(Keys.A, MakeCoord(1, 2));
            KeyMap.Add(Keys.D4, MakeCoord(1, 3));
            KeyMap.Add(Keys.Z, MakeCoord(1, 4));
            KeyMap.Add(Keys.S, MakeCoord(1, 5));
            KeyMap.Add(Keys.E, MakeCoord(1, 6));
            KeyMap.Add(Keys.LShiftKey, MakeCoord(1, 7));
            KeyMap.Add(Keys.Shift, MakeCoord(1, 7));
            KeyMap.Add(Keys.ShiftKey, MakeCoord(1, 7));

            KeyMap.Add(Keys.Back, MakeCoord(0, 0));
            KeyMap.Add(Keys.Enter, MakeCoord(0, 1));
            KeyMap.Add(Keys.Right, MakeCoord(0, 2));
            KeyMap.Add(Keys.F7, MakeCoord(0, 3));
            KeyMap.Add(Keys.F1, MakeCoord(0, 4));
            KeyMap.Add(Keys.F3, MakeCoord(0, 5));
            KeyMap.Add(Keys.F5, MakeCoord(0, 6));
            KeyMap.Add(Keys.Down, MakeCoord(0, 7));

            // Oem3 // = Ö
            // Oem7 // = Ä
            // Menu == ALT

            /*
            KeyMap.Add(Keys.NumPad0, MakeCoord(4, 3));
            KeyMap.Add(Keys.NumPad1, MakeCoord(7, 0));
            KeyMap.Add(Keys.NumPad2, MakeCoord(7, 3));
            KeyMap.Add(Keys.NumPad3, MakeCoord(1, 0));
            KeyMap.Add(Keys.NumPad4, MakeCoord(1, 3));
            KeyMap.Add(Keys.NumPad5, MakeCoord(2, 0));
            KeyMap.Add(Keys.NumPad6, MakeCoord(2, 3));
            KeyMap.Add(Keys.NumPad7, MakeCoord(3, 0));
            KeyMap.Add(Keys.NumPad8, MakeCoord(3, 3));
            KeyMap.Add(Keys.NumPad9, MakeCoord(4, 0));
            */
        }

        void OnKey(Keys _key, bool _down)
        {
            if (_key == Keys.Menu) Key_ALT = _down;
            if (_key == Keys.ControlKey) Key_Control = _down;
            if (_key == Keys.ShiftKey) Key_Shift = _down;

            if ((_key == Keys.Q) && (Key_Control) && (Key_ALT))
            {
                SetBit(5, 6, _down); // @
                return;
            }

            if ((_key == Keys.Oem102) && (!Key_Shift)) // "<"
            {
                SetBit(5, 7, _down); // , / <
                SetBit(5, 4, false); // . / >
                SetBit(1, 7, _down); // SHIFT
                return;
            }

            if ((_key == Keys.Oem102) && (Key_Shift)) // ">"
            {
                SetBit(5, 7, false); // , / <
                SetBit(5, 4, _down); // . / >
                SetBit(1, 7, _down); // SHIFT
                return;
            }

            if (_key == Keys.Oem5)
            {
                SetBit(7, 1, _down); // <-
                return;
            }

            if (_key == Keys.Left)
            {
                SetBit(0, 2, _down); // Cursor Right/Left
                SetBit(1, 7, _down); // SHIFT
                return;
            }

            if (_key == Keys.Up)
            {
                SetBit(0, 7, _down); // Cursor Up/Down
                SetBit(1, 7, _down); // SHIFT
                return;
            }

            int pos = 0;
            if (KeyMap.TryGetValue(_key, out pos))
            {
                int col = pos & 0xff;
                int row = (pos >> 8);

                SetBit(col, row, _down);
            }
        }

        public override void OnKeyDown(Keys _key)
        {
            OnKey(_key, true);
        }

        public override void OnKeyUp(Keys _key)
        {
            OnKey(_key, false);
        }
    }
}
