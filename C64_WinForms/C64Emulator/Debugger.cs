using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C64_WinForms.C64Emulator
{

    public class ZeroPage
    {
        // https://www.c64-wiki.de/wiki/Zeropage

        byte[] Data = new byte[512];

        byte FAC1_E;
        byte[] FAC1_M = new byte[4];
        byte FAC1_Sign;

        byte[] String_0x100 = new byte[16];

        public ZeroPage(CPU_6510 _cpu)
        {            
            for (int i = 0; i < 512; i++)
            {
                Data[i] = _cpu.PeekMemory(i);
            }

            FAC1_E = Data[0x61];
            FAC1_M[0] = Data[0x62];
            FAC1_M[1] = Data[0x63];
            FAC1_M[2] = Data[0x64];
            FAC1_M[3] = Data[0x65];
            FAC1_Sign = Data[0x66];

            for (int i=0; i<16; i++)
                String_0x100[i] = _cpu.PeekMemory(0x100 + i);
        }
    }

    public struct CallstackDetail
    {
        public ushort Adress;
        public CPUState CPUState;

        public override string ToString()
        {
            string str = string.Format(".{0,4:X4}", Adress);

            string strFunc = "";
            if (Debugger.KernalFunctions.TryGetValue(Adress, out strFunc))
            {
                str += string.Format(": {0}", strFunc);
            }

            return str;
        }
    }

    public class Debugger
    {
        public Stack<CallstackDetail> CallStack = new Stack<CallstackDetail>();
        SortedSet<ushort> Breakpoints = new SortedSet<ushort>();
        string[] ParamPrintMask;
        public DecodedInstruction[] Trace = new DecodedInstruction[1024];
        int lastTraceIndex = 0;

        public static Dictionary<int, string> KernalFunctions = new Dictionary<int, string>();

        public bool TraceJSRs = false;
        public bool TraceOPCodes = false;

        public int JSR_Depth = 0;

        public void AddBreakpoint(ushort _adr)
        {
            if (!Breakpoints.Contains(_adr))
                Breakpoints.Add(_adr);
        }

        public void RemoveBreakpoint(ushort _adr)
        {
            if (Breakpoints.Contains(_adr))
                Breakpoints.Remove(_adr);
        }

        public bool IsBreakpoint(ushort _adr)
        {
            return Breakpoints.Contains(_adr);
        }

        public void OnAfter_JSR(CPU_6510 _cpu, DecodedInstruction _instr, int _newPC)
        {
            JSR_Depth++;

            //  JSR
            CallstackDetail det = new CallstackDetail();
            det.Adress = _instr.adr16;
            det.CPUState = _cpu.GetState();

            string func;
            if (KernalFunctions.TryGetValue(det.Adress, out func))
            {

            }

            if (TraceJSRs)
            {
                Console.Out.WriteLine(det.ToString());                
            }
            
            //TraceOPCodes = false;
            CallStack.Push(det);
        }

        public void OnAfter_RTS(CPU_6510 _cpu, DecodedInstruction _instr)
        {
            //  RTS
            if (JSR_Depth > 0)
            {
                CallStack.Pop();
                JSR_Depth--;
            }
        }

        public void OnReadMem(int _fullAdress)
        {
            if ((_fullAdress >= 0x61) && (_fullAdress <= 0x66))
            {

            }
        }

        public void OnWriteMem(int _fullAdress, byte _data)
        {
            if (_fullAdress == 0x0063)
            {
            }
            if ((_fullAdress >= 0x100) && (_fullAdress <= 0x10A))
            {
                // TraceOPCodes = false;
                //  string area
            }
        }

        public void OnMemoryChanged(int _fullAdress, byte _oldValue, byte _newValue)
        {
            if (_fullAdress == 0x0063)
            {
                //  key pressed

            }
        }


        public void OnNextOpcode(CPU_6510 _cpu, DecodedInstruction _instr)
        {
            ushort adr = _instr.PC;

            lastTraceIndex++;
            if (lastTraceIndex >= 64)
                lastTraceIndex = 0;

            Trace[lastTraceIndex] = _instr;

            ushort a0 = adr;

            if (adr == 0xBE1D)
            {

            }

            // TraceOPCodes = false;
            if (IsBreakpoint(adr))
            {
                //TraceOPCodes = true;
            }
            // TraceOPCodes = false;

            // AddBreakpoint(0xB91D);

            if (false)
            {
                string func = "";
                if (KernalFunctions.TryGetValue(adr, out func))
                {
                    Console.Out.WriteLine("${0,4:X4}: {1}", adr, func);
                }
            }

            if (TraceOPCodes)
            {
                string func = "";
                if (KernalFunctions.TryGetValue(adr, out func))
                {
                    Console.Out.WriteLine("${0,4:X4}: {1}", adr, func);
                }

                Console.Out.WriteLine(_instr.ToString());
            }
        }

        public Debugger()
        {
            // AddBreakpoint(0xE5CD); // auf tastatur warten        

            //AddBreakpoint(0xe433); // basic-meldung ausgeben
            // AddBreakpoint(0xbddf);
            // AddBreakpoint(0xeab6); // taste gedrückt und erkannt!
                        
            // AddBreakpoint(0xe43a); // 38911 BASIC BYTES FREE 
            //  note: 120 bytes wird korrekt gedruckt - 121 bytes nicht (A = 0, X = 121)
            // AddBreakpoint(0xbdcd);
            // AddBreakpoint(0xbdd7);
            // AddBreakpoint(0xbdda);
            //AddBreakpoint(0xB91D);

            //  funktion prüfen:
            // Rechtsverschieben eines Registers
            // .,B983 A2 25    LDX #$25        Offset-Zeiger auf Register


            //  
            ParamPrintMask = new string[(int)AddressMode.other];
            ParamPrintMask[(int)AddressMode.Implied]        = "{0}";
            ParamPrintMask[(int)AddressMode.Immediate]      = "{0} #${1,2:X2}";
            ParamPrintMask[(int)AddressMode.ZeroPage]       = "{0} ${1,2:X2}";
            ParamPrintMask[(int)AddressMode.ZeroPageX]      = "{0} ${1,2:X2},X";
            ParamPrintMask[(int)AddressMode.ZeroPageY]      = "{0} ${1,2:X2},Y";
            ParamPrintMask[(int)AddressMode.IndZeroPageX]   = "{0} (${1,2:X2},X)";
            ParamPrintMask[(int)AddressMode.IndZeroPageY]   = "{0} (${1,2:X2}),Y";
            ParamPrintMask[(int)AddressMode.Absolute] = "{0} ${2,2:X2}{1,2:X2}";
            ParamPrintMask[(int)AddressMode.AbsoluteX] = "{0} ${2,2:X2}{1,2:X2},X";
            ParamPrintMask[(int)AddressMode.AbsoluteY] = "{0} ${2,2:X2}{1,2:X2},Y";
            ParamPrintMask[(int)AddressMode.Indirect] = "{0} (${2,2:X2}{1,2:X2})";
            ParamPrintMask[(int)AddressMode.Relative] = "{0} ${3,4:X4}  ->  +${1,2:X2}";

            //  init function map
            KernalFunctions.Add(0xE000,"BASIC-Funktion EXP – Fortsetzung von $BFFF");
            KernalFunctions.Add(0xE043,"Polynomberechnung");
            KernalFunctions.Add(0xE08D,"2 Konstanten für RND");
            KernalFunctions.Add(0xE097,"BASIC-Funktion RND");
            KernalFunctions.Add(0xE0F9,"Fehlerauswertung nach I/O-Routinen in BASIC");
            KernalFunctions.Add(0xE10C,"PETSCII-Zeichen ausgeben mit CHROUT, Wert muss im Akku stehen");
            KernalFunctions.Add(0xE112,"PETSCII-Zeichen holen mit CHRIN (Eingabegerät wählbar)");
            KernalFunctions.Add(0xE118,"Ausgabegerät setzen mit CHKOUT");
            KernalFunctions.Add(0xE11E,"Eingabegerät setzen mit CHKIN");
            KernalFunctions.Add(0xE124,"Zeichen aus Tastaturpuffer in Akku holen mit GETIN");
            KernalFunctions.Add(0xE12A,"BASIC-Befehl SYS");
            KernalFunctions.Add(0xE156,"BASIC-Befehl SAVE");
            KernalFunctions.Add(0xE165,"BASIC-Befehl VERIFY");
            KernalFunctions.Add(0xE168,"BASIC-Befehl LOAD");
            KernalFunctions.Add(0xE1BE,"BASIC-Befehl OPEN");
            KernalFunctions.Add(0xE1C7,"BASIC-Befehl CLOSE");
            KernalFunctions.Add(0xE1D4,"Parameter für LOAD, SAVE und VERIFY aus BASIC-Text holen");
            KernalFunctions.Add(0xE200,"Prüft auf Komma und holt 1-Byte-Wert nach X");
            KernalFunctions.Add(0xE206,"Prüft auf weitere Zeichen");
            KernalFunctions.Add(0xE20E,"Prüft auf Komma und weitere Zeichen");
            KernalFunctions.Add(0xE219,"Parameter für OPEN und CLOSE holen");
            KernalFunctions.Add(0xE264,"BASIC-Funktion COS");
            KernalFunctions.Add(0xE26B,"BASIC-Funktion SIN");
            KernalFunctions.Add(0xE2B4,"BASIC-Funktion TAN");
            KernalFunctions.Add(0xE2E0,"Trigonometrische Konstante 1.570796327 = PI/2");
            KernalFunctions.Add(0xE2E5,"Trig. Konstante 6.28318531 = 2*PI");
            KernalFunctions.Add(0xE2EA,"Trig. Konstante 0.25");
            KernalFunctions.Add(0xE2EF,"Trig. Konstante 5 = Polynomgrad, dann 6 Koeffizienten");
            KernalFunctions.Add(0xE2F0,"Trig. Konstante -14.3813907");
            KernalFunctions.Add(0xE2F5,"Trig. Konstante 42.0077971");
            KernalFunctions.Add(0xE2FA,"Trig. Konstante -76.7041703");
            KernalFunctions.Add(0xE2FF,"Trig. Konstante 81.6052237");
            KernalFunctions.Add(0xE304,"Trig. Konstante -41.3417021");
            KernalFunctions.Add(0xE309,"Trig. Konstante 6.28318531 = 2*PI");
            KernalFunctions.Add(0xE30E,"BASIC-Funktion ATN");
            KernalFunctions.Add(0xE33E,"ATN Konstante 11 = Polynomgrad, dann 12 Koeffizienten");
            KernalFunctions.Add(0xE33F,"ATN Konstante -0.00068479391");
            KernalFunctions.Add(0xE344,"ATN Konstante 0.00485094216");
            KernalFunctions.Add(0xE349,"ATN Konstante -0.161117018");
            KernalFunctions.Add(0xE34E,"ATN Konstante 0.034209638");
            KernalFunctions.Add(0xE353,"ATN Konstante -0.0542791328");
            KernalFunctions.Add(0xE358,"ATN Konstante 0.0724571965");
            KernalFunctions.Add(0xE35D,"ATN Konstante -0.0898023954");
            KernalFunctions.Add(0xE362,"ATN Konstante 0.110932413");
            KernalFunctions.Add(0xE367,"ATN Konstante -0.142839808");
            KernalFunctions.Add(0xE36C,"ATN Konstante 0.19999912");
            KernalFunctions.Add(0xE371,"ATN Konstante -0.333333316");
            KernalFunctions.Add(0xE376,"ATN Konstante 1.00");
            KernalFunctions.Add(0xE37B,"BASIC-Warmstart nach RUNSTOP/RESTORE bzw. BRK (NMI-Einsprung)");
            KernalFunctions.Add(0xE394,"BASIC-Kaltstart (Reset)");
            KernalFunctions.Add(0xE3A2,"Kopie der CHRGET-Routine für die Zeropage");
            KernalFunctions.Add(0xE3BA,"Konstante 0.811635157 = Anfangswert für RND-Funktion:");
            KernalFunctions.Add(0xE3BF,"RAM initialisieren für BASIC");
            KernalFunctions.Add(0xE422,"Einschaltmeldung ausgeben");
            KernalFunctions.Add(0xE447,"Tabelle der BASIC-Vektoren (für $0300)");
            KernalFunctions.Add(0xE453,"BASIC-Vektoren aus der Tabelle laden nach $0300 ff.");
            KernalFunctions.Add(0xE45F,"Text der Einschaltmeldungen");
            KernalFunctions.Add(0xE4AD,"BASIC-CHKOUT Routine");
            KernalFunctions.Add(0xE4B7,"Unbenutzter Bereich (ist mit $AA gefüllt)");
            KernalFunctions.Add(0xE4D3,"Patch für RS-232-Routinen");
            KernalFunctions.Add(0xE4DA,"Schreibt Hintergrundfarbe in Farbram (von CLR benutzt, mindert das Flimmern)");
            KernalFunctions.Add(0xE4E0,"Pause (8.5 Sec.), nachdem eine Datei auf der Kassette gefunden wurde");
            KernalFunctions.Add(0xE4EC,"Timerkonstanten für RS-232 Baud Rate, PAL-Version");
            KernalFunctions.Add(0xE500,"IOBASE: Gibt die Basisadresse des CIA in X/Y aus");
            KernalFunctions.Add(0xE505,"SCREEN: Bildschirmgröße einlesen: 40 Spalten in X, 25 Zeilen in Y");
            KernalFunctions.Add(0xE50A,"PLOT: Setzt/holt Cursorposition: X = Zeile, Y = Spalte");
            KernalFunctions.Add(0xE518,"Initialisiert I/O (Bildschirm und Tastatur)");
            KernalFunctions.Add(0xE544,"Löscht Bildschirmspeicher");
            KernalFunctions.Add(0xE566,"Cursor Home: bringt Cursor in Grundposition (oben links)");
            KernalFunctions.Add(0xE56C,"berechnet die Cursorposition, setzt Bildschirmzeiger");
            KernalFunctions.Add(0xE59A,"Videocontroller initialisieren und Cursor Home (wird nicht benutzt)");
            KernalFunctions.Add(0xE5A0,"Videocontroller initialisieren");
            KernalFunctions.Add(0xE5B4,"Holt ein Zeichen aus dem Tastaturpuffer");
            KernalFunctions.Add(0xE5CA,"Wartet auf Tastatureingabe");
            KernalFunctions.Add(0xE632,"Holt ein Zeichen vom Bildschirm");
            KernalFunctions.Add(0xE684,"Testet auf Hochkomma und kehrt ggf. das Hochkomma-Flag $D4 um (EOR #$01)");
            KernalFunctions.Add(0xE691,"Gibt Zeichen auf Bildschirm aus");
            KernalFunctions.Add(0xE6B6,"Springt in neue Zeile bzw. fügt neue Zeile ein");
            KernalFunctions.Add(0xE701,"Rückschritt in vorhergehende Zeile");
            KernalFunctions.Add(0xE716,"Ausgabe (des Zeichens in A) auf Bildschirm incl. Steuerzeichen, Farben");
            KernalFunctions.Add(0xE87C,"Nächste Zeile setzen, ggf. Scrollen");
            KernalFunctions.Add(0xE891,"Aktion nach Taste RETURN");
            KernalFunctions.Add(0xE8A1,"Cursor zur vorigen Zeile, wenn er am Zeilenanfang rückwärts bewegt wird");
            KernalFunctions.Add(0xE8B3,"Cursor zur nächsten Zeile, wenn er am Zeilenende vorwärts bewegt wird");
            KernalFunctions.Add(0xE8CB,"prüft, ob Zeichen in A einer der 16 Farbcodes ist und setzt Farbe entsprechend");
            KernalFunctions.Add(0xE8DA,"Tabelle der Farbcodes – 16 Bytes");
            KernalFunctions.Add(0xE8EA,"Bildschirm scrollen, schiebt Bildschirm um eine Zeile nach oben");
            KernalFunctions.Add(0xE965,"Fügt leere Fortsetzungzeile ein");
            KernalFunctions.Add(0xE9C8,"Schiebt Zeile nach oben");
            KernalFunctions.Add(0xE9E0,"Berechnet Zeiger auf Farbram und Startadresse des Bildschirmspeichers");
            KernalFunctions.Add(0xE9F0,"Setzt Zeiger auf Bildschirmspeicher für Zeile X");
            KernalFunctions.Add(0xE9FF, "Löscht eine Bildschirmzeile (Zeile in X)");
            KernalFunctions.Add(0xEA13,"Setzt Blinkzähler und Farbramzeiger");
            KernalFunctions.Add(0xEA1C,"Schreibt ein Zeichen mit Farbe auf dem Bildschirm (Bildschirmcode im Akku, Farbe in X)");
            KernalFunctions.Add(0xEA24,"Berechnet den Farbram-Zeiger zur aktuellen Cursorposition");
            KernalFunctions.Add(0xEA31,"Interrupt-Routine, verarbeitet alle IRQ-Interrupts");
            KernalFunctions.Add(0xEA81,"Holt A/X/Y aus dem Stapel zurück und beendet IRQ");
            KernalFunctions.Add(0xEA87,"SCNKEY: Tastaturabfrage");
            KernalFunctions.Add(0xEB48,"Prüft auf Shift, Control, Commodore");
            KernalFunctions.Add(0xEB79,"Zeiger auf Tastatur-Dekodiertabelle für Umwandlung der Matrixwerte in PETSCII");
            KernalFunctions.Add(0xEB81,"Dekodiertabelle ungeshiftet");
            KernalFunctions.Add(0xEBC2,"Dekodiertabelle geshiftet");
            KernalFunctions.Add(0xEC03,"Dekodiertabelle mit Commodore-Taste");
            KernalFunctions.Add(0xEC44,"Prüft auf PETSCII-Codes für Steuerzeichen");
            KernalFunctions.Add(0xEC78,"Dekodiertabelle mit Control-Taste");
            KernalFunctions.Add(0xECB9,"Konstanten für Videocontroller");
            KernalFunctions.Add(0xECE7,"Text 'LOAD'(CR) 'RUN'(CR) für den Tastaturpuffer nach Drücken von SHIFT RUN/STOP");
            KernalFunctions.Add(0xECF0,"Tabelle der LSB der Bildschirmzeilen-Anfänge");
            KernalFunctions.Add(0xED09,"TALK: Sendet TALK auf seriellem Bus");
            KernalFunctions.Add(0xED0C,"LISTEN: Sendet LISTEN auf seriellen Bus");
            KernalFunctions.Add(0xED40,"Gibt ein Byte (aus $95) auf seriellen Bus aus");
            KernalFunctions.Add(0xEDB9,"SECOND: Sendet Sekundäradresse nach LISTEN");
            KernalFunctions.Add(0xEDBE,"Gibt ATN frei");
            KernalFunctions.Add(0xEDC7,"TKSA: Gibt Sekundäradresse nach TALK aus");
            KernalFunctions.Add(0xEDDD,"CIOUT: Gibt ein Byte auf seriellem Bus aus");
            KernalFunctions.Add(0xEDEF,"UNTLK: Sendet UNTALK auf seriellem Bus");
            KernalFunctions.Add(0xEDFE,"UNLSN: Sendet UNLISTEN auf seriellem Bus");
            KernalFunctions.Add(0xEE13,"ACPTR: Holt ein Zeichen vom seriellen Bus");
            KernalFunctions.Add(0xEE85,"Clock-Leitung low");
            KernalFunctions.Add(0xEE8E,"Clock-Leitung high");
            KernalFunctions.Add(0xEE97,"Data-Leitung low");
            KernalFunctions.Add(0xEEA0,"Data-Leitung high");
            KernalFunctions.Add(0xEEA9,"Holt Bit vom seriellen Bus ins Carry-Flag");
            KernalFunctions.Add(0xEEB3,"Verzögerung 1 ms");
            KernalFunctions.Add(0xEEBB,"RS-232 Ausgabe");
            KernalFunctions.Add(0xEF06,"Sendet ein Byte");
            KernalFunctions.Add(0xEF2E,"RS-232 Fehlerbehandlung");
            KernalFunctions.Add(0xEF4A,"Berechnet Anzahl der zu sendenden Bits +1");
            KernalFunctions.Add(0xEF59,"Sammelt Bits zu einem Byte");
            KernalFunctions.Add(0xEF7E,"Ermöglicht den Empfang eines Bytes während NMI");
            KernalFunctions.Add(0xEF90,"Testet Startbit nach Empfang");
            KernalFunctions.Add(0xEFE1,"Ausgabe auf RS-232");
            KernalFunctions.Add(0xF017,"Gibt ein RS-232-Zeichen aus");
            KernalFunctions.Add(0xF04D,"Initialisiert RS-232 für Eingabe");
            KernalFunctions.Add(0xF086,"Liest ein RS-232-Zeichen ein");
            KernalFunctions.Add(0xF0A4,"Schützt seriellen Bus und Bandbetrieb vor NMIs");
            KernalFunctions.Add(0xF0BD,"Tabelle der I/O-Meldungen");
            KernalFunctions.Add(0xF12B,"Gibt eine I/O-Meldung der Tabelle aus (Y als Offset)");
            KernalFunctions.Add(0xF13E,"GETIN: Holt ein Zeichen vom Eingabegerät");
            KernalFunctions.Add(0xF157,"CHRIN: Eingabe eines Zeichens");
            KernalFunctions.Add(0xF199,"Holt ein Zeichen vom Band / vom seriellen Bus / von RS-232");
            KernalFunctions.Add(0xF1CA,"CHROUT: Gibt ein Zeichen aus");
            KernalFunctions.Add(0xF20E,"CHKIN: Öffnet Eingabekanal");
            KernalFunctions.Add(0xF250,"CHKOUT: Öffnet Ausgabekanal");
            KernalFunctions.Add(0xF291,"CLOSE: Schließt Datei, logische Dateinummer im Akku");
            KernalFunctions.Add(0xF30F,"Sucht logische Datei (Nummer in X)");
            KernalFunctions.Add(0xF31F,"Setzt Datei-Parameter");
            KernalFunctions.Add(0xF32F,"CLALL; Schließt alle Ein-/Ausgabe-Kanäle");
            KernalFunctions.Add(0xF333,"CLRCHN: Schließt aktiven I/O-Kanal");
            KernalFunctions.Add(0xF34A,"OPEN: Datei öffnen (Dateinummer in $B8)");
            KernalFunctions.Add(0xF3D5,"Datei öffnen auf seriellem Bus");
            KernalFunctions.Add(0xF409,"Datei öffnen auf RS-232");
            KernalFunctions.Add(0xF49E,"LOAD: Daten ins RAM laden von Peripheriegeräten, aber nicht von Tastatur(0), RS-323(2), Bildschirm(3)");
            KernalFunctions.Add(0xF4B8,"Laden vom seriellen Bus");
            KernalFunctions.Add(0xF539,"Laden für Band");
            KernalFunctions.Add(0xF5AF,"Gibt Meldung 'SEARCHING' bzw. 'SEARCHING FOR' aus");
            KernalFunctions.Add(0xF5C1,"Dateiname ausgeben");
            KernalFunctions.Add(0xF5D2,"'LOADING' bzw. 'VERIFYING' ausgeben");
            KernalFunctions.Add(0xF5DD,"SAVE: Daten vom RAM auf Peripheriegeräte sichern, aber nicht auf Tastatur(0), RS-323(2), Bildschirm(3)");
            KernalFunctions.Add(0xF5FA,"Speichern auf seriellen Bus");
            KernalFunctions.Add(0xF65F,"Speichern auf Band");
            KernalFunctions.Add(0xF68F,"'SAVING' ausgeben");
            KernalFunctions.Add(0xF69B,"UDTIM: Erhöht TIME und fragt STOP-Taste ab");
            KernalFunctions.Add(0xF6DD,"RDTIM: Uhrzeit lesen (TIME)");
            KernalFunctions.Add(0xF6E4,"SETTIM: Setzt Uhrzeit (TIME)");
            KernalFunctions.Add(0xF6ED,"STOP: Fragt STOP-Taste ab");
            KernalFunctions.Add(0xF6FB,"Ausgabe der Fehlermeldung 'TOO MANY FILES'");
            KernalFunctions.Add(0xF6FE,"Ausgabe der Fehlermeldung 'FILE OPEN'");
            KernalFunctions.Add(0xF701,"Ausgabe der Fehlermeldung 'FILE NOT OPEN'");
            KernalFunctions.Add(0xF704,"Ausgabe der Fehlermeldung 'FILE NOT FOUND'");
            KernalFunctions.Add(0xF707,"Ausgabe der Fehlermeldung 'DEVICE NOT PRESENT'");
            KernalFunctions.Add(0xF70A,"Ausgabe der Fehlermeldung 'NOT INPUT FILE'");
            KernalFunctions.Add(0xF70D,"Ausgabe der Fehlermeldung 'NOT OUTPUT FILE'");
            KernalFunctions.Add(0xF710,"Ausgabe der Fehlermeldung 'MISSING FILENAME'");
            KernalFunctions.Add(0xF713,"Ausgabe der Fehlermeldung 'ILLEGAL DEVICE NUMBER'");
            KernalFunctions.Add(0xF72C,"Lädt nächsten Kassettenvorspann");
            KernalFunctions.Add(0xF76A,"Schreibt Kassettenvorspann");
            KernalFunctions.Add(0xF7D0,"Holt Startadresse des Bandpuffers und prüft, ob gültig");
            KernalFunctions.Add(0xF7D7,"Setzt Start- und End-Zeiger des Bandpuffers");
            KernalFunctions.Add(0xF7EA,"Lädt Kassettenvorspann zum angegebenen Dateinamen");
            KernalFunctions.Add(0xF80D,"Erhöht Bandpufferzeiger");
            KernalFunctions.Add(0xF817,"Fragt Bandtaste für Lesen ab und gibt Meldungen aus");
            KernalFunctions.Add(0xF82E,"Prüft ob Bandtaste gedrückt");
            KernalFunctions.Add(0xF838,"Wartet auf Bandtaste für Schreiben, gibt ggf. Meldung aus");
            KernalFunctions.Add(0xF841,"Liest Block vom Band");
            KernalFunctions.Add(0xF84A,"Lädt vom Band");
            KernalFunctions.Add(0xF864,"Schreiben auf Band vorbereiten");
            KernalFunctions.Add(0xF875,"Allgemeine Routine für Lesen und Schreiben vom/auf Band");
            KernalFunctions.Add(0xF8D0,"Prüft auf STOP-Taste während Kassetten-Nutzung");
            KernalFunctions.Add(0xF8E2,"Band für Lesen vorbereiten");
            KernalFunctions.Add(0xF92C,"Band lesen; IRQ-Routine");
            KernalFunctions.Add(0xFA60,"Lädt/prüft Zeichen vom Band");
            KernalFunctions.Add(0xFB8E,"Setzt Bandzeiger auf Programmstart");
            KernalFunctions.Add(0xFB97,"Initialisiert Bitzähler für serielle Ausgabe");
            KernalFunctions.Add(0xFBA6,"Schreiben auf Band");
            KernalFunctions.Add(0xFBCD,"Start der IRQ-Routine für Band schreiben");
            KernalFunctions.Add(0xFC93,"Beendet Rekorderbetrieb");
            KernalFunctions.Add(0xFCB8,"Setzt IRQ-Vektor zurück auf Standard");
            KernalFunctions.Add(0xFCCA,"Schaltet Rekordermotor aus");
            KernalFunctions.Add(0xFCD1,"Prüft, ob Endadresse erreicht (Vergleich $AC/$AD mit $AE/$AF)");
            KernalFunctions.Add(0xFCDB,"Erhöht Adresszeiger");
            KernalFunctions.Add(0xFCE2,"RESET – Routine");
            KernalFunctions.Add(0xFD02,"Prüft auf Steckmodul");
            KernalFunctions.Add(0xFD10,"Text 'CBM80' für Modulerkennung");
            KernalFunctions.Add(0xFD15,"RESTOR: Rücksetzen der Ein- und Ausgabe-Vektoren auf Standardwerte");
            KernalFunctions.Add(0xFD1A,"VECTOR: Setzt Vektoren abhängig von X/Y");
            KernalFunctions.Add(0xFD30,"Tabelle der Kernal-Vektoren für $0314-$0333 (16-mal 2 Byte)");
            KernalFunctions.Add(0xFD50,"RAMTAS: Initialisiert Zeiger für den Arbeitsspeicher");
            KernalFunctions.Add(0xFD9B,"Tabelle der IRQ-Vektoren (4-mal 2 Byte)");
            KernalFunctions.Add(0xFDA3,"IOINIT: Interrupt-Initialisierung");
            KernalFunctions.Add(0xFDDD,"Setzt Timer");
            KernalFunctions.Add(0xFDF9,"SETNAM: Setzt Parameter für Dateinamen");
            KernalFunctions.Add(0xFE00,"SETLFS: Setzt Parameter für aktive Datei");
            KernalFunctions.Add(0xFE07,"READST: Holt I/O-Status");
            KernalFunctions.Add(0xFE18,"SETMSG: Setzt Status als Flag für Betriebssystem-Meldungen");
            KernalFunctions.Add(0xFE21,"SETTMO: Setzt Timeout für seriellen Bus");
            KernalFunctions.Add(0xFE25,"Liest/setzt Obergrenze des BASIC-RAM (nach/von X/Y)");
            KernalFunctions.Add(0xFE34,"MEMBOT: Liest/setzt Untergrenze des BASIC-RAM (nach/von X/Y)");
            KernalFunctions.Add(0xFE43,"NMI Einsprung");
            KernalFunctions.Add(0xFE47,"Standard-NMI-Routine");
            KernalFunctions.Add(0xFE66,"Warmstart BASIC (BRK-Routine)");
            KernalFunctions.Add(0xFEBC,"Interrupt-Ende (holt Y, X, A vom Stack und RTI)");
            KernalFunctions.Add(0xFEC2,"Tabelle mit Timerkonstanten für RS-232 Baudrate, NTSC-Version");
            KernalFunctions.Add(0xFED6,"NMI-Routine für RS-232 Eingabe");
            KernalFunctions.Add(0xFF07,"NMI-Routine für RS-232 Ausgabe");
            KernalFunctions.Add(0xFF43,"IRQ-Einsprung aus Bandroutine");
            KernalFunctions.Add(0xFF48,"IRQ-Einsprung");
            KernalFunctions.Add(0xFF5B,"CINT: Video-Reset");
            KernalFunctions.Add(0xFF80,"Kernal Versions-Nummer");            
            KernalFunctions.Add(0xFF81,"CINT: Initalisierung Bildschirm-Editor");
            KernalFunctions.Add(0xFF84,"IOINIT: Initialiserung der Ein- und Ausgabe");
            KernalFunctions.Add(0xFF87,"RAMTAS: Initalisieren des RAMs, Kassettenpuffer einrichten, Anfang des Bildschirmspeichers 1024 ($0400) setzen");
            KernalFunctions.Add(0xFF8A,"RESTOR: Rücksetzen der Ein- und Ausgabevektoren auf Standard");
            KernalFunctions.Add(0xFF8D,"VECTOR: Abspeichern von RAM bzw. Vektorverwaltung der Sprungvektoren");
            KernalFunctions.Add(0xFF90,"SETMSG: Steuerung von KERNAL-Meldungen");
            KernalFunctions.Add(0xFF93,"SECOND: Übertragung der Sekundäradresse nach LISTEN-Befehl");
            KernalFunctions.Add(0xFF96,"TKSA: Übertragung der Sekundäradresse nach TALK-Befehl");
            KernalFunctions.Add(0xFF99,"MEMTOP: Setzen oder Lesen des Zeigers auf BASIC-RAM-Ende");
            KernalFunctions.Add(0xFF9C,"MEMBOT: Setzen oder Lesen des Zeigers auf BASIC-RAM-Anfang");
            KernalFunctions.Add(0xFF9F,"SCNKEY: Abfrage der Tastatur");
            KernalFunctions.Add(0xFFA2,"SETTMO: Setzen der Zeitsperre für seriellen Bus (nur für zusätzliche IEEE-Karten)");
            KernalFunctions.Add(0xFFA5,"ACPTR: Byte-Eingabe (serieller Port)");
            KernalFunctions.Add(0xFFA8,"CIOUT: Byte-Ausgabe (serieller Bus)");
            KernalFunctions.Add(0xFFAB,"UNTLK: Senden des UNTALK-Befehls für seriellen Bus");
            KernalFunctions.Add(0xFFAE,"UNLSN: Senden des UNLISTEN-Befehls für seriellen Bus zur Beendigung der Datenübertragung");
            KernalFunctions.Add(0xFFB1,"LISTEN: Befehl LISTEN für Geräte am seriellen Bus (Start Datenempfang bei Peripheriegeräten)");
            KernalFunctions.Add(0xFFB4,"TALK: TALK auf den seriellen Bus senden");
            KernalFunctions.Add(0xFFB7,"READST: Lesen des Ein-/Ausgabestatusworts, also Fehlermeldungen des KERNALs (vergl. BASIC-Systemvariable STATUS bzw. ST)");
            KernalFunctions.Add(0xFFBA,"SETLFS: Setzen der Geräteadressen (logische, Primär- und Sekundäradresse)");
            KernalFunctions.Add(0xFFBD,"SETNAM: Festlegen des Dateinamens");
            KernalFunctions.Add(0xFFC0,"OPEN: Logische Datei öffnen (vergl. BASIC-Befehl OPEN)");
            KernalFunctions.Add(0xFFC3,"CLOSE: Logische Datei schließen (vergl. BASIC-Befehl CLOSE)");
            KernalFunctions.Add(0xFFC6,"CHKIN: Eingabe-Kanal öffnen");
            KernalFunctions.Add(0xFFC9,"CHKOUT: Ausgabe-Kanal öffnen");
            KernalFunctions.Add(0xFFCC,"CLRCHN: Schließt Ein- und Ausgabekanal");
            KernalFunctions.Add(0xFFCF,"CHRIN: Zeicheneingabe");
            KernalFunctions.Add(0xFFD2,"CHROUT: Zeichenausgabe");
            KernalFunctions.Add(0xFFD5,"LOAD: Daten ins RAM laden von Peripheriegeräten, aber nicht von Tastatur (0), RS-323 (2), Bildschirm (3) (vergl. BASIC-Befehl LOAD)");
            KernalFunctions.Add(0xFFD8,"SAVE: Daten vom RAM auf Peripheriegeräte sichern, aber nicht auf Tastatur (0), RS-323 (2), Bildschirm (3) (vergl. BASIC-Befehl SAVE)");
            KernalFunctions.Add(0xFFDB,"SETTIM: Setzen der Uhrzeit (vergl. BASIC-Systemvariable TIME$/TI$)");
            KernalFunctions.Add(0xFFDE,"RDTIM: Uhrzeit lesen (vergl. BASIC-Systemvariablen TIME/TI und TIME$/TI$)");
            KernalFunctions.Add(0xFFE1,"STOP: Abfrage der Taste");
            KernalFunctions.Add(0xFFE4,"GETIN: Zeichen vom Eingabegerät einlesen");
            KernalFunctions.Add(0xFFE7,"CLALL: Schließen alle Kanäle und Dateien");
            KernalFunctions.Add(0xFFEA,"UDTIM: Weiterstellen der Uhrzeit");
            KernalFunctions.Add(0xFFED,"SCREEN: Anzahl der Bildschirmspalten und -zeilen ermitteln (Rückgabe im X- und Y-Register)");
            KernalFunctions.Add(0xFFF0,"PLOT: Setzen oder Lesen der Cursorpostion (X-/Y-Position)");
            KernalFunctions.Add(0xFFF3,"IOBASE: Rückmeldung der Basisadressen für Ein- und Ausgabegeräte");
        }
    }
}