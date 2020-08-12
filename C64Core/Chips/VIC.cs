using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64Emulator.Chips

{
    /*
    https://www.c64-wiki.de/wiki/VIC
    http://codebase64.org/doku.php?id=base:vic

    http://dustlayer.com/vic-ii/2013/4/25/vic-ii-for-beginners-beyond-the-screen-rasters-cycle

    NOTE: (todo!)
    from the viewpoint of VICII
    Now dont ask me how this mechanism works but there are two memory areas handled differently where for the VIC the character ROM 
    is mapped in. Unless Ultimax mode is selected by an expansion port cartridge, at these areas the VIC will _always_ 'see' the char 
    rom instead of the RAM. If you set a sprite/bitmap/screen/character memory to read its data from $1000-$2000 or $9000-$a000 the read 
    will be always done from the character rom. These areas are: 
    */

    public class VIC2_Settings
    {
        public int PixelsPerScanLine = 403;
        public int RasterLine_Row0 = 55;
        public int NumScanLines = 312;

        public int BorderLeft = 42;
        public int BorderTop = 55;

        public int SpriteDisplacementX = -24;
        public int SpriteDisplacementY = -50;
    }

    public struct VIC2_State
    {
        public bool OutputEnabled;
        public int BorderColor;
        public int BackgrundColor;

        //  bank
        public int BankBaseAddress;
        public int ScreenAddress;
        public int CharMemAddress;
        public int BitmapAddress;

        //  graph modes
        public bool MCM; //  MultiColorMode
        public bool BMM; //  Bitmap Mode
        public bool ECM; //  extended char color mode

        public int TextColumns;
        public int TextRows;
        public int OffsetX;
        public int OffsetY;

        //  current rendering parameters
        public int HorStartPixel;
        public int FirstPixel;
        public int LastPixel;
        public int FirstScreenLine; // 0 or more in 24 row-mode
        public int LastScreenLine; // 199 or less in 24 row-mode
    }

    public class VIC_II : Chip
    {
        C64 C64;
        byte[] ColorRAM = new byte[40];

        public VIC2_Settings Settings = new VIC2_Settings();
        public VIC2_State VICState = new VIC2_State();

        //public int RasterLine;
        public int Frames = 0;

        //  registers
        byte D000_Spr0_X;
        byte D001_Spr0_Y;
        byte D002_Spr1_X;
        byte D003_Spr1_Y;
        byte D004_Spr2_X;
        byte D005_Spr2_Y;
        byte D006_Spr3_X;
        byte D007_Spr3_Y;
        byte D008_Spr4_X;
        byte D009_Spr4_Y;
        byte D00A_Spr5_X;
        byte D00B_Spr5_Y;
        byte D00C_Spr6_X;
        byte D00D_Spr6_Y;
        byte D00E_Spr7_X;
        byte D00F_Spr7_Y;
        byte D010_Sprs_XBit8;
        byte D011_Status;
        byte D012_RasterLO;
        byte D013_LightpenX;
        byte D014_LightpenY;
        byte D015_Spr_Enabled;
        byte D016_Status2;
        byte D017_Spr_StretchY;
        byte D018_MemCtrl;
        byte D019_IRQFlagsLatch = 0;
        byte D01A_IRQMask;
        byte D01B;
        byte D01C;
        byte D01D_Spr_StretchX;
        byte D01E_SpriteSpriteCollision;
        byte D01F_SpriteBackgroundCollision;
        byte D020_BorderColor;
        byte D021_BackgroundColor;
        byte D022;
        byte D023;
        byte D024;
        byte D025;
        byte D026;
        byte D027;
        byte D028;
        byte D029;
        byte D02A;
        byte D02B;
        byte D02C;
        byte D02D;
        byte D02E;
        byte D02F;
        byte D030;

        public int RasterlineIRQ = -1;
        public int currentRasterLine = -1;

        public VIC_II(C64 _c64) : base(0xd000, 0xd3ff)
        {
            C64 = _c64;            
        }

        public bool HasIRQ()
        {
            return ((D019_IRQFlagsLatch & D01A_IRQMask) > 0);
        }

        public void NotifyIRQ(byte _mask)
        {
            D019_IRQFlagsLatch |= _mask;
        }

        public bool IsBadLine(int _rasterLine)
        {
            int y = _rasterLine - Settings.BorderTop;
            if ((y < 0) || (y >= 200))
                return false;

            return ((y % 8) == 0);
        }

        public bool OutputEnabled()
        {
            bool enabled = ((D011_Status & 16) > 0);
            return enabled;
        }

        void UpdateRasterLine(int line)
        {
            //  update current rasterline in statusregister

            byte rasterLO = (byte)(line & 0xff);

            D012_RasterLO = rasterLO;
            D011_Status &= 0x7f;
            if (line > 255) D011_Status |= 0x80;

            // check for IRQ
            if (line == RasterlineIRQ)
            {
                NotifyIRQ(0x01); // == Raster-IRQ
            }
        }

        public int ProcessRasterLine(out byte[] _pixels)
        {
            currentRasterLine++;

            if (currentRasterLine >= Settings.NumScanLines)
            {
                //  certical retrace
                VerticalRetrace();
                currentRasterLine = 0;
            }

            VICState = GetState();
            UpdateRasterLine(currentRasterLine - 5); // <- works fine in choplifter (textmode -> graphicsmode switch)

            _pixels = new byte[512];

            // if (_outputEnabled)
            {
                RenderLineToArray(currentRasterLine, ref _pixels);
                RenderBorderToArray(currentRasterLine, ref _pixels);

                //  debug
                if (DebugOptions.RenderIRQRasterLine)
                {
                    if (currentRasterLine == RasterlineIRQ)
                    {
                        for (int i = 0; i < VICState.FirstPixel; i += 3)
                            _pixels[i] = 1;
                    }
                }
            }

            if (IsBadLine(currentRasterLine))
                return 40;

            return 0;
        }
        
        /*
        void CopyByteLineToBitmap(byte[] _pixels, Bitmap _screen, int _y)
        {
            BitmapData data = _screen.LockBits(
                new Rectangle(0, _y, _screen.Width, 1),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            System.Runtime.InteropServices.Marshal.Copy(_pixels, 0, data.Scan0, _screen.Width);
            _screen.UnlockBits(data);
        }
        */

        byte ReadRAM(int _address)
        {
            int totalAddress = VICState.BankBaseAddress + _address;            

            if (((totalAddress >= 0x1000) && (totalAddress < 0x2000)) ||
                ((totalAddress >= 0x9000) && (totalAddress < 0xa000)))
            {
                int adr = C64.MPU.CHAREN_ROM.BaseAddress + (totalAddress % 0x1000);
                return C64.MPU.CHAREN_ROM.Read(adr, false);
            }

            // byte mem = C64.MPU.RAM.Read(totalAddress, false);
            byte mem = C64.MPU.Read(totalAddress, false);
            return mem;
        }
                
        void Render_StandardCharacterMode(int screenLine, int _startX, ref byte[] _pixels)
        {
            ushort scr = GetScreenAddress();
            ushort charmap = GetCharMemAddress();

            int subLine = screenLine % 8;
            int textLine = (screenLine / 8);

            int x = _startX;

            for (int column = 0; column < 40; column++)
            {
                byte c = ReadRAM(scr + textLine * 40 + column);

                byte colorRam = ColorRAM[column];

                byte bits = ReadRAM(charmap + c * 8 + subLine);

                for (int b = 0x80; b >= 0x01; b >>= 1)
                {
                    if ((bits & b) > 0)
                    {
                        _pixels[x] = colorRam;
                    }
                    else
                    {
                        _pixels[x] = D021_BackgroundColor;
                    }
                    x++;
                }
            }
        }

        void Render_MultiColorCharacterMode(int screenLine, int _startX, ref byte[] _pixels)
        {
            int scr = VICState.ScreenAddress;
            int charmap = VICState.CharMemAddress;

            int subLine = screenLine % 8;
            int textLine = (screenLine / 8);

            int x = _startX;

            for (int column = 0; column < 40; column++)
            {                
                byte charIndex = ReadRAM(scr + textLine * 40 + column);                
                byte colorRam = ColorRAM[column];
                byte charPix = ReadRAM(charmap + charIndex * 8 + subLine);

                bool useMulticolor = ((colorRam & 0x08) > 0);
                colorRam &= 0x07;

                if (useMulticolor)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int colIndex = ((charPix & 0xc0) >> 6);
                        charPix <<= 2;

                        byte col = 0;

                        switch (colIndex)
                        {
                            case 0x00: col = D021_BackgroundColor; break;
                            case 0x01: col = D022; break;
                            case 0x02: col = D023; break;
                            case 0x03: col = colorRam; break;
                        }

                        _pixels[x] = col;
                        x++;
                        _pixels[x] = col;
                        x++;
                    }
                }

                else // monochrom
                {
                    for (int i=0; i<8; i++)
                    {
                        bool bit = (charPix > 127);
                        charPix <<= 1;

                        if (bit)
                        {
                            _pixels[x] = colorRam;
                        }
                        else
                        {
                            _pixels[x] = D021_BackgroundColor;
                        }

                        x++;
                    }
                }
            }
        }

        void Render_AllCharacterModes(int screenLine, ref byte[] _pixels)
        {
            int OffsetX = (D016_Status2 & 0x07);
            bool MultiColorModus = ((D016_Status2 & 0x10) > 0);            

            ushort scr = GetScreenAddress();
            ushort charmap = GetCharMemAddress();

            int subLine = screenLine % 8;
            int textLine = (screenLine / 8);

            int x = Settings.BorderLeft + OffsetX;

            for (int column = 0; column < 40; column++)
            {
                byte c = ReadRAM(scr + textLine * 40 + column);
                //if (c != 0)
                {
                    byte colorRam = ColorRAM[column];
                    byte bits = ReadRAM(charmap + c * 8 + subLine);

                    bool useMulticolor = ((MultiColorModus) && ((colorRam & 0x08) > 0));
                    if (useMulticolor)
                        colorRam &= 0x07;

                    if (useMulticolor)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            int colIndex = ((bits & 0xc0) >> 6);
                            bits <<= 2;

                            byte col = 0;

                            switch (colIndex)
                            {
                                case 0x00: col = D021_BackgroundColor; break;
                                case 0x01: col = D022; break;
                                case 0x02: col = D023; break;
                                case 0x03: col = colorRam; break;
                            }

                            _pixels[x] = col;
                            x++;
                            _pixels[x] = col;
                            x++;
                        }
                    }

                    else // monochrom
                    {
                        for (int b = 0x80; b >= 0x01; b >>= 1)
                        {
                            if ((bits & b) > 0)
                            {
                                _pixels[x] = colorRam;
                            }
                            else
                            {
                                _pixels[x] = D021_BackgroundColor;
                            }
                            x++;
                        }
                    }
                }
            }
        }

        void Render_MultiColorBitmapMode(int screenLine, ref byte[] _pixels)
        {
            int OffsetX = (D016_Status2 & 0x07);
            int bitmapAdr = GetBitmapAddress();
            int scrAdr = GetScreenAddress();

            int subLine = screenLine % 8;
            int textLine = (screenLine / 8);

            int x = Settings.BorderLeft + OffsetX;

            for (int column = 0; column < 40; column++)
            {
                byte scrMem = ReadRAM(scrAdr + textLine * 40 + column);
                byte bmpMem = ReadRAM(bitmapAdr + textLine * 40 * 8 + column * 8 + subLine);
                byte colorRam = ColorRAM[column];

                byte bits = bmpMem;

                {
                    for (int i = 0; i < 4; i++)
                    {
                        int colIndex = ((bits & 0xc0) >> 6);
                        bits <<= 2;

                        byte col = 0;

                        switch (colIndex)
                        {
                            case 0x00: col = D021_BackgroundColor; break;
                            case 0x01: col = (byte)(scrMem >> 4); break;
                            case 0x02: col = (byte)(scrMem & 0x0f); break;
                            case 0x03: col = (byte)(colorRam & 0x0f); break;
                        }

                        _pixels[x] = col;
                        x++;
                        _pixels[x] = col;
                        x++;
                    }

                }
            }
        }

        void Render_BitmapMode(int screenLine, ref byte[] _pixels)
        {
            int OffsetX = (D016_Status2 & 0x07);
            int bitmapAdr = GetBitmapAddress();
            int scrAdr = GetScreenAddress();

            int subLine = screenLine % 8;
            int textLine = (screenLine / 8);

            int x = Settings.BorderLeft + OffsetX;

            for (int column = 0; column < 40; column++)
            {
                byte scrMem = ReadRAM(scrAdr + textLine * 40 + column);
                byte bmpMem = ReadRAM(bitmapAdr + textLine * 40 * 8 + column * 8 + subLine);
                byte colorRam = ColorRAM[column];

                byte bits = bmpMem;
                for (int b = 0x80; b >= 0x01; b >>= 1)
                {
                    if ((bits & b) > 0)
                    {
                        _pixels[x] = colorRam;
                    }
                    else
                    {
                        _pixels[x] = D021_BackgroundColor;
                    }
                    x++;
                }
            }
        }

        void RenderBorderToArray(int y, ref byte[] _pixels)
        {
            byte col = (byte)VICState.BorderColor;

            for (int i = 0; i < VICState.FirstPixel; i++)
                _pixels[i] = col;

            for (int i = VICState.LastPixel+1; i < Settings.PixelsPerScanLine; i++)
                _pixels[i] = col;
        }

        void RenderLineToArray(int _rasterLine, ref byte[] _pixels)
        {
            if ((_rasterLine < Settings.BorderTop) || (_rasterLine >= Settings.BorderTop + 200))
            {
                for (int i = 0; i < 403; i++)
                    _pixels[i] = (byte)VICState.BorderColor;
                return;
            }

            int screenLine = _rasterLine - Settings.RasterLine_Row0 - VICState.OffsetY;

            if (screenLine < 0)
                return;

            if (screenLine > 199)
                return;

            //  black
            for (int i = 0; i < 403; i++)
                _pixels[i] = 0;

            //  simluate DMA
            if (screenLine % 8 == 0)            
            {
                int textLine = screenLine / 8;
                int adr = 0xd800 + 40 * textLine;
                for (int i = 0; i < 40; i++)
                {
                    byte col = C64.ColorRAM.Read(adr, false);
                    col &= 0x0f;
                    adr++;
                    ColorRAM[i] = col;
                }
            }

            if ((_rasterLine >= 216) && (_rasterLine <= 225))
            {
                //  spy spy 2: intro - scroll-effekt klappt nicht
            }

            int startX = Settings.BorderLeft + VICState.OffsetX;

            int GraphicsMode = 0;
            if (VICState.MCM) GraphicsMode += 1;
            if (VICState.BMM) GraphicsMode += 2;
            if (VICState.ECM) GraphicsMode += 4;

            switch (GraphicsMode)
            {
                case 0:
                    {
                        //  monochrome text
                        Render_StandardCharacterMode(screenLine, startX, ref _pixels);
                        break;
                    }
                case 1:
                    {                        
                        //  multi-color text
                        Render_MultiColorCharacterMode(screenLine, startX, ref _pixels);
                        break;
                    }
                case 2:
                    {
                        //  bitmap mode
                        Render_BitmapMode(screenLine, ref _pixels);
                        break;
                    }

                case 3:
                    {
                        //  multicolor bitmap mode
                        Render_MultiColorBitmapMode(screenLine, ref _pixels);
                        break;
                    }

                case 4:
                    {
                        //  Extended Background Color Mode 
                        //  comic bakery uses this mode!
                        // test only
                        Render_MultiColorBitmapMode(screenLine, ref _pixels);
                        break;
                    }

                default:
                    {
                        break;
                    }
            }

            //  draw sprites
            byte[] sprCollisionMap = new byte[640];
            for (int i = 0; i < 640; i++)
                sprCollisionMap[i] = 0;

            int[] spritePixels = new int[640];
            for (int i = 0; i < 640; i++)
                spritePixels[i] = -1;

            bool sprColl = false;
            bool bgColl = false;

            for (int i = 0; i < 8; i++)
            {
                RenderSpriteLine(7-i, spritePixels, _pixels, _rasterLine, sprCollisionMap, ref bgColl, ref sprColl);
            }

            //  copy sprite-pixels on screen
            for (int i=0; i<403; i++)
            {
                if (spritePixels[i] >= 0)
                    _pixels[i] = (byte)spritePixels[i];
            }

            if (bgColl)
                NotifyIRQ(0x02);

            if (sprColl)
                NotifyIRQ(0x04);
        }

        public bool IsSpriteEnabled(int _sprNum)
        {
            return ((D015_Spr_Enabled & (1 << _sprNum)) > 0);
        }

        public bool IsBackgroundColor(byte _col)
        {
            if (_col == VICState.BackgrundColor) return true;
            //if (_col == D022) return true;
            //if (_col == D023) return true;
            return false;
        }

        public void RenderSpriteLine(int _sprNum, int[] _spritePixels, byte[] _screenPixels, int _rasterLine, byte[] _sprCollisionMap, ref bool _bkCollision, ref bool _sprCollision)
        {
            byte currSpriteBit = (byte)(1 << _sprNum);

            if (!IsSpriteEnabled(_sprNum))
                return;

            ushort sprX = Read(0xd000 + 2 * _sprNum, true);
            ushort sprY = Read(0xd001 + 2 * _sprNum, true);

            if ((D010_Sprs_XBit8 & (1 << _sprNum)) > 0)
                sprX += 256;

            bool stretchY = ((D017_Spr_StretchY & (1 << _sprNum)) > 0);
            bool spriteBehindBackground = ((D01B & (1 << _sprNum)) > 0);
            bool multiColor = ((D01C & (1 << _sprNum)) > 0);
            bool stretchX = ((D01D_Spr_StretchX& (1 << _sprNum)) > 0);
            byte sprColor = (byte)(Read(0xd027 + _sprNum, true) & 0x0f);

            int x0 = sprX + Settings.BorderLeft + Settings.SpriteDisplacementX;
            int y0 = sprY + Settings.BorderTop + Settings.SpriteDisplacementY;

            int currLine = _rasterLine - y0;
            if (stretchY)
                currLine /= 2;
            
            if ((currLine >= 0) && (currLine <= 20))
            {

                //  debug
                if (DebugOptions.RenderSpriteBorder)
                {
                    int w = 23;
                    if (stretchX) w = w * 2 + 1;

                    byte col = (byte)_sprNum;

                    if (currLine % 2 == 0)
                        _spritePixels[x0] = col;
                    else
                        _spritePixels[x0 + w] = col;

                    if ((currLine == 0) || (currLine == 20))
                    {
                        for (int i = 0; i < w; i+=2)
                            _spritePixels[x0 + i] = col;
                    }
                }

                //  determine sprite pointer
                byte p = ReadRAM(VICState.ScreenAddress + 0x03f8 + _sprNum);
                //p = 0xbd;
                //p = 0xbc;
                int sprPtr = p * 64;                

                sprPtr += currLine * 3;
                
                int p24 = ReadRAM(sprPtr) << 16;                
                p24 |= ReadRAM(sprPtr + 1) << 8;
                p24 |= ReadRAM(sprPtr + 2);

                if (p24 == 0)
                    return;

                //p24 = 0x000012f3;
                //multiColor = true;

                int px = x0;

                if (multiColor)
                {
                    int pixelWidth = 2;
                    if (stretchX)
                        pixelWidth = 4;

                    for (int x = 0; x < 12; x++)
                    {
                        byte col = (byte)((p24 >> (22 - 2*x)) & 0x03);

                        byte sprCol = sprColor;
                        if (col == 1)
                            sprCol = D025;
                        if (col == 3)
                            sprCol = D026;

                        for (int i = 0; i < pixelWidth; i++)
                        {
                            if (col > 0)
                            {
                                bool bgCollision = false;
                                bool sprCollision = false;

                                if ((px >= VICState.FirstPixel) && (px <= VICState.LastPixel))
                                {
                                    bool isBG = IsBackgroundColor(_screenPixels[px]);
                                    bgCollision = !isBG;
                                    sprCollision = (_sprCollisionMap[px] > 0);
                                    if ((isBG) || (!spriteBehindBackground))
                                        _spritePixels[px] = sprCol;

                                    _sprCollisionMap[px] |= currSpriteBit;

                                    if (bgCollision)
                                    {
                                        D01F_SpriteBackgroundCollision |= currSpriteBit;
                                        _bkCollision = true;
                                    }

                                    if (sprCollision)
                                    {
                                        D01E_SpriteSpriteCollision |= _sprCollisionMap[px];
                                        _sprCollision = true;
                                    }
                                }
                            }
                            px++;
                        }
                    }
                }
                else
                {
                    int pixelWidth = 1;
                    if (stretchX)
                        pixelWidth = 2;

                    for (int x = 0; x < 24; x++)
                    {
                        for (int i=0; i< pixelWidth; i++)
                        {
                            byte col = (byte)((p24 >> (23-x)) & 0x01);
                            //if ((p24 & (1 << (23 - x))) > 0)
                            if (col > 0)
                            {
                                bool bgCollision = false;
                                bool sprCollision = false;

                                if ((px >= VICState.FirstPixel) && (px <= VICState.LastPixel))
                                {
                                    bool isBG = IsBackgroundColor(_screenPixels[px]);

                                    bgCollision = !isBG;
                                    sprCollision = (_sprCollisionMap[px] > 0);

                                    if ((isBG) || (!spriteBehindBackground))
                                        _spritePixels[px] = sprColor;

                                    _sprCollisionMap[px] |= currSpriteBit;

                                    if (bgCollision)
                                   {
                                        D01F_SpriteBackgroundCollision |= currSpriteBit;
                                        _bkCollision = true;
                                    }

                                    if (sprCollision)
                                    {
                                        byte collSpr = _sprCollisionMap[px];
                                        D01E_SpriteSpriteCollision |= _sprCollisionMap[px];
                                        D01E_SpriteSpriteCollision |= currSpriteBit;
                                        _sprCollision = true;
                                    }

                                }
                            }
                            px++;
                        }
                    }
                }
            }
        }

        ushort GetBankAddress()
        {
            int bank = (C64.CIA2.PRA & 0x03);

            if (bank == 0) return 0xc000;
            if (bank == 1) return 0x8000;
            if (bank == 2) return 0x4000;
            if (bank == 3) return 0x0000;

            return 0x0000;
        }

        public ushort GetBitmapAddress()
        {
            ushort adr = 0;

            if ((D018_MemCtrl & 8) > 0)
                adr += 0x2000;
            
            return adr;
        }

        public ushort GetCharMemAddress()
        {
            int b = ((D018_MemCtrl >> 1) & 0x07);

            ushort adr = (ushort)(b * 0x800);
            return adr;
        }

        public ushort GetScreenAddress()
        {
            int b = ((D018_MemCtrl >> 4) & 0x0f);
            
            ushort adr = (ushort)(b * 0x400);
            return adr;
        }

        public override byte Read(int _fullAddress, bool _internal)
        {
            switch (_fullAddress)
            {
                case 0xd000: return D000_Spr0_X;
                case 0xd001: return D001_Spr0_Y;
                case 0xd002: return D002_Spr1_X;
                case 0xd003: return D003_Spr1_Y;
                case 0xd004: return D004_Spr2_X;
                case 0xd005: return D005_Spr2_Y;
                case 0xd006: return D006_Spr3_X;
                case 0xd007: return D007_Spr3_Y;
                case 0xd008: return D008_Spr4_X;
                case 0xd009: return D009_Spr4_Y;
                case 0xd00A: return D00A_Spr5_X;
                case 0xd00B: return D00B_Spr5_Y;
                case 0xd00C: return D00C_Spr6_X;
                case 0xd00D: return D00D_Spr6_Y;
                case 0xd00E: return D00E_Spr7_X;
                case 0xd00F: return D00F_Spr7_Y;
                case 0xd010: return D010_Sprs_XBit8;
                case 0xd011: return D011_Status;
                case 0xd012: return D012_RasterLO;
                case 0xd013: return D013_LightpenX;
                case 0xd014: return D014_LightpenY;
                case 0xd015: return D015_Spr_Enabled;
                case 0xD016: return D016_Status2;
                case 0xd017: return D017_Spr_StretchY;
                case 0xd018: return D018_MemCtrl;
                case 0xd019:
                    {
                        // The bit 7 in the latch $d019 reflects the inverted state of the IRQ output
                        // of the VIC.
                        byte val = D019_IRQFlagsLatch;
                        if (HasIRQ())
                            val |= 0x80;

                        return val;
                    }
                case 0xD01A: return D01A_IRQMask;
                case 0xd01b: return D01B;
                case 0xd01c: return D01C;
                case 0xd01d: return D01D_Spr_StretchX;
                case 0xd01e:
                    {
                        byte data = D01E_SpriteSpriteCollision;
                        if (!_internal)
                            D01E_SpriteSpriteCollision = 0;
                        return data;
                    }

                case 0xd01f:
                    {
                        byte data = D01F_SpriteBackgroundCollision;
                        if (!_internal)
                            D01F_SpriteBackgroundCollision = 0;
                        return data;
                    }

                case 0xd020: return D020_BorderColor;
                case 0xd021: return D021_BackgroundColor;
                case 0xd022: return D022;
                case 0xd023: return D023;
                case 0xd024: return D024;
                case 0xd025: return D025;
                case 0xd026: return D026;
                case 0xd027: return D027;
                case 0xd028: return D028;
                case 0xd029: return D029;
                case 0xd02a: return D02A;
                case 0xd02b: return D02B;
                case 0xd02c: return D02C;
                case 0xd02d: return D02D;
                case 0xd02e: return D02E;
                case 0xd02f: return D02F;
                case 0xd030: return D030;
            }

            return base.Read(_fullAddress, _internal);
        }

        public override void Write(int _fullAddress, byte _val, bool _internal)
        {
            switch (_fullAddress)
            {
                case 0xd000: D000_Spr0_X = _val; break;
                case 0xd001: D001_Spr0_Y = _val; break;
                case 0xd002: D002_Spr1_X = _val; break;
                case 0xd003: D003_Spr1_Y = _val; break;
                case 0xd004: D004_Spr2_X = _val; break;
                case 0xd005: D005_Spr2_Y = _val; break;
                case 0xd006: D006_Spr3_X = _val; break;
                case 0xd007: D007_Spr3_Y = _val; break;
                case 0xd008: D008_Spr4_X = _val; break;
                case 0xd009: D009_Spr4_Y = _val; break;
                case 0xd00A: D00A_Spr5_X = _val; break;
                case 0xd00B: D00B_Spr5_Y = _val; break;
                case 0xd00C: D00C_Spr6_X = _val; break;
                case 0xd00D: D00D_Spr6_Y = _val; break;
                case 0xd00E: D00E_Spr7_X = _val; break;
                case 0xd00F: D00F_Spr7_Y = _val; break;
                case 0xd010: D010_Sprs_XBit8 = _val; break;
                case 0xd011:
                    {
                        D011_Status = _val;

                        RasterlineIRQ &= 0xff;
                        if ((D011_Status & 0x80) > 0)
                            RasterlineIRQ += 256;

                        break;
                    }
                case 0xd012:
                    {
                        RasterlineIRQ = _val;
                        if ((D011_Status & 0x80) > 0)
                            RasterlineIRQ += 256;
                        break;
                    }
                case 0xd013: D013_LightpenX = _val; break;
                case 0xd014: D014_LightpenY = _val; break;
                case 0xd015: D015_Spr_Enabled = _val; break;
                case 0xD016:
                    {
                        D016_Status2 = _val;
                        break;
                    }
                case 0xd017: D017_Spr_StretchY = _val; break;
                case 0xd018: D018_MemCtrl = _val; break;
                case 0xd019:
                    {
                        if (_internal)
                        {
                            D019_IRQFlagsLatch = _val;
                            break;
                        }

                        //  set bit to clear IRQ-Flag
                        D019_IRQFlagsLatch &= (byte)(~_val);
                        D019_IRQFlagsLatch &= 0x7f; // clear bit 8
                        _val = D019_IRQFlagsLatch;
                        break;
                    }

                case 0xD01A:
                    {
                        D01A_IRQMask = _val;
                        break;
                    }
                case 0xd01b: D01B = _val; break;
                case 0xd01c: D01C = _val; break;
                case 0xd01d: D01D_Spr_StretchX = _val; break;
                case 0xd01e: D01E_SpriteSpriteCollision = _val; break;
                case 0xd01f: D01F_SpriteBackgroundCollision = _val; break;
                case 0xd020:
                    {
                        D020_BorderColor = (byte)(_val & 0x0f);
                        break;
                    }
                case 0xd021:
                    {
                        D021_BackgroundColor = (byte)(_val & 0x0f);
                        break;
                    }
                case 0xd022: D022 = _val; break;
                case 0xd023: D023 = _val; break;
                case 0xd024: D024 = _val; break;
                case 0xd025: D025 = _val; break;
                case 0xd026: D026 = _val; break;
                case 0xd027: D027 = _val; break;
                case 0xd028: D028 = _val; break;
                case 0xd029: D029 = _val; break;
                case 0xd02a: D02A = _val; break;
                case 0xd02b: D02B = _val; break;
                case 0xd02c: D02C = _val; break;
                case 0xd02d: D02D = _val; break;
                case 0xd02e: D02E = _val; break;
                case 0xd02f: D02F = _val; break;
                case 0xd030: D030 = _val; break;
            }

            base.Write(_fullAddress, _val, _internal);
        }

        public void VerticalRetrace()
        {
            if (RasterlineIRQ >= Settings.NumScanLines)
               UpdateRasterLine(RasterlineIRQ);

            if (OutputEnabled())
            {
                Frames++;
            }

        }

        public VIC2_State GetState()
        {
            VIC2_State state = new VIC2_State();

            state.OutputEnabled = (Read(0xd011, true) & BIT.B4) > 0;
            
            state.BorderColor = (Read(0xd020, true) & 0x0f);
            state.BackgrundColor = (Read(0xd021, true) & 0x0f);

            state.OffsetY = (Read(0xd011, true) & 0x07) - 3;
            state.OffsetX = (Read(0xd016, true) & 0x07);

            state.TextColumns = 38;
            if ((Read(0xd016, true) & 0x08) > 0)
                state.TextColumns = 40;

            state.TextRows = 24;
            if ((Read(0xd011, true) & BIT.B3) > 0)
                state.TextRows = 25;

            byte _D011 = Read(0xd011, true);
            byte _D016 = Read(0xd016, true);

            state.BMM = (_D011 & BIT.B5) > 0;
            state.ECM = (_D011 & BIT.B6) > 0;
            state.MCM = (_D016 & BIT.B4) > 0;

            //  memory-bank
            state.BankBaseAddress = GetBankAddress();

            byte VIC_D018_MemCtrl = Read(0xd018, true);
            state.ScreenAddress = ((VIC_D018_MemCtrl >> 4) & 0x0f) * 0x400;

            state.BitmapAddress = 0;
            if ((VIC_D018_MemCtrl & 8) > 0)
                state.BitmapAddress += 0x2000;

            state.CharMemAddress = ((VIC_D018_MemCtrl >> 1) & 0x07) * 0x800;
            state.CharMemAddress = GetCharMemAddress();
            //  rendering parameters
            state.HorStartPixel = Settings.BorderLeft + state.OffsetX;

            state.FirstPixel = Settings.BorderLeft;
            if (state.TextColumns == 38)
                state.FirstPixel += 7;
            state.LastPixel = state.FirstPixel + (8 * state.TextColumns) - 1;

            state.FirstScreenLine = 0;
            state.LastScreenLine = 199;

            if (state.TextRows == 24)
            {
                state.FirstScreenLine = 0;
                state.LastScreenLine = 191;
                /*
                if ((D011_Status & 0x08) == 0)
                {
                    minScreenLine = 6;
                    maxScreenLine = 196;
                    screenLine += 3;
                }
                */
            }

            return state;
        }

        public override void Reset()
        {
            currentRasterLine = -1;
            Frames = 0;
            RasterlineIRQ = -1;

            base.Reset();
        }
    }

}
