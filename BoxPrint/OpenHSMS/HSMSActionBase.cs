using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OSG.Com.HSMS.Common;
using BoxPrint.Alarm;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using BoxPrint.Modules;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.CVLine;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;

namespace BoxPrint.OpenHSMS
{
    public class HSMSActionBase
    {
        #region S1F*
        /// <summary>
        /// Are You There Request
        /// </summary>
        /// <param name="primaryMsg"></param>
        /// <returns></returns>
        public virtual DataItem DoAction_S1F1(DataMessage primaryMsg)
        {
            //230302 사양변경 s
            //#region Variable 
            //string sMDLN = "SCS"; ;
            //string sSoftrev = GlobalData.SCSProgramVersion;
            //#endregion 

            //#region Data pasing 
            ////HeaderOnly이므로 Pasing할 Data없음 
            //#endregion

            //#region Data Check 

            //#endregion 

            //#region Make Relply Data 

            //DataItem diOnlineDataInfo = new DataItem(ItemFormatCode.List);				        // L2 OnlineDataInfo
            //diOnlineDataInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, 0));              // L2-1 B[1] COMMACK 

            //DataItem diEQOnlineInfo = new DataItem(ItemFormatCode.List);                        // L2-2 L4 
            //diEQOnlineInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, sMDLN));			    // L4-1 A[6] MDLN
            //diEQOnlineInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, sSoftrev));        // L4-2 A[6] SOFTREV
            //diOnlineDataInfo.AddChildItem(diEQOnlineInfo);

            //#endregion 

            //return diOnlineDataInfo;
            return null; //HeaderOnly
            //230302 사양변경 s
        }


        // S1F3(Selected Equipment Status Request)
        public virtual DataItem DoAction_S1F3(DataMessage primaryMsg)
        {
            #region Variable 
            List<int> liSVIDList = new List<int>();
            #endregion 

            #region Data Pasing 
            if (primaryMsg.Body != null && primaryMsg.Body.ChildItems != null)
            {
                foreach (var svitem in primaryMsg.Body.ChildItems)
                {
                    int nSVID = int.Parse(svitem.Value.ToString());
                    liSVIDList.Add(nSVID);
                }
            }
            #endregion 

            #region Data Check

            #endregion 

            #region Make Reply Data
            DataItem diSVIDDataInfo = new DataItem(ItemFormatCode.List);
            if (liSVIDList.Count > 0)
            {
                foreach (var svitem in liSVIDList)
                {
                    DataItem diSVIDInfo = GlobalData.Current.HSMS.GetSVIDItem(svitem);
                    if (diSVIDInfo != null)
                    {
                        diSVIDDataInfo.AddChildItem(diSVIDInfo);
                    }
                }
            }
            else
            {
                diSVIDDataInfo = GlobalData.Current.HSMS.GetAllSVIDListItem();
            }
            #endregion 

            return diSVIDDataInfo;
        }



        // S1F13(Establish Commnunication Request)
        public virtual DataItem DoAction_S1F13(DataMessage primaryMsg)
        {
            #region Variable
            string sMDLN = "SCS";
            string sSoftrev = GlobalData.SCSProgramVersion;

            #endregion

            #region Data Pasing 

            #endregion

            #region Data Check 
            #endregion

            #region Make Reply Data 
            DataItem diOnlineDataInfo = new DataItem(ItemFormatCode.List);				        // L2 OnlineDataInfo
            diOnlineDataInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, 0));              // L2-1 B[1] COMMACK 

            DataItem diEQOnlineInfo = new DataItem(ItemFormatCode.List);                        // L2-2 L4 
            diEQOnlineInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, sMDLN));			    // L4-1 A[6] MDLN
            diEQOnlineInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, sSoftrev));          // L4-2 A[6] SOFTREV
            diOnlineDataInfo.AddChildItem(diEQOnlineInfo);
            #endregion 

            return diOnlineDataInfo;
        }
        // S1F15(Request OFF-LINE )
        public virtual DataItem DoAction_S1F15(DataMessage primaryMsg)
        {
            #region Variable 
            Byte OFLAck = 0;
            #endregion 

            #region Data Pasing 

            #endregion 

            #region Data Check 
            //이미 OffLine 이면 Nak
            if (GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Offline_EQ)
            {
                OFLAck = 1;
            }
            #endregion

            #region Make Reply Data 
            #endregion
            //220802 조숭진 PrimaryMessagePostAction 에서 처리
            //if (OFLAck == 0)
            //{
            //    GlobalData.Current.MainBooth.CurrentOnlineState = eOnlineState.Offline; //OffLine 변경
            //}
            return new DataItem(ItemFormatCode.Bin, OFLAck);
        }
        // S1F17(Request ON-LINE )
        public virtual DataItem DoAction_S1F17(DataMessage primaryMsg)
        {
            #region Variable 
            Byte ONLAck = 0;
            #endregion 

            #region Data Pasing 

            #endregion

            #region Data Check 
            if (GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Remote) //이미 리모트이면 OK
                ONLAck = 2; //[230503 CIM 검수] ONLAck 1 -> 2
            #endregion

            #region Make Reply Data 
            #endregion

            //220802 조숭진 PrimaryMessagePostAction 에서 처리
            //if (ONLAck == 0)
            //{
            //    GlobalData.Current.MainBooth.CurrentOnlineState = eOnlineState.Remote; //Online Mode Change to Remote
            //}
            return new DataItem(ItemFormatCode.Bin, ONLAck);
        }
        #endregion

        #region S2F*

        // S2F31(Date and Time Set Send)
        public virtual DataItem DoAction_S2F31(DataMessage primaryMsg)
        {
            #region Variable 
            Byte nAck = 0;
            DateTime? dtTimeValue = null;
            string sTime;
            bool bTimeSetSuccess;
            #endregion

            #region Data Pasing 
            //SuHwan_20220524 : ACK 운영이 나오면 추가하자
            sTime = primaryMsg.Body.Value.ToString();
            switch (sTime.Length)
            {
                case 16:
                    dtTimeValue = DateTime.ParseExact(sTime, "yyyyMMddHHmmssff", System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case 14:
                    dtTimeValue = DateTime.ParseExact(sTime, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                    break;
                default:
                    nAck = 1;
                    break;
            }
            #endregion

            #region Data Check 
            #endregion

            #region Make Reply Data 
            if (dtTimeValue != null)
            {
                bTimeSetSuccess = MainHelper.SetLocaltime(dtTimeValue.Value);
                nAck = bTimeSetSuccess ? (byte)0 : (byte)1;
            }
            #endregion

            return new DataItem(ItemFormatCode.Bin, nAck);
        }
        public virtual DataItem DoAction_S2F41(DataMessage primaryMsg)
        {
            //220523 조숭진 hsms s2계열 메세지 추가 s
            string rcmd = primaryMsg.Body.FirstChild.Value.ToString();

            DataItem dataItem = null;

            switch (rcmd)
            {
                case "RESUME":
                case "PAUSE":
                    dataItem = DoAction_S2F41_ResumePause(primaryMsg);
                    break;
                case "CANCEL":
                case "ABORT":
                case "BUZZER":
                case "REMOVE":
                    dataItem = DoAction_S2F41_CancelAbortRemoveBuzzer(primaryMsg);
                    break;
                case "INSTALL":
                case "PORTTYPECHANGE":
                case "CARRIERGEN":
                    dataItem = DoAction_S2F41_InstallPortTypeChangeCarriergen(primaryMsg);
                    break;
                case "VALIDATIONRESULT":
                    dataItem = DoAction_S2F41_VALIDATIONRESULT(primaryMsg);
                    break;
                //2024.06.26 lim, 셀버퍼 자동 Keyin 기능 추가
                case "CARRIERINFOUPDATE":
                    dataItem = DoAction_S2F41_InfoUpdate(primaryMsg);
                    break;
                default:
                    dataItem = DoAction_S2F41_Abnormal(primaryMsg);
                    break;
            }

            return dataItem;
            //220523 조숭진 hsms s2계열 메세지 추가 e
        }
        //2024.06.27 lim, 셀버퍼 자동 Keyin 기능 추가 hsms s2계열 메세지 추가
        protected virtual DataItem DoAction_S2F41_InfoUpdate(DataMessage primaryMsg)
        {
            string rcmd = string.Empty, cpval1 = string.Empty, cpname1 = string.Empty, cpval2 = string.Empty, cpname2 = string.Empty, cpval3 = string.Empty, cpname3 = string.Empty;
            eHCACKCodeList nAck = eHCACKCodeList.HCAckCode4;
            eCPACKCodeList nCPAck1 = eCPACKCodeList.CPAckCode;
            eCPACKCodeList nCPAck2 = eCPACKCodeList.CPAckCode;
            eCPACKCodeList nCPAck3 = eCPACKCodeList.CPAckCode;

            DataItem diRcmdAckInfo = new DataItem(ItemFormatCode.List);
            DataItem diCPAckList = new DataItem(ItemFormatCode.List);
            DataItem diCPAckInfo1 = new DataItem(ItemFormatCode.List);
            DataItem diCPAckInfo2 = new DataItem(ItemFormatCode.List);
            DataItem diCPAckInfo3 = new DataItem(ItemFormatCode.List);

            try
            {
                rcmd = primaryMsg.Body.FirstChild.Value.ToString();
                DataItem diProcessCmdlist1 = primaryMsg.Body.ChildItems[1].ChildItems[0];
                DataItem diProcessCmdlist2 = primaryMsg.Body.ChildItems[1].ChildItems[1];
                DataItem diProcessCmdlist3 = primaryMsg.Body.ChildItems[1].ChildItems[2];

                cpname1 = diProcessCmdlist1.ChildItems[0].Value.ToString().Trim();
                cpval1 = diProcessCmdlist1.ChildItems[1].Value.ToString().Trim();
                cpname2 = diProcessCmdlist2.ChildItems[0].Value.ToString().Trim();
                cpval2 = diProcessCmdlist2.ChildItems[1].Value.ToString().Trim();
                cpname3 = diProcessCmdlist3.ChildItems[0].Value.ToString().Trim();
                cpval3 = diProcessCmdlist3.ChildItems[1].Value.ToString().Trim();

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "HSMSManager({0}) Exception: {1}", primaryMsg.Name, ex.ToString());

                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode6));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));

                return diRcmdAckInfo;
            }

            if (rcmd == "CARRIERINFOUPDATE")
            {
                if (cpname1 != "CARRIERID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck1 = eCPACKCodeList.CPNAckCode1;
                }
                if (cpname2 != "PRODUCTEMPTY")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode1;
                }
                if (cpname3 != "PALLETSIZE")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck3 = eCPACKCodeList.CPNAckCode1;
                }

                
                //CarrierItem TargetCarrier = CarrierStorage.Instance.GetCarrierItem(cpval2);
                var portItem = GlobalData.Current.PortManager.AllCVList.Where(c => c.PC_CarrierID == cpval1 && c.CarrierExist == true).FirstOrDefault();
                if (portItem == null)   // 해당 케리어 ID가 포트에 있는지 확인
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck1 = eCPACKCodeList.CPNAckCode2;
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname1));
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck1));
                    diCPAckList.AddChildItem(diCPAckInfo1);
                }
                else //if (portItem != null) 
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "DoAction_S2F41 InfoUpdate Module : {0}", portItem.ModuleName);

                    if ((cpval3 == "SHORT" && portItem.PC_PalletSize != ePalletSize.Cell_Short) ||
                        (cpval3 == "LONG" && portItem.PC_PalletSize != ePalletSize.Cell_Long))
                    {
                        //Mismatch
                        //2024.08.12 lim, Nack 값 변경 mismatch cassette size 나머지는 필요 없음
                        nAck = eHCACKCodeList.HCNAckCode10;
                        //nAck = eHCACKCodeList.HCNAckCode3;
                        //nCPAck3 = eCPACKCodeList.CPNAckCode2;
                        //diCPAckInfo3.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname3));
                        //diCPAckInfo3.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck3));
                        //diCPAckList.AddChildItem(diCPAckInfo3);
                    }
                }
            }
            else
            {
                nAck = eHCACKCodeList.HCNAckCode3;
                nCPAck2 = eCPACKCodeList.CPNAckCode2;
            }

            if (nAck == eHCACKCodeList.HCAckCode4)
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
            }
            else
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                diRcmdAckInfo.AddChildItem(diCPAckList);
            }
            return diRcmdAckInfo;
        }

        //220523 조숭진 hsms s2계열 메세지 추가
        protected virtual DataItem DoAction_S2F41_Abnormal(DataMessage primaryMsg)
        {
            DataItem diRcmdAckInfo = new DataItem(ItemFormatCode.List);				        // L2 RcmdAckInfo
            diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode1));
            diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));

            return diRcmdAckInfo;
        }

        //220523 조숭진 hsms s2계열 메세지 추가
        protected virtual DataItem DoAction_S2F41_ResumePause(DataMessage primaryMsg)
        {
            string rcmd = string.Empty;
            eHCACKCodeList nAck = eHCACKCodeList.HCAckCode4; //Command ack 4 리턴

            DataItem diRcmdAckInfo = new DataItem(ItemFormatCode.List);

            try
            {
                rcmd = primaryMsg.Body.FirstChild.Value.ToString();
            }
            catch (Exception)
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode1));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
                return diRcmdAckInfo;
            }

            if (rcmd == "RESUME")
            {
                if (GlobalData.Current.MainBooth.SCState == eSCState.AUTO)
                {
                    nAck = eHCACKCodeList.HCNAckCode2;
                }
            }
            else
            {
                if (GlobalData.Current.MainBooth.SCState != eSCState.AUTO)
                {
                    nAck = eHCACKCodeList.HCNAckCode2;
                }
            }

            if (nAck != eHCACKCodeList.HCAckCode4)
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
                return diRcmdAckInfo;
            }

            //Ack SCS 상태 변환 지시
            if (rcmd == "RESUME")
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(200);
                    GlobalData.Current.MainBooth.SCSResumeCommand();
                });
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(200);
                    GlobalData.Current.MainBooth.SCSPauseCommand();
                });
            }

            diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
            diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
            return diRcmdAckInfo;
        }

        //220523 조숭진 hsms s2계열 메세지 추가
        protected virtual DataItem DoAction_S2F41_CancelAbortRemoveBuzzer(DataMessage primaryMsg)
        {
            string rcmd = string.Empty, cpval = string.Empty, cpname = string.Empty;
            eHCACKCodeList nAck = eHCACKCodeList.HCAckCode4;
            //eCPACKCodeList nCPAck = eCPACKCodeList.CPAckCode;  ///비사용 코드 제거

            DataItem diRcmdAckInfo = new DataItem(ItemFormatCode.List);
            DataItem diCPAckList = new DataItem(ItemFormatCode.List);

            //DataItem diCPAckInfo = new DataItem(ItemFormatCode.List);//SuHwan_20220526 : 추가
            Dictionary<string, eCPACKCodeList> dicCommandParameter = new Dictionary<string, eCPACKCodeList>();//CP 정보 저장용

            try
            {
                rcmd = primaryMsg.Body.FirstChild.Value.ToString();
                DataItem diProcessCmdSet = primaryMsg.Body.ChildItems[1].ChildItems[0];

                cpname = diProcessCmdSet.ChildItems[0].Value.ToString().Trim();
                cpval = diProcessCmdSet.ChildItems[1].Value.ToString().Trim();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "HSMSManager({0}) Exception: {1}", primaryMsg.Name, ex.ToString());

                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode6));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));

                return diRcmdAckInfo;
            }

            if (rcmd == "CANCEL")
            {
                //SuHwan_20220531 : 여기서 리턴ID 를 설정하는데.. 이상하다.. 나중에 수정하자
                var mcsJobBuffer = GlobalData.Current.McdList.Where(c => c.CommandID == cpval).FirstOrDefault();

                if (mcsJobBuffer == null)
                {
                    nAck = eHCACKCodeList.HCNAckCode1;
                    dicCommandParameter.Add("COMMANDID", eCPACKCodeList.CPNAckCode2);
                }
                else if (cpname != "COMMANDID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    dicCommandParameter.Add("COMMANDID", eCPACKCodeList.CPNAckCode1);
                    mcsJobBuffer.JobResult = eJobResultCode.OTHER_ERROR;
                }
                //else if(mcsJobBuffer.TCStatus != eTCState.QUEUED) //해당 조건 삭제 따로 Command 는 ack 올리고 TransferCancelFailed 보고올림
                //{
                //    nAck = eHCACKCodeList.HCNAckCode2;
                //    dicCommandParameter.Add("COMMANDID", eCPACKCodeList.CPNAckCode2);
                //    mcsJobBuffer.JobResult = eJobResultCode.OTHER_ERROR;
                //}
                else
                {
                    //mcsJobBuffer.SetJobAbort(true); //Job Cancel 기입
                    dicCommandParameter.Add("COMMANDID", eCPACKCodeList.CPAckCode);
                    mcsJobBuffer.JobResult = eJobResultCode.SUCCESS;
                }
            }
            else if (rcmd == "ABORT")
            {
                //SuHwan_20220531 : 여기서 리턴ID 를 설정하는데.. 이상하다.. 나중에 수정하자
                var mcsJobBuffer = GlobalData.Current.McdList.Where(c => c.CommandID == cpval).FirstOrDefault();

                if (mcsJobBuffer == null)
                {
                    nAck = eHCACKCodeList.HCNAckCode1;
                    dicCommandParameter.Add("COMMANDID", eCPACKCodeList.CPNAckCode2);
                }
                else if (cpname != "COMMANDID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    dicCommandParameter.Add("COMMANDID", eCPACKCodeList.CPNAckCode1);
                    mcsJobBuffer.JobResult = eJobResultCode.OTHER_ERROR;
                }
                //else if (mcsJobBuffer.TCStatus != eTCState.TRANSFERRING && mcsJobBuffer.TCStatus != eTCState.PAUSED) //해당 조건 삭제 따로 Command 는 ack 올리고 TransferCancelFailed 보고올림
                //{
                //    nAck = eHCACKCodeList.HCNAckCode2;
                //    dicCommandParameter.Add("COMMANDID", eCPACKCodeList.CPNAckCode2);
                //    mcsJobBuffer.JobResult = eJobResultCode.OTHER_ERROR;
                //}
                else
                {
                    dicCommandParameter.Add("COMMANDID", eCPACKCodeList.CPAckCode);
                    mcsJobBuffer.JobResult = eJobResultCode.SUCCESS;
                }
            }
            else if (rcmd == "BUZZER")
            {
                if (cpname != "PORTID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    //nCPAck = eCPACKCodeList.CPNAckCode1;
                }
                if (GlobalData.Current.PortManager.AllCVList.Where(c => c.ModuleName == cpval).Count() == 0)
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    //nCPAck = eCPACKCodeList.CPNAckCode2;
                }
                else
                {
                    var item = GlobalData.Current.PortManager.AllCVList.Where(c => c.ModuleName == cpval).FirstOrDefault();
                    if (item != null)
                    {
                        //if (item.GetBuzzerState() != eBuzzerControlMode.BuzzerOFF) //이미 부저 On 상태이면 Skip
                        //{
                        //    nAck = eHCACKCodeList.HCNAckCode5; //이미 buzzer되어 있으면 hcnack 5 처리
                        //}

                    }
                    //port buzzer에 대한 buzzer flag처리
                    //buzzer 실행할 수 없는 상태이면 hcnack 3 처리
                }
            }
            else if (rcmd == "REMOVE")   //REMOVE
            {
                if (cpname != "CARRIERID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    //nCPAck = eCPACKCodeList.CPNAckCode1;
                    dicCommandParameter.Add("CARRIERID", eCPACKCodeList.CPNAckCode1);
                }

                if (GlobalData.Current.ShelfMgr.AllData.Where(r => r.CarrierID == cpval).Count() == 0)
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    //nCPAck = eCPACKCodeList.CPNAckCode2;
                    dicCommandParameter.Add("CARRIERID", eCPACKCodeList.CPNAckCode2);
                }
                else
                {
                    var item = GlobalData.Current.ShelfMgr.AllData.Where(r => r.CarrierID == cpval).FirstOrDefault();

                    if (item.CarrierState != eCarrierState.COMPLETED || !item.CheckCarrierExist())
                    {
                        nAck = eHCACKCodeList.HCNAckCode2;
                        dicCommandParameter.Add("CARRIERID", eCPACKCodeList.CPNAckCode2);
                    }
                    else
                    {
                        ShelfManager.Instance.RequestCarrierRemove(item); //Shelf 에서 캐리어 제거

                        //shelfitem에 removed를 추가하여 동일 rcmd오는것에 대해 nack치자.
                        if (item.Removed == true)
                        {
                            nAck = eHCACKCodeList.HCNAckCode5;
                        }
                        else
                        {
                            item.NotifyRemoved();
                        }
                    }
                }
            }
            else
            {
                nAck = eHCACKCodeList.HCNAckCode3;
                //nCPAck = eCPACKCodeList.CPNAckCode1;
            }

            if (nAck != eHCACKCodeList.HCAckCode4)
            {
                foreach (var item in dicCommandParameter)
                {
                    DataItem diCPAckInfo = new DataItem(ItemFormatCode.List);//SuHwan_20220526 : 추가
                    diCPAckInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.Key));
                    diCPAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)item.Value));
                    diCPAckList.AddChildItem(diCPAckInfo);
                }

                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode3));
                diRcmdAckInfo.AddChildItem(diCPAckList);


                //diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode3));
                //diCPAckInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname));
                //diCPAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck));
                //diCPAckList.AddChildItem(diCPAckInfo);
                //diRcmdAckInfo.AddChildItem(diCPAckList);
            }
            else
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
            }

            return diRcmdAckInfo;
        }

        //220523 조숭진 hsms s2계열 메세지 추가
        protected virtual DataItem DoAction_S2F41_InstallPortTypeChangeCarriergen(DataMessage primaryMsg)
        {
            string rcmd = string.Empty, cpval1 = string.Empty, cpname1 = string.Empty, cpval2 = string.Empty, cpname2 = string.Empty;
            eHCACKCodeList nAck = eHCACKCodeList.HCAckCode4;
            eCPACKCodeList nCPAck1 = eCPACKCodeList.CPAckCode;
            eCPACKCodeList nCPAck2 = eCPACKCodeList.CPAckCode;

            DataItem diRcmdAckInfo = new DataItem(ItemFormatCode.List);
            DataItem diCPAckList = new DataItem(ItemFormatCode.List);
            DataItem diCPAckInfo1 = new DataItem(ItemFormatCode.List);
            DataItem diCPAckInfo2 = new DataItem(ItemFormatCode.List);

            try
            {
                rcmd = primaryMsg.Body.FirstChild.Value.ToString();
                DataItem diProcessCmdlist1 = primaryMsg.Body.ChildItems[1].ChildItems[0];
                DataItem diProcessCmdlist2 = primaryMsg.Body.ChildItems[1].ChildItems[1];

                cpname1 = diProcessCmdlist1.ChildItems[0].Value.ToString().Trim();
                cpval1 = diProcessCmdlist1.ChildItems[1].Value.ToString().Trim();
                cpname2 = diProcessCmdlist2.ChildItems[0].Value.ToString().Trim();
                cpval2 = diProcessCmdlist2.ChildItems[1].Value.ToString().Trim();

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "HSMSManager({0}) Exception: {1}", primaryMsg.Name, ex.ToString());

                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode6));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));

                return diRcmdAckInfo;
            }

            if (rcmd == "INSTALL")
            {
                if (cpname1 != "CARRIERID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck1 = eCPACKCodeList.CPNAckCode1;

                }
                if (cpname2 != "CARRIERLOC")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode1;
                }
                if (CarrierStorage.Instance.CarrierContain(cpval1)) //이미 존재하는 캐리어
                {
                    nAck = eHCACKCodeList.HCNAckCode5;
                    nCPAck1 = eCPACKCodeList.CPNAckCode2;
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname1));
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck1));
                    diCPAckList.AddChildItem(diCPAckInfo1);
                }

                ShelfItem sItem = GlobalData.Current.ShelfMgr.GetShelf(cpval2);
                if (sItem == null) //해당 쉘프가 SCS 에 있는지 체크
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode1;
                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname2));
                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck2));
                    diCPAckList.AddChildItem(diCPAckInfo2);
                }
                else if (sItem != null && sItem.CheckCarrierExist()) //쉘프에 캐리어가 이미 존재하는지 체크
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode2;
                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname2));
                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck2));
                    diCPAckList.AddChildItem(diCPAckInfo2);
                }
                //빈 캐리어 추가.
                if (nAck == eHCACKCodeList.HCAckCode4)
                {
                    ShelfManager.Instance.HostCommand_CarrierInstall(cpval2, cpval1);
                }
            }
            else if (rcmd == "PORTTYPECHANGE")
            {
                if (cpname1 != "PORTID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck1 = eCPACKCodeList.CPNAckCode1;

                }
                if (cpname2 != "PORTINOUTTYPE")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode1;
                }
                if (GlobalData.Current.PortManager.AllCVList.Where(c => c.ModuleName == cpval1).Count() == 0)
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck1 = eCPACKCodeList.CPNAckCode2;
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname1));
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck1));
                    diCPAckList.AddChildItem(diCPAckInfo1);
                }

                //if (cpval2 != "1" && cpval2 != "2" && cpval2 != "3") //Both 는 Nack .
                if (cpval2 != "1" && cpval2 != "2")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck1 = eCPACKCodeList.CPNAckCode2;

                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname2));
                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck2));
                    diCPAckList.AddChildItem(diCPAckInfo2);
                }

                if (nAck == eHCACKCodeList.HCAckCode4)
                {
                    Modules.Conveyor.CV_BaseModule item = GlobalData.Current.PortManager.AllCVList.Where(d => d.ModuleName == cpval1).FirstOrDefault();

                    if ((int)item.PortInOutType == Convert.ToInt32(cpval2))
                    {
                        nAck = eHCACKCodeList.HCNAckCode5;
                    }
                    //해당 포트라인에 Carrier가 존재하면 Nak 
                    var TargetLine = GlobalData.Current.PortManager.GetLineModule(item.LineName);
                    if (TargetLine != null)
                    {
                        foreach (var cv in TargetLine.ModuleList)
                        {
                            if (cv.CheckInoutTypeChangeAble() == false) //변경 불가
                            {
                                nAck = eHCACKCodeList.HCNAckCode2; //Currently not able to execute
                                break;
                            }
                        }
                        //타입 변경 동작은 PrimaryMessagePostAction 에서 처리함.
                    }
                    else //라인없이 포트가 존재할수는 없지만 없는경우 예외 처리함
                    {
                        nAck = eHCACKCodeList.HCNAckCode2; //Currently not able to execute
                    }
                }

            }
            else if (rcmd == "CARRIERGEN")   //CARRIERGEN 메뉴얼 인풋 포트에서 생성
            {
                if (cpname1 != "CARRIERID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck1 = eCPACKCodeList.CPNAckCode1;

                }
                if (cpname2 != "CARRIERLOC")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode1;
                }

                if (CarrierStorage.Instance.CarrierContain(cpval1)) //이미 존재하는 캐리어
                {
                    nAck = eHCACKCodeList.HCNAckCode5;
                    nCPAck1 = eCPACKCodeList.CPNAckCode2;
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname1));
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck1));
                    diCPAckList.AddChildItem(diCPAckInfo1);
                }

                CV_BaseModule cItem = GlobalData.Current.PortManager.GetCVModule(cpval2);
                if (cItem == null) //해당 포트가 SCS 에 있는지 체크
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode1;
                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname2));
                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck2));
                    diCPAckList.AddChildItem(diCPAckInfo2);
                }
                else if (!cItem.CarrierExistBySensor()) //쉘프에 캐리어가 감지 안됬는데 생성요청이 내려오면 Nak
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode2;
                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname2));
                    diCPAckInfo2.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck2));
                    diCPAckList.AddChildItem(diCPAckInfo2);
                }

                //캐리어 생성 요청이 내려왔으므로 캐리어를 생성.
                if (nAck == eHCACKCodeList.HCAckCode4)
                {
                    cItem.SetCarrierGeneratorRequset(cpval1, string.Empty); //해당 포트에 캐리어 생성 명령을 보낸다.
                }

            }
            else
            {
                nAck = eHCACKCodeList.HCNAckCode3;
                nCPAck2 = eCPACKCodeList.CPNAckCode2;
            }

            if (nAck == eHCACKCodeList.HCAckCode4)
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
            }
            else
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                diRcmdAckInfo.AddChildItem(diCPAckList);
            }

            return diRcmdAckInfo;
        }

        protected virtual DataItem DoAction_S2F41_VALIDATIONRESULT(DataMessage primaryMsg)
        {
            string rcmd = string.Empty, cpval1 = string.Empty, cpname1 = string.Empty, cpval2 = string.Empty, cpname2 = string.Empty, cpval3 = string.Empty, cpname3 = string.Empty;
            eHCACKCodeList nAck = eHCACKCodeList.HCAckCode4;
            eCPACKCodeList nCPAck1 = eCPACKCodeList.CPAckCode;
            eCPACKCodeList nCPAck2 = eCPACKCodeList.CPAckCode;

            DataItem diRcmdAckInfo = new DataItem(ItemFormatCode.List);
            DataItem diCPAckList = new DataItem(ItemFormatCode.List);
            DataItem diCPAckInfo1 = new DataItem(ItemFormatCode.List);
            DataItem diCPAckInfo2 = new DataItem(ItemFormatCode.List);

            try
            {
                rcmd = primaryMsg.Body.FirstChild.Value.ToString();
                DataItem diProcessCmdlist1 = primaryMsg.Body.ChildItems[1].ChildItems[0];
                DataItem diProcessCmdlist2 = primaryMsg.Body.ChildItems[1].ChildItems[1];
                DataItem diProcessCmdlist3 = primaryMsg.Body.ChildItems[1].ChildItems[2];

                cpname1 = diProcessCmdlist1.ChildItems[0].Value.ToString().Trim();
                cpval1 = diProcessCmdlist1.ChildItems[1].Value.ToString().Trim();
                cpname2 = diProcessCmdlist2.ChildItems[0].Value.ToString().Trim();
                cpval2 = diProcessCmdlist2.ChildItems[1].Value.ToString().Trim();
                cpname3 = diProcessCmdlist3.ChildItems[0].Value.ToString().Trim();
                cpval3 = diProcessCmdlist3.ChildItems[1].Value.ToString().Trim();

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "HSMSManager({0}) Exception: {1}", primaryMsg.Name, ex.ToString());

                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode6));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));

                return diRcmdAckInfo;
            }

            if (rcmd == "VALIDATIONRESULT")
            {
                if (cpname1 != "PORTID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck1 = eCPACKCodeList.CPNAckCode1;

                }
                if (cpname2 != "CARRIERID")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode1;
                }
                if (cpname3 != "VALIDATIONCHECKRESULT")
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    nCPAck2 = eCPACKCodeList.CPNAckCode1;
                }

                CarrierItem TargetCarrier = CarrierStorage.Instance.GetCarrierItem(cpval2);
                if (TargetCarrier == null) //없는 캐리어에 커맨드 내려옴
                {
                    nAck = eHCACKCodeList.HCNAckCode6;
                    nCPAck1 = eCPACKCodeList.CPNAckCode2;
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.ASCII, cpname2));
                    diCPAckInfo1.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nCPAck1));
                    diCPAckList.AddChildItem(diCPAckInfo1);
                }
                //NG value 업데이트
                if (nAck == eHCACKCodeList.HCAckCode4)
                {
                    if(cpval3 == "1")
                    {
                        TargetCarrier.ValidationNG = "1";
                    }
                    else if (cpval3 == "0")
                    {
                        TargetCarrier.ValidationNG = "";
                    }
                    GlobalData.Current.DBManager.DbSetProcedureCarrierInfo(TargetCarrier, false); //디비 업데이트
                }
            }
            else //RCMD MISMATCH
            {
                nAck = eHCACKCodeList.HCNAckCode3;
                nCPAck2 = eCPACKCodeList.CPNAckCode2;
            }

            if (nAck == eHCACKCodeList.HCAckCode4)
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
            }
            else
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                diRcmdAckInfo.AddChildItem(diCPAckList);
            }

            return diRcmdAckInfo;
        }



        //220523 조숭진 hsms s2계열 메세지 추가
        //220523 조숭진 하위리스트에 대한 cpnack에 대해 고객사 문의필요하여 중단. 사양서도 누락됨.
        //S2F49(Enhanced Host Command Send)
        public virtual DataItem DoAction_S2F49(DataMessage primaryMsg)
        {
            string rcmd = string.Empty;
            string commcpname1 = string.Empty, commcpname2 = string.Empty, commcpname3 = string.Empty;
            string commcpval1 = string.Empty, commcpval2 = string.Empty;
            string transcpname1 = string.Empty, transcpname2 = string.Empty, transcpname3 = string.Empty, transcpname4 = string.Empty, transcpname5 = string.Empty;
            string finalloccpname1 = string.Empty;      //20220801 조숭진 추가
            string transcpval1 = string.Empty, transcpval2 = string.Empty, transcpval3 = string.Empty, transcpval4 = string.Empty, transcpval5 = string.Empty;

            eHCACKCodeList nAck = eHCACKCodeList.HCAckCode4;
            //eCPACKCodeList nCPAck1 = eCPACKCodeList.CPAckCode;

            DataItem diRcmdAckInfo = new DataItem(ItemFormatCode.List);
            DataItem diCPAckInfo = new DataItem(ItemFormatCode.List);

            try
            {
                DataItem diTransferCmdlist1 = primaryMsg.Body.ChildItems[3].ChildItems[0];
                DataItem diTransferCmdlist2 = primaryMsg.Body.ChildItems[3].ChildItems[1];
                DataItem diTransferCmdlist3 = primaryMsg.Body.ChildItems[3].ChildItems[2];      //20220801 조숭진 추가 FINALLOCATION


                DataItem diCmdInfolist = diTransferCmdlist1.ChildItems[1];
                DataItem diTransInfolist = diTransferCmdlist2.ChildItems[1];
                //DataItem diFinalloclist = diTransferCmdlist3.ChildItems[1];             //20220801 조숭진 추가

                rcmd = primaryMsg.Body.ChildItems[2].Value.ToString().Trim(); //TRANSFER
                commcpname1 = diTransferCmdlist1.FirstChild.Value.ToString().Trim(); //COMMMANDINFO
                commcpname2 = diCmdInfolist.ChildItems[0].ChildItems[0].Value.ToString().Trim();    //
                commcpname3 = diCmdInfolist.ChildItems[1].ChildItems[0].Value.ToString().Trim();    //

                commcpval1 = diCmdInfolist.ChildItems[0].ChildItems[1].Value.ToString().Trim();     //COMMANDID
                commcpval2 = diCmdInfolist.ChildItems[1].ChildItems[1].Value.ToString().Trim();     //PRIORITY
                transcpname1 = diTransferCmdlist2.FirstChild.Value.ToString().Trim();
                transcpname2 = diTransInfolist.ChildItems[0].ChildItems[0].Value.ToString().Trim();
                transcpname3 = diTransInfolist.ChildItems[1].ChildItems[0].Value.ToString().Trim();
                transcpname4 = diTransferCmdlist3.ChildItems[0].ChildItems[0].Value.ToString().Trim();  //FINAL LOC     
                //transcpname5 = diTransferCmdlist3.ChildItems[1].ChildItems[0].Value.ToString().Trim();  //WINDERDIRCTION OH 사양에 따로 없음

                transcpval1 = diTransInfolist.ChildItems[0].ChildItems[1].Value.ToString().Trim();  //CARRIERID
                transcpval2 = diTransInfolist.ChildItems[1].ChildItems[1].Value.ToString().Trim();  //SOURCE
                transcpval3 = diTransInfolist.ChildItems[2].ChildItems[1].Value.ToString().Trim();  //DEST
                transcpval4 = diTransferCmdlist3.ChildItems[0].ChildItems[1].Value.ToString().Trim();  //FINAL LOC       
                //transcpval5 = diTransferCmdlist3.ChildItems[1].ChildItems[1].Value.ToString().Trim();  //WINDERDIRCTION OH 사양에 따로 없음

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "HSMSManager({0}) Exception: {1}", primaryMsg.Name, ex.ToString());

                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode6));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
                return diRcmdAckInfo;
            }

            if (GlobalData.Current.MainBooth.SCState == eSCState.INIT) //초기화 동작중에는 명령 거절
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode2));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
                return diRcmdAckInfo;
            }

            if (GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Offline_EQ) //온라인 리모트 상태가 아니면 명령 거절.
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode2));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
                return diRcmdAckInfo;
            }

            if (rcmd != "TRANSFER")
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode1));
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
                return diRcmdAckInfo;
            }

            if (commcpname1 != "COMMANDINFO")
            {
                nAck = eHCACKCodeList.HCNAckCode3;
                DataItem InvalidList = new DataItem(ItemFormatCode.List);
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "COMMANDINFO"));
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode2));
                diCPAckInfo.AddChildItem(InvalidList);
            }

            //CommandID 중복 체크
            if (GlobalData.Current.McdList.IsCommandIDContain(commcpval1))
            {
                nAck = eHCACKCodeList.HCNAckCode5;
                DataItem InvalidList = new DataItem(ItemFormatCode.List);
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "COMMANDID"));
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode2));
                diCPAckInfo.AddChildItem(InvalidList);
            }

            //CarrierID 중복 체크
            if (GlobalData.Current.McdList.IsCarrierIDContain(transcpval1))
            {
                nAck = eHCACKCodeList.HCNAckCode5;
                DataItem InvalidList = new DataItem(ItemFormatCode.List);
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "CARRIERID"));
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode2));
                diCPAckInfo.AddChildItem(InvalidList);
            }

            //Priority 유효성 체크
            if (int.TryParse(commcpval2, out int priority))
            {
                if (priority < 1 || priority > 99) //1 ~ 99 까지 유효
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    DataItem InvalidList = new DataItem(ItemFormatCode.List);
                    InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "PRIORITY"));
                    InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode3));
                    diCPAckInfo.AddChildItem(InvalidList);
                }
            }
            else  //파싱 실패
            {
                nAck = eHCACKCodeList.HCNAckCode3;
                DataItem InvalidList = new DataItem(ItemFormatCode.List);
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "PRIORITY"));
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode3));
                diCPAckInfo.AddChildItem(InvalidList);
            }

            eCarrierSize TargetCarrierSize = eCarrierSize.Unknown; //S2F49 내려올때 캐리어 사이즈 체크 추가.
            //CarrierID 존재 체크 스토리지
            if (!CarrierStorage.Instance.CarrierContain(transcpval1))
            {
                nAck = eHCACKCodeList.HCNAckCode6;
                DataItem InvalidList = new DataItem(ItemFormatCode.List);
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "CARRIERID"));
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode1));
                diCPAckInfo.AddChildItem(InvalidList);
            }
            else
            {
                CarrierItem cItem = CarrierStorage.Instance.GetCarrierItem(transcpval1);
                if (cItem != null)
                {
                    TargetCarrierSize = cItem.CarrierSize;
                }
            }
            ICarrierStoreAble Source;
            ICarrierStoreAble Dest;
            //Source 유효성 체크
            //Shelf 는 Zone Name 으로 들어온다.
            bool SourceZoneNameShelfExist = ShelfManager.Instance.CheckShelfZoneNameExist(transcpval2);
            if (SourceZoneNameShelfExist)
            {
                //캐리어 ID 로 실제 쉘프 정보를 가져온다.
                Source = ShelfManager.Instance.GetShelfItemByCarrierID(transcpval1);
                if (Source == null)
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    DataItem InvalidList = new DataItem(ItemFormatCode.List);
                    InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "SOURCE"));
                    InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode1));
                    diCPAckInfo.AddChildItem(InvalidList);
                }
            }
            else
            {
                Source = GlobalData.Current.GetGlobalCarrierStoreAbleObject(transcpval2); //목적지가 포트이거나 크레인일수도 있다.
                if (Source is CV_BaseModule) //인라인에서 LP 위치로 소스명령이 들어오면 OP 로 바꾼다.
                {
                    Source = null;//GlobalData.Current.PortManager.GetInlineRobotAccessPort(Source as CV_BaseModule);
                }
                if (Source == null)
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    DataItem InvalidList = new DataItem(ItemFormatCode.List);
                    InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "SOURCE"));
                    InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode1));
                    diCPAckInfo.AddChildItem(InvalidList);
                }
                if (Source is ShelfItem sItem)
                {
                    if(sItem.CheckCarrierExist() == false || sItem.CarrierID != transcpval1) //소스가 Shelf 인데 화물이 없다. //241014 RGJ 캐리어 아이디 체크 추가.
                    {
                        nAck = eHCACKCodeList.HCNAckCode6; //241014 RGJ Nak 6으로 변경.조범석 매니저 요청.
                        DataItem InvalidList = new DataItem(ItemFormatCode.List);
                        InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "SOURCE"));
                        InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode1));
                        diCPAckInfo.AddChildItem(InvalidList);
                    }
                }
                else if (Source is RMModuleBase rmItem) //241014 RGJ 소스가 크레인 
                {
                    if(!rmItem.CarrierExistSensor || rmItem.GetCarrierID() != transcpval1) //데이터랑 센서 체크 이상
                    {
                        nAck = eHCACKCodeList.HCNAckCode6; //241014 RGJ Nak 6으로 변경.조범석 매니저 요청.
                        DataItem InvalidList = new DataItem(ItemFormatCode.List);
                        InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "SOURCE"));
                        InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode1));
                        diCPAckInfo.AddChildItem(InvalidList);
                    }
                }
                else if (Source is CV_BaseModule cvItem) //241014 RGJ 포트는 포트 라인에 캐리어 있으면 유효로 간주.
                {
                    CVLineModule PortLine = cvItem.ParentModule as CVLineModule;
                    if(PortLine == null || !PortLine.CheckCarrierExistInLine(transcpval1)) //포트 라인에 있는지?
                    {
                        nAck = eHCACKCodeList.HCNAckCode6; //241014 RGJ Nak 6으로 변경.조범석 매니저 요청.
                        DataItem InvalidList = new DataItem(ItemFormatCode.List);
                        InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "SOURCE"));
                        InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode1));
                        diCPAckInfo.AddChildItem(InvalidList);
                    }
                }
            }


            //Dest 유효성 체크
            //Shelf 는 Zone Name 으로 들어온다.
            bool DestZoneNameShelfExist = ShelfManager.Instance.CheckShelfZoneNameExist(transcpval3);
            if (DestZoneNameShelfExist)
            {
                //해당 Zone과 매칭되는 적절한 Shelf 를 고른다.
                Dest = ShelfManager.Instance.GetProperDestShelf(transcpval3, Source);
                if (Dest == null) //ZoneName 이 존재하지만 Null 리턴은 가용쉘프가 없는경우 => ZoneFull 상태
                {
                    nAck = eHCACKCodeList.HCNAckCode17; //Specific Shelf Zone is Full
                    //Dest = GlobalData.Current.PortManager.GetProperOutPort(Source, TargetCarrierSize); //삭제 [MCS 에서 포트로 대체 반송 명령 내림]
                }
            }
            else
            {
                Dest = GlobalData.Current.GetGlobalCarrierStoreAbleObject(transcpval3); //목적지가 포트이거나 크레인일수도 있다.
                if (Dest is CV_BaseModule) //인라인에서 LP 위치로 소스명령이 들어오면 OP 로 바꾼다.
                {
                    Dest = null;// GlobalData.Current.PortManager.GetInlineRobotAccessPort(Dest as CV_BaseModule);
                }
                if (Dest == null)
                {
                    nAck = eHCACKCodeList.HCNAckCode3;
                    DataItem InvalidList = new DataItem(ItemFormatCode.List);
                    InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "DEST"));
                    InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode1));
                    diCPAckInfo.AddChildItem(InvalidList);
                }
            }

            if (Dest != null && Dest.CheckCarrierSizeAcceptable(TargetCarrierSize) == false) //목적지로 해당 사이즈 투입 불가능
            {
                nAck = eHCACKCodeList.HCNAckCode10;
                DataItem InvalidList = new DataItem(ItemFormatCode.List);
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "DEST"));
                InvalidList.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eCPACKCodeList.CPNAckCode2));
                diCPAckInfo.AddChildItem(InvalidList);
            }



            //FianlLocation 유효성체크 사양 컨펌후 구현

            if (nAck == eHCACKCodeList.HCAckCode || nAck == eHCACKCodeList.HCAckCode4) //Zone Full Ack 17 추가-> 사양 번경으로 Nak 임 
            {
                //파라미터 체크가 끝났으면 작업 생성
                //MAKE TRANSFER COMMAND 
                bool bCreateSuccess = GlobalData.Current.McdList.CreateMCSHostJob(commcpval1, transcpval1, transcpval3, Source, Dest, null, priority);
                if (bCreateSuccess)
                {
                    diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                    diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
                }
                else
                {
                    diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)eHCACKCodeList.HCNAckCode2));
                    diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.List, 0));
                }

            }
            else  //파라미터 이상
            {
                diRcmdAckInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, (byte)nAck));
                diRcmdAckInfo.AddChildItem(diCPAckInfo);
                return diRcmdAckInfo;
            }
            return diRcmdAckInfo;
        }

        #endregion

        #region S5F*
        /// <summary>
        /// S5F5(List Alarm Request)
        /// </summary>
        /// <param name="primaryMsg"></param>
        /// <returns></returns>
        [Obsolete("비사용 메시지")]
        public virtual DataItem DoAction_S5F5(DataMessage primaryMsg)
        {
            #region Variable
            List<long> liALIDList = new List<long>();
            bool bAllALCD = false;
            #endregion

            #region Data Pasing

            if (primaryMsg.Body != null)
            {
                if (primaryMsg.Body.ChildItems.Count == 0)
                    bAllALCD = true;
                else
                {
                    foreach (var item in primaryMsg.Body.ChildItems)
                    {
                        long lALID = int.Parse(item.Value.ToString());
                        if (lALID == 0)
                        {
                            bAllALCD = true;
                        }
                        liALIDList.Add(lALID);
                    }
                }
            }


            #endregion

            #region Data Check

            #endregion

            #region Make Reply Data
            DataItem diAllAlarmList = new DataItem(ItemFormatCode.List);
            foreach (var item in GlobalData.Current.Alarm_Manager.getAllAlarmList())
            {
                if (bAllALCD || liALIDList.Contains(item.iAlarmID))
                {
                    DataItem diAlarmData = new DataItem(ItemFormatCode.List);
                    diAlarmData.AddChildItem(new DataItem(ItemFormatCode.Bin, 128));
                    diAlarmData.AddChildItem(new DataItem(ItemFormatCode.U4, item.iAlarmID));
                    diAlarmData.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.AlarmName));
                    diAllAlarmList.AddChildItem(diAlarmData);
                }
            }

            #endregion

            return diAllAlarmList;

        }
        #endregion

        #region S6F11*s
        /// <summary>
        /// S6F11 CEID = 1 Offline
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID1(Dictionary<string, object> args)
        {
            #region Collect Data
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 1));                                    //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            //DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            //diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 1));                                        //<U2 RPTID>
            //DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[5]
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 CommandID>
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A100 ErrorID>
            //DataItem diSTKUnitList = new DataItem(ItemFormatCode.List);                                             //L[2]
            //diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.ASCII,""));                                          //<A64 StockedUnitID>
            //diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.U2, 0));                                             //<U2 StockedUnitState>
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII,""));                                      //<A64 RecoveryOptions>
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                         //<U4 ErrorNumber>
            //diCommandList.AddChildItem(diSTKUnitList);
            //diRPIDData.AddChildItem(diCommandList);
            //diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion

            return diCEID;
        }
        /// <summary>
        /// S6F11 CEID = 3 OnlineRemote
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID3(Dictionary<string, object> args)
        {
            #region Collect Data
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                    //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            //DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            //diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 1));                                        //<U2 RPTID>
            //DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[5]
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 CommandID>
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A100 ErrorID>
            //DataItem diSTKUnitList = new DataItem(ItemFormatCode.List);                                             //L[2]
            //diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                          //<A64 StockedUnitID>
            //diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.U2, 0));                                             //<U2 StockedUnitState>
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                      //<A64 RecoveryOptions>
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                         //<U4 ErrorNumber>
            //diCommandList.AddChildItem(diSTKUnitList);
            //diRPIDData.AddChildItem(diCommandList);
            //diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion

            return diCEID;
        }


        /// <summary>
        /// S6F11 CEID = 101 AlarmCleared
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID101(Dictionary<string, object> args)
        {
            #region Collect Data
            AlarmData aData = null;
            string JobID = string.Empty;
            AlarmRecoveryCmd ARC = new AlarmRecoveryCmd();
            ICarrierStoreAble MB = null;
            if (args.ContainsKey("JOBID"))
            {
                JobID = (string)args["JOBID"];
            }

            if (args.ContainsKey("ALARM"))
            {
                aData = (AlarmData)args["ALARM"];
            }
            if (aData == null)
            {
                return null;
            }

            MB = GlobalData.Current.GetGlobalCarrierStoreAbleObject(aData.ModuleName);
            if (aData.AlarmRecoveryList != null && aData.AlarmRecoveryList.Count() >= 1)
            {
                ARC = aData.AlarmRecoveryList[0];
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 101));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 1));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[5]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobID));                              //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.AlarmName));                    //<A100 ErrorID>
            DataItem diSTKUnitList = new DataItem(ItemFormatCode.List);                                         //L[2]
            diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.ModuleName));                       //<A64 StockedUnitID>
            diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.U2, MB != null ? MB.GetUnitServiceState() : 1 ));                   //<U2 StockedUnitState>
            diCommandList.AddChildItem(diSTKUnitList);
            if (aData.RecoveryOption == null)
            {
                aData.RecoveryOption = string.Empty;
            }
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.RecoveryOption));               //<A64 RecoveryOptions>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, aData.iAlarmID));                        //<U4 ErrorNumber>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion

            return diCEID;
        }
        /// <summary>
        /// S6F11 CEID = 102 AlarmSet
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID102(Dictionary<string, object> args)
        {
            #region Collect Data
            AlarmData aData = null;
            string JobID = string.Empty;
            AlarmRecoveryCmd ARC = new AlarmRecoveryCmd();
            ICarrierStoreAble MB = null;
            if (args.ContainsKey("JOBID"))
            {
                JobID = (string)args["JOBID"];
            }

            if (args.ContainsKey("ALARM"))
            {
                aData = (AlarmData)args["ALARM"];
            }
            if (aData == null)
            {
                return null;
            }

            MB = GlobalData.Current.GetGlobalCarrierStoreAbleObject(aData.ModuleName);
            if (aData.AlarmRecoveryList != null && aData.AlarmRecoveryList.Count() >= 1)
            {
                ARC = aData.AlarmRecoveryList[0];
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 102));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 1));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[5]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobID));                              //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.AlarmName));                    //<A100 ErrorID>
            DataItem diSTKUnitList = new DataItem(ItemFormatCode.List);                                         //L[2]
            diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.ModuleName));                       //<A64 StockedUnitID>
            diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.U2, MB == null ? 1 : MB.GetUnitServiceState())); //<U2 StockedUnitState>
            diCommandList.AddChildItem(diSTKUnitList);
            if (aData.RecoveryOption == null)
            {
                aData.RecoveryOption = string.Empty;
            }
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.RecoveryOption));                       //<A64 RecoveryOptions>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, aData.iAlarmID));                        //<U4 ErrorNumber>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 103 SCAutoCompleted
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID103(Dictionary<string, object> args)
        {
            #region Collect Data
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 103));                                  //<U2 CEID>

            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            //DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            //diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 1));                                        //<U2 RPTID>
            //DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[5]
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 CommandID>
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A100 ErrorID>
            //DataItem diSTKUnitList = new DataItem(ItemFormatCode.List);                                             //L[2]
            //diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.ASCII, STKUnitID));                                  //<A64 StockedUnitID>
            //diSTKUnitList.AddChildItem(new DataItem(ItemFormatCode.U2, STKSTATE));                                      //<U2 StockedUnitState>
            //diCommandList.AddChildItem(diSTKUnitList);
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                      //<A64 RecoveryOptions>
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                         //<U4 ErrorNumber>
            //diRPIDData.AddChildItem(diCommandList);
            //diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 105 SCPauseCompleted
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID105(Dictionary<string, object> args)
        {
            #region Collect Data
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 105));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]

            diCEID.AddChildItem(diRPIDList);
            #endregion

            return diCEID;
        }
        /// <summary>
        /// S6F11 CEID = 106 SCPaused
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID106(Dictionary<string, object> args)
        {
            #region Collect Data
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 106));                                  //<U2 CEID>

            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]

            diCEID.AddChildItem(diRPIDList);
            #endregion

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 107 SCPauseInitiated
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID107(Dictionary<string, object> args)
        {
            #region Collect Data

            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 107));                                  //<U2 CEID>

            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]

            diCEID.AddChildItem(diRPIDList);
            #endregion

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 201 TransferAbortCompleted
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID201(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 201));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            
            //2024.05.07 lim, 추가 확인
            //if(string.IsNullOrEmpty(JobData.CarrierLoc)) //CarrierLoc이 비어 있다면 (자재는 이미 제거 했다고 판단) 
            //{
            //    //TransferAbortCompleted 보고시 DEST 로 채워서 보고 요청(조범석 매니저 협의)
            //    diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 CarrierLoc>
            //}
            //else
            {
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            }
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, JobData.JobResult));                         //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;

        }

        /// <summary>
        /// S6F11 CEID = 202 TransferAbortFail
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID202(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }

            if (JobData == null)
            {
                JobData = new McsJob();

                if (args.ContainsKey("COMMANDID"))
                {
                    JobData.CommandID = (string)args["COMMANDID"];
                    JobData.JobResult = eJobResultCode.MISMATCH_ID;
                }
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 202));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, eJobResultCode.SUCCESS));                         //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 203 TransferAbortInitiated
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID203(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }

            if (JobData == null)
            {
                JobData = new McsJob();

                if (args.ContainsKey("COMMANDID"))
                {
                    JobData.CommandID = (string)args["COMMANDID"];
                    JobData.JobResult = eJobResultCode.MISMATCH_ID;
                }
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 203));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, eJobResultCode.SUCCESS));                    //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 204 TransferCancelCompleted
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID204(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 204));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, JobData.JobResult));                         //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 206 vransferCancelInitiated 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID206(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;

            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }

            if (JobData == null)
            {
                JobData = new McsJob();

                if (args.ContainsKey("COMMANDID"))
                {
                    JobData.CommandID = (string)args["COMMANDID"];
                    JobData.JobResult = eJobResultCode.MISMATCH_ID;
                }
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 206));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, JobData.JobResult));                         //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 205 TransferCancelFailed
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID205(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;

            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }

            if (JobData == null)
            {
                JobData = new McsJob();

                if (args.ContainsKey("COMMANDID"))
                {
                    JobData.CommandID = (string)args["COMMANDID"];
                    JobData.JobResult = eJobResultCode.MISMATCH_ID;
                }
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 205));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, JobData.JobResult));                         //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }


        /// <summary>
        /// S6F11 CEID = 207 TransferCompleted
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID207(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            bool VoidJob = false; //240514 RGJ 강제 완료보고 할때 목적지로 보고해야함.
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (args.ContainsKey("VOIDJOB"))
            {
                VoidJob = (bool)args["VOIDJOB"];
            }
            if (JobData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 207));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            
            if (VoidJob) //CarrierLoc Dest 로 보고 
            {
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                     //<A64 CarrierLoc>
            }
            else
            {
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            }

            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>

            if (VoidJob) //CarrierZoneName Dest 로 보고
            {
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                     //<A64 CarrierZoneName>
            }
            else
            {
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            }


            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, JobData.JobResult));                         //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }
        /// <summary>
        /// S6F11 CEID = 208 TransferInitiated
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID208(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 208));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, JobData.JobResult));                         //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 209 TransferPaused
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID209(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 209));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, JobData.JobResult));                         //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 210 TransferResumed
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID210(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 210));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 2));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, JobData.JobResult));                         //<U2 ResultCode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }


        /// <summary>
        /// S6F11 CEID = 301 RPTID = 3 CarrierInstallCompleted
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID301(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            ShelfItem sItem = null;
            if (args.ContainsKey("SHELFITEM"))
            {
                sItem = (ShelfItem)args["SHELFITEM"];
            }
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null || sItem == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 301));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 14));                                       //<U2 RPTID> 3에서 14로 바꿔야함.
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, sItem.CarrierID));                        //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, sItem.iLocName));                         //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, sItem.ZONE));                             //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, eHandoffType.MANUAL));                       //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.TargetCVPortType));               //<A32 PortType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLotID));                   //<A64 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, sItem.GetCarrierHSMSPalletSize())); //231002 RGJ EnHancedCarriers PalletSize 추가. 아직 미적용
            
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 302 RPTID = 3 CarrierRemovedCompleted
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID302(Dictionary<string, object> args)
        {
            #region Collect Data
            string JobID = string.Empty;
            CarrierItem cItem = null;
            ICarrierStoreAble sItem = null;
            if (args.ContainsKey("JOBID"))
            {
                JobID = (string)args["JOBID"];
            }

            if (args.ContainsKey("CARRIERITEM"))
            {
                cItem = (CarrierItem)args["CARRIERITEM"];
            }
            if (cItem == null)
            {
                return null;
            }
            if (args.ContainsKey("SHELFITEM"))
            {
                sItem = (ICarrierStoreAble)args["SHELFITEM"];
            }
            if (args.ContainsKey("CRANEITEM"))
            {
                sItem = (ICarrierStoreAble)args["CRANEITEM"];
            }
            if (sItem == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 302));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobID));                                  //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.CarrierID));                        //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, sItem.iLocName));                         //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, sItem.iZoneName));                        //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, eHandoffType.MANUAL));                         //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A32 PortType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.LotID));                            //<A64 LotID>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 303 RPTID = 3 CarrierRemoved
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID303(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            CV_BaseModule Port = null;
            string CarrierID = "";
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
                if (args.ContainsKey("CARRIERID"))
                {
                    CarrierID = (string)args["CARRIERID"];
                }
            }
            if (JobData == null && Port == null)
            {
                return null;
            }

            #endregion
            if (JobData != null) //JobData 로 보고
            {
                #region Make Report Data
                DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
                diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
                diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 303));                                  //<U2 CEID>
                DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
                DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
                diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
                DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 CarrierLoc>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.iZoneName));                         //<A64 CarrierZoneName>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.AssignRMName));                   //<A64 StockerCraneID>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetHandOffType()));                  //<U2 HandoffType>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.TargetCVPortType));               //<A32 PortType>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLotID));                   //<A64 LotID>
                diRPIDData.AddChildItem(diCommandList);
                diRPIDList.AddChildItem(diRPIDData);
                diCEID.AddChildItem(diRPIDList);
                #endregion;

                return diCEID;
            }
            else if (Port != null) //포트 데이터로 보고
            {
                #region Make Report Data
                DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
                diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
                diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 303));                                  //<U2 CEID>
                DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
                DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
                diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
                DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, string.Empty));                           //<A64 CommandID>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CarrierID));                              //<A64 CarrierID>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 CarrierLoc>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, string.Empty));                           //<A64 Dest>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 CarrierZoneName>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, string.Empty));                           //<A64 StockerCraneID>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetHandOffType()));                       //<U2 HandoffType>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.PortType.ToString()));               //<A32 PortType>
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, string.Empty));                           //<A64 LotID>
                diRPIDData.AddChildItem(diCommandList);
                diRPIDList.AddChildItem(diRPIDData);
                diCEID.AddChildItem(diRPIDList);
                #endregion;

                return diCEID;
            }
            return null;
        }

        /// <summary>
        /// S6F11 CEID = 304 RPTID = 3 CarrierResumed
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID304(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 304));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.AssignedRM?.ModuleName));         //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, eHandoffType.AUTO));                         //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A32 PortType> //[230503 CIM 검수] CarrierResumed 포트 타입 공백
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLotID));                   //<A64 LotID>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 305 RPTID = 3 CarrierStored
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID305(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 305));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.AssignRMName));                       //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, (int)JobData.HandoffType));               //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.TargetCVPortType));               //<A32 PortType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLotID));                   //<A64 LotID>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 306 RPTID = 3 CarrierStoredAlt
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID306(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 306));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLoc));                     //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                   //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierZoneName));                //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.AssignRMName));                   //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, (int)JobData.HandoffType));                  //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A32 PortType> //[230503 CIM 검수] CarrierResumed 포트 타입 공백
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLotID));                   //<A64 LotID>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 307 RPTID = 3 CarrierTransferring
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID307(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;

            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }
            RMModuleBase RM = JobData.AssignedRM;
            if (RM == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 307));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, RM.ModuleName));                          //<A64 CarrierLoc> //[230503 CIM 검수]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                   //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, RM.iZoneName));                           //<A64 CarrierZoneName> //[230503 CIM 검수]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.AssignRMName));                   //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, (int)JobData.HandoffType));                  //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A32 PortType> //[230503 CIM 검수] 포트타입 공백으로
            //diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.TargetCVPortType));             //<A32 PortType> 
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierLotID));                   //<A64 LotID>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }


        /// <summary>
        /// S6F11 CEID = 308 RPTID = 3 CarrierWaitIn
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID308(Dictionary<string, object> args)
        {
            #region Collect Data
            CarrierItem cItem = null;
            CV_BaseModule portItem = null;
            if (args.ContainsKey("CARRIERITEM"))
            {
                cItem = (CarrierItem)args["CARRIERITEM"];
            }
            if (args.ContainsKey("PORT"))
            {
                portItem = (CV_BaseModule)args["PORT"];
            }
            if (cItem == null || portItem == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 308));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.CarrierID));                        //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.CarrierLocation));                  //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, "")); //SCS 에서는 공백으로 보고함.       //<A64 Dest>  
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.CarrierZoneName));                  //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, portItem.GetHandOffType()));                 //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, portItem.PortType.ToString()));           //<A32 PortType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.LotID));                            //<A64 LotID>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }



        /// <summary>
        /// S6F11 CEID = 309 RPTID = 3 CarrierWaitOut
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID309(Dictionary<string, object> args)
        {
            #region Collect Data
            CarrierItem cItem = null;
            McsJob JobData = null;
            CV_BaseModule portItem = null;
            if (args.ContainsKey("CARRIERITEM"))
            {
                cItem = (CarrierItem)args["CARRIERITEM"];
            }
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (args.ContainsKey("PORT"))
            {
                portItem = (CV_BaseModule)args["PORT"];
            }

            if (portItem == null)
            {
                return null;
            }
            if(JobData == null && cItem == null)
            {
                return null;
            }

            string CommandID = JobData != null ? JobData.CommandID : string.Empty;
            string CraneID = JobData != null ? JobData.AssignRMName : string.Empty;
           
            string CarrierID; //240521 RGJ CarrierItem Null 케이스 보완
            string LotID = string.Empty; //240521 RGJ CarrierItem Null 케이스 보완

            if (cItem == null) //캐리어 아이템이 Null 일경우 
            { 
                CarrierID = JobData.CarrierID; //JobData 에서 가져온다.
            }
            else
            {
                CarrierID = cItem.CarrierID;
                LotID = cItem.LotID;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 309));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CommandID));                              //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CarrierID));                        //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, portItem.ModuleName));                    //<A64 CarrierLoc>
            //20231221 RGJ WaitOut 보고시에는 Dest는 LP Port 로 보고해야함.
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, portItem.iZoneName));                     //<A64 Dest>     
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, portItem.iZoneName));                     //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CraneID));                                //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, portItem.GetHandOffType()));                 //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, portItem.PortType.ToString()));           //<A32 PortType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, LotID));                            //<A64 LotID>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }


        /// <summary>
        /// S6F11 CEID = 310 RPTID = 9 ZoneCapacityChange
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID310(Dictionary<string, object> args)
        {
            #region Collect Data
            int Capa = 0;
            string ZoneName;

            if (args.ContainsKey("ZONENAME"))
            {
                ZoneName = (string)args["ZONENAME"];
            }
            else
            {
                return null;
            }
            Capa = GlobalData.Current.ShelfMgr.CalcShelfZoneCapa(ZoneName); //쉘프부터 검색해본다.
            if (Capa == 0) //없으면 포트 조회
            {
               Capa = GlobalData.Current.PortManager.CalcPortZoneCapa(ZoneName);
            }

            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));               //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 310));             //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                  //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                    //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 9));                   //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                    //L[2]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ZoneName));       //<A64 ZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Capa));               //<A64 ZoneCapacity>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 311 RPTID = 3 CarrierLocationChanged
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID311(Dictionary<string, object> args)
        {
            CarrierItem cItem = null;
            CV_BaseModule portItem = null;

            #region Collect Data
            if (args.ContainsKey("CARRIERITEM"))
            {
                cItem = (CarrierItem)args["CARRIERITEM"];
            }
            if (args.ContainsKey("PORT"))
            {
                portItem = (CV_BaseModule)args["PORT"];
            }
            if (cItem == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 311));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.CarrierID));                        //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, portItem.ModuleName));                    //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, portItem.ModuleName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, portItem.iZoneName));                     //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, portItem.GetHandOffType()));                         //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, portItem.PortType.ToString()));           //<A32 PortType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.LotID));                            //<A64 LotID>

            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }


        /// <summary>
        /// S6F11 CEID = 312 RPTID = 3 CarrierGeneratorRequest
        /// 메뉴얼 포트에 캐리어 안착되고 Key In 완료 되었을때 캐리어 생성 요청
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID312(Dictionary<string, object> args)
        {
            #region Collect Data

            CarrierItem cItem = null;
            CV_BaseModule port = null;
            if (args.ContainsKey("CARRIERITEM"))
            {
                cItem = (CarrierItem)args["CARRIERITEM"];
            }
            if (cItem == null)
            {
                return null;
            }
            if (args.ContainsKey("PORT"))
            {
                port = (CV_BaseModule)args["PORT"];
            }
            if (port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 312));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.CarrierID));                        //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, port.iLocName));                          //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, port.iZoneName));                         //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, port.GetHandOffType()));                    //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, port.PortType));                          //<A32 PortType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.LotID));                            //<A64 LotID>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        //2024.06.26 lim, 셀버퍼 자동 Keyin 기능 추가
        /// <summary>
        /// S6F11 CEID = 313 RPTID = 3 CarrierInfoRequest
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID313(Dictionary<string, object> args)
        {
            #region Collect Data
            CarrierItem cItem = null;
            CV_BaseModule port = null;

            if (args.ContainsKey("CARRIERITEM"))
            {
                cItem = (CarrierItem)args["CARRIERITEM"];
            }
            if (cItem == null)
            {
                return null;
            }
            if (args.ContainsKey("PORT"))
            {
                port = (CV_BaseModule)args["PORT"];
            }
            if (port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 313));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 3));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.CarrierID));                        //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, port.iLocName));                          //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, port.iZoneName));                         //<A64 CarrierZoneName>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ""));                                     //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, port.GetHandOffType()));                    //<U2 HandoffType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, port.PortType));                          //<A32 PortType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, cItem.LotID));                            //<A64 LotID>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 401 RPTID = 4 PortInService
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID401(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 401));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 4));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.IsInPort ? "1" : "2"));                 //<U2 PoritInOutType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.PortAccessMode));                           //<U2 PortAccessMode>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCarrierID()));             //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetPortCarrierState()));                //<U2  CarrierState>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentCarrierType()));           //<A64 CarrierType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentProductEmpty()));             //<U2 ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentLot()));                   //<A32 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentTrayType()));                 //<U2 TrayType>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 402 RPTID = 4 PortOutOfService
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID402(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                            //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                           //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 402));                                         //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                            //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                              //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 4));                                             //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                              //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                            //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.IsInPort ? "1" : "2"));                     //<U2 PoritInOutType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.PortAccessMode));                               //<U2 PortAccessMode>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCarrierID()));                 //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetPortCarrierState()));                    //<U2  CarrierState>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentCarrierType()));               //<A64 CarrierType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentProductEmpty()));                 //<U2 ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentLot()));                       //<A32 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentTrayType()));                     //<U2 TrayType>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 403 RPTID = 4 PortTransferBlocked
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID403(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 403));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 4));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.IsInPort ? "1" : "2"));                 //<U2 PoritInOutType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.PortAccessMode));                      //<U2 PortAccessMode>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCarrierID()));                    //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetPortCarrierState()));                //<U2  CarrierState>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentCarrierType()));           //<A64 CarrierType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentProductEmpty()));             //<U2 ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentLot()));                   //<A32 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentTrayType()));                 //<U2 TrayType>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 404 RPTID = 4 PortReadyToLoad
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID404(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 404));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 4));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.IsInPort ? "1" : "2"));                 //<U2 PoritInOutType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.PortAccessMode));                      //<U2 PortAccessMode>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCarrierID()));             //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetPortCarrierState()));                //<U2  CarrierState>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentCarrierType()));           //<A64 CarrierType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentProductEmpty()));             //<U2 ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentLot()));                   //<A32 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentTrayType()));                 //<U2 TrayType>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 405 RPTID = 4 PortReadyToUnload
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID405(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 405));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 4));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.IsInPort ? "1" : "2"));                 //<U2 PoritInOutType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.PortAccessMode));                           //<U2 PortAccessMode>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCarrierID()));             //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetPortCarrierState()));                //<U2  CarrierState>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentCarrierType()));           //<A64 CarrierType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentProductEmpty()));             //<U2 ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentLot()));                   //<A32 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentTrayType()));                 //<U2 TrayType>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 406 RPTID = 4 PortTypeChanged
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID406(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 406));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 4));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.IsInPort ? "1" : "2"));                 //<U2 PoritInOutType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.PortAccessMode));                           //<U2 PortAccessMode>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCarrierID()));             //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetPortCarrierState()));                //<U2  CarrierState>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentCarrierType()));           //<A64 CarrierType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentProductEmpty()));             //<U2 ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentLot()));                   //<A32 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentTrayType()));                 //<U2 TrayType>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 407 RPTID = 4 PortModeChanged
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID407(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 407));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 4));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.IsInPort ? "1" : "2"));                 //<U2 PoritInOutType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.PortAccessMode));                           //<U2 PortAccessMode>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCarrierID()));             //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetPortCarrierState()));                //<U2  CarrierState>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentCarrierType()));           //<A64 CarrierType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentProductEmpty()));             //<U2 ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentLot()));                   //<A32 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentTrayType()));                 //<U2 TrayType>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 408 RPTID = 13 NotchingModeChanged
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Obsolete("비사용 메시지")]
        protected virtual DataItem MakeDataItem_S6F11_CEID408(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 408));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 13));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                          //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                      //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, (int)Port.GetNotchingMode()));             //<U2 NotchingMode>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 601 RPTID = 7 CarrierIDRead
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID601(Dictionary<string, object> args)
        {
            #region Collect Data
            CarrierItem CItem = null;
            if (args.ContainsKey("CARRIERITEM"))
            {
                CItem = (CarrierItem)args["CARRIERITEM"];
            }
            if (CItem == null)
            {
                return null;
            }
            short ValidationCheck = 0;
            if (CItem.LastReadResult != eIDReadStatus.SUCCESS)
            {
                ValidationCheck = 0; //CIM 검수시 0으로 보고
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 601));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 7));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                          //L[17]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.CarrierLocation));                //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.LastReadResult));                    //<U2  IDReadStatus>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.ProductEmpty));                      //<U2  ProductEmpty>
            LogManager.WriteConsoleLog(eLogLevel.Info, "MakeDataItem_S6F11 CarrierIDRead ProductEmpty {0}", CItem.ProductEmpty);
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.Polarity));                          //<U2  Polarity>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.WinderDirection));                   //<U2  WinderDirection>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, CItem.ProductQuantity));                   //<U4  ProductQuantity>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.InnerTrayType));                     //<U2  InnerTrayType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, ValidationCheck));                         //<U2  ValidationCheck>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.TrayType));                          //<U2  TrayType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.UncoatedPart));                      //<U2  UnCoatedPart>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.First_Lot));                      //<A64 FirstLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Second_Lot));                     //<A64 SecondLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Third_Lot));                      //<A64 ThirdLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Fourth_Lot));                     //<A64 FourthLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Fifth_Lot));                      //<A64 FifthLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Sixth_Lot));                      //<A64 SixthLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.HSMSPalletSize));                 //<A64 PalletSize>
            LogManager.WriteConsoleLog(eLogLevel.Info, "MakeDataItem_S6F11 CarrierIDRead  PalletSize {0}", CItem.HSMSPalletSize);
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.CarrierHeight));                     //<U2 CarrierHeight> //230324 RGJ MCS 사양 추가 
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 604 RPTID = 7 IDReadError
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID604(Dictionary<string, object> args)
        {
            #region Collect Data
            CarrierItem CItem = null;
            if (args.ContainsKey("CARRIERITEM"))
            {
                CItem = (CarrierItem)args["CARRIERITEM"];
            }
            if (CItem == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 604));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 7));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                          //L[17]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.CarrierLocation));                //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, 1));                                       //<U2  IDReadStatus>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.ProductEmpty));                      //<U2  ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.Polarity));                          //<U2  Polarity>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.WinderDirection));                   //<U2  WinderDirection>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, CItem.ProductQuantity));                   //<U4  ProductQuantity>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.InnerTrayType));                     //<U2  InnerTrayType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, 0));                                       //<U2  ValidationCheck>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.TrayType));                          //<U2  TrayType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.UncoatedPart));                      //<U2  UnCoatedPart>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.First_Lot));                      //<A64 FirstLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Second_Lot));                     //<A64 SecondLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Third_Lot));                      //<A64 ThirdLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Fourth_Lot));                     //<A64 FourthLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Fifth_Lot));                      //<A64 FifthLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Sixth_Lot));                      //<A64 SixthLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.HSMSPalletSize));                 //<A64 PalletSize>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.CarrierHeight));                     //<U2 CarrierHeight> //230324 RGJ MCS 사양 추가 
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 605 RPTID = 10 OperatorInitiatedAction
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID605(Dictionary<string, object> args)
        {
            #region Collect Data
            String CommandType;
            McsJob JobData = null;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }
            if (JobData == null)
            {
                return null;
            }

            CommandType = args.ContainsKey("COMMANDTYPE") ? (string)args["COMMANDTYPE"] : JobData.CommandType; //[230503 CIM 검수] Command Type 보고시 지정할수도 있게함

            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 605));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 10));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                          //L[6]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CommandID));                      //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CommandType));                    //<A20 CommandType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.Source));                         //<A64 Source>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobData.DestZoneName));                    //<A64 Dest>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, JobData.ScheduledPriority));                 //<U2 Priority>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 606 RPTID = 7 FireEmergencyAlarm
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID606(Dictionary<string, object> args)
        {
            #region Collect Data
            CarrierItem CItem = null;
            if (args.ContainsKey("CARRIERITEM"))
            {
                CItem = (CarrierItem)args["CARRIERITEM"];
            }
            if (CItem == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 606));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 7));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                          //L[17]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.CarrierID));                      //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.CarrierLocation));                //<A64 CarrierLoc>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.LastReadResult));                    //<U2  IDReadStatus>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.ProductEmpty));                      //<U2  ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.Polarity));                          //<U2  Polarity>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.WinderDirection));                   //<U2  WinderDirection>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, CItem.ProductQuantity));                   //<U4  ProductQuantity>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.InnerTrayType));                     //<U2  InnerTrayType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, 0));                                       //<U2  ValidationCheck>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.TrayType));                          //<U2  TrayType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.UncoatedPart));                      //<U2  UnCoatedPart>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.First_Lot));                      //<A64 FirstLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Second_Lot));                     //<A64 SecondLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Third_Lot));                      //<A64 ThirdLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Fourth_Lot));                     //<A64 FourthLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Fifth_Lot));                      //<A64 FifthLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.Sixth_Lot));                      //<A64 SixthLot>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.HSMSPalletSize));                 //<A64 PalletSize>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.CarrierHeight));                     //<U2 CarrierHeight> //230324 RGJ MCS 사양 추가 
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 607 RPTID = 4 CarrierLoadRequest
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Obsolete("비사용 메시지")]
        protected virtual DataItem MakeDataItem_S6F11_CEID607(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 607));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 4));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.IsInPort ? "1" : "2"));                 //<U2 PoritInOutType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.PortAccessMode));                           //<U2 PortAccessMode>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCarrierID()));             //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetPortCarrierState()));                //<U2  CarrierState>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentCarrierType()));           //<A64 CarrierType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentProductEmpty()));             //<U2 ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentLot()));                   //<A32 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentTrayType()));                 //<U2 TrayType>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 608 RPTID = 4 CarrierUnloadRequest
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Obsolete("비사용 메시지")]
        protected virtual DataItem MakeDataItem_S6F11_CEID608(Dictionary<string, object> args)
        {
            #region Collect Data
            CV_BaseModule Port = null;
            if (args.ContainsKey("PORT"))
            {
                Port = (CV_BaseModule)args["PORT"];
            }
            if (Port == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 608));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                       //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                         //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 4));                                        //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                         //L[9]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.ModuleName));                        //<A64 PortID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.IsInPort ? "1" : "2"));                 //<U2 PoritInOutType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.PortAccessMode));                           //<U2 PortAccessMode>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCarrierID()));             //<A64 CarrierID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetPortCarrierState()));                //<U2  CarrierState>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentCarrierType()));           //<A64 CarrierType>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentProductEmpty()));             //<U2 ProductEmpty>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Port.GetCurrentLot()));                   //<A32 LotID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Port.GetCurrentTrayType()));                 //<U2 TrayType>
            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 701 RPTID = 5 CraneActive
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID701(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob Job = null;

            if (args.ContainsKey("JOBDATA"))
            {
                Job = (McsJob)args["JOBDATA"];
            }
            if (Job == null)
            {
                return null;
            }

            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                            //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                           //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 701));                                         //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                              //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                                //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 5));                                               //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                                //L[3]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Job.CommandID));                              //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Job.AssignRMName));                               //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Job.AssignedRM.GetUnitServiceState()));          //<U2 CraneState>

            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 702 RPTID = 5 CraneIdle
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID702(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob Job = null;

            if (args.ContainsKey("JOBDATA"))
            {
                Job = (McsJob)args["JOBDATA"];
            }
            if (Job == null)
            {
                return null;
            }

            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                          //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                         //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 702));                                       //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                            //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                              //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 5));                                             //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                              //L[3]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Job.CommandID));                            //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Job.AssignRMName));                              //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Job.AssignedRM.GetUnitServiceState()));                //<U2 CraneState>

            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 703 RPTID = 5 CraneStateChanged
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID703(Dictionary<string, object> args)
        {
            #region Collect Data
            McsJob JobData = null;
            RMModuleBase Crane;
            string CommandID = string.Empty;
            if (args.ContainsKey("JOBDATA"))
            {
                JobData = (McsJob)args["JOBDATA"];
            }

            if (JobData != null)
            {
                CommandID = JobData.CommandID;
                Crane = JobData.AssignedRM;
                if (Crane == null)
                {
                    return null;
                }
            }
            else //JobData 가 없을경우 Crane 상태만 보고
            {
                if (args.ContainsKey("CRANE"))
                {
                    Crane = (RMModuleBase)args["CRANE"];
                }
                else
                {
                    return null;
                }
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                        //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                       //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 703));                                     //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                          //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                            //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 5));                                           //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                            //L[3]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CommandID));                         //<A64 CommandID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, Crane.ModuleName));                          //<A64 StockerCraneID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U2, Crane.GetUnitServiceState()));                  //<U2 CraneState>

            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }


        /// <summary>
        /// S6F11 CEID = 801 RPTID = 6 UnitAlarmCleared
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Obsolete("비사용 메시지")]
        protected virtual DataItem MakeDataItem_S6F11_CEID801(Dictionary<string, object> args)
        {
            #region Collect Data
            AlarmData aData = null;
            if (args.ContainsKey("ALARM"))
            {
                aData = (AlarmData)args["ALARM"];
            }
            if (aData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                        //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                       //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 801));                                     //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                          //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                            //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 6));                                           //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                            //L[3]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.ModuleName));                         //<A64 StockerUnitID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, aData.iAlarmID));                           //<U4 AlarmID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.AlarmName));                             //<A80 AlarmText>

            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 802 RPTID = 6 UnitAlarmSet
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [Obsolete("비사용 메시지")]
        protected virtual DataItem MakeDataItem_S6F11_CEID802(Dictionary<string, object> args)
        {
            #region Collect Data
            AlarmData aData = null;
            if (args.ContainsKey("ALARM"))
            {
                aData = (AlarmData)args["ALARM"];
            }
            if (aData == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                        //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                       //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 802));                                     //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                          //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                            //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 6));                                           //<U2 RPTID>
            DataItem diCommandList = new DataItem(ItemFormatCode.List);                                            //L[3]
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.ModuleName));                         //<A64 StockerUnitID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.U4, aData.iAlarmID));                           //<U4 AlarmID>
            diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, aData.AlarmName));                             //<A80 AlarmText>

            diRPIDData.AddChildItem(diCommandList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 901 RPTID = 11 ShelfInService
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID901(Dictionary<string, object> args)
        {
            #region Collect Data
            ShelfItem sItem = null;
            if (args.ContainsKey("SHELFITEM"))
            {
                sItem = (ShelfItem)args["SHELFITEM"];
            }
            if (sItem == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 901));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                    //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                      //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 11));                                     //<U2 RPTID>
            DataItem diShelfDataList = new DataItem(ItemFormatCode.List);                                       //L[3]
            diShelfDataList.AddChildItem(new DataItem(ItemFormatCode.ASCII, sItem.GetTagName()));                  //<A64 ShelfName>
            diShelfDataList.AddChildItem(new DataItem(ItemFormatCode.U2, sItem.ShelfState));                       //<U2 ShelfState>
            diShelfDataList.AddChildItem(new DataItem(ItemFormatCode.U2, sItem.ShelfHSMSStatus));                      //<U2 ShelfStatus>

            diRPIDData.AddChildItem(diShelfDataList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        /// <summary>
        /// S6F11 CEID = 902 RPTID = 11 ShelfOutOfService
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual DataItem MakeDataItem_S6F11_CEID902(Dictionary<string, object> args)
        {
            #region Collect Data
            ShelfItem sItem = null;
            if (args.ContainsKey("SHELFITEM"))
            {
                sItem = (ShelfItem)args["SHELFITEM"];
            }
            if (sItem == null)
            {
                return null;
            }
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                     //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                    //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 902));                                  //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                    //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                      //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 11));                                     //<U2 RPTID>
            DataItem diShelfDataList = new DataItem(ItemFormatCode.List);                                       //L[3]
            diShelfDataList.AddChildItem(new DataItem(ItemFormatCode.ASCII, sItem.GetTagName()));                  //<A64 ShelfName>
            diShelfDataList.AddChildItem(new DataItem(ItemFormatCode.U2, sItem.ShelfState));                       //<U2 ShelfState>
            diShelfDataList.AddChildItem(new DataItem(ItemFormatCode.U2, sItem.ShelfHSMSStatus));                   //<U2 ShelfStatus>

            diRPIDData.AddChildItem(diShelfDataList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }

        protected virtual DataItem MakeDataItem_S6F11_CEID903(Dictionary<string, object> args)
        {
            #region Collect Data
            //ShelfItem sItem = null;
            string ZoneName;
            int AvailableCapa = 0;

            //if (args.ContainsKey("SHELFITEM"))
            //{
            //    sItem = (ShelfItem)args["SHELFITEM"];
            //}
            //if (sItem == null)
            //{
            //    return null;
            //}

            if (args.ContainsKey("ZONENAME"))
            {
                ZoneName = (string)args["ZONENAME"];
            }
            else
            {
                return null;
            }

            AvailableCapa = GlobalData.Current.ShelfMgr.CalcShelfZoneCapa(ZoneName); //쉘프부터 검색해본다.
            #endregion

            #region Make Report Data
            DataItem diCEID = new DataItem(ItemFormatCode.List);                                        //L[3]
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U4, 0));                                        //<U4 DATAID>                                     
            diCEID.AddChildItem(new DataItem(ItemFormatCode.U2, 903));                                      //<U2 CEID>
            DataItem diRPIDList = new DataItem(ItemFormatCode.List);                                        //L[1]
            DataItem diRPIDData = new DataItem(ItemFormatCode.List);                                            //L[2]
            diRPIDData.AddChildItem(new DataItem(ItemFormatCode.U2, 14));                                       //<U2 RPTID>
            DataItem diShelfDataList = new DataItem(ItemFormatCode.List);                                           //L[3]
            diShelfDataList.AddChildItem(new DataItem(ItemFormatCode.ASCII, ZoneName));                                 //<A64 ShelfName>
            diShelfDataList.AddChildItem(new DataItem(ItemFormatCode.U2, AvailableCapa));                               //<U2 ShelfState>
            DataItem diDisabledLocationList = new DataItem(ItemFormatCode.List);                                        //L[1]
            //DataItem diDisabledLocations = new DataItem(ItemFormatCode.List);                                             //L[2]

            var DisableShelf = GlobalData.Current.ShelfMgr.AllData.Where(s => s.iZoneName == ZoneName && !s.SHELFUSE && !s.DeadZone);
            if (DisableShelf.Count() == 0)
            {
                DataItem diDisabledLocations = new DataItem(ItemFormatCode.List);
                diDisabledLocationList.AddChildItem(diDisabledLocations);
            }
            else
            {
                foreach (ShelfItem sitem in DisableShelf)
                {
                    DataItem diDisabledLocations = new DataItem(ItemFormatCode.List);
                    diDisabledLocations.AddChildItem(new DataItem(ItemFormatCode.ASCII, sitem.iLocName));                       //<A64 ShelfLocation>
                    diDisabledLocations.AddChildItem(new DataItem(ItemFormatCode.ASCII, sitem.CarrierID));                      //<A64 CarrierID>
                    diDisabledLocationList.AddChildItem(diDisabledLocations);
                }
            }

            diShelfDataList.AddChildItem(diDisabledLocationList);
            diRPIDData.AddChildItem(diShelfDataList);
            diRPIDList.AddChildItem(diRPIDData);
            diCEID.AddChildItem(diRPIDList);
            #endregion;

            return diCEID;
        }
        #endregion

        #region S10F*
        // S10F3(Terminal Display)
        public virtual DataItem DoAction_S10F3(DataMessage primaryMsg)
        {
            #region Variable 
            Byte ONLAck = 0;
            #endregion

            #region Data Pasing 
            if (primaryMsg.Body != null)
            {
                string TID = (string)primaryMsg.Body.ChildItems[0].Value.ToString();       //230215 TID Binary에서 ASCII로 변경. //SKOH2 현장은 Binary 로 내려옮
                //string TCode = (string)primaryMsg.Body.ChildItems[1].Value; //포맷만 구현. 사양은 아직 미정의됨
                string TCode = "";
                string TMessage = (string)primaryMsg.Body.ChildItems[1].Value;

                //221226 HHJ SCS 개선     //Terminal Message LayOut 화면에서 삭제
                //MainWindow.GetMainWindowInstance().AddTerminalMessage(TMessage,true);
                GlobalData.Current.TerminalMessageChangedOccur(primaryMsg.Header.MakeTime, eHostMessageDirection.eHostToEq, TMessage);

                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    string tempmsg = string.Format("{0}/{1}/{2}", TID, TCode, TMessage);
                    ClientSetProcedure(primaryMsg.Header.MakeTime, eHostMessageDirection.eHostToEq, tempmsg);
                }
            }
            #endregion

            #region Data Check 
            #endregion

            #region Make Reply Data 
            #endregion

            return new DataItem(ItemFormatCode.Bin, ONLAck);
        }
        #endregion

        #region MakeDataItem For HSMS Message Sending

        /// <summary>
        /// S1F1 Are You There
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual DataItem MakeDataItem_S1F1(Dictionary<string, object> args)
        {
            //string sMDLN = "SCS"; ;
            //string sSoftrev = GlobalData.SCSProgramVersion;
            //DataItem diAliveCheck = null;
            //diAliveCheck = new DataItem(ItemFormatCode.List);
            //diAliveCheck.AddChildItem(new DataItem(ItemFormatCode.ASCII, sMDLN));        //ALARM ID
            //diAliveCheck.AddChildItem(new DataItem(ItemFormatCode.ASCII, sSoftrev));    //ALARM NAME
            //return diAliveCheck;
            return null;
        }

        /// <summary>
        /// S1F13 Establish Communication Request
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual DataItem MakeDataItem_S1F13(Dictionary<string, object> args)
        {
            string sMDLN = "SCS"; ;
            string sSoftrev = GlobalData.SCSProgramVersion;
            DataItem diECR = null;
            diECR = new DataItem(ItemFormatCode.List);
            diECR.AddChildItem(new DataItem(ItemFormatCode.ASCII, sMDLN));
            diECR.AddChildItem(new DataItem(ItemFormatCode.ASCII, sSoftrev));
            return diECR;
        }

        /// <summary>
        /// S2F17 Date and Time Request
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual DataItem MakeDataItem_S2F17(Dictionary<string, object> args)
        {
            //Header Only
            return null;
        }


        /// <summary>
        /// S5F1 Alarm Report
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual DataItem MakeDataItem_S5F1(Dictionary<string, object> args)
        {
            DataItem diAlarmInfo = null;

            string moduleID = (string)args["MODULEID"];
            AlarmData alarm = (AlarmData)args["ALARM"];
            bool alarmSet = (bool)args["SETALARM"];
            //string stringAlign = String.Format("{0,-80}", alarm.AlarmName);
            string alarmNameBuffer = alarm.AlarmName.Length > 80 ? alarm.AlarmName.Substring(0, 80) : alarm.AlarmName; //Alarm Name 길이 확인

            diAlarmInfo = new DataItem(ItemFormatCode.List);
            diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, alarmSet ? 128 : 0));   //CODE SET :128       Clear :0
            diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.U4, alarm.iAlarmID));        //ALARM ID
            diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, alarmNameBuffer));    //ALARM NAME
            diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, alarm.ModuleName));   //<A64 StockedUnitID>   UnitID 로 올림
            return diAlarmInfo;
        }

        //2024.06.18 lim, Alarm 보고 추가
        /// <summary>
        /// S5F1 Alarm Report
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual DataItem MakeDataItem_S5F101(Dictionary<string, object> args)
        {
            DataItem diAlarmInfo = null;


            string moduleID = (string)args["MODULEID"];
            AlarmData alarm = (AlarmData)args["ALARM"];
            bool alarmSet = (bool)args["SETALARM"];

            string CarrierID = string.IsNullOrEmpty(alarm.CarrierID) ? "" : alarm.CarrierID;
            string JobID = (string)args["JOBID"];
            string CarrierLoc = (string)args["CARRIERLOC"];
			
			//2024.10.07 lim, EQPID로 보고 하면 안됨 by 정연동 
            string UnitID = (alarm.ModuleName == GlobalData.Current.EQPID) ? "" : alarm.ModuleName;

            //string stringAlign = String.Format("{0,-80}", alarm.AlarmName);
            string alarmNameBuffer = alarm.AlarmName.Length > 80 ? alarm.AlarmName.Substring(0, 80) : alarm.AlarmName; //Alarm Name 길이 확인

            diAlarmInfo = new DataItem(ItemFormatCode.List);
            diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, alarmSet ? 128 : 0));   //CODE SET :128       Clear :0
            diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.U4, alarm.iAlarmID));        //ALARM ID
            diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, alarmNameBuffer));    //ALARM NAME
            diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, UnitID));   //<A64 StockedUnitID>   UnitID 로 올림
            DataItem diCommandData = new DataItem(ItemFormatCode.List);                       //<L[n] 1

            //if (!string.IsNullOrEmpty(CarrierID) || !string.IsNullOrEmpty(JobID))
            {
                DataItem diCommandList = new DataItem(ItemFormatCode.List);                     //<L[3]
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CarrierID));      //CARRIERID
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, JobID));          //COMMANDID
                diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CarrierLoc));     //LOCATION
                diCommandData.AddChildItem(diCommandList);
            }
            diAlarmInfo.AddChildItem(diCommandData);
            return diAlarmInfo;
        }

        /// <summary>
        /// S6F11 Event Report Send
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual DataItem MakeDataItem_S6F11(Dictionary<string, object> args)
        {
            int ceid = int.Parse(args[MessageDictionaryNames.CEID].ToString());
            DataItem dataItem = null;
            switch (ceid)
            {
                #region RPTID = 1
                case 1:  // Offline
                    dataItem = MakeDataItem_S6F11_CEID1(args);
                    break;
                case 3:  //OnlineRemote
                    dataItem = MakeDataItem_S6F11_CEID3(args);
                    break;


                case 101: //AlarmCleared
                    dataItem = MakeDataItem_S6F11_CEID101(args);
                    break;
                case 102: //AlarmSet
                    dataItem = MakeDataItem_S6F11_CEID102(args);
                    break;


                case 103: //SCAutoCompleted
                    dataItem = MakeDataItem_S6F11_CEID103(args);
                    break;
                case 105: //SCPauseCompleted
                    dataItem = MakeDataItem_S6F11_CEID105(args);
                    break;
                case 106: //SCPaused
                    dataItem = MakeDataItem_S6F11_CEID106(args);
                    break;
                case 107: //SCPauseInitiated
                    dataItem = MakeDataItem_S6F11_CEID107(args);
                    break;
                #endregion

                #region RPTID 2
                case 201: //TransferAbortCompleted
                    dataItem = MakeDataItem_S6F11_CEID201(args);
                    break;
                case 202: //TransferAbortFailed
                    dataItem = MakeDataItem_S6F11_CEID202(args);
                    break;
                case 203: //TransferAbortInitiated
                    dataItem = MakeDataItem_S6F11_CEID203(args);
                    break;
                case 204: //TransferCancelCompleted
                    dataItem = MakeDataItem_S6F11_CEID204(args);
                    break;
                case 205: //TransferCancelFailed
                    dataItem = MakeDataItem_S6F11_CEID205(args);
                    break;
                case 206: //TransferCancelInitiated
                    dataItem = MakeDataItem_S6F11_CEID206(args);
                    break;
                case 207: //TransferCompleted
                    dataItem = MakeDataItem_S6F11_CEID207(args);
                    break;
                case 208: //TransferInitiated
                    dataItem = MakeDataItem_S6F11_CEID208(args);
                    break;
                case 209: //TransferPaused
                    dataItem = MakeDataItem_S6F11_CEID209(args);
                    break;
                case 210: //TransferResumed
                    dataItem = MakeDataItem_S6F11_CEID210(args);
                    break;
                #endregion

                #region RPTID 3
                case 301: //CarrierInstallCompleted
                    dataItem = MakeDataItem_S6F11_CEID301(args);
                    break;
                case 302: //CarrierRemoveCompleted
                    dataItem = MakeDataItem_S6F11_CEID302(args);
                    break;
                case 303: //CarrierRemoved
                    dataItem = MakeDataItem_S6F11_CEID303(args);
                    break;
                case 304: //CarrierResumed
                    dataItem = MakeDataItem_S6F11_CEID304(args);
                    break;
                case 305: //CarrierStored
                    dataItem = MakeDataItem_S6F11_CEID305(args);
                    break;
                case 306: //CarrierStoredAlt
                    dataItem = MakeDataItem_S6F11_CEID306(args);
                    break;
                case 307: //CarrierTransferring
                    dataItem = MakeDataItem_S6F11_CEID307(args);
                    break;
                case 308: //CarrierWaitIn
                    dataItem = MakeDataItem_S6F11_CEID308(args);
                    break;
                case 309: //CarrierWaitOut
                    dataItem = MakeDataItem_S6F11_CEID309(args);
                    break;
                case 310: //ZoneCapacityChange
                    dataItem = MakeDataItem_S6F11_CEID310(args);
                    break;
                case 311: //CarrierLocationChanged
                    dataItem = MakeDataItem_S6F11_CEID311(args);
                    break;
                case 312: //CarrierGeneratorRequest
                    dataItem = MakeDataItem_S6F11_CEID312(args);
                    break;
                case 313: //CarrierInfoRequest
                    dataItem = MakeDataItem_S6F11_CEID313(args);
                    break;
                #endregion

                #region RPTID 4
                case 401: //PortInService
                    dataItem = MakeDataItem_S6F11_CEID401(args);
                    break;
                case 402: //PortOutOfService
                    dataItem = MakeDataItem_S6F11_CEID402(args);
                    break;
                case 403: //PortTransferBlocked
                    dataItem = MakeDataItem_S6F11_CEID403(args);
                    break;
                case 404: //PortReadyToLoad
                    dataItem = MakeDataItem_S6F11_CEID404(args);
                    break;
                case 405: //PortReadyToUnload
                    dataItem = MakeDataItem_S6F11_CEID405(args);
                    break;
                case 406://PortTypeChanged
                    dataItem = MakeDataItem_S6F11_CEID406(args);
                    break;
                case 407: //PortModeChanged
                    dataItem = MakeDataItem_S6F11_CEID407(args);
                    break;
                #endregion

                #region RPTID 13
                case 408: //NotchingModeChanged
                    dataItem = MakeDataItem_S6F11_CEID408(args);
                    break;
                #endregion


                //RPTID = 7
                case 601: //CarrierIDRead
                    dataItem = MakeDataItem_S6F11_CEID601(args);
                    break;
                //RPTID = 7
                case 604: //IDReadError
                    dataItem = MakeDataItem_S6F11_CEID604(args);
                    break;
                //RPTID = 10
                case 605: //OperatorInitiatedAction
                    dataItem = MakeDataItem_S6F11_CEID605(args);
                    break;
                //RPTID = 7
                case 606: //FireEmergencyAlarm
                    dataItem = MakeDataItem_S6F11_CEID606(args);
                    break;
                //RPTID = 4
                case 607: //CarrierLoadRequest
                    dataItem = MakeDataItem_S6F11_CEID607(args);
                    break;
                //RPTID = 4
                case 608: //CarrierUnloadRequest
                    dataItem = MakeDataItem_S6F11_CEID608(args);
                    break;

                #region RPTID 5
                case 701: //Crane Active
                    dataItem = MakeDataItem_S6F11_CEID701(args);
                    break;
                case 702: //CraneIdle
                    dataItem = MakeDataItem_S6F11_CEID702(args);
                    break;
                case 703: //CraneStateChanged
                    dataItem = MakeDataItem_S6F11_CEID703(args);
                    break;
                #endregion

                #region RPTID 6
                case 801: //UnitAlarmCleared
                    dataItem = MakeDataItem_S6F11_CEID801(args);
                    break;
                case 802: //UnitAlarmSet
                    dataItem = MakeDataItem_S6F11_CEID802(args);
                    break;
                #endregion

                #region RPTID 11
                case 901: //ShelfInService
                    dataItem = MakeDataItem_S6F11_CEID901(args);
                    break;
                case 902: //ShelfOutService
                    dataItem = MakeDataItem_S6F11_CEID902(args);
                    break;
                #endregion

                #region RPTID 14
                case 903:
                    dataItem = MakeDataItem_S6F11_CEID903(args);
                    break;
                #endregion

                default:
                    break;
            }

            return dataItem;
        }

        /// <summary>
        /// S10F1 Terminal Request
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual DataItem MakeDataItem_S10F1(Dictionary<string, object> args)
        {
            DataItem diTeminalRequest;

            string TMessage = (string)args["TMESSAGE"];

            diTeminalRequest = new DataItem(ItemFormatCode.List);
            diTeminalRequest.AddChildItem(new DataItem(ItemFormatCode.Bin, 0));            //Terminal ID : 0
            diTeminalRequest.AddChildItem(new DataItem(ItemFormatCode.ASCII, TMessage));    //Terminal Text

            return diTeminalRequest;
        }
        #endregion

        private void ClientSetProcedure(DateTime dt, eHostMessageDirection direction, string msg)
        {
            ClientReqList reqBuffer = new ClientReqList
            {
                EQPID = GlobalData.Current.EQPID,
                CMDType = dt.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Target = "TERMINALMSG",
                TargetID = direction.ToString(),
                TargetValue = msg,
                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Requester = eServerClientType.Server,
                JobID = string.Empty,
            };

            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);
        }
    }
}
