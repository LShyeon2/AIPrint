using System.ComponentModel;
using System.Windows.Controls;

namespace BoxPrint.GUI.UserControls
{
    /// <summary>
    /// ControlSystemLegendView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ControlSystemLegendView : UserControl, INotifyPropertyChanged
    {
        //SuHwan_20221226 : [1차 UI검수] 폰트 사이즈 설정
        protected int _UIFontSize_Large = 13;  //큰폰트
        public int UIFontSize_Large
        {
            get => _UIFontSize_Large;
            set
            {
                if (_UIFontSize_Large == value) return;
                _UIFontSize_Large = value;

                RaisePropertyChanged("UIFontSize_Large");
            }
        }

        protected int _UIFontSize_Medium = 11; //중간폰트
        public int UIFontSize_Medium
        {
            get => _UIFontSize_Medium;
            set
            {
                if (_UIFontSize_Medium == value) return;
                _UIFontSize_Medium = value;

                RaisePropertyChanged("UIFontSize_Medium");
            }
        }

        protected int _UIFontSize_Small = 9;  //작은폰트
        public int UIFontSize_Small
        {
            get => _UIFontSize_Small;
            set
            {
                if (_UIFontSize_Small == value) return;
                _UIFontSize_Small = value;

                RaisePropertyChanged("UIFontSize_Small");
            }
        }

        //재산변경 이벤트
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public ControlSystemLegendView()
        {
            InitializeComponent();

            UIFontSize_Large = 14;
            UIFontSize_Medium = 12;
            UIFontSize_Small = 10;
            DataContext = this;
        }
    }
}
