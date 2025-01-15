using Stockerfirmware.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stockerfirmware.DataList.CV
{
    /// <summary>
    /// 트레이 데이타를 보관하는 클래스. 아스키값을 기본으로 처리한다.
    /// </summary>
    [Obsolete]
    public class Tray
    {
        //Tray ID         8	00~07			Tray의 고유번호로써, RFID Tag의 전면에 Marking됨.  ▶Protected			8201234	O O   O O   O O
        //Eqp NO          8	08~15			해당 Lot이 Dispatching받은 설비번호          Tray Spec 정보사용으로 Drop   O O   O X
        //Tray Spec       8	08~15			☞ MBT공정에서 EQP NO자리에 Tray Spec으로 사용          ADS12345 X   O X   X O
        //QTY			  6	16~21			Tray 묶음별 QTY			10250		X O   X
        //TQTY            6	22~27			Tatal QTY : LOT 안에 총 Chip 수			23800		X O   X
        //Lot ID		 12	28~39			LotID or "CTRAY", "ETRAY"			WZD034F43 O   O O   O O   O
        //STEP            6	40~45			해당 STEP정보           T130 O   O X
        //Lot Type        2	46~47			자재 진행여부 판단을 위해 PP,PE,PQ,E% 등의 정보를 기록함.PQ X   O X
        //Part NO        28 48~75			Part NO         K4T1G084QR-HCW0000-WCX2LW O   X O   X
        //PKG Code		  4	76~79			PKG Code            2LW X   X O   X
        //PLot ID		 12 80~91			1) Merge 될 경우, Merge된 母LotID를 기입함.  2) Rework자재 LOTID기록         WZD034F43 X   O O   O
        //LotSeq          8	92~99			한 Lot이 2개이상의 실물로 이동할 경우, 이들의 순번을 기입함.           1001 (10개의 실물중 1번째) X X   X X
        //Flag			  2	100~101			MVP공정 ☞ "IPIS설비" Turn over 유무 구분관리          00 : Bypass , 01 : Turn over        X X   O
        //                                  PKG공정 ☞ Rework tray 유무 구분관리         10 : Rework 無, 11 : Rework 有    O X
        //Reject		  2	102~103			2라인 검사 Reject구분 RJ  X X   X X
        //Bin정보		  2	104~105			Bin 정보          8	X X   X X
        //Reserve		  2	106~107							X X   X X
        //Tray 매수       4	108~111			LOT 안에 Tray 총 매수			35	X X   O X

        #region Tray Property
        public string CarrierID { get; private set;}
        public string EQPNo { get; private set; }
        public string QTY { get; private set; }
        public string TQTY { get; private set; }
        public string LotID { get; private set; }

        public string Lot_1 { get; private set; }
        public string Lot_2 { get; private set; }
        public string Lot_3 { get; private set; }
        public string Lot_4 { get; private set; }
        public string Lot_5 { get; private set; }
        public string Lot_6 { get; private set; }

        


        public string Step { get; private set; }
        public string LotType { get; private set; }
        public string PartNo { get; private set; }
        public string PKGCode { get; private set; }
        public string PLotID { get; private set; }
        public string LotSeq { get; private set; }
        public string Flag { get; private set; }
        public string Reject { get; private set; }
        public string BinInfo { get; private set; }
        public string Reserved { get; private set; }
        public string TrayCounter { get; private set; }
        public eTrayHeight TrayHeight
        {
            get;
            private set;
        }
        public eCarrierSize TrayWidth
        {
            get;
            private set;
        }
        #endregion

        private string RawAsciiTagID;
        private static int SimulSeq
        {
            get
            {
                int seq = 0;
                string StrSeq = INI_Helper.ReadValue("Tray", "SimulSeq");
                if(!string.IsNullOrEmpty(StrSeq))
                {
                    int.TryParse(StrSeq, out seq);
                }
                return seq;
            }
            set
            {
                if (value > 999999)
                    value = 0;
                INI_Helper.WriteValue("Tray", "SimulSeq",value.ToString());
            }
        }
        public double CV_SimulPosition = 0; // Simulation Only
        public Tray(string TagID,bool IsAsciiTagID,eTrayHeight height = eTrayHeight.OverHeight)
        {
            CV_SimulPosition = 0;
            if (!string.IsNullOrEmpty(TagID))
            {
                if (IsAsciiTagID)
                {
                    ParseTagID(TagID);
                }
                else
                {
                    ParseHexaTagID(TagID);
                }
            }
            else
            {
                this.CarrierID = "ERROR";
            }
            SetTrayHeight(height);
        }

        public Tray(string TagID, eCarrierSize width)
        {
            CV_SimulPosition = 0;
            TrayWidth = width;
            if (!string.IsNullOrEmpty(TagID))
            {
                CarrierID = TagID;
            }
            else
            {
                this.CarrierID = "ERROR";
            }
        }
        public string CarrierCurrentLocation { get; set; }
        public eIDReadStatus LastReadResult { get; set; }

        public ePolarity Polarity { get; set; }

        public int InnerTrayType { get; set; }

        public string CarrierType { get; set; }

        public eCarrierState CarrierState
        {
            get;
            set;
        }
        public eProductEmpty ProductEmpty
        {
            get;
            set;
        }
        public eWinderDirection WinderDirection
        {
            get;
            set;
        }
        public eUnCoatedPart UnCoatedPart
        {
            get;
            set;
        }
        public eTrayType TrayType
        {
            get;
            set;
        }
        public int ProductQuantitiy
        {
            get;
            set;
        }
        public string GetReportTagID()
        {
            return RawAsciiTagID;
        }

        public bool UpdateTagID(string Tag,bool isAscii = true)
        {
            if(isAscii)
            {
                ParseTagID(Tag); 
            }
            else
            {
                ParseHexaTagID(Tag);
            }
          
            return true;
        }
        public void UpdateCarrierID(string carrierID)
        {
            if (string.IsNullOrEmpty(carrierID))
            {
                this.CarrierID = "ERROR";
            }
            else
            {
                this.CarrierID = carrierID;
            }
        }
        private bool ParseTagID(string AsciiTag)
        {
            if (GlobalData.Current.UseBCR == true)
                return BCR_ParseTagID(AsciiTag);
            else
                return RFID_ParseTagID(AsciiTag);
        }

        private bool RFID_ParseTagID(string AsciiTag)
        {
            try
            {
                if (string.IsNullOrEmpty(AsciiTag))
                {
                    return false;
                }
                if (AsciiTag.Length != 112) //사양서상 112자리 문자열
                {
                    if (GlobalData.Current.GlobalSimulMode)
                    {
                        AsciiTag = CreateSimulTagID();
                    }
                    else
                    {
                        return false;
                    }
                }
                this.CarrierID =  AsciiTag.Substring(0, 8);
                this.EQPNo = AsciiTag.Substring(8, 8);
                this.QTY = AsciiTag.Substring(16, 6);
                this.TQTY = AsciiTag.Substring(22, 6);
                this.LotID = AsciiTag.Substring(28, 12);
                this.Step = AsciiTag.Substring(40, 6);
                this.LotType = AsciiTag.Substring(46, 2);
                this.PartNo = AsciiTag.Substring(48, 28);
                this.PKGCode = AsciiTag.Substring(76, 4);
                this.PLotID = AsciiTag.Substring(80, 12);
                this.LotSeq = AsciiTag.Substring(92, 8);
                this.Flag = AsciiTag.Substring(100,2);
                this.Reject = AsciiTag.Substring(102, 2);
                this.BinInfo = AsciiTag.Substring(104, 2);
                this.Reserved = AsciiTag.Substring(106, 2);
                this.TrayCounter = AsciiTag.Substring(108, 4);

                RawAsciiTagID = AsciiTag;
                return true;

            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        private bool BCR_ParseTagID(string AsciiTag)
        {
            try
            {
                if (string.IsNullOrEmpty(AsciiTag))
                {
                    return false;
                }
                if (AsciiTag.Length <= 21 + 8) // 21자리 LOT + 가변 DVC
                {
                    if (GlobalData.Current.GlobalSimulMode)
                    {
                        AsciiTag = CreateSimulTagID();
                    }
                    else
                    {

                        RawAsciiTagID = AsciiTag;
                        return true;
                    }
                }
                //211202 RGJ 소박스 SETUP
                this.LotID       = AsciiTag.Substring(0, 10); 
                this.QTY         = AsciiTag.Substring(10, 5);
                this.Step        = AsciiTag.Substring(15, 4);
                this.BinInfo     = AsciiTag.Substring(19, 2);     
                this.CarrierID   = AsciiTag.Substring(0, 8); //LOT ID 8자리를 CARRIER ID 로 간주

                RawAsciiTagID = string.Format("{0}{1}{2}{3}",
                   LotID,QTY,Step,BinInfo);

                return true;

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        private void ParseHexaTagID(string HexTag)
        {
            string AsciiTag = GetHexStringToAsciiString(HexTag);
            ParseTagID(AsciiTag);
        }

        public string GetHexTagID()
        {
            return GetAsciiStringtoHexString(RawAsciiTagID);
        }
        public string GetHexCarrierID()
        {
            return GetAsciiStringtoHexString(CarrierID);
        }

        public static string CreateSimulTagID()
        {
            string CarrierID = string.Format("TR{0:D6}", SimulSeq);
            SimulSeq++;
            return string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}{15}",
                CarrierID, "ADS12345", "10250 ", "20500 ", "WZD034F43   ", "T130  ", "PQ", "K4T1G084QR-HCW0000-WCX2LWRZA", "2LWX", "WZD034F43   ","        ", "00", "  ", "8 ", "  ", "35  ");
        }
    

        public void SetTrayHeight(eTrayHeight height)
        {
            TrayHeight = height;
        }

        public static string GetHexStringToAsciiString(string Hexstring)
        {
            try
            {
                char c;
                StringBuilder sb = new StringBuilder();
                if (string.IsNullOrEmpty(Hexstring))
                {
                    return string.Empty;
                }
                int hexLength = Hexstring.Length / 2;
                for (int i = 0; i < hexLength; i++)
                {
                    c = (char)Byte.Parse(Hexstring.Substring(i * 2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    sb.Append(c);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(Stockerfirmware.eLogLevel.Info, ex.ToString());
                return string.Empty;
            }

        }

        public static string GetAsciiStringtoHexString(string Asciistring)
        {
            try
            {
                if(Asciistring == null)
                {
                    return "";
                }
                StringBuilder sb = new StringBuilder();
                foreach (var c in Asciistring)
                {
                    sb.AppendFormat("{0:X2}", (int)c);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(Stockerfirmware.eLogLevel.Info, ex.ToString());
                return "ERROR";
            }
        }
        /// <summary>
        /// 0 = OK
        /// 99 = Powder STK
        /// Etc = Error
        /// </summary>
        /// <returns></returns>
        public int GetValidationCheck()
        {
            return 0;
        }
    }
}
