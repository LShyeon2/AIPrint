using System;
using System.Runtime.InteropServices;



namespace BoxPrint.CCLinkHelper
{
    public class CCLinkHelper
    {
        [DllImport("MDFUNC32.dll")]
        public static extern short mdInit(Int32 Path);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdOpen(short Chan, short Mode, ref Int32 Path);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdClose(Int32 Path);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdSend(Int32 Path, short Stno, short Devtyp, short Devno, ref short Size, ref short Buf);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdReceive(Int32 Path, short Stno, short Devtyp, short Devno, ref short Size, ref short Buf);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdDevSet(Int32 Path, short Stno, short Devtyp, short Devno);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdDevRst(Int32 Path, short Stno, short Devtyp, short Devno);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdRandW(Int32 Path, short Stno, short Dev, short Buf, short bufsiz);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdRandR(Int32 Path, short Stno, short Dev, short Buf, short bufsiz);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdControl(Int32 Path, short Stno, short Buf);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdTypeRead(Int32 Path, short Stno, ref short buf);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdBdLedRead(Int32 Path, ref short buf);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdBdModRead(Int32 Path, short Mode);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdBdModSet(Int32 Path, short Mode);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdBdRst(Int32 Path);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdBdSwRead(Int32 path, ref short buf);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdBdVerRead(Int32 Path, ref short buf);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdWaitBdEvent(Int32 Path, ref short eventno, Int32 timeout, ref short signaledno, ref short details);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdSendEx(Int32 Path, Int32 Netno, Int32 Stno, Int32 Devtyp, Int32 devno, ref Int32 size, ref short buf);
        [DllImport("MDFUNC32.dll")]
        public static extern Int32 mdReceiveEx(Int32 path, Int32 Netno, Int32 Stno, Int32 Devtyp, Int32 devno, ref Int32 size, ref short buf);
        [DllImport("MDFUNC32.dll")]
        public static extern short mdDevSetEx(Int32 path, Int32 Netno, Int32 Stno, Int32 Devtyp, Int32 devno);
        [DllImport("MDFUNC32.dll")]
        public static extern Int32 mdDevRstEx(Int32 Path, Int32 Netno, Int32 Stno, Int32 Devtyp, Int32 devno);
        [DllImport("MDFUNC32.dll")]
        public static extern Int32 mdRandWEx(Int32 path, Int32 Netno, Int32 Stno, ref Int32 dev, ref short buf, Int32 bufsiz);
        [DllImport("MDFUNC32.dll")]
        public static extern Int32 mdRandREx(Int32 path, Int32 Netno, Int32 Stno, ref Int32 dev, ref short buf, Int32 bufsiz);
    }

    public enum DeviceType
    {
        DevX = 1,
        DevY = 2,
        DevL = 3,
        DevM = 4,
        DevSM = 5,
        DevF = 6,
        DevTT = 7,
        DevTC = 8,
        DevCT = 9,
        DevCC = 10,
        DevTN = 11,
        DevCN = 12,
        DevD = 13,
        DevSD = 14,
        DevTM = 15,
        DevTS = 16,
        DevTS2 = 16002,
        DevTS3 = 16003,
        DevCM = 17,
        DevCS = 18,
        DevCS2 = 18002,
        DevCS3 = 18003,
        DevA = 19,
        DevZ = 20,
        DevV = 21,
        DevR = 22,

        DevB = 23,
        DevW = 24,

        DevSTT = 26,
        DevSTC = 27,
        DevQSW = 28,
        DevQV = 30,
        DevMRB = 33,
        DevMAB = 34,
        DevSTN = 35,
        DevWw = 36,
        DevWr = 37,
        DevFS = 40,
        DevSPB = 50,
        DevSPB1 = 501,
        DevSPB2 = 502,
        DevSPB3 = 503,
        DevSPB4 = 504,
        DevSPX = 51,
        DevSPY = 52,
        DevUSER = 100,
        DevMAIL = 101,
        DevMAILNC = 102,

        DevRBM = -32768,
        DevRAB = -32736,
        DevRX = -32735,
        DevRY = -32734,
        DevRW = -32732,
        DevARB = -32704,
        DevSB = -32669,
        DevSW = -32668,
    }

}
