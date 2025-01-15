using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels;
using System.Windows.Controls;

namespace BoxPrint.GUI.UserControls
{
    /// <summary>
    /// CraneIOView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CraneIOView : UserControl
    {
        //ViewModel은 공통으로 사용한다.
        private CraneIODetailViewModel vm;
        public CraneIOView(bool IsPlayBack)
        {
            InitializeComponent();

            vm = new CraneIODetailViewModel(IsPlayBack);
            DataContext = vm;
        }

        public void AbleControl(ControlBase selectunit)
        {
            vm.AbleViewModel(selectunit);
        }

        public void DisableControl()
        {
            vm.DisableViewmodel();
        }
    }
}
