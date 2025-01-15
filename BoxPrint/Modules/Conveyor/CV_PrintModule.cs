using BoxPrint.DataList.MCS;
using BoxPrint.DataList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoxPrint.Log;
using BoxPrint.Modules.Print;

namespace BoxPrint.Modules.Conveyor
{
    public class CV_PrintModule : CV_BaseModule
    {
        private bool bCarrierLoctionChanged = false;

        INK_SQUID CVUnitModule = null;

        public CV_PrintModule(string mName, bool simul) : base(mName, simul)
        {
            CVModuleType = eCVType.Print;
            PortType = ePortType.BP; //Buffer Port
            _PortAccessMode = ePortAceessMode.AUTO;
        }
        protected override void CVMainRun()
        {

            GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne(); //모든 모듈 생성전까지 Run 대기
            GlobalData.Current.MRE_FirstPLCReadEvent.WaitOne(); //처음 PLC Read 전까지 Run 대기
            CV_ActionResult Result = null;
            NextCVCommand = eCVCommand.Initialize;
            bool bFirstInit = true;
            PC_SCSMode = this.PortInOutType; //초기화 작업시 현재 포트 모드를 써준다.
            //230302 프로그램 첫 기동 시 resume전 reconsile을 위해 한번만 체크하게 한다.
            if (!CarrierExistBySensor())
            {
                ResetCarrierData();
            }
            else
            {
                if (CarrierStorage.Instance.GetInModuleCarrierItem(ModuleName) is CarrierItem carrier)
                {
                    UpdateCarrier(carrier.CarrierID, false);
                }
            }
            SetTrackPause(!CVUSE);//사이클 돌기전에 USE 상태에 따른 TrackPause 신호를 준다.
            PC_PortEnable = CVUSE;


            while (!ThreadExitRequested)
            {
                try //-메인 루프 예외 발생시 로그 찍도록 추가.
                {
                    //CheckPrintState();

                    //DefaultSlot.SetCarrierExist(PLC_CarrierSensor);     //221014 HHJ SCS 개선     //- C/V CarrierExist 실시간 반영
                    if (!CVUSE) //비사용 포트는 여기서 스탑
                    {
                        CurrentActionDesc = "Wait Port Enable";
                        Thread.Sleep(LocalStepCycleDelay);
                        continue;
                    }

                    //if (CVUnitModule.)
                    //if (DoAbnormalCheck()) //에러체크는 항상 하도록 변경.
                    //{
                    //    NextCVCommand = eCVCommand.ErrorHandling;
                    //}

                    if (AutoManualState != eCVAutoManualState.AutoRun && NextCVCommand != eCVCommand.ErrorHandling) //에러핸들링은 바로 들어간다.
                    {
                        //220803 조숭진 최초 이니셜 시 outofserv
                        //hice보고 s
                        if (GlobalData.Current.MainBooth != null
                            && GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Remote
                            && bFirstInit == true)
                        {
                            bFirstInit = false;
                            //S6F11 402
                            //GlobalData.Current.HSMS.SendS6F11(402, "PORT", this);
                        }
                        //220803 조숭진 최초 이니셜 시 outofservice보고 e
                        //포트 타입 변경 요청이 있으면 변경한다.
                        if (CheckPLCPortTypeChangeRequest())
                        {
                            ePortInOutType ReqType = PLC_PortType;
                            ChangePortInOutType(ReqType);
                        }
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        Thread.Sleep(LocalStepCycleDelay);
                        continue;
                    }
                    //241202 RGJ 포트 Main Run 시스템 상태가 Auto 가 아니면 대기
                    if (GlobalData.Current.MainBooth.SCState != eSCState.AUTO)
                    {
                        if (NextCVCommand == eCVCommand.Initialize || NextCVCommand == eCVCommand.ErrorHandling)
                        {
                            ;//시스템 상태 오토 아니어도 들어가야 함.
                        }
                        else
                        {
                            Thread.Sleep(LocalStepCycleDelay);
                            continue;
                        }
                    }
                    switch (NextCVCommand)
                    {
                        case eCVCommand.Initialize:
                            Result = InitializeAction();
                            break;
                        case eCVCommand.AutoAction:
                            Result = CVAutoAction();
                            break;
                        case eCVCommand.ReceiveCarrier:
                            break;
                        case eCVCommand.ErrorHandling:
                            Result = ErrorHandlingAction();
                            break;
                        default:
                            break;
                    }

                    if (Result.actionResult != eCV_ActionResult.Complete)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Error, "Module:{0} NextCVCommand:{1} ErrorMessage:{2}", ModuleName, NextCVCommand, Result.message);
                        NextCVCommand = eCVCommand.Initialize;
                        //NextCVCommand = eCVCommand.ErrorHandling; //비정상 완료면 에러 핸들링으로 보낸다.
                    }
                    //Result GUI 보고
                    LastActionResult = Result?.message;
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }

        }

        protected override CV_ActionResult InitializeAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module InitializeAction Start", this.ModuleName);
            CurrentActionDesc = "초기화 동작중입니다.";
            LocalActionStep = 0;
            PC_SCSMode = this.PortInOutType; //초기화 작업시 현재 포트 모드를 써준다.
            PC_TransferPossible = false; //초기화시 반송 명령 해제
            //240102 rhj 포트 타입 변경 요청이 있으면 변경한다.

            if (CVUnitModule.TCPSocketConnect() == false)
            {

                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Print Disconnect");
            }



            NextCVCommand = eCVCommand.AutoAction;
            //220803 조숭진 이니셜라이즈 완료 후 inservice 보고
            //GlobalData.Current.HSMS.SendS6F11(401, "PORT", this);

            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "InitializeAction 완료.");
        }
        protected override CV_ActionResult CVAutoAction()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module CVAutoAction Start", this.ModuleName);
            CV_ActionResult errorResult = new CV_ActionResult(ModuleName, eCV_ActionResult.ErrorOccured, "동작중 에러 발생하였습니다.");
            CurrentActionDesc = "오토 동작중입니다.";
            try
            {
                while (true)
                {
                    if (CVUnitModule.CheckConnection() == eUnitConnection.Disconnect)
                    {
                        NextCVCommand = eCVCommand.Initialize;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "Print 통신 이상으로 CVAutoAction 중단.");
                    }
                    if (PortInOutTypeChanged)
                    {
                        PortInOutTypeChanged = false;
                        NextCVCommand = eCVCommand.Initialize;
                        Thread.Sleep(LocalStepCycleDelay);
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "PortInOutType 변경으로 CVAutoAction 중단.");
                    }
                    //if (DoAbnormalCheck())
                    //{
                    //    CurrentActionDesc = "에러 발생.에러 해제를 기다립니다.";
                    //    NextCVCommand = eCVCommand.ErrorHandling;
                    //    return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "상태 이상 으로 CVAutoAction 중단.");
                    //}
                    if (AutoManualState != eCVAutoManualState.AutoRun)
                    {
                        CurrentActionDesc = "Auto 모드를 기다립니다.";
                        Thread.Sleep(LocalStepCycleDelay);
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "메뉴얼 모드 변경으로 CVAutoAction 중단.");
                    }
                    if (CheckPLCPortTypeChangeRequest() && !CarrierExistBySensor())
                    {
                        NextCVCommand = eCVCommand.Initialize;
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "PLCPortTypeChangeRequest 요청으로 ReceiveCarrierAction 중단");
                    }

                    //Print job change 체크

                    


                    Thread.Sleep(LocalStepCycleDelay);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "예외 발생으로 인한 CVAutoAction 중단.");
            }
        }

        #region Inkjet Print

        #region send Command
        public bool EnablPrintCompleteAcknowledgementSend()
        {
            bool result = CVUnitModule.SendMessage("A");
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Enabl Print Complete Acknowledgement Send : {result}");
            return result;
        }
        public bool ReadAutoDataStatusSend()
        {
            bool result = CVUnitModule.SendMessage("C");
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Read Auto Data Status Send : {result}");

            return result;
        }
        public bool WriteAutoDataRecordSend(params object[] args)
        {
            int cnt = args.Count();

            int floor = (int)args[0]; 
            string boxType = (string)args[1]; 
            int inkejctNo = (int)args[2]; 
            int currentBoxCount = (int)args[3];

            StringBuilder sb = new StringBuilder();
            sb.Append("D");
            sb.Append(floor);
            sb.Append(boxType);
            sb.Append(inkejctNo);
            sb.Append($"{currentBoxCount}".PadLeft(7, '0'));

            bool result = CVUnitModule.SendMessage(sb.ToString());
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Write Auto Data Record Send(floor:{floor}, boxType:{boxType}, inkejctNo:{inkejctNo}), currentBoxCount:{currentBoxCount} : {result}");

            return result;
        }
        public bool ClearAutoDataQueueSend()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("D");
            sb.Append("_CLEAR_ADQ_");

            bool result = CVUnitModule.SendMessage(sb.ToString());
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Clear Auto Data Queue Send : {result}");

            return result;
        }
        public bool ReadInkLevelSend()
        {
            bool result = CVUnitModule.SendMessage("o");
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Read Ink Level Send : {result}");

            return result;
        }
        public bool GetAutoDataStringSend()
        {
            bool result = CVUnitModule.SendMessage("GET_AUTO_DATA_STRING");
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Get Auto Data String Send : {result}");

            return result;
        }


        public bool WriteWorkingFileName(string fileName)
        {
            string cmd = $"N{fileName}";
            bool result = CVUnitModule.SendMessage(cmd);
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Write Working File Name Send({fileName}) : {result}");
            return result;
        }
        public bool SetBuildMessage()
        {
            bool result = CVUnitModule.SendMessage("B");
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send : {result}");
            return result;
        }

        public bool SetCreatePrintBitMap()
        {
            bool result = CVUnitModule.SendMessage("P");
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send : {result}");
            return result;
        }


        #endregion


        public bool UnitModuleCommand(string cmd, params object[] args)
        {

            //INK_SQUID print = CVUnitModule as INK_SQUID;

            if (CVUnitModule == null) 
                return false;

            ePrintCommand pc = (ePrintCommand)Enum.Parse(typeof(ePrintCommand), cmd);

            switch(pc)
            {
                case ePrintCommand.EnablePrintComplete:
                    EnablPrintCompleteAcknowledgementSend();
                    break;
                case ePrintCommand.ReadAutoDataState:
                    ReadAutoDataStatusSend();
                    break;
                case ePrintCommand.WriteAutoDataRedord:
                    WriteAutoDataRecordSend(args);
                    break;
                case ePrintCommand.GetAutoDataString:
                    GetAutoDataStringSend();
                    break;
                case ePrintCommand.ClearAutoDataQueue:
                    ClearAutoDataQueueSend();
                    break;
                case ePrintCommand.ReadInkLevel:
                    ReadInkLevelSend();
                    break;
                case ePrintCommand.Build:
                    break;
                case ePrintCommand.GetPrintDirection:
                    break;
                case ePrintCommand.SetPrintDirection:
                    break;
                case ePrintCommand.GetPrintDelay:
                    break;
                case ePrintCommand.SetPrintDelay:
                    break;
                case ePrintCommand.GetManualSpeed:
                    break;
                case ePrintCommand.SetManualSpeed:
                    break;
                case ePrintCommand.ReadSystemDateTime:
                    break;
                case ePrintCommand.WriteSystemDateTime:
                    break;
                default:
                    return false;
            }

            return true;
        }

        #endregion Inkjet Print
    }
}
