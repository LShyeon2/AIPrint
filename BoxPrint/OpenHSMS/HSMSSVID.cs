using OSG.Com.HSMS.Common;
using BoxPrint.Alarm;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;

namespace BoxPrint.OpenHSMS
{
    public class HSMSSVID
    {
        public static DataItem SVID24_ControlState()
        {
            return new DataItem(ItemFormatCode.U2, GlobalData.Current.MainBooth.CurrentOnlineState);
        }
        public static DataItem SVID31_EnHancedCarriers()
        {
            DataItem diEnHancedCarriers = new DataItem(ItemFormatCode.List);
            foreach (var item in GlobalData.Current.ShelfMgr.AllData)
            {
                if (!item.CheckCarrierExist())
                {
                    continue;
                }
                //string.Format("{0:yyyyMMddHHmmssff}", item.InstallTime);

                System.DateTime DT = DateTime.Parse(item.InstallTime);
                string strInstallTime = DT.ToString("yyyyMMddHHmmssff");

                DataItem diEnhancedCarrierInfo = new DataItem(ItemFormatCode.List);
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.CarrierID));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.TagName));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.ZONE));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, strInstallTime));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, item.CarrierState));

                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, item.GetCarriereWinderDirection())); //20230308 공통사양으로 바뀜.
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, item.GetCarrierHeight()));

                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.GetCarrierHSMSPalletSize())); //231002 RGJ EnHancedCarriers PalletSize 추가. 아직 미적용

                diEnHancedCarriers.AddChildItem(diEnhancedCarrierInfo);
            }
            foreach (var pitem in GlobalData.Current.PortManager.AllCVList)
            {
                if (!pitem.CarrierExistByData())
                {
                    continue;
                }
                DataList.CarrierItem CItem = pitem.InSlotCarrier;
                if (CItem != null)
                {

                    System.DateTime DT = DateTime.Parse(CItem.CarryInTime);
                    string strInstallTime = DT.ToString("yyyyMMddHHmmssff");

                    DataItem diEnhancedCarrierInfo = new DataItem(ItemFormatCode.List);
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.CarrierID));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, pitem.ModuleName));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, pitem.iZoneName));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, strInstallTime));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.CarrierState));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.WinderDirection));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, CItem.CarrierHeight));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, CItem.HSMSPalletSize)); //231002 RGJ EnHancedCarriers PalletSize 추가. 아직 미적용

                    diEnHancedCarriers.AddChildItem(diEnhancedCarrierInfo);
                }
            }

            var RModule = GlobalData.Current.mRMManager.FirstRM;
            DataList.CarrierItem RItem = GlobalData.Current.mRMManager.FirstRM.InSlotCarrier;
            if (RItem != null)
            {
                System.DateTime DT = DateTime.Parse(RItem.CarryInTime);
                string strInstallTime = DT.ToString("yyyyMMddHHmmssff");
                DataItem diEnhancedCarrierInfo = new DataItem(ItemFormatCode.List);
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, RItem.CarrierID));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, RModule.ModuleName));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, RModule.iZoneName));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, strInstallTime));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, RItem.CarrierState));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, RItem.WinderDirection));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, RItem.CarrierHeight));
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, RItem.HSMSPalletSize)); //231002 RGJ EnHancedCarriers PalletSize 추가. 아직 미적용

                diEnHancedCarriers.AddChildItem(diEnhancedCarrierInfo);
            }

            if (GlobalData.Current.SCSType == eSCSType.Dual)
            {
                var RModule2 = GlobalData.Current.mRMManager.SecondRM;
                DataList.CarrierItem RItem2 = GlobalData.Current.mRMManager.SecondRM.InSlotCarrier;
                if (RItem2 != null)
                {
                    System.DateTime DT = DateTime.Parse(RItem2.CarryInTime);        //221027 조숭진 수정
                    string strInstallTime = DT.ToString("yyyyMMddHHmmssff");
                    DataItem diEnhancedCarrierInfo = new DataItem(ItemFormatCode.List);
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, RItem2.CarrierID));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, RModule2.ModuleName));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, RModule2.iZoneName));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, strInstallTime));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, RItem2.CarrierState));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, RItem2.WinderDirection));
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, RItem2.CarrierHeight));
                   diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, RItem.HSMSPalletSize)); //231002 RGJ EnHancedCarriers PalletSize 추가. 아직 미적용

                    diEnHancedCarriers.AddChildItem(diEnhancedCarrierInfo);
                }
            }

            return diEnHancedCarriers;
        }

        public static DataItem SVID34_EnHancedPorts()
        {
            DataItem diEnHancedCarriers = new DataItem(ItemFormatCode.List);
            foreach (var citem in GlobalData.Current.PortManager.AllCVList)
            {
                //[2308017 CIM 검수] RGJ 라인 검수 해당 내용 보고해야 한다고함
                //if(citem.CheckConnectedWithManualPort() && citem.PortType != ePortType.LP) //[230503 CIM 검수] 메뉴얼라인 포트이고 LP가 아니면 포트 보고 생략  
                //{
                //    continue;
                //}
                DataItem diEnhancedCarrierInfo = new DataItem(ItemFormatCode.List);
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, citem.ModuleName));                     //PortIDi
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, citem.GetCurrentPortTransferState()));     //PortTransferStatei
                diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, citem.PortAccessMode));                     //PortAccessModeli       //230302 automanualstate -> portaccessmode로 변경
                
                if(citem.PortType == ePortType.BP)
                {
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, "3"));   //[230503 CIM 검수] BP 는 3 both 로 고정함
                }
                else
                {
                    diEnhancedCarrierInfo.AddChildItem(new DataItem(ItemFormatCode.U2, citem.IsInPort ? "1" : "2"));                //PortInOutTypei
                }
                diEnHancedCarriers.AddChildItem(diEnhancedCarrierInfo);
            }

            return diEnHancedCarriers;
        }

        //220517 조숭진 hsms 메세지 추가 s
        public static DataItem SVID35_EnHancedTransfers()
        {
            DataItem diEnHancedTransfers = new DataItem(ItemFormatCode.List);
            {
                foreach (var item in GlobalData.Current.McdList)
                {
                    if (item.JobType != "TRANSFER") //반송 작업만 리컨사일시 보고 올린다.
                    {
                        continue;
                    }
                    DataItem diTransfers = new DataItem(ItemFormatCode.List);

                    diTransfers.AddChildItem(new DataItem(ItemFormatCode.U2, item.TransferState));     //tranfserstate
                    DataItem diCommandInfos = new DataItem(ItemFormatCode.List);
                    {
                        diCommandInfos.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.CommandID));        //commandID
                        diCommandInfos.AddChildItem(new DataItem(ItemFormatCode.U2, item.Priority));            //priority                        
                    }
                    diTransfers.AddChildItem(diCommandInfos);
                    DataItem diTransferInfos = new DataItem(ItemFormatCode.List);
                    {
                        diTransferInfos.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.CarrierID));       //carrierID
                        diTransferInfos.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.CarrierLoc));      //carrierloc
                        diTransferInfos.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.DestZoneName));     //Zone Name 목적지로 보고해야함.
                    }
                    diTransfers.AddChildItem(diTransferInfos);
                    diEnHancedTransfers.AddChildItem(diTransfers);
                }

            }
            return diEnHancedTransfers;
        }

        public static DataItem SVID37_EnHancedActiveZones()
        {
            ///쉘프 및 포트 멀티 존 타입 대응할수 있도록 변경..
            Dictionary<string, ActiveZone> DicZone = new Dictionary<string, ActiveZone>();

            foreach (var item in GlobalData.Current.ShelfMgr.AllData) //쉘프 존 순회
            {
                if (item == null || item.DeadZone)
                {
                    continue;
                }
                if(!DicZone.ContainsKey(item.ZONE)) //신규 키면 계산한다.
                {
                    ActiveZone AZ = new ActiveZone(item.ZONE, eZoneType.SHELF);//신규 존이면 새로 생성
                    AZ.ZoneTotalCount = ShelfManager.Instance.CalcShelfZoneTotalCount(item.ZONE);
                    AZ.ZoneCapa = ShelfManager.Instance.CalcShelfZoneCapa(item.ZONE);
                    DicZone.Add(AZ.ZoneName,AZ);
                }   
            }
            foreach (var citem in GlobalData.Current.PortManager.AllCVList) //포트 존 순회
            {
                if (citem == null)
                {
                    continue;
                }
                if (!DicZone.ContainsKey(citem.iZoneName)) //신규 키면 계산한다.
                {
                    ActiveZone AZ = new ActiveZone(citem.iZoneName, eZoneType.PORT);//신규 존이면 새로 생성
                    AZ.ZoneTotalCount = GlobalData.Current.PortManager.CalcShelfZoneTotalCount(citem.iZoneName);
                    AZ.ZoneCapa = GlobalData.Current.PortManager.CalcPortZoneCapa(citem.iZoneName);
                    DicZone.Add(AZ.ZoneName, AZ);
                }
            }

            DataItem diEnHancedActiveZones = new DataItem(ItemFormatCode.List); //메시지 생성
            foreach (KeyValuePair<string, ActiveZone> zitem in DicZone) // 딕셔너리 값으로 보고
            {
                DataItem diEnHancedZoneData = new DataItem(ItemFormatCode.List);
                diEnHancedZoneData.AddChildItem(new DataItem(ItemFormatCode.ASCII, zitem.Value.ZoneName));                  //Zone Name
                diEnHancedZoneData.AddChildItem(new DataItem(ItemFormatCode.U2, zitem.Value.ZoneCapa));                     //Zone Capa
                diEnHancedZoneData.AddChildItem(new DataItem(ItemFormatCode.U2, zitem.Value.ZoneTotalCount));               //Zone Size
                diEnHancedZoneData.AddChildItem(new DataItem(ItemFormatCode.U2, zitem.Value.ZoneType));                     //Zone Type
                diEnHancedActiveZones.AddChildItem(diEnHancedZoneData);
            }
            DicZone.Clear();
            return diEnHancedActiveZones;
        }

        public static DataItem SVID87_EnHancedShelves()
        {
            DataItem diEnHancedShelves = new DataItem(ItemFormatCode.List);

            foreach (var item in GlobalData.Current.ShelfMgr.AllData)
            {
                if (item == null || item.DeadZone)
                    continue;

                DataItem diShelvesInfo = new DataItem(ItemFormatCode.List);
                diShelvesInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.TagName)); //Shelf Name
                diShelvesInfo.AddChildItem(new DataItem(ItemFormatCode.U2, item.ShelfState)); //Shelf State
                diShelvesInfo.AddChildItem(new DataItem(ItemFormatCode.U2, item.ShelfHSMSStatus)); //Shelf Status
                diShelvesInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.ZONE));     //Shelf ZoneName
                diEnHancedShelves.AddChildItem(diShelvesInfo);
            }
            return diEnHancedShelves;
        }

        public static DataItem SVID5_AlarmSet()
        {
            DataItem diAlarmSet = new DataItem(ItemFormatCode.List);

            if (GlobalData.Current.Alarm_Manager.ActiveAlarmList.Count == 0)
            {
                return diAlarmSet;
            }
            else
            {
                List<AlarmData> ActiveList = GlobalData.Current.Alarm_Manager.GetActiveList(); //20230207 RGJ 열거예외 발생 가능성 방지 추가.
                foreach (AlarmData item in ActiveList)
                { 
                    DataItem diAlarmInfo = new DataItem(ItemFormatCode.List);
                    diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, GlobalData.Current.EQPID));
                    diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.U4, item.iAlarmID));
                    diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.AlarmName));
                    diAlarmSet.AddChildItem(diAlarmInfo);
                }
            }
            return diAlarmSet;
        }

        //2024.09.06 lim, 리컨사일 시 Alarmset -> EnhancedSpecificAlarmReport 변경
        public static DataItem SVID119_EnhancedSpecificAlarmReports()
        {
            DataItem diAlarmSet = new DataItem(ItemFormatCode.List);

            if (GlobalData.Current.Alarm_Manager.ActiveAlarmList.Count == 0)
            {
                return diAlarmSet;
            }
            else
            {
                List<AlarmData> ActiveList = GlobalData.Current.Alarm_Manager.GetActiveList(); //20230207 RGJ 열거예외 발생 가능성 방지 추가.
                foreach (AlarmData item in ActiveList)
                {
                    string alarmNameBuffer = item.AlarmName.Length > 80 ? item.AlarmName.Substring(0, 80) : item.AlarmName; //Alarm Name 길이 확인
					//2024.10.07 lim, EQPID로 보고 하면 안됨 by 정연동 
					string UnitID = (item.ModuleName == GlobalData.Current.EQPID) ? "" : item.ModuleName;
					
                    DataItem diAlarmInfo = new DataItem(ItemFormatCode.List);
                    diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.Bin, 128));   //CODE SET :128       Clear :0
                    diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.U4, item.iAlarmID));        //ALARM ID
                    diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, alarmNameBuffer));    //ALARM NAME
                    diAlarmInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, UnitID));   //<A64 StockedUnitID>   UnitID 로 올림
                    DataItem diCommandData = new DataItem(ItemFormatCode.List);                       //<L[n] 1

                    string CarrierLoc = string.Empty;
                    string RelateJobID = string.Empty;
                    string CarrierID = string.Empty;

                    //var module = GlobalData.Current.GetModuleByName(item.ModuleName);
                    //if (module is RMModuleBase)
                    //{
                    //    McsJob RelateJob = GlobalData.Current.McdList.Where(j => j.AssignRMName == module.ModuleName).FirstOrDefault();
                    //}
                    //else if (module is CV_ManualModule)
                    //{ 
                    //}

                    if (!string.IsNullOrEmpty(item.CarrierID))
                    {
                        CarrierID = item.CarrierID;
                        McsJob RelateJob = GlobalData.Current.McdList.Where(j => !string.IsNullOrEmpty(j.CarrierID) && j.CarrierID == item.CarrierID).FirstOrDefault();
                        if (RelateJob != null)
                        {
                            RelateJobID = RelateJob.CommandID;
                            CarrierLoc = RelateJob.CarrierLoc;
                        }
                    }
                    DataItem diCommandList = new DataItem(ItemFormatCode.List);                     //<L[3]
                    diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.CarrierID));      //CARRIERID
                    diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, RelateJobID));          //COMMANDID
                    diCommandList.AddChildItem(new DataItem(ItemFormatCode.ASCII, CarrierLoc));     //LOCATION
                    diCommandData.AddChildItem(diCommandList);
                    diAlarmInfo.AddChildItem(diCommandData);
                    diAlarmSet.AddChildItem(diAlarmInfo);
                }
            }
            return diAlarmSet;
        }
        public static DataItem SVID26_CraneState()
        {
            DataItem diCraneState = new DataItem(ItemFormatCode.List);

            foreach (var item in GlobalData.Current.mRMManager.ModuleList)
            {
                DataItem diCraneInfo = new DataItem(ItemFormatCode.List);

                diCraneInfo.AddChildItem(new DataItem(ItemFormatCode.ASCII, item.Value.ModuleName));
                diCraneInfo.AddChildItem(new DataItem(ItemFormatCode.U2, item.Value.GetUnitServiceState()));
                diCraneState.AddChildItem(diCraneInfo);
            }
            return diCraneState;
        }

        public static DataItem SVID60_SCState()
        {
            //DataItem diSCState = new DataItem(ItemFormatCode.List);
            //diSCState.AddChildItem(new DataItem(ItemFormatCode.U2, GlobalData.Current.MainBooth.SCState));
            //return diSCState;

            //220804 RGJ SVID 60 보고사양 수정
            DataItem diSCState = new DataItem(ItemFormatCode.U2, GlobalData.Current.MainBooth.SCState);
            return diSCState;
        }
        //220517 조숭진 hsms 메세지 추가 e
    }
}
