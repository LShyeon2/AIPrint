using BoxPrint.DataList;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace BoxPrint.GUI.UIControls
{
    public class UIControlBase : Control
    {
        protected bool m_isFirstTimeLoaded = true;

        public UIControlBase()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                InitDesignTimeValue();
            }
            else
            {
                Loaded += ControlBase_Loaded;
            }
        }

        protected void Control_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OnDataContextChange(sender, e);
        }

        /// <summary>
        /// Base는 Override한 Class에서 항시 호출되어야한다.
        /// </summary>
        protected virtual void OnDataContextChange(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetBinding(e.NewValue);
        }

        protected virtual void OnFirstTimeLoaded()
        {
            SetBinding(DataContext);
        }

        protected virtual void InitDesignTimeValue() { }

        protected virtual void SetBinding(object data) { }

        protected virtual void ControlBase_Loaded(object sender, RoutedEventArgs e) { }

        #region DependencyProperty Collection
        public static readonly DependencyProperty UnitNameProperty =
            DependencyProperty.Register("UnitName", typeof(string), typeof(UIControlBase), new PropertyMetadata(string.Empty));
        public string UnitName
        {
            get { return (string)GetValue(UnitNameProperty); }
            set { SetValue(UnitNameProperty, value); }
        }

        //221129 YSW Layout RM UnitName 표시
        public static readonly DependencyProperty ControlNameProperty =
            DependencyProperty.Register("ControlName", typeof(string), typeof(UIControlBase), new PropertyMetadata(string.Empty));
        public string ControlName
        {
            get { return (string)GetValue(ControlNameProperty); }
            set { SetValue(ControlNameProperty, value); }
        }

        //221221 YSW TextDegree : RM ControlName Text Angle
        public static readonly DependencyProperty TextDegreeProperty =
            DependencyProperty.Register("TextDegree", typeof(double), typeof(UIControlBase), new PropertyMetadata((double)0));
        public double TextDegree
        {
            get { return (double)GetValue(TextDegreeProperty); }
            set { SetValue(TextDegreeProperty, value); }
        }

        //221130 YSW 포트에 포트 경로 번호입력 TEST
        public static readonly DependencyProperty TrackIDProperty =
            DependencyProperty.Register("TrackID", typeof(string), typeof(UIControlBase), new PropertyMetadata(string.Empty));
        public string TrackID
        {
            get { return (string)GetValue(TrackIDProperty); }
            set { SetValue(TrackIDProperty, value); }
        }

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(UIControlBase), new PropertyMetadata(string.Empty));
        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }
        public static readonly DependencyProperty LastLoadedTimeProperty =
            DependencyProperty.Register("LastLoadedTime", typeof(string), typeof(UIControlBase), new PropertyMetadata(string.Empty));
        public string LastLoadedTime
        {
            get { return (string)GetValue(LastLoadedTimeProperty); }
            set { SetValue(LastLoadedTimeProperty, value); }
        }

        //220607 조숭진 ObservableCollection -> ObservableList 변경
        public static readonly DependencyProperty SlotListProperty =
            DependencyProperty.Register("SlotList", typeof(ObservableList<Slot>), typeof(UIControlBase), new PropertyMetadata(null, SlotListPropertyChangedCallback));
        public ObservableList<Slot> SlotList
        {
            get { return (ObservableList<Slot>)GetValue(SlotListProperty); }
            set { SetValue(SlotListProperty, value); }
        }
        private static void SlotListPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        public static readonly DependencyProperty CraneArmStateProperty =
            DependencyProperty.Register("CraneArmState", typeof(eCraneArmState), typeof(UIControlBase), new PropertyMetadata(eCraneArmState.Center));
        public eCraneArmState CraneArmState
        {
            get { return (eCraneArmState)GetValue(CraneArmStateProperty); }
            set { SetValue(CraneArmStateProperty, value); }
        }
        public static readonly DependencyProperty ShelfTypeProperty =
            DependencyProperty.Register("ShelfType", typeof(eShelfType), typeof(UIControlBase), new PropertyMetadata(eShelfType.Unknown));
        public eShelfType ShelfType
        {
            get { return (eShelfType)GetValue(ShelfTypeProperty); }
            set { SetValue(ShelfTypeProperty, value); }
        }
        public static readonly DependencyProperty DeadZoneProperty =
            DependencyProperty.Register("DeadZone", typeof(bool), typeof(UIControlBase), new PropertyMetadata(true));
        public bool DeadZone
        {
            get { return (bool)GetValue(DeadZoneProperty); }
            set { SetValue(DeadZoneProperty, value); }
        }
        public static readonly DependencyProperty ShelfBusyRmProperty =
            DependencyProperty.Register("ShelfBusyRm", typeof(eShelfBusyRm), typeof(UIControlBase), new PropertyMetadata(eShelfBusyRm.Unknown));
        public eShelfBusyRm ShelfBusyRm
        {
            get { return (eShelfBusyRm)GetValue(ShelfBusyRmProperty); }
            set { SetValue(ShelfBusyRmProperty, value); }
        }
        public static readonly DependencyProperty ShelfEnableProperty =
            DependencyProperty.Register("SHELFUSE", typeof(bool), typeof(UIControlBase), new PropertyMetadata(true));
        public bool ShelfEnable
        {
            get { return (bool)GetValue(ShelfEnableProperty); }
            set { SetValue(ShelfEnableProperty, value); }
        }


        //220609 HHJ SCS 개선     //- Shelf UIControl 변경
        public static readonly DependencyProperty ShelfStatusProperty =
            DependencyProperty.Register("ShelfStatus", typeof(eShelfStatus), typeof(UIControlBase), new PropertyMetadata(eShelfStatus.EMPTY));
        public eShelfStatus ShelfStatus
        {
            get { return (eShelfStatus)GetValue(ShelfStatusProperty); }
            set { SetValue(ShelfStatusProperty, value); }
        }
        //220610 HHJ SCS 개선     //- Crane UIControl 변경
        public static readonly DependencyProperty CraneStateProperty =
            DependencyProperty.Register("CraneState", typeof(eCraneUIState), typeof(UIControlBase), new PropertyMetadata(eCraneUIState.OFFLINE));
        public eCraneUIState CraneState
        {
            get { return (eCraneUIState)GetValue(CraneStateProperty); }
            set { SetValue(CraneStateProperty, value); }
        }

        //241001 HDK Crane 작업가능상태 표시 개선
        public static readonly DependencyProperty CraneSCModeProperty =
            DependencyProperty.Register("CraneSCStatus", typeof(eCraneSCMode), typeof(UIControlBase), new PropertyMetadata(eCraneSCMode.OFFLINE));
        public eCraneSCMode CraneSCStatus
        {
            get { return (eCraneSCMode)GetValue(CraneSCModeProperty); }
            set { SetValue(CraneSCModeProperty, value); }
        }

        public static readonly DependencyProperty PortInOutTypeProperty =
            DependencyProperty.Register("PortInOutType", typeof(ePortInOutType), typeof(UIControlBase), new PropertyMetadata(ePortInOutType.INPUT));
        public ePortInOutType PortInOutType
        {
            get { return (ePortInOutType)GetValue(PortInOutTypeProperty); }
            set { SetValue(PortInOutTypeProperty, value); }
        }

        //220805 조숭진
        public static readonly DependencyProperty CVEnableProperty =
            DependencyProperty.Register("CVUSE", typeof(bool), typeof(UIControlBase), new PropertyMetadata(true));
        //220902 HHJ SCS 개선     //- CV Able, Disable UI 반응 추가
        //public bool CVEnable
        public bool CVUSE
        {
            get { return (bool)GetValue(CVEnableProperty); }
            set { SetValue(CVEnableProperty, value); }
        }
        //220919 HHJ SCS 개선     //- ForkAxisPosition Biding Item 변경
        //220914 HHJ SCS 개선     //- RM Fork Position UI 연동
        public static readonly DependencyProperty ForkAxisPositionProperty =
            DependencyProperty.Register("ForkAxisPosition", typeof(decimal), typeof(UIControlBase), new PropertyMetadata((decimal)0));
        public decimal ForkAxisPosition
        {
            get { return (decimal)GetValue(ForkAxisPositionProperty); }
            set { SetValue(ForkAxisPositionProperty, value); }
        }
        //230118 HHJ SCS 개선
        public static readonly DependencyProperty SelectorProperty =
            DependencyProperty.Register("Selector", typeof(bool), typeof(UIControlBase), new PropertyMetadata(false));
        public bool Selector
        {
            get { return (bool)GetValue(SelectorProperty); }
            set { SetValue(SelectorProperty, value); }
        }
        public static readonly DependencyProperty SelectZIndexProperty =
            DependencyProperty.Register("SelectZIndex", typeof(int), typeof(UIControlBase), new PropertyMetadata(0));
        public int SelectZIndex
        {
            get { return (int)GetValue(SelectZIndexProperty); }
            set { SetValue(SelectZIndexProperty, value); }
        }
        //230214 HHJ SCS 개선
        public static readonly DependencyProperty CVWayProperty =
            DependencyProperty.Register("CVWay", typeof(eCVWay), typeof(UIControlBase), new PropertyMetadata(eCVWay.BottomToTop));
        public eCVWay CVWay
        {
            get { return (eCVWay)GetValue(CVWayProperty); }
            set { SetValue(CVWayProperty, value); }
        }
        //230217 HHJ SCS 개선     //기존 CVModuleType Binding 처리할 수 있도록 변경      //Default는 Plain
        public static readonly DependencyProperty CVModuleTypeProperty =
            DependencyProperty.Register("CVModuleType", typeof(eCVType), typeof(UIControlBase), new PropertyMetadata(eCVType.Plain));
        public eCVType CVModuleType
        {
            get { return (eCVType)GetValue(CVModuleTypeProperty); }
            set { SetValue(CVModuleTypeProperty, value); }
        }
        //230217 HHJ SCS 개선     //CV UI State 관련 추가     //Default는 Manual
        public static readonly DependencyProperty ConveyorUIStateProperty =
            DependencyProperty.Register("ConveyorUIState", typeof(eConveyorUIState), typeof(UIControlBase), new PropertyMetadata(eConveyorUIState.Manual));
        public eConveyorUIState ConveyorUIState
        {
            get { return (eConveyorUIState)GetValue(ConveyorUIStateProperty); }
            set { SetValue(ConveyorUIStateProperty, value); }
        }
        public static readonly DependencyProperty IsTrackPauseProperty =
            DependencyProperty.Register("IsTrackPause", typeof(bool), typeof(UIControlBase), new PropertyMetadata(false));
        public bool IsTrackPause
        {
            get { return (bool)GetValue(IsTrackPauseProperty); }
            set { SetValue(IsTrackPauseProperty, value); }
        }
        public static readonly DependencyProperty PortAccessModeProperty =
            DependencyProperty.Register("PortAccessMode", typeof(ePortAceessMode), typeof(UIControlBase), new PropertyMetadata(ePortAceessMode.AUTO));
        public ePortAceessMode PortAccessMode
        {
            get { return (ePortAceessMode)GetValue(PortAccessModeProperty); }
            set { SetValue(PortAccessModeProperty, value); }
        }
        //230307 HHJ SCS 개선
        public static readonly DependencyProperty IsPlayBackProperty =
            DependencyProperty.Register("IsPlayBack", typeof(bool), typeof(UIControlBase), new PropertyMetadata(false));
        public bool IsPlayBack
        {
            get { return (bool)GetValue(IsPlayBackProperty); }
            set { SetValue(IsPlayBackProperty, value); }
        }
        public static readonly DependencyProperty LayOutAngleProperty =
            DependencyProperty.Register("LayOutAngle", typeof(eLayOutAngle), typeof(UIControlBase), new PropertyMetadata(eLayOutAngle.eAngle0));
        public eLayOutAngle LayOutAngle
        {
            get { return (eLayOutAngle)GetValue(LayOutAngleProperty); }
            set { SetValue(LayOutAngleProperty, value); }
        }

        //230405 HHJ SCS 개선     //- Memo 기능 추가
        public static readonly DependencyProperty ShelfMemoProperty =
            DependencyProperty.Register("ShelfMemo", typeof(string), typeof(UIControlBase), new PropertyMetadata(string.Empty));
        public string ShelfMemo
        {
            get { return (string)GetValue(ShelfMemoProperty); }
            set { SetValue(ShelfMemoProperty, value); }
        }
        public static readonly DependencyProperty MemoPathSizeProperty =
            DependencyProperty.Register("MemoPathSize", typeof(double), typeof(UIControlBase), new PropertyMetadata(double.NaN));
        public double MemoPathSize
        {
            get { return (double)GetValue(MemoPathSizeProperty); }
            set { SetValue(MemoPathSizeProperty, value); }
        }
        //230517 HHJ SCS 개선     //- BCR Path 변경
        public static readonly DependencyProperty PortBCRStateProperty =
            DependencyProperty.Register("PortBCRState", typeof(eBCRState), typeof(UIControlBase), new PropertyMetadata(eBCRState.NoBCR));
        public eBCRState PortBCRState
        {
            get { return (eBCRState)GetValue(PortBCRStateProperty); }
            set { SetValue(PortBCRStateProperty, value); }
        }

        public static readonly DependencyProperty CraneSC_StateProperty =
            DependencyProperty.Register("CraneSC_State", typeof(eCraneSCState), typeof(UIControlBase), new PropertyMetadata(eCraneSCState.OFFLINE));
        public eCraneSCState CraneSC_State
        {
            get { return (eCraneSCState)GetValue(CraneSC_StateProperty); }
            set { SetValue(CraneSC_StateProperty, value); }
        }
        //231101 HHJ Shelf NG State 추가
        public static readonly DependencyProperty ShelfNGStateProperty =
            DependencyProperty.Register("ShelfNGState", typeof(int), typeof(UIControlBase), new PropertyMetadata(0));
        public int ShelfNGState
        {
            get { return (int)GetValue(ShelfNGStateProperty); }
            set { SetValue(ShelfNGStateProperty, value); }
        }
        //241030 HoN 화재 관련 추가 수정        -. 화재수조 가용조건 변경 -> 화재수조는 적재조건, 완료조건 Carrier Sensor 무시 요청
        public static readonly DependencyProperty WaterPoolExistProperty =
            DependencyProperty.Register("WaterPoolExist", typeof(bool), typeof(UIControlBase), new PropertyMetadata(false));
        public bool WaterPoolExist
        {
            get { return (bool)GetValue(WaterPoolExistProperty); }
            set { SetValue(WaterPoolExistProperty, value); }
        }
        #endregion
    }

    public class ControlBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Binding ControlName
        /// </summary>
        [XmlIgnore]
        private string _ControlName = string.Empty;
        [XmlIgnore]
        public string ControlName
        {
            get
            {
                return _ControlName;
            }
            protected set
            {
                if (_ControlName == value) return;

                _ControlName = value;
                RaisePropertyChanged("ControlName");
            }
        }
        /// <summary>
        /// Capacity
        /// </summary>
        [XmlIgnore]
        protected int _Capacity;
        [XmlIgnore]
        public int Capacity
        {
            get { return _Capacity; }
            set
            {
                if (_Capacity == value) return;

                _Capacity = value;
                RaisePropertyChanged("Capacity");
            }
        }

        //220607 조숭진 ObservableCollection -> ObservableList 변경
        /// <summary>
        /// SlotList
        /// </summary>
        [XmlIgnore]
        private ObservableList<Slot> _SlotList = null;
        [XmlIgnore]
        public ObservableList<Slot> SlotList
        {
            get
            {
                return _SlotList;
            }
        }
        public Slot this[int islot]     //0Base로 구성한다.
        {
            get
            {
                if (_SlotList == null      //SlotList가 없는데 호출하면 에러
                    || islot > (_Capacity - 1)  //islot은 총 슬랏의 갯수 - 1보다 크면 에러 (0 Base)
                    || islot < 0)               //islot 0보다 적으면(음수) 에러
                {
                    throw new ArgumentOutOfRangeException("SlotNum Error. SlotID : " + islot.ToString());
                }

                return _SlotList.Where(r => r.SlotNum == islot).FirstOrDefault();
            }
        }
        public ControlBase()
        {

        }
        public ControlBase(string controlname, int capacity)
        {
            ControlName = controlname;
            Capacity = capacity;

            //캐파 만큼 슬랏을 생성해준다.
            _SlotList = new ObservableList<Slot>();       //220607 조숭진 ObservableCollection -> ObservableList 변경
            Slot slot = null;
            //0Base로 구성한다.
            for (int i = 0; i < Capacity; i++)
            {
                slot = new Slot(i, controlname);
                slot.SlotStateChanged += slot_SlotStateChanged;
                _SlotList.Add(slot);
            }
        }
        protected Slot DefaultSlot
        {
            get
            {
                return _SlotList[0];
            }
        }
        public CarrierItem InSlotCarrier
        {
            get
            {
                if (DefaultSlot.MaterialExist)
                {
                    return CarrierStorage.Instance.GetCarrierItem(DefaultSlot.MaterialName);
                }
                else
                {
                    return null;
                }
            }
        }

        //230118 HHJ SCS 개선
        protected int _SelectZIndex;
        public int SelectZIndex
        {
            get => _SelectZIndex;
            set
            {
                _SelectZIndex = value;
                RaisePropertyChanged("SelectZIndex");
            }
        }
        protected bool _Selector;
        public bool Selector
        {
            get => _Selector;
            set
            {
                _Selector = value;

                if (value)
                    SelectZIndex = 9999;
                else
                    SelectZIndex = 0;
                RaisePropertyChanged("Selector");
            }
        }


        //슬랏 상태 변경이 필요할까?
        private void slot_SlotStateChanged(object sender, SlotEventArgs e)
        {

        }
        public virtual int GetUnitServiceState()
        {
            throw new NotImplementedException("GetUnitServiceState() 는 구현되지 않았습니다.");
        }
    }
}
