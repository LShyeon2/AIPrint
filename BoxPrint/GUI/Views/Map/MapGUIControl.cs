using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BoxPrint.GUI.Views.Map
{
    public class MapGUIControl : Button, INotifyPropertyChanged
    {
        public delegate void EventHandler_PopupMouse(UIElement ui);
        public static event EventHandler_PopupMouse _EventCall_MapPopupMouseEnter;
        public static event EventHandler_PopupMouse _EventCall_MapPopupMouseLeave;
        public static event EventHandler_PopupMouse _EventCall_MapPopupMouseDown;

        //설비 넘버(목적지 경로 기준)
        private int _indexNumber;
        public int IndexNumber
        {
            get { return _indexNumber; }
            set
            {
                if (_indexNumber != value)
                {
                    _indexNumber = value;
                    OnPropertyChanged("IndexNumber");
                }
            }
        }

        //221229 YSW Map View안에 각 SCS의 Tooltip에 IP 항목 추가
        private string _SCSIP;
        public string SCSIP
        {
            get { return _SCSIP; }
            set
            {
                if (_SCSIP != value)
                {
                    _SCSIP = value;
                    OnPropertyChanged("SCSIP");
                }
            }
        }

        //EQP ID(DB 검색용)
        private string _EQPID;
        public string EQPID
        {
            get { return _EQPID; }
            set
            {
                if (_EQPID != value)
                {
                    _EQPID = value;
                    OnPropertyChanged("EQPID");
                }
            }
        }

        //표시되는 이름
        private string _displayName;
        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged("DisplayName");
                }
            }
        }

        //폰트 사이즈
        private int _displayNameFontSize;
        public int DisplayNameFontSize
        {
            get { return _displayNameFontSize; }
            set
            {
                if (_displayNameFontSize != value)
                {
                    _displayNameFontSize = value;
                    OnPropertyChanged("DisplayNameFontSize");
                }
            }
        }

        //테그
        private string _controlTag;
        public string ControlTag
        {
            get { return _controlTag; }
            set
            {
                if (_controlTag != value)
                {
                    _controlTag = value;
                    OnPropertyChanged("ControlTag");
                }
            }
        }

        //애니메이션 동작 
        private bool _isBeginStoryboard;
        public bool isBeginStoryboard
        {
            get { return _isBeginStoryboard; }
            set
            {
                if (_isBeginStoryboard != value)
                {
                    _isBeginStoryboard = value;
                    OnPropertyChanged("isBeginStoryboard");
                }
            }
        }

        //메인 색상
        private SolidColorBrush _mainColor;
        public SolidColorBrush MainColor
        {
            get { return _mainColor; }
            set
            {
                if (_mainColor != value)
                {
                    _mainColor = value;
                    OnPropertyChanged("MainColor");
                }
            }
        }

        //서브 색상
        private SolidColorBrush _subColor;
        public SolidColorBrush SubColor
        {
            get { return _subColor; }
            set
            {
                if (_subColor != value)
                {
                    _subColor = value;
                    OnPropertyChanged("SubColor");
                }
            }
        }

        //마우스 오버 색상
        private SolidColorBrush _mouseOverColor;
        public SolidColorBrush MouseOverColor
        {
            get { return _mouseOverColor; }
            set
            {
                if (_mouseOverColor != value)
                {
                    _mouseOverColor = value;
                    OnPropertyChanged("MouseOverColor");
                }
            }
        }

        //현재 선택된 RM 표시 
        private bool _isSelect;
        public bool isSelect
        {
            get { return _isSelect; }
            set
            {
                if (!_isSelect.Equals(value))
                {
                    _isSelect = value;
                    OnPropertyChanged("isSelect");
                }
            }
        }

        //현재 Kiosk 설정
        private bool _isSettingKiosk;
        public bool isSettingKiosk
        {
            get { return _isSettingKiosk; }
            set
            {
                if (!_isSettingKiosk.Equals(value))
                {
                    _isSettingKiosk = value;
                    OnPropertyChanged("isSettingKiosk");
                }
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public MapGUIControl()
        {
            //여기서 db에 가져온 데이타를 넣어준다
        }

        #region    마우스 이벤트 모음 ---------->
        //마우스 다운 이벤트
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            //base.OnPreviewMouseUp(e);
            _EventCall_MapPopupMouseDown(this);
        }
        //마우스 엔터 이벤트
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            //base.OnMouseEnter(e);

            _EventCall_MapPopupMouseEnter(this);
        }
        //마우스 리브 이벤트
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            //base.OnMouseLeave(e);

            _EventCall_MapPopupMouseLeave(this);
        }
        //마우스 다운 이벤트
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            //_EventCall_MapPopupMouseDown(this);
        }
        #endregion 마우스 이벤트 모음 <----------

        //재산변경
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

}
