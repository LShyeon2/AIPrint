using Newtonsoft.Json;
using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.DataList;
using BoxPrint.GUI.UIControls;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BoxPrint.GUI.ViewModels
{
    public class ConveyorIODetailViewModel : ViewModelBase
    {
        private ControlBase SelectUnit = null;
        private PLCDataItem SelectItem = null;//SuHwan_20230207 : 피엘씨 쓰기 추가
        private eDataChangeUnitType curType = eDataChangeUnitType.eBooth;

        private bool IsPlayBackControl = false;

        #region Binding
        //ItemAddress_AddressOffset_BitOffset 으로 생성 필요
        #region TrackingData
        //AddressOffset - 0 - CarrierID
        private object _PCtoPLC_0_0;
        public object PCtoPLC_0_0
        {
            get => _PCtoPLC_0_0;
            set => Set("PCtoPLC_0_0", ref _PCtoPLC_0_0, value);
        }

        //AddressOffset - 20 - DestinationCpu
        private object _PCtoPLC_20_0;
        public object PCtoPLC_20_0
        {
            get => _PCtoPLC_20_0;
            set => Set("PCtoPLC_20_0", ref _PCtoPLC_20_0, value);
        }

        //AddressOffset - 21 - DestinationTrack
        private object _PCtoPLC_21_0;
        public object PCtoPLC_21_0
        {
            get => _PCtoPLC_21_0;
            set => Set("PCtoPLC_21_0", ref _PCtoPLC_21_0, value);
        }

        //AddressOffset - 22 - TrayType
        private object _PCtoPLC_22_0;
        public object PCtoPLC_22_0
        {
            get => _PCtoPLC_22_0;
            set => Set("PCtoPLC_22_0", ref _PCtoPLC_22_0, value);
        }

        //AddressOffset - 23 - TrayStackCount
        private object _PCtoPLC_23_0;
        public object PCtoPLC_23_0
        {
            get => _PCtoPLC_23_0;
            set => Set("PCtoPLC_23_0", ref _PCtoPLC_23_0, value);
        }

        //AddressOffset - 24 - Polarity
        private object _PCtoPLC_24_0;
        public object PCtoPLC_24_0
        {
            get => _PCtoPLC_24_0;
            set => Set("PCtoPLC_24_0", ref _PCtoPLC_24_0, value);
        }

        //AddressOffset - 25 - ProductEmpty
        private object _PCtoPLC_25_0;
        public object PCtoPLC_25_0
        {
            get => _PCtoPLC_25_0;
            set => Set("PCtoPLC_25_0", ref _PCtoPLC_25_0, value);
        }

        //AddressOffset - 26 - WinderDirection
        private object _PCtoPLC_26_0;
        public object PCtoPLC_26_0
        {
            get => _PCtoPLC_26_0;
            set => Set("PCtoPLC_26_0", ref _PCtoPLC_26_0, value);
        }

        //AddressOffset - 27 - ProductQuantity
        private object _PCtoPLC_27_0;
        public object PCtoPLC_27_0
        {
            get => _PCtoPLC_27_0;
            set => Set("PCtoPLC_27_0", ref _PCtoPLC_27_0, value);
        }

        //AddressOffset - 28 - CellPackLine
        private object _PCtoPLC_28_0;
        public object PCtoPLC_28_0
        {
            get => _PCtoPLC_28_0;
            set => Set("PCtoPLC_28_0", ref _PCtoPLC_28_0, value);
        }

        //AddressOffset - 29 - InnerTrayType
        private object _PCtoPLC_29_0;
        public object PCtoPLC_29_0
        {
            get => _PCtoPLC_29_0;
            set => Set("PCtoPLC_29_0", ref _PCtoPLC_29_0, value);
        }

        //AddressOffset - 30 - PalletSize
        private object _PCtoPLC_30_0;
        public object PCtoPLC_30_0
        {
            get => _PCtoPLC_30_0;
            set => Set("PCtoPLC_30_0", ref _PCtoPLC_30_0, value);
        }

        //AddressOffset - 31 - UnCoatedPart
        private object _PCtoPLC_31_0;
        public object PCtoPLC_31_0
        {
            get => _PCtoPLC_31_0;
            set => Set("PCtoPLC_31_0", ref _PCtoPLC_31_0, value);
        }

        //AddressOffset - 32 - CoreType
        private object _PCtoPLC_32_0;
        public object PCtoPLC_32_0
        {
            get => _PCtoPLC_32_0;
            set => Set("PCtoPLC_32_0", ref _PCtoPLC_32_0, value);
        }

        //AddressOffset - 33 - ProductEnd
        private object _PCtoPLC_33_0;
        public object PCtoPLC_33_0
        {
            get => _PCtoPLC_33_0;
            set => Set("PCtoPLC_33_0", ref _PCtoPLC_33_0, value);
        }

        //AddressOffset - 34 - ValidationNG
        private object _PCtoPLC_34_0;
        public object PCtoPLC_34_0
        {
            get => _PCtoPLC_34_0;
            set => Set("PCtoPLC_34_0", ref _PCtoPLC_34_0, value);
        }

        //AddressOffset - 35 - WayPoint
        private object _PCtoPLC_35_0;
        public object PCtoPLC_35_0
        {
            get => _PCtoPLC_35_0;
            set => Set("PCtoPLC_35_0", ref _PCtoPLC_35_0, value);
        }
        #endregion

        #region Shared
        //AddressOffset - 36 - VibrationData
        private object _PCtoPLC_36_0;
        public object PCtoPLC_36_0
        {
            get => _PCtoPLC_36_0;
            set => Set("PCtoPLC_36_0", ref _PCtoPLC_36_0, value);
        }

        //AddressOffset - 37 - TrackPause
        private object _PCtoPLC_37_0;
        public object PCtoPLC_37_0
        {
            get => _PCtoPLC_37_0;
            set => Set("PCtoPLC_37_0", ref _PCtoPLC_37_0, value);
        }

        //AddressOffset - 38 - CimInOutMode
        private object _PCtoPLC_38_0;
        public object PCtoPLC_38_0
        {
            get => _PCtoPLC_38_0;
            set => Set("PCtoPLC_38_0", ref _PCtoPLC_38_0, value);
        }

        //AddressOffset - 39 - CimErrorCode
        private object _PCtoPLC_39_0;
        public object PCtoPLC_39_0
        {
            get => _PCtoPLC_39_0;
            set => Set("PCtoPLC_39_0", ref _PCtoPLC_39_0, value);
        }

        //AddressOffset - 40 - Buzzer
        private object _PCtoPLC_40_0;
        public object PCtoPLC_40_0
        {
            get => _PCtoPLC_40_0;
            set => Set("PCtoPLC_40_0", ref _PCtoPLC_40_0, value);
        }

        //AddressOffset - 41 - McsSelect
        private object _PCtoPLC_41_0;
        public object PCtoPLC_41_0
        {
            get => _PCtoPLC_41_0;
            set => Set("PCtoPLC_41_0", ref _PCtoPLC_41_0, value);
        }
        #region Shared Bit
        //AddressOffset - 42 - SharedBit - 0 - BcrComplete
        private object _PCtoPLC_42_0 = false;
        public object PCtoPLC_42_0
        {
            get => _PCtoPLC_42_0;
            set => Set("PCtoPLC_42_0", ref _PCtoPLC_42_0, value);
        }

        //AddressOffset - 42 - SharedBit - 1 - BcrFail
        private object _PCtoPLC_42_1 = false;
        public object PCtoPLC_42_1
        {
            get => _PCtoPLC_42_1;
            set => Set("PCtoPLC_42_1", ref _PCtoPLC_42_1, value);
        }

        //AddressOffset - 42 - SharedBit - 2 - PLCWriteReqFlag(TransferPossible)
        private object _PCtoPLC_42_2 = false;
        public object PCtoPLC_42_2
        {
            get => _PCtoPLC_42_2;
            set => Set("PCtoPLC_42_2", ref _PCtoPLC_42_2, value);
        }

        //AddressOffset - 42 - SharedBit - 3 - Reserved
        private object _PCtoPLC_42_3 = false;
        public object PCtoPLC_42_3
        {
            get => _PCtoPLC_42_3;
            set => Set("PCtoPLC_42_3", ref _PCtoPLC_42_3, value);
        }

        //AddressOffset - 42 - SharedBit - 4 - LightCurtainSensor
        private object _PCtoPLC_42_4 = false;
        public object PCtoPLC_42_4
        {
            get => _PCtoPLC_42_4;
            set => Set("PCtoPLC_42_4", ref _PCtoPLC_42_4, value);
        }

        //AddressOffset - 42 - SharedBit - 5 - PortTypeChange
        private object _PCtoPLC_42_5 = false;
        public object PCtoPLC_42_5
        {
            get => _PCtoPLC_42_5;
            set => Set("PCtoPLC_42_5", ref _PCtoPLC_42_5, value);
        }

        //AddressOffset - 42 - SharedBit - 6 - Reserved
        private object _PCtoPLC_42_6 = false;
        public object PCtoPLC_42_6
        {
            get => _PCtoPLC_42_6;
            set => Set("PCtoPLC_42_6", ref _PCtoPLC_42_6, value);
        }

        //AddressOffset - 42 - SharedBit - 7 - Reserved
        private object _PCtoPLC_42_7 = false;
        public object PCtoPLC_42_7
        {
            get => _PCtoPLC_42_7;
            set => Set("PCtoPLC_42_7", ref _PCtoPLC_42_7, value);
        }

        //AddressOffset - 42 - SharedBit - 8 - CimAlarmClear
        private object _PCtoPLC_42_8 = false;
        public object PCtoPLC_42_8
        {
            get => _PCtoPLC_42_8;
            set => Set("PCtoPLC_42_8", ref _PCtoPLC_42_8, value);
        }

        //AddressOffset - 42 - SharedBit - 9 - CimReportComp
        private object _PCtoPLC_42_9 = false;
        public object PCtoPLC_42_9
        {
            get => _PCtoPLC_42_9;
            set => Set("PCtoPLC_42_9", ref _PCtoPLC_42_9, value);
        }

        //AddressOffset - 42 - SharedBit - 10 - Reserved
        private object _PCtoPLC_42_10 = false;
        public object PCtoPLC_42_10
        {
            get => _PCtoPLC_42_10;
            set => Set("PCtoPLC_42_10", ref _PCtoPLC_42_10, value);
        }

        //AddressOffset - 42 - SharedBit - 11 - Reserved
        private object _PCtoPLC_42_11 = false;
        public object PCtoPLC_42_11
        {
            get => _PCtoPLC_42_11;
            set => Set("PCtoPLC_42_11", ref _PCtoPLC_42_11, value);
        }

        //AddressOffset - 42 - SharedBit - 12 - Reserved
        private object _PCtoPLC_42_12 = false;
        public object PCtoPLC_42_12
        {
            get => _PCtoPLC_42_12;
            set => Set("PCtoPLC_42_12", ref _PCtoPLC_42_12, value);
        }

        //AddressOffset - 42 - SharedBit - 13 - Reserved
        private object _PCtoPLC_42_13 = false;
        public object PCtoPLC_42_13
        {
            get => _PCtoPLC_42_13;
            set => Set("PCtoPLC_42_13", ref _PCtoPLC_42_13, value);
        }

        //AddressOffset - 42 - SharedBit - 14 - Reserved
        private object _PCtoPLC_42_14 = false;
        public object PCtoPLC_42_14
        {
            get => _PCtoPLC_42_14;
            set => Set("PCtoPLC_42_14", ref _PCtoPLC_42_14, value);
        }

        //AddressOffset - 42 - SharedBit - 15 - Reserved
        private object _PCtoPLC_42_15 = false;
        public object PCtoPLC_42_15
        {
            get => _PCtoPLC_42_15;
            set => Set("PCtoPLC_42_15", ref _PCtoPLC_42_15, value);
        }
        #endregion
        #endregion

        #region PLC
        //AddressOffset - 43 - ErrorCode
        private object _PLCtoPC_43_0;
        public object PLCtoPC_43_0
        {
            get => _PLCtoPC_43_0;
            set => Set("PLCtoPC_43_0", ref _PLCtoPC_43_0, value);
        }

        //AddressOffset - 46 - Position
        private object _PLCtoPC_46_0;
        public object PLCtoPC_46_0
        {
            get => _PLCtoPC_46_0;
            set => Set("PLCtoPC_46_0", ref _PLCtoPC_46_0, value);
        }

        //AddressOffset - 47 - MGDetectInfo
        private object _PLCtoPC_47_0;
        public object PLCtoPC_47_0
        {
            get => _PLCtoPC_47_0;
            set => Set("PLCtoPC_47_0", ref _PLCtoPC_47_0, value);
        }

        //AddressOffset - 48 - PortType
        private object _PLCtoPC_48_0;
        public object PLCtoPC_48_0
        {
            get => _PLCtoPC_48_0;
            set => Set("PLCtoPC_48_0", ref _PLCtoPC_48_0, value);
        }

        #region PLC State Bit
        //AddressOffset - 44 - SharedBit - 0 - KeySW
        private object _PLCtoPC_44_0 = false;
        public object PLCtoPC_44_0
        {
            get => _PLCtoPC_44_0;
            set => Set("PLCtoPC_44_0", ref _PLCtoPC_44_0, value);
        }

        //AddressOffset - 44 - SharedBit - 1 - CVRunState
        private object _PLCtoPC_44_1 = false;
        public object PLCtoPC_44_1
        {
            get => _PLCtoPC_44_1;
            set => Set("PLCtoPC_44_1", ref _PLCtoPC_44_1, value);
        }

        //AddressOffset - 44 - SharedBit - 2 - ProtAccessMode
        private object _PLCtoPC_44_2 = false;
        public object PLCtoPC_44_2
        {
            get => _PLCtoPC_44_2;
            set => Set("PLCtoPC_44_2", ref _PLCtoPC_44_2, value);
        }

        //AddressOffset - 44 - SharedBit - 3 - BcrReadReq
        private object _PLCtoPC_44_3 = false;
        public object PLCtoPC_44_3
        {
            get => _PLCtoPC_44_3;
            set => Set("PLCtoPC_44_3", ref _PLCtoPC_44_3, value);
        }

        //AddressOffset - 44 - SharedBit - 4 - DestReady
        private object _PLCtoPC_44_4 = false;
        public object PLCtoPC_44_4
        {
            get => _PLCtoPC_44_4;
            set => Set("PLCtoPC_44_4", ref _PLCtoPC_44_4, value);
        }

        //AddressOffset - 44 - SharedBit - 5 - SCInputHSRead
        private object _PLCtoPC_44_5 = false;
        public object PLCtoPC_44_5
        {
            get => _PLCtoPC_44_5;
            set => Set("PLCtoPC_44_5", ref _PLCtoPC_44_5, value);
        }

        //AddressOffset - 44 - SharedBit - 6 - SCOutputHSRead
        private object _PLCtoPC_44_6 = false;
        public object PLCtoPC_44_6
        {
            get => _PLCtoPC_44_6;
            set => Set("PLCtoPC_44_6", ref _PLCtoPC_44_6, value);
        }

        //AddressOffset - 44 - SharedBit - 7 - ScsTransferReq
        private object _PLCtoPC_44_7 = false;
        public object PLCtoPC_44_7
        {
            get => _PLCtoPC_44_7;
            set => Set("PLCtoPC_44_7", ref _PLCtoPC_44_7, value);
        }

        //AddressOffset - 44 - SharedBit - 8 - ScsTransferComp
        private object _PLCtoPC_44_8 = false;
        public object PLCtoPC_44_8
        {
            get => _PLCtoPC_44_8;
            set => Set("PLCtoPC_44_8", ref _PLCtoPC_44_8, value);
        }

        //AddressOffset - 44 - SharedBit - 9 - AgvFireShutterLoc
        private object _PLCtoPC_44_9 = false;
        public object PLCtoPC_44_9
        {
            get => _PLCtoPC_44_9;
            set => Set("PLCtoPC_44_9", ref _PLCtoPC_44_9, value);
        }

        //AddressOffset - 44 - SharedBit - 10 - LoadReq
        private object _PLCtoPC_44_10 = false;
        public object PLCtoPC_44_10
        {
            get => _PLCtoPC_44_10;
            set => Set("PLCtoPC_44_10", ref _PLCtoPC_44_10, value);
        }

        //AddressOffset - 44 - SharedBit - 11 - CVLoadComp
        private object _PLCtoPC_44_11 = false;
        public object PLCtoPC_44_11
        {
            get => _PLCtoPC_44_11;
            set => Set("PLCtoPC_44_11", ref _PLCtoPC_44_11, value);
        }

        //AddressOffset - 44 - SharedBit - 12 - UnloadReq
        private object _PLCtoPC_44_12 = false;
        public object PLCtoPC_44_12
        {
            get => _PLCtoPC_44_12;
            set => Set("PLCtoPC_44_12", ref _PLCtoPC_44_12, value);
        }

        //AddressOffset - 44 - SharedBit - 13 - CVUnloadComp
        private object _PLCtoPC_44_13 = false;
        public object PLCtoPC_44_13
        {
            get => _PLCtoPC_44_13;
            set => Set("PLCtoPC_44_13", ref _PLCtoPC_44_13, value);
        }

        //AddressOffset - 44 - SharedBit - 14 - NGUnloadReq
        private object _PLCtoPC_44_14 = false;
        public object PLCtoPC_44_14
        {
            get => _PLCtoPC_44_14;
            set => Set("PLCtoPC_44_14", ref _PLCtoPC_44_14, value);
        }

        //AddressOffset - 44 - SharedBit - 15 - Reserved
        private object _PLCtoPC_44_15 = false;
        public object PLCtoPC_44_15
        {
            get => _PLCtoPC_44_15;
            set => Set("PLCtoPC_44_15", ref _PLCtoPC_44_15, value);
        }
        #endregion

        #region PLC Sensor Bit
        //AddressOffset - 45 - SharedBit - 0 - Exist
        private object _PLCtoPC_45_0 = false;
        public object PLCtoPC_45_0
        {
            get => _PLCtoPC_45_0;
            set => Set("PLCtoPC_45_0", ref _PLCtoPC_45_0, value);
        }

        //AddressOffset - 45 - SharedBit - 1 - EmptyBobinSensor
        private object _PLCtoPC_45_1 = false;
        public object PLCtoPC_45_1
        {
            get => _PLCtoPC_45_1;
            set => Set("PLCtoPC_45_1", ref _PLCtoPC_45_1, value);
        }

        //AddressOffset - 45 - SharedBit - 2 - MaterialSensor
        private object _PLCtoPC_45_2 = false;
        public object PLCtoPC_45_2
        {
            get => _PLCtoPC_45_2;
            set => Set("PLCtoPC_45_2", ref _PLCtoPC_45_2, value);
        }

        //AddressOffset - 45 - SharedBit - 3 - Reserved
        private object _PLCtoPC_45_3 = false;
        public object PLCtoPC_45_3
        {
            get => _PLCtoPC_45_3;
            set => Set("PLCtoPC_45_3", ref _PLCtoPC_45_3, value);
        }

        //AddressOffset - 45 - SharedBit - 4 - Reserved
        private object _PLCtoPC_45_4 = false;
        public object PLCtoPC_45_4
        {
            get => _PLCtoPC_45_4;
            set => Set("PLCtoPC_45_4", ref _PLCtoPC_45_4, value);
        }

        //AddressOffset - 45 - SharedBit - 5 - PortTypeChange
        private object _PLCtoPC_45_5 = false;
        public object PLCtoPC_45_5
        {
            get => _PLCtoPC_45_5;
            set => Set("PLCtoPC_45_5", ref _PLCtoPC_45_5, value);
        }

        //AddressOffset - 45 - SharedBit - 6 - Reserved
        private object _PLCtoPC_45_6 = false;
        public object PLCtoPC_45_6
        {
            get => _PLCtoPC_45_6;
            set => Set("PLCtoPC_45_6", ref _PLCtoPC_45_6, value);
        }

        //AddressOffset - 45 - SharedBit - 7 - Reserved
        private object _PLCtoPC_45_7 = false;
        public object PLCtoPC_45_7
        {
            get => _PLCtoPC_45_7;
            set => Set("PLCtoPC_45_7", ref _PLCtoPC_45_7, value);
        }

        //AddressOffset - 45 - SharedBit - 8 - PLCAlarmClear
        private object _PLCtoPC_45_8 = false;
        public object PLCtoPC_45_8
        {
            get => _PLCtoPC_45_8;
            set => Set("PLCtoPC_45_8", ref _PLCtoPC_45_8, value);
        }

        //AddressOffset - 45 - SharedBit - 9 - Reserved
        private object _PLCtoPC_45_9 = false;
        public object PLCtoPC_45_9
        {
            get => _PLCtoPC_45_9;
            set => Set("PLCtoPC_45_9", ref _PLCtoPC_45_9, value);
        }

        //AddressOffset - 45 - SharedBit - 10 - Reserved
        private object _PLCtoPC_45_10 = false;
        public object PLCtoPC_45_10
        {
            get => _PLCtoPC_45_10;
            set => Set("PLCtoPC_45_10", ref _PLCtoPC_45_10, value);
        }

        //AddressOffset - 45 - SharedBit - 11 - Reserved
        private object _PLCtoPC_45_11 = false;
        public object PLCtoPC_45_11
        {
            get => _PLCtoPC_45_11;
            set => Set("PLCtoPC_45_11", ref _PLCtoPC_45_11, value);
        }

        //AddressOffset - 45 - SharedBit - 12 - ForcedDataDelete
        private object _PLCtoPC_45_12 = false;
        public object PLCtoPC_45_12
        {
            get => _PLCtoPC_45_12;
            set => Set("PLCtoPC_45_12", ref _PLCtoPC_45_12, value);
        }

        //AddressOffset - 45 - SharedBit - 13 - Reserved
        private object _PLCtoPC_45_13 = false;
        public object PLCtoPC_45_13
        {
            get => _PLCtoPC_45_13;
            set => Set("PLCtoPC_45_13", ref _PLCtoPC_45_13, value);
        }

        //AddressOffset - 45 - SharedBit - 14 - PortSize
        private object _PLCtoPC_45_14 = false;
        public object PLCtoPC_45_14
        {
            get => _PLCtoPC_45_14;
            set => Set("PLCtoPC_45_14", ref _PLCtoPC_45_14, value);
        }

        //AddressOffset - 45 - SharedBit - 15 - Reserved
        private object _PLCtoPC_45_15 = false;
        public object PLCtoPC_45_15
        {
            get => _PLCtoPC_45_15;
            set => Set("PLCtoPC_45_15", ref _PLCtoPC_45_15, value);
        }
        #endregion

        #region OptionalBit
        //AddressOffset - 49 - SharedBit - 0 - Reserved
        private object _PLCtoPC_49_0 = false;
        public object PLCtoPC_49_0
        {
            get => _PLCtoPC_49_0;
            set => Set("PLCtoPC_49_0", ref _PLCtoPC_49_0, value);
        }

        //AddressOffset - 49 - SharedBit - 1 - Reserved
        private object _PLCtoPC_49_1 = false;
        public object PLCtoPC_49_1
        {
            get => _PLCtoPC_49_1;
            set => Set("PLCtoPC_49_1", ref _PLCtoPC_49_1, value);
        }

        //AddressOffset - 49 - SharedBit - 2 - Reserved(TransferPossible)
        private object _PLCtoPC_49_2 = false;
        public object PLCtoPC_49_2
        {
            get => _PLCtoPC_49_2;
            set => Set("PLCtoPC_49_2", ref _PLCtoPC_49_2, value);
        }

        //AddressOffset - 49 - SharedBit - 3 - Reserved
        private object _PLCtoPC_49_3 = false;
        public object PLCtoPC_49_3
        {
            get => _PLCtoPC_49_3;
            set => Set("PLCtoPC_49_3", ref _PLCtoPC_49_3, value);
        }

        //AddressOffset - 49 - SharedBit - 4 - Reserved
        private object _PLCtoPC_49_4 = false;
        public object PLCtoPC_49_4
        {
            get => _PLCtoPC_49_4;
            set => Set("PLCtoPC_49_4", ref _PLCtoPC_49_4, value);
        }

        //AddressOffset - 49 - SharedBit - 5 - Reserved
        private object _PLCtoPC_49_5 = false;
        public object PLCtoPC_49_5
        {
            get => _PLCtoPC_49_5;
            set => Set("PLCtoPC_49_5", ref _PLCtoPC_49_5, value);
        }

        //AddressOffset - 49 - SharedBit - 6 - Reserved
        private object _PLCtoPC_49_6 = false;
        public object PLCtoPC_49_6
        {
            get => _PLCtoPC_49_6;
            set => Set("PLCtoPC_49_6", ref _PLCtoPC_49_6, value);
        }

        //AddressOffset - 49 - SharedBit - 7 - Reserved
        private object _PLCtoPC_49_7 = false;
        public object PLCtoPC_49_7
        {
            get => _PLCtoPC_49_7;
            set => Set("PLCtoPC_49_7", ref _PLCtoPC_49_7, value);
        }

        //AddressOffset - 49 - SharedBit - 8 - Reserved
        private object _PLCtoPC_49_8 = false;
        public object PLCtoPC_49_8
        {
            get => _PLCtoPC_49_8;
            set => Set("PLCtoPC_49_8", ref _PLCtoPC_49_8, value);
        }

        //AddressOffset - 49 - SharedBit - 9 - Reserved
        private object _PLCtoPC_49_9 = false;
        public object PLCtoPC_49_9
        {
            get => _PLCtoPC_49_9;
            set => Set("PLCtoPC_49_9", ref _PLCtoPC_49_9, value);
        }

        //AddressOffset - 49 - SharedBit - 10 - Reserved
        private object _PLCtoPC_49_10 = false;
        public object PLCtoPC_49_10
        {
            get => _PLCtoPC_49_10;
            set => Set("PLCtoPC_49_10", ref _PLCtoPC_49_10, value);
        }

        //AddressOffset - 49 - SharedBit - 11 - Reserved
        private object _PLCtoPC_49_11 = false;
        public object PLCtoPC_49_11
        {
            get => _PLCtoPC_49_11;
            set => Set("PLCtoPC_49_11", ref _PLCtoPC_49_11, value);
        }

        //AddressOffset - 49 - SharedBit - 12 - Reserved
        private object _PLCtoPC_49_12 = false;
        public object PLCtoPC_49_12
        {
            get => _PLCtoPC_49_12;
            set => Set("PLCtoPC_49_12", ref _PLCtoPC_49_12, value);
        }

        //AddressOffset - 49 - SharedBit - 13 - Reserved
        private object _PLCtoPC_49_13 = false;
        public object PLCtoPC_49_13
        {
            get => _PLCtoPC_49_13;
            set => Set("PLCtoPC_49_13", ref _PLCtoPC_49_13, value);
        }

        //AddressOffset - 49 - SharedBit - 14 - Reserved
        private object _PLCtoPC_49_14 = false;
        public object PLCtoPC_49_14
        {
            get => _PLCtoPC_49_14;
            set => Set("PLCtoPC_49_14", ref _PLCtoPC_49_14, value);
        }

        //AddressOffset - 49 - SharedBit - 15 - Reserved
        private object _PLCtoPC_49_15 = false;
        public object PLCtoPC_49_15
        {
            get => _PLCtoPC_49_15;
            set => Set("PLCtoPC_49_15", ref _PLCtoPC_49_15, value);
        }
        #endregion
        #endregion
        #endregion

        public ConveyorIODetailViewModel(bool IsPlayBack)
        {
            IsPlayBackControl = IsPlayBack;
        }

        public void AbleViewModel(ControlBase selectunit)
        {
            curType = eDataChangeUnitType.ePort;

            //if (!IsPlayBackControl)
            {
                SelectUnit = selectunit as CV_BaseModule;
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
                        if (SelectUnit is CV_BaseModule cv)
                            changeItems = cv.PLCtoPC;
                    }
                }
                else if (changeType.Equals(eDataChangeProperty.eIO_PCtoPLC))
                {
                    //if (!IsPlayBackControl)
                    {
                        if (SelectUnit is CV_BaseModule cv)
                            changeItems = cv.PCtoPLC;
                    }
                }
                else
                    return;

                if (changeItems is null)
                    return;

                KeyValuePair<string, PLCDataItem> firstitem = changeItems.Where(r => !r.Value.ItemName.Contains("BatchRead")).OrderBy(s => s.Value.AddressOffset).FirstOrDefault();
                int iStartAddress = firstitem.Value.AddressOffset;

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
                CV_BaseModule cv = SelectUnit as CV_BaseModule;
                if (cv is null)
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
                        foreach (var item in cv.PCtoPLC.Values)
                        {
                            if (item.AddressOffset == bufferAddressOffset && item.BitOffset == bufferBitOffset)
                            {
                                SelectItem = item;
                            }
                        }
                    }
                    //240830 HoN MouseEnter Address Data 표시     //우선 전부 처리되도록, 사용하면 안되는 부분은 받아오고 처리한다.
                    if (bufferArea == "PLCtoPC")
                    {
                        foreach (var item in cv.PLCtoPC.Values)
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
                CV_BaseModule cv = SelectUnit as CV_BaseModule;
                if (cv is null)
                    return;

                if (SelectItem == null)
                    return;

                //230301 클라이언트에서 io변경 요청대응
                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    GlobalData.Current.protocolManager.Write(cv.ModuleName, cv.PCtoPLC, SelectItem.ItemName, rcvValue);
                }
                else
                {
                    ClientSetProcedure(cv.ModuleName, SelectItem, rcvValue);
                }

                LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1}에 {2} 을/를 Write하였습니다.", SelectUnit.ControlName, SelectItem.ItemName, rcvValue),
                    "WRITE", SelectItem.ItemName, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 10,
                    SelectUnit.ControlName, SelectItem.ItemName, rcvValue);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        public void SetBitIOData(PLCDataItem PItem, bool bitData)
        {
            string bitvalue = string.Empty;
            try
            {
                //PlayBackControl은 진행되면 안됨.
                if (IsPlayBackControl)
                    return;

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                CV_BaseModule cv = SelectUnit as CV_BaseModule;
                if (cv is null)
                    return;

                if (SelectItem == null || PItem == null)
                {
                    return;
                }

                //230301 클라이언트에서 io변경 요청대응
                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    GlobalData.Current.protocolManager.Write(cv.ModuleName, cv.PCtoPLC, PItem.ItemName, bitData);
                }
                else
                {
                    ClientSetProcedure(cv.ModuleName, PItem, bitData.ToString());
                }
                bitvalue = bitData ? "ON" : "OFF";

                LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1}에 {2} 을/를 Write하였습니다.", SelectUnit.ControlName, SelectItem.ItemName, bitvalue),
                    "WRITE", SelectItem.ItemName, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 10,
                    SelectUnit.ControlName, SelectItem.ItemName, bitvalue);
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
