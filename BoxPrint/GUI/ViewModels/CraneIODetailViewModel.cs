using Newtonsoft.Json;
using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.DataList;
using BoxPrint.GUI.UIControls;
using BoxPrint.Log;
using BoxPrint.Modules.RM;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BoxPrint.GUI.ViewModels
{
    public class CraneIODetailViewModel : ViewModelBase
    {
        private ControlBase SelectUnit = null;
        private PLCDataItem SelectItem = null;//SuHwan_20230207 : 피엘씨 쓰기 추가
        private eDataChangeUnitType curType = eDataChangeUnitType.eBooth;

        private bool IsPlayBackControl = false;

        #region Binding
        //ItemAddress_AddressOffset_BitOffset 으로 생성 필요
        #region PLCtoPC
        //AddressOffset - 0 - Fork1 CarrierID
        private object _PLCtoPC_0_0;
        public object PLCtoPC_0_0
        {
            get => _PLCtoPC_0_0;
            set => Set("PLCtoPC_0_0", ref _PLCtoPC_0_0, value);
        }

        //AddressOffset - 20 - Fork2 CarrierID
        private object _PLCtoPC_20_0;
        public object PLCtoPC_20_0
        {
            get => _PLCtoPC_20_0;
            set => Set("PLCtoPC_20_0", ref _PLCtoPC_20_0, value);
        }

        //AddressOffset - 40 - PalletInfo
        private object _PLCtoPC_40_0;
        public object PLCtoPC_40_0
        {
            get => _PLCtoPC_40_0;
            set => Set("PLCtoPC_40_0", ref _PLCtoPC_40_0, value);
        }

        //AddressOffset - 41 - JobComp
        private object _PLCtoPC_41_0;
        public object PLCtoPC_41_0
        {
            get => _PLCtoPC_41_0;
            set => Set("PLCtoPC_41_0", ref _PLCtoPC_41_0, value);
        }

        //AddressOffset - 42 - JobType
        private object _PLCtoPC_42_0;
        public object PLCtoPC_42_0
        {
            get => _PLCtoPC_42_0;
            set => Set("PLCtoPC_42_0", ref _PLCtoPC_42_0, value);
        }

        //AddressOffset - 43 - Fork1CommandNumber
        private object _PLCtoPC_43_0;
        public object PLCtoPC_43_0
        {
            get => _PLCtoPC_43_0;
            set => Set("PLCtoPC_43_0", ref _PLCtoPC_43_0, value);
        }

        //AddressOffset - 44 - Fork1SourceBank
        private object _PLCtoPC_44_0;
        public object PLCtoPC_44_0
        {
            get => _PLCtoPC_44_0;
            set => Set("PLCtoPC_44_0", ref _PLCtoPC_44_0, value);
        }

        //AddressOffset - 45 - Fork1SourceBay
        private object _PLCtoPC_45_0;
        public object PLCtoPC_45_0
        {
            get => _PLCtoPC_45_0;
            set => Set("PLCtoPC_45_0", ref _PLCtoPC_45_0, value);
        }

        //AddressOffset - 46 - Fork1SourceLevel
        private object _PLCtoPC_46_0;
        public object PLCtoPC_46_0
        {
            get => _PLCtoPC_46_0;
            set => Set("PLCtoPC_46_0", ref _PLCtoPC_46_0, value);
        }

        //AddressOffset - 47 - Fork1SourceWorkPlace
        private object _PLCtoPC_47_0;
        public object PLCtoPC_47_0
        {
            get => _PLCtoPC_47_0;
            set => Set("PLCtoPC_47_0", ref _PLCtoPC_47_0, value);
        }

        //AddressOffset - 48 - Fork1DestBank
        private object _PLCtoPC_48_0;
        public object PLCtoPC_48_0
        {
            get => _PLCtoPC_48_0;
            set => Set("PLCtoPC_48_0", ref _PLCtoPC_48_0, value);
        }

        //AddressOffset - 49 - Fork1DestBay
        private object _PLCtoPC_49_0;
        public object PLCtoPC_49_0
        {
            get => _PLCtoPC_49_0;
            set => Set("PLCtoPC_49_0", ref _PLCtoPC_49_0, value);
        }

        //AddressOffset - 50 - Fork1DestLevel
        private object _PLCtoPC_50_0;
        public object PLCtoPC_50_0
        {
            get => _PLCtoPC_50_0;
            set => Set("PLCtoPC_50_0", ref _PLCtoPC_50_0, value);
        }

        //AddressOffset - 51 - Fork1DestWorkPlace
        private object _PLCtoPC_51_0;
        public object PLCtoPC_51_0
        {
            get => _PLCtoPC_51_0;
            set => Set("PLCtoPC_51_0", ref _PLCtoPC_51_0, value);
        }

        //AddressOffset - 52 - CommandUseFork
        private object _PLCtoPC_52_0;
        public object PLCtoPC_52_0
        {
            get => _PLCtoPC_52_0;
            set => Set("PLCtoPC_52_0", ref _PLCtoPC_52_0, value);
        }

        //AddressOffset - 53 - Fork2CommandNumber
        private object _PLCtoPC_53_0;
        public object PLCtoPC_53_0
        {
            get => _PLCtoPC_53_0;
            set => Set("PLCtoPC_53_0", ref _PLCtoPC_53_0, value);
        }

        //AddressOffset - 54 - Fork2SourceBank
        private object _PLCtoPC_54_0;
        public object PLCtoPC_54_0
        {
            get => _PLCtoPC_54_0;
            set => Set("PLCtoPC_54_0", ref _PLCtoPC_54_0, value);
        }

        //AddressOffset - 55 - Fork2SourceBay
        private object _PLCtoPC_55_0;
        public object PLCtoPC_55_0
        {
            get => _PLCtoPC_55_0;
            set => Set("PLCtoPC_55_0", ref _PLCtoPC_55_0, value);
        }

        //AddressOffset - 56 - Fork2SourceLevel
        private object _PLCtoPC_56_0;
        public object PLCtoPC_56_0
        {
            get => _PLCtoPC_56_0;
            set => Set("PLCtoPC_56_0", ref _PLCtoPC_56_0, value);
        }

        //AddressOffset - 57 - Fork2SourceWorkPlace
        private object _PLCtoPC_57_0;
        public object PLCtoPC_57_0
        {
            get => _PLCtoPC_57_0;
            set => Set("PLCtoPC_57_0", ref _PLCtoPC_57_0, value);
        }

        //AddressOffset - 58 - Fork2DestBank
        private object _PLCtoPC_58_0;
        public object PLCtoPC_58_0
        {
            get => _PLCtoPC_58_0;
            set => Set("PLCtoPC_58_0", ref _PLCtoPC_58_0, value);
        }

        //AddressOffset - 59 - Fork2DestBay
        private object _PLCtoPC_59_0;
        public object PLCtoPC_59_0
        {
            get => _PLCtoPC_59_0;
            set => Set("PLCtoPC_59_0", ref _PLCtoPC_59_0, value);
        }

        //AddressOffset - 60 - Fork2DestLevel
        private object _PLCtoPC_60_0;
        public object PLCtoPC_60_0
        {
            get => _PLCtoPC_60_0;
            set => Set("PLCtoPC_60_0", ref _PLCtoPC_60_0, value);
        }

        //AddressOffset - 61 - Fork2DestWorkPlace
        private object _PLCtoPC_61_0;
        public object PLCtoPC_61_0
        {
            get => _PLCtoPC_61_0;
            set => Set("PLCtoPC_61_0", ref _PLCtoPC_61_0, value);
        }

        //AddressOffset - 62 - CommandAck
        private object _PLCtoPC_62_0;
        public object PLCtoPC_62_0
        {
            get => _PLCtoPC_62_0;
            set => Set("PLCtoPC_62_0", ref _PLCtoPC_62_0, value);
        }

        //AddressOffset - 63 - 지상반모드
        private object _PLCtoPC_63_0;
        public object PLCtoPC_63_0
        {
            get => _PLCtoPC_63_0;
            set => Set("PLCtoPC_63_0", ref _PLCtoPC_63_0, value);
        }

        //AddressOffset - 64 - 기상반동작모드
        private object _PLCtoPC_64_0;
        public object PLCtoPC_64_0
        {
            get => _PLCtoPC_64_0;
            set => Set("PLCtoPC_64_0", ref _PLCtoPC_64_0, value);
        }

        //AddressOffset - 65 - 기상반동작상태
        private object _PLCtoPC_65_0;
        public object PLCtoPC_65_0
        {
            get => _PLCtoPC_65_0;
            set => Set("PLCtoPC_65_0", ref _PLCtoPC_65_0, value);
        }

        //AddressOffset - 66 - 기상반화물유무
        private object _PLCtoPC_66_0;
        public object PLCtoPC_66_0
        {
            get => _PLCtoPC_66_0;
            set => Set("PLCtoPC_66_0", ref _PLCtoPC_66_0, value);
        }

        //AddressOffset - 67 - 기상반Active
        private object _PLCtoPC_67_0;
        public object PLCtoPC_67_0
        {
            get => _PLCtoPC_67_0;
            set => Set("PLCtoPC_67_0", ref _PLCtoPC_67_0, value);
        }

        //AddressOffset - 68 - FireState
        private object _PLCtoPC_68_1;
        public object PLCtoPC_68_1
        {
            get => _PLCtoPC_68_1;
            set => Set("PLCtoPC_68_1", ref _PLCtoPC_68_1, value);
        }

        //AddressOffset - 69 - WarningCode
        private object _PLCtoPC_69_0;
        public object PLCtoPC_69_0
        {
            get => _PLCtoPC_69_0;
            set => Set("PLCtoPC_69_0", ref _PLCtoPC_69_0, value);
        }

        //AddressOffset - 70 - ErrorCode
        private object _PLCtoPC_70_0;
        public object PLCtoPC_70_0
        {
            get => _PLCtoPC_70_0;
            set => Set("PLCtoPC_70_0", ref _PLCtoPC_70_0, value);
        }

        //AddressOffset - 71 - Fork1Extend
        private object _PLCtoPC_71_0;
        public object PLCtoPC_71_0
        {
            get => _PLCtoPC_71_0;
            set => Set("PLCtoPC_71_0", ref _PLCtoPC_71_0, value);
        }

        //AddressOffset - 72 - Fork2Extend
        private object _PLCtoPC_72_0;
        public object PLCtoPC_72_0
        {
            get => _PLCtoPC_72_0;
            set => Set("PLCtoPC_72_0", ref _PLCtoPC_72_0, value);
        }

        //AddressOffset - 73 - Fork1Error
        private object _PLCtoPC_73_0;
        public object PLCtoPC_73_0
        {
            get => _PLCtoPC_73_0;
            set => Set("PLCtoPC_73_0", ref _PLCtoPC_73_0, value);
        }

        //AddressOffset - 74 - Fork2Error
        private object _PLCtoPC_74_0;
        public object PLCtoPC_74_0
        {
            get => _PLCtoPC_74_0;
            set => Set("PLCtoPC_74_0", ref _PLCtoPC_74_0, value);
        }

        //AddressOffset - 75 - CVForking
        private object _PLCtoPC_75_0;
        public object PLCtoPC_75_0
        {
            get => _PLCtoPC_75_0;
            set => Set("PLCtoPC_75_0", ref _PLCtoPC_75_0, value);
        }

        //AddressOffset - 76 - 75 초과시 사용될 Reserved
        private object _PLCtoPC_76_0;
        public object PLCtoPC_76_0
        {
            get => _PLCtoPC_76_0;
            set => Set("PLCtoPC_76_0", ref _PLCtoPC_76_0, value);
        }

        //AddressOffset - 77 - XAxis
        private object _PLCtoPC_77_0;
        public object PLCtoPC_77_0
        {
            get => _PLCtoPC_77_0;
            set => Set("PLCtoPC_77_0", ref _PLCtoPC_77_0, value);
        }

        //AddressOffset - 79 - FireJobCancel
        private object _PLCtoPC_79_0;
        public object PLCtoPC_79_0
        {
            get => _PLCtoPC_79_0;
            set => Set("PLCtoPC_79_0", ref _PLCtoPC_79_0, value);
        }

        //AddressOffset - 80 - ZAxis
        private object _PLCtoPC_80_0;
        public object PLCtoPC_80_0
        {
            get => _PLCtoPC_80_0;
            set => Set("PLCtoPC_80_0", ref _PLCtoPC_80_0, value);
        }

        //AddressOffset - 82 - ForkAxis
        private object _PLCtoPC_82_0;
        public object PLCtoPC_82_0
        {
            get => _PLCtoPC_82_0;
            set => Set("PLCtoPC_82_0", ref _PLCtoPC_82_0, value);
        }

        //AddressOffset - 84 - CurrentBank
        private object _PLCtoPC_84_0;
        public object PLCtoPC_84_0
        {
            get => _PLCtoPC_84_0;
            set => Set("PLCtoPC_84_0", ref _PLCtoPC_84_0, value);
        }

        //AddressOffset - 85 - CurrentBay
        private object _PLCtoPC_85_0;
        public object PLCtoPC_85_0
        {
            get => _PLCtoPC_85_0;
            set => Set("PLCtoPC_85_0", ref _PLCtoPC_85_0, value);
        }

        //AddressOffset - 86 - CurrentLevel
        private object _PLCtoPC_86_0;
        public object PLCtoPC_86_0
        {
            get => _PLCtoPC_86_0;
            set => Set("PLCtoPC_86_0", ref _PLCtoPC_86_0, value);
        }

        //AddressOffset - 87 - CurrentWorkPlace
        private object _PLCtoPC_87_0;
        public object PLCtoPC_87_0
        {
            get => _PLCtoPC_87_0;
            set => Set("PLCtoPC_87_0", ref _PLCtoPC_87_0, value);
        }

        //AddressOffset - 88 - XVibrationData
        private object _PLCtoPC_88_0;
        public object PLCtoPC_88_0
        {
            get => _PLCtoPC_88_0;
            set => Set("PLCtoPC_88_0", ref _PLCtoPC_88_0, value);
        }

        //AddressOffset - 89 - ZVibrationData
        private object _PLCtoPC_89_0;
        public object PLCtoPC_89_0
        {
            get => _PLCtoPC_89_0;
            set => Set("PLCtoPC_89_0", ref _PLCtoPC_89_0, value);
        }

        //AddressOffset - 90 - Front Double Storage
        private object _PLCtoPC_90_0;
        public object PLCtoPC_90_0
        {
            get => _PLCtoPC_90_0;
            set => Set("PLCtoPC_90_0", ref _PLCtoPC_90_0, value);
        }

        //AddressOffset - 90 - Front Double Storage
        private object _PLCtoPC_90_1;
        public object PLCtoPC_90_1
        {
            get => _PLCtoPC_90_1;
            set => Set("PLCtoPC_90_1", ref _PLCtoPC_90_1, value);
        }
        #endregion

        #region PCtoPLC
        //AddressOffset - 0 - Fork1 CarrierID
        private object _PCtoPLC_0_0;
        public object PCtoPLC_0_0
        {
            get => _PCtoPLC_0_0;
            set => Set("PCtoPLC_0_0", ref _PCtoPLC_0_0, value);
        }

        //AddressOffset - 20 - Fork2 CarrierID
        private object _PCtoPLC_20_0;
        public object PCtoPLC_20_0
        {
            get => _PCtoPLC_20_0;
            set => Set("PCtoPLC_20_0", ref _PCtoPLC_20_0, value);
        }

        //AddressOffset - 40 - Reserve
        private object _PCtoPLC_40_0;
        public object PCtoPLC_40_0
        {
            get => _PCtoPLC_40_0;
            set => Set("PCtoPLC_40_0", ref _PCtoPLC_40_0, value);
        }

        //AddressOffset - 41 - Reserve
        private object _PCtoPLC_41_0;
        public object PCtoPLC_41_0
        {
            get => _PCtoPLC_41_0;
            set => Set("PCtoPLC_41_0", ref _PCtoPLC_41_0, value);
        }

        //AddressOffset - 42 - JobType
        private object _PCtoPLC_42_0;
        public object PCtoPLC_42_0
        {
            get => _PCtoPLC_42_0;
            set => Set("PCtoPLC_42_0", ref _PCtoPLC_42_0, value);
        }

        //AddressOffset - 43 - Fork1CommandNumber
        private object _PCtoPLC_43_0;
        public object PCtoPLC_43_0
        {
            get => _PCtoPLC_43_0;
            set => Set("PCtoPLC_43_0", ref _PCtoPLC_43_0, value);
        }

        //AddressOffset - 44 - Fork1SourceBank
        private object _PCtoPLC_44_0;
        public object PCtoPLC_44_0
        {
            get => _PCtoPLC_44_0;
            set => Set("PCtoPLC_44_0", ref _PCtoPLC_44_0, value);
        }

        //AddressOffset - 45 - Fork1SourceBay
        private object _PCtoPLC_45_0;
        public object PCtoPLC_45_0
        {
            get => _PCtoPLC_45_0;
            set => Set("PCtoPLC_45_0", ref _PCtoPLC_45_0, value);
        }

        //AddressOffset - 46 - Fork1SourceLevel
        private object _PCtoPLC_46_0;
        public object PCtoPLC_46_0
        {
            get => _PCtoPLC_46_0;
            set => Set("PCtoPLC_46_0", ref _PCtoPLC_46_0, value);
        }

        //AddressOffset - 47 - Fork1SourceWorkPlace
        private object _PCtoPLC_47_0;
        public object PCtoPLC_47_0
        {
            get => _PCtoPLC_47_0;
            set => Set("PCtoPLC_47_0", ref _PCtoPLC_47_0, value);
        }

        //AddressOffset - 48 - Fork1DestBank
        private object _PCtoPLC_48_0;
        public object PCtoPLC_48_0
        {
            get => _PCtoPLC_48_0;
            set => Set("PCtoPLC_48_0", ref _PCtoPLC_48_0, value);
        }

        //AddressOffset - 49 - Fork1DestBay
        private object _PCtoPLC_49_0;
        public object PCtoPLC_49_0
        {
            get => _PCtoPLC_49_0;
            set => Set("PCtoPLC_49_0", ref _PCtoPLC_49_0, value);
        }

        //AddressOffset - 50 - Fork1DestLevel
        private object _PCtoPLC_50_0;
        public object PCtoPLC_50_0
        {
            get => _PCtoPLC_50_0;
            set => Set("PCtoPLC_50_0", ref _PCtoPLC_50_0, value);
        }

        //AddressOffset - 51 - Fork1DestWorkPlace
        private object _PCtoPLC_51_0;
        public object PCtoPLC_51_0
        {
            get => _PCtoPLC_51_0;
            set => Set("PCtoPLC_51_0", ref _PCtoPLC_51_0, value);
        }

        //AddressOffset - 52 - CommandUseFork
        private object _PCtoPLC_52_0;
        public object PCtoPLC_52_0
        {
            get => _PCtoPLC_52_0;
            set => Set("PCtoPLC_52_0", ref _PCtoPLC_52_0, value);
        }

        //AddressOffset - 53 - Fork2CommandNumber
        private object _PCtoPLC_53_0;
        public object PCtoPLC_53_0
        {
            get => _PCtoPLC_53_0;
            set => Set("PCtoPLC_53_0", ref _PCtoPLC_53_0, value);
        }

        //AddressOffset - 54 - Fork2SourceBank
        private object _PCtoPLC_54_0;
        public object PCtoPLC_54_0
        {
            get => _PCtoPLC_54_0;
            set => Set("PCtoPLC_54_0", ref _PCtoPLC_54_0, value);
        }

        //AddressOffset - 55 - Fork2SourceBay
        private object _PCtoPLC_55_0;
        public object PCtoPLC_55_0
        {
            get => _PCtoPLC_55_0;
            set => Set("PCtoPLC_55_0", ref _PCtoPLC_55_0, value);
        }

        //AddressOffset - 56 - Fork2SourceLevel
        private object _PCtoPLC_56_0;
        public object PCtoPLC_56_0
        {
            get => _PCtoPLC_56_0;
            set => Set("PCtoPLC__0", ref _PCtoPLC_56_0, value);
        }

        //AddressOffset - 57 - Fork2SourceWorkPlace
        private object _PCtoPLC_57_0;
        public object PCtoPLC_57_0
        {
            get => _PCtoPLC_57_0;
            set => Set("PCtoPLC_57_0", ref _PCtoPLC_57_0, value);
        }

        //AddressOffset - 58 - Fork2DestBank
        private object _PCtoPLC_58_0;
        public object PCtoPLC_58_0
        {
            get => _PCtoPLC_58_0;
            set => Set("PCtoPLC_58_0", ref _PCtoPLC_58_0, value);
        }

        //AddressOffset - 59 - Fork2DestBay
        private object _PCtoPLC_59_0;
        public object PCtoPLC_59_0
        {
            get => _PCtoPLC_59_0;
            set => Set("PCtoPLC_59_0", ref _PCtoPLC_59_0, value);
        }

        //AddressOffset - 60 - Fork2DestLevel
        private object _PCtoPLC_60_0;
        public object PCtoPLC_60_0
        {
            get => _PCtoPLC_60_0;
            set => Set("PCtoPLC_60_0", ref _PCtoPLC_60_0, value);
        }

        //AddressOffset - 61 - Fork2DestWorkPlace
        private object _PCtoPLC_61_0;
        public object PCtoPLC_61_0
        {
            get => _PCtoPLC_61_0;
            set => Set("PCtoPLC_61_0", ref _PCtoPLC_61_0, value);
        }

        //AddressOffset - 62 - CommandAck
        private object _PCtoPLC_62_0;
        public object PCtoPLC_62_0
        {
            get => _PCtoPLC_62_0;
            set => Set("PCtoPLC_62_0", ref _PCtoPLC_62_0, value);
        }

        //AddressOffset - 63 - Reserve
        private object _PCtoPLC_63_0;
        public object PCtoPLC_63_0
        {
            get => _PCtoPLC_63_0;
            set => Set("PCtoPLC_63_0", ref _PCtoPLC_63_0, value);
        }

        //AddressOffset - 64 - RemoteControl
        private object _PCtoPLC_64_0;
        public object PCtoPLC_64_0
        {
            get => _PCtoPLC_64_0;
            set => Set("PCtoPLC_64_0", ref _PCtoPLC_64_0, value);
        }

        //AddressOffset - 64.3 - PC_ErrorReset
        private object _PCtoPLC_64_3;
        public object PCtoPLC_64_3
        {
            get => _PCtoPLC_64_3;
            set => Set("PCtoPLC_64_3", ref _PCtoPLC_64_3, value);
        }

        //AddressOffset - 65 - CimErrorCode
        private object _PCtoPLC_65_0;
        public object PCtoPLC_65_0
        {
            get => _PCtoPLC_65_0;
            set => Set("PCtoPLC_65_0", ref _PCtoPLC_65_0, value);
        }
        #endregion
        #endregion

        public CraneIODetailViewModel(bool IsPlayBack)
        {
            IsPlayBackControl = IsPlayBack;
        }

        public void AbleViewModel(ControlBase selectunit)
        {
            curType = eDataChangeUnitType.eCrane;

            //if (!IsPlayBackControl)
            {
                SelectUnit = selectunit as RMModuleBase;
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
                RMModuleBase rm = SelectUnit as RMModuleBase;
                if (rm is null)
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
                        foreach (var item in rm.PCtoPLC.Values)
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
                        foreach (var item in rm.PLCtoPC.Values)
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
                RMModuleBase rm = SelectUnit as RMModuleBase;
                if (rm is null)
                    return;

                if (SelectItem == null)
                    return;

                //230301 클라이언트에서 io변경 요청대응
                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    GlobalData.Current.protocolManager.Write(rm.ModuleName, rm.PCtoPLC, SelectItem.ItemName, rcvValue);
                }
                else
                {
                    ClientSetProcedure(rm.ModuleName, SelectItem, rcvValue);
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
                RMModuleBase rm = SelectUnit as RMModuleBase;
                if (rm is null)
                    return;

                if (SelectItem == null || PItem == null)
                {
                    return;
                }

                //230301 클라이언트에서 io변경 요청대응
                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    GlobalData.Current.protocolManager.Write(rm.ModuleName, rm.PCtoPLC, PItem.ItemName, bitData);
                }
                else
                {
                    ClientSetProcedure(rm.ModuleName, PItem, bitData.ToString());
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

        private void ParseIOData(eDataChangeProperty changeType, object changeData)
        {
            try
            {
                ConcurrentDictionary<string, PLCDataItem> changeItems = null;
                if (changeType.Equals(eDataChangeProperty.eIO_PLCtoPC))
                {
                    //if (!IsPlayBackControl)
                    {
                        if (SelectUnit is RMModuleBase rm)
                            changeItems = rm.PLCtoPC;
                    }
                }
                else if (changeType.Equals(eDataChangeProperty.eIO_PCtoPLC))
                {
                    //if (!IsPlayBackControl)
                    {
                        if (SelectUnit is RMModuleBase rm)
                            changeItems = rm.PCtoPLC;
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
