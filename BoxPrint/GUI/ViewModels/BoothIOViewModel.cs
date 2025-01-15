using Newtonsoft.Json;
using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.DataList;
using BoxPrint.GUI.UIControls;
using BoxPrint.Modules;
using BoxPrint.Modules.Conveyor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BoxPrint.GUI.ViewModels
{
    public class BoothIOViewModel : ViewModelBase
    {
        private ControlBase SelectUnit = null;
        private PLCDataItem SelectItem = null;//SuHwan_20230207 : 피엘씨 쓰기 추가
        private eDataChangeUnitType curType = eDataChangeUnitType.eBooth;

        private bool IsPlayBackControl = false;

        #region Binding
        #region PCtoPLC
        //AddressOffset - 0 - InterlockRelease
        private object _PCtoPLC_0_0;
        public object PCtoPLC_0_0
        {
            get => _PCtoPLC_0_0;
            set => Set("PCtoPLC_0_0", ref _PCtoPLC_0_0, value);
        }
        //AddressOffset - 1 - HeartBeat
        private object _PCtoPLC_1_0;
        public object PCtoPLC_1_0
        {
            get => _PCtoPLC_1_0;
            set => Set("PCtoPLC_1_0", ref _PCtoPLC_1_0, value);
        }
        //AddressOffset - 2 - TimeSync_YY
        private object _PCtoPLC_2_0;
        public object PCtoPLC_2_0
        {
            get => _PCtoPLC_2_0;
            set => Set("PCtoPLC_2_0", ref _PCtoPLC_2_0, value);
        }
        //AddressOffset - 3 - TimeSync_MM
        private object _PCtoPLC_3_0;
        public object PCtoPLC_3_0
        {
            get => _PCtoPLC_3_0;
            set => Set("PCtoPLC_3_0", ref _PCtoPLC_3_0, value);
        }
        //AddressOffset - 4 - TimeSync_DD
        private object _PCtoPLC_4_0;
        public object PCtoPLC_4_0
        {
            get => _PCtoPLC_4_0;
            set => Set("PCtoPLC_4_0", ref _PCtoPLC_4_0, value);
        }
        //AddressOffset - 5 - TimeSync_hh
        private object _PCtoPLC_5_0;
        public object PCtoPLC_5_0
        {
            get => _PCtoPLC_5_0;
            set => Set("PCtoPLC_5_0", ref _PCtoPLC_5_0, value);
        }
        //AddressOffset - 6 - TimeSync_mm
        private object _PCtoPLC_6_0;
        public object PCtoPLC_6_0
        {
            get => _PCtoPLC_6_0;
            set => Set("PCtoPLC_6_0", ref _PCtoPLC_6_0, value);
        }
        //AddressOffset - 7 - TimeSync_ss
        private object _PCtoPLC_7_0;
        public object PCtoPLC_7_0
        {
            get => _PCtoPLC_7_0;
            set => Set("PCtoPLC_7_0", ref _PCtoPLC_7_0, value);
        }
        //AddressOffset - 8 - ScsVersion1
        private object _PCtoPLC_8_0;
        public object PCtoPLC_8_0
        {
            get => _PCtoPLC_8_0;
            set => Set("PCtoPLC_8_0", ref _PCtoPLC_8_0, value);
        }
        //AddressOffset - 9 - ScsVersion2
        private object _PCtoPLC_9_0;
        public object PCtoPLC_9_0
        {
            get => _PCtoPLC_9_0;
            set => Set("PCtoPLC_9_0", ref _PCtoPLC_9_0, value);
        }

        //AddressOffset - 10 - TowerLamp_HPRed
        private object _PCtoPLC_10_0;
        public object PCtoPLC_10_0
        {
            get => _PCtoPLC_10_0;
            set => Set("PCtoPLC_10_0", ref _PCtoPLC_10_0, value);
        }
        //AddressOffset - 11 - TowerLamp_Yellow
        private object _PCtoPLC_11_0;
        public object PCtoPLC_11_0
        {
            get => _PCtoPLC_11_0;
            set => Set("PCtoPLC_11_0", ref _PCtoPLC_11_0, value);
        }
        //AddressOffset - 12 - TowerLamp_HPGreen
        private object _PCtoPLC_12_0;
        public object PCtoPLC_12_0
        {
            get => _PCtoPLC_12_0;
            set => Set("PCtoPLC_12_0", ref _PCtoPLC_12_0, value);
        }
        //AddressOffset - 13 - TowerLamp_HPBlue
        private object _PCtoPLC_13_0;
        public object PCtoPLC_13_0
        {
            get => _PCtoPLC_13_0;
            set => Set("PCtoPLC_13_0", ref _PCtoPLC_13_0, value);
        }
        //AddressOffset - 14 - TowerLamp_HPWhite
        private object _PCtoPLC_14_0;
        public object PCtoPLC_14_0
        {
            get => _PCtoPLC_14_0;
            set => Set("PCtoPLC_14_0", ref _PCtoPLC_14_0, value);
        }
        //AddressOffset - 15 - TowerLamp_OPRed
        private object _PCtoPLC_15_0;
        public object PCtoPLC_15_0
        {
            get => _PCtoPLC_15_0;
            set => Set("PCtoPLC_15_0", ref _PCtoPLC_15_0, value);
        }
        //AddressOffset - 16 - TowerLamp_OPYellow
        private object _PCtoPLC_16_0;
        public object PCtoPLC_16_0
        {
            get => _PCtoPLC_16_0;
            set => Set("PCtoPLC_16_0", ref _PCtoPLC_16_0, value);
        }
        //AddressOffset - 17 - TowerLamp_OPGreen
        private object _PCtoPLC_17_0;
        public object PCtoPLC_17_0
        {
            get => _PCtoPLC_17_0;
            set => Set("PCtoPLC_17_0", ref _PCtoPLC_17_0, value);
        }
        //AddressOffset - 18 - TowerLamp_OPBlue
        private object _PCtoPLC_18_0;
        public object PCtoPLC_18_0
        {
            get => _PCtoPLC_18_0;
            set => Set("PCtoPLC_18_0", ref _PCtoPLC_18_0, value);
        }
        //AddressOffset - 19 - TowerLamp_OPWhite
        private object _PCtoPLC_19_0;
        public object PCtoPLC_19_0
        {
            get => _PCtoPLC_19_0;
            set => Set("PCtoPLC_19_0", ref _PCtoPLC_19_0, value);
        }
        //AddressOffset - 20 - BuzzerHP
        private object _PCtoPLC_20_0;
        public object PCtoPLC_20_0
        {
            get => _PCtoPLC_20_0;
            set => Set("PCtoPLC_20_0", ref _PCtoPLC_20_0, value);
        }
        //AddressOffset - 21 - BuzzerOP
        private object _PCtoPLC_21_0;
        public object PCtoPLC_21_0
        {
            get => _PCtoPLC_21_0;
            set => Set("PCtoPLC_21_0", ref _PCtoPLC_21_0, value);
        }
        //AddressOffset - 25 - Crane1_Availability
        private object _PCtoPLC_25_0;
        public object PCtoPLC_25_0
        {
            get => _PCtoPLC_25_0;
            set => Set("PCtoPLC_25_0", ref _PCtoPLC_25_0, value);
        }
        //AddressOffset - 26 - Crane2_Availability
        private object _PCtoPLC_26_0;
        public object PCtoPLC_26_0
        {
            get => _PCtoPLC_26_0;
            set => Set("PCtoPLC_26_0", ref _PCtoPLC_26_0, value);
        }
        //AddressOffset - 28 - SystemStart
        private object _PCtoPLC_28_0;
        public object PCtoPLC_28_0
        {
            get => _PCtoPLC_28_0;
            set => Set("PCtoPLC_28_0", ref _PCtoPLC_28_0, value);
        }

        #region PCtoPLC Bit
        //AddressOffset - 29 - PCtoPLC Bit - 0 - PauseReq
        private object _PCtoPLC_29_0 = false;
        public object PCtoPLC_29_0
        {
            get => _PCtoPLC_29_0;
            set => Set("PCtoPLC_29_0", ref _PCtoPLC_29_0, value);
        }
        //AddressOffset - 29 - PCtoPLC Bit - 1 - ResumeReq
        private object _PCtoPLC_29_1 = false;
        public object PCtoPLC_29_1
        {
            get => _PCtoPLC_29_1;
            set => Set("PCtoPLC_29_1", ref _PCtoPLC_29_1, value);
        }
        //AddressOffset - 29 - PCtoPLC Bit - 2 - TimeSyncReq
        private object _PCtoPLC_29_2 = false;
        public object PCtoPLC_29_2
        {
            get => _PCtoPLC_29_2;
            set => Set("PCtoPLC_29_2", ref _PCtoPLC_29_2, value);
        }
        //AddressOffset - 29 - PCtoPLC Bit - 8 - RM1ReportComp
        private object _PCtoPLC_29_8 = false;
        public object PCtoPLC_29_8
        {
            get => _PCtoPLC_29_8;
            set => Set("PCtoPLC_29_8", ref _PCtoPLC_29_8, value);
        }
        //AddressOffset - 29 - PCtoPLC Bit - 9 - RM2ReportComp
        private object _PCtoPLC_29_9 = false;
        public object PCtoPLC_29_9
        {
            get => _PCtoPLC_29_9;
            set => Set("PCtoPLC_29_9", ref _PCtoPLC_29_9, value);
        }
        #endregion
        #endregion

        #region PLCtoPC
        //AddressOffset - 1 - HeartBeat
        private object _PLCtoPC_1_0;
        public object PLCtoPC_1_0
        {
            get => _PLCtoPC_1_0;
            set => Set("PLCtoPC_1_0", ref _PLCtoPC_1_0, value);
        }
        //AddressOffset - 3 - FireShutterOperation
        private object _PLCtoPC_3_0;
        public object PLCtoPC_3_0
        {
            get => _PLCtoPC_3_0;
            set => Set("PLCtoPC_3_0", ref _PLCtoPC_3_0, value);
        }
        #endregion

        #region PLCtoPC Bit
        //AddressOffset - 29 - PLCtoPC Bit - 0 - PauseResponse
        private object _PLCtoPC_29_0 = false;
        public object PLCtoPC_29_0
        {
            get => _PLCtoPC_29_0;
            set => Set("PLCtoPC_29_0", ref _PLCtoPC_29_0, value);
        }
        //AddressOffset - 29 - PLCtoPC Bit - 1 - ResumeResponse
        private object _PLCtoPC_29_1 = false;
        public object PLCtoPC_29_1
        {
            get => _PLCtoPC_29_1;
            set => Set("PLCtoPC_29_1", ref _PLCtoPC_29_1, value);
        }
        //AddressOffset - 29 - PLCtoPC Bit - 2 - TimeSyncResponse
        private object _PLCtoPC_29_2 = false;
        public object PLCtoPC_29_2
        {
            get => _PLCtoPC_29_2;
            set => Set("PLCtoPC_29_2", ref _PLCtoPC_29_2, value);
        }
        //AddressOffset - 29 - PLCtoPC Bit - 8 - PauseState
        private object _PLCtoPC_29_8 = false;
        public object PLCtoPC_29_8
        {
            get => _PLCtoPC_29_8;
            set => Set("PLCtoPC_29_8", ref _PLCtoPC_29_8, value);
        }
        //AddressOffset - 29 - PLCtoPC Bit - 9 - AutoState
        private object _PLCtoPC_29_9 = false;
        public object PLCtoPC_29_9
        {
            get => _PLCtoPC_29_9;
            set => Set("PLCtoPC_29_9", ref _PLCtoPC_29_9, value);
        }
        //AddressOffset - 29 - PLCtoPC Bit - 10 - DoorOpenState
        private object _PLCtoPC_29_10 = false;
        public object PLCtoPC_29_10
        {
            get => _PLCtoPC_29_10;
            set => Set("PLCtoPC_29_10", ref _PLCtoPC_29_10, value);
        }
        #endregion
        #endregion

        public BoothIOViewModel(bool IsPlayBack)
        {
            IsPlayBackControl = IsPlayBack;
        }

        public void AbleViewModel(ControlBase selectunit)
        {
            curType = eDataChangeUnitType.eBooth;

            //if (!IsPlayBackControl)
            {
                SelectUnit = selectunit as Booth_SKSCS;
                GlobalData.Current.protocolManager.PLCMap.OnUnitDataChanged += OnUnitDataChangedAction;
                //230103 HHJ SCS 개선
                //GlobalData.Current.UnitIODataReqRequest(curType, SelectUnit.ControlName);
                GlobalData.Current.UnitIODataReqRequest(curType, SelectUnit.ControlName, false);
            }
        }

        public void DisableViewmodel()
        {
            //if (!IsPlayBackControl)
                GlobalData.Current.protocolManager.PLCMap.OnUnitDataChanged -= OnUnitDataChangedAction;
        }

        private void OnUnitDataChangedAction(eDataChangeProperty changeType, eDataChangeUnitType unitType, string unitName, object changeData)
        {
            try
            {
                //선택 유닛이 없으면 진행하지않음
                if (SelectUnit is null)
                    return;

                //여기서는 IO만 처리
                if (!(changeType.Equals(eDataChangeProperty.eIO_PLCtoPC) || changeType.Equals(eDataChangeProperty.eIO_PCtoPLC)))
                    return;

                //여기서는 Port만 처리
                if (!unitType.Equals(curType))
                    return;

                //현재 유닛과 같아야함
                if (!SelectUnit.ControlName.Equals(unitName))
                    return;

                ParseIOData(changeType, changeData);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        private void ParseIOData(eDataChangeProperty changeType, object changeData)
        {
            try
            {
                ConcurrentDictionary<string, PLCDataItem> changeItems = null;
                if (changeType.Equals(eDataChangeProperty.eIO_PLCtoPC))
                {
                    //if (!IsPlayBackControl)
                    {
                        if (SelectUnit is Booth_SKSCS booth)
                            changeItems = booth.PLCtoPC;
                    }
                }
                else if (changeType.Equals(eDataChangeProperty.eIO_PCtoPLC))
                {
                    //if (!IsPlayBackControl)
                    {
                        if (SelectUnit is Booth_SKSCS booth)
                            changeItems = booth.PCtoPLC;
                    }
                }
                else
                    return;

                if (changeItems is null)
                    return;

                KeyValuePair<string, PLCDataItem> firstitem = changeItems.Where(r => !r.Value.ItemName.Contains("BatchRead")).OrderBy(s => s.Value.AddressOffset).FirstOrDefault();
                int iStartAddress = 0;      //Booth는 0부터 시작

                foreach (PLCDataItem item in changeItems.Values.OrderBy(o => o.AddressOffset).ThenBy(o => o.BitOffset))
                {
                    if (item.ItemName.Contains("BatchRead"))
                        continue;

                    int iChangeDataAddress = item.AddressOffset - iStartAddress;
                    int iChangeDataSize = item.Size;
                    byte[] ItemData = new byte[iChangeDataSize * 2];

                    Array.Copy((byte[])changeData, iChangeDataAddress * 2, ItemData, 0, iChangeDataSize * 2);

                    object readdata = ProtocolHelper.ParseIOData(item, ItemData);

                    string memberKey = string.Format("{0}_{1}_{2}", item.Area.ToString(), item.AddressOffset, item.BitOffset);
                    var v = this.GetType().GetMember(memberKey).SingleOrDefault();

                    if (v != null && v is PropertyInfo pinfo)
                    {
                        if (item.DataType.Equals(eDataType.Bool))
                            pinfo.SetValue(this, readdata.Equals(1) ? true : false);
                        //230911 HHJ enum Value string으로 변경
                        //else
                        //    pinfo.SetValue(this, readdata);
                        else
                        {
                            pinfo.SetValue(this, ProtocolHelper.ReadValueConverter(curType, item, readdata));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        /// <summary>
        /// 테그로 검색하여 PLCDataItem 받아오기
        /// </summary>
        /// <param name="rcvTag">ex : PCtoPLC_0_0 </param>
        /// <returns></returns>
        public PLCDataItem GetPLCDataItem(string rcvTag)
        {
            try
            {
                //PlayBackControl은 진행되면 안됨.
                if (IsPlayBackControl)
                    return null;

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                Booth_SKSCS booth = SelectUnit as Booth_SKSCS;
                if (booth is null)
                    return null;

                SelectItem = null;

                if (!string.IsNullOrEmpty(rcvTag))
                {
                    string[] bufferSplitr = rcvTag.Split('_');
                    string bufferArea = string.IsNullOrEmpty(bufferSplitr[0]) ? string.Empty : bufferSplitr[0];
                    int bufferAddressOffset = string.IsNullOrEmpty(bufferSplitr[1]) ? 0 : Convert.ToInt32(bufferSplitr[1]);
                    int bufferBitOffset = string.IsNullOrEmpty(bufferSplitr[2]) ? 0 : Convert.ToInt32(bufferSplitr[2]);

                    if (bufferArea == "PCtoPLC")
                    {
                        foreach (var item in booth.PCtoPLC.Values)
                        {
                            if (item.AddressOffset == bufferAddressOffset && item.BitOffset == bufferBitOffset)
                            {
                                SelectItem = item;
                            }
                        }
                    }
                }

                return SelectItem;
            }
            catch (Exception ex)
            {
                _ = ex;
                return null;
            }
        }

        /// <summary>
        /// 데이타 쓰기
        /// </summary>
        /// <param name="rcvValue"></param>
        public void SetIOData(string rcvValue)
        {
            try
            {
                //PlayBackControl은 진행되면 안됨.
                if (IsPlayBackControl)
                    return;

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                Booth_SKSCS booth = SelectUnit as Booth_SKSCS;
                if (booth is null)
                    return;

                if (SelectItem == null)
                    return;

                //230301 클라이언트에서 io변경 요청대응
                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    GlobalData.Current.protocolManager.Write(booth.ModuleName, booth.PCtoPLC, SelectItem.ItemName, rcvValue);
                }
                else
                {
                    ClientSetProcedure(booth.ModuleName, SelectItem, rcvValue);
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        public void SetBitIOData(PLCDataItem PItem, bool bitData)
        {
            try
            {
                //PlayBackControl은 진행되면 안됨.
                if (IsPlayBackControl)
                    return;

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                Booth_SKSCS booth = SelectUnit as Booth_SKSCS;
                if (booth is null)
                    return;

                if (SelectItem == null || PItem == null)
                {
                    return;
                }

                //230301 클라이언트에서 io변경 요청대응
                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    GlobalData.Current.protocolManager.Write(booth.ModuleName, booth.PCtoPLC, PItem.ItemName, bitData);
                }
                else
                {
                    ClientSetProcedure(booth.ModuleName, PItem, bitData.ToString());
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        //230301 클라이언트에서 io변경 요청대응
        private void ClientSetProcedure(string ModuleName, PLCDataItem pItem, string DataValue)
        {
            ClientReqList reqBuffer = new ClientReqList
            {
                EQPID = GlobalData.Current.EQPID,
                CMDType = DataValue,
                Target = "IO",
                TargetID = ModuleName,
                TargetValue = JsonConvert.SerializeObject(pItem),
                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Requester = eServerClientType.Client,
                JobID = string.Empty,
            };

            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);
        }
    }
}
