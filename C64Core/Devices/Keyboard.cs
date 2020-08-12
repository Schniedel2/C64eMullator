using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64Emulator.Devices
{
    public class Keyboard : Device
    {
        byte[] KeyState = new byte[8];

        public Keyboard()
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

        public void Press_SHIFT()
        {
            SetBit(1, 7, true);
        }

        public void Press_RSHIFT()
        {
            SetBit(6, 4, true);
        }

        public void Press_Commodore()
        {
            SetBit(1, 7, true);
        }

        public void Press_ESC()
        {
            SetBit(7, 1, true);
        }

        public void Press_CTRL()
        {
            SetBit(7, 2, true);
        }

        public void PressKey(string _key)
        {
            switch (_key.ToUpper())
            {
                case "A": { SetBit(1, 2, true); break; }
                case "B": { SetBit(3, 4, true); break; }
                case "C": { SetBit(2, 4, true); break; }
                case "D": { SetBit(2, 2, true); break; }
                case "E": { SetBit(1, 6, true); break; }
                case "F": { SetBit(2, 5, true); break; }
                case "G": { SetBit(3, 2, true); break; }
                case "H": { SetBit(3, 5, true); break; }
                case "I": { SetBit(4, 1, true); break; }
                case "J": { SetBit(4, 2, true); break; }
                case "K": { SetBit(4, 5, true); break; }
                case "L": { SetBit(5, 2, true); break; }
                case "M": { SetBit(4, 4, true); break; }
                case "N": { SetBit(4, 7, true); break; }
                case "O": { SetBit(4, 6, true); break; }
                case "P": { SetBit(5, 1, true); break; }
                case "Q": { SetBit(7, 6, true); break; }
                case "R": { SetBit(2, 1, true); break; }
                case "S": { SetBit(1, 5, true); break; }
                case "T": { SetBit(2, 6, true); break; }
                case "U": { SetBit(3, 6, true); break; }
                case "V": { SetBit(3, 7, true); break; }
                case "W": { SetBit(1, 1, true); break; }
                case "X": { SetBit(2, 7, true); break; }
                case "Y": { SetBit(3, 1, true); break; }
                case "Z": { SetBit(1, 4, true); break; }

                case "*": { SetBit(6, 1, true); break; }
                case "-": { SetBit(5, 3, true); break; }
                case "OEMPERIOD":
                case ".": { SetBit(5, 4, true); break; }
                case ":": { SetBit(5, 5, true); break; }
                case "OEMCOMMA":
                case ",": { SetBit(5, 7, true); break; }
                case "@": { SetBit(5, 6, true); break; }

                case "D0":
                case "0": { SetBit(4, 3, true); break; }
                case "D1":
                case "1": { SetBit(7, 0, true); break; }
                case "D2":
                case "2": { SetBit(7, 3, true); break; }
                case "D3":
                case "3": { SetBit(1, 0, true); break; }
                case "D4":
                case "4": { SetBit(1, 3, true); break; }
                case "D5":
                case "5": { SetBit(2, 0, true); break; }
                case "D6":
                case "6": { SetBit(2, 3, true); break; }
                case "D7":
                case "7": { SetBit(3, 0, true); break; }
                case "D8":
                case "8": { SetBit(3, 3, true); break; }
                case "D9":
                case "9": { SetBit(4, 0, true); break; }

                case "ESCAPE":
                case "ESC": { SetBit(7, 1, true); break; }

                case "CR":
                case "RETURN":
                case "ENTER": { SetBit(0, 1, true); break; }

                case "LSHIFT":
                case "LEFTSHIFT": { SetBit(1, 7, true); break; }
                //case "SHIFT": { SetBit(1, 7, true); break; }

                case "RSHIFT":
                case "RIGHTSHIFT": { SetBit(6, 4, true); break; }

                case "LCTRL":
                case "LEFTCTRL":
                case "RIGHTCTRL":
                case "RCTRL":
                case "CTRL": { SetBit(7, 2, true); break; }

				case " ":
				case "SPC":
				case "SPACE": { SetBit(7, 4, true); break; }

				case "LWIN":
                case "C=":
                case "COMMODORE": { SetBit(7, 5, true); break; }

                case "PAUSE":
                case "RUNSTOP": { SetBit(7, 7, true); break; }

                case "RESTORE": { SetBit(7, 7, true); break; }

                case "F1": { SetBit(0, 4, true); break; }
                case "F3": { SetBit(0, 5, true); break; }
                case "F5": { SetBit(0, 6, true); break; }
                case "F7": { SetBit(0, 3, true); break; }

                case "F2": { SetBit(0, 4, true); Press_SHIFT();  break; }
                case "F4": { SetBit(0, 5, true); Press_SHIFT(); break; }
                case "F6": { SetBit(0, 6, true); Press_SHIFT(); break; }
                case "F8": { SetBit(0, 3, true); Press_SHIFT(); break; }

                case "UP": { SetBit(0, 7, true); Press_SHIFT(); break; }
                case "DOWN": { SetBit(0, 7, true); break; }
                case "LEFT": { SetBit(0, 2, true); Press_SHIFT(); break; }
                case "RIGHT": { SetBit(0, 2, true); break; }

                case "BACK": { SetBit(0, 0, true); break; }

                case "POS1":
                case "CLR":
                case "HOME": { SetBit(6, 3, true); break; }

                default:
                {
                        break;
                }
            }

        }

    }
}
