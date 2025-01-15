using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels;
using System.Windows.Controls;

namespace BoxPrint.GUI.UserControls
{
    /// <summary>
    /// ConveyorIOView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ConveyorIOView : UserControl
    {
        //ViewModel은 공통으로 사용한다.
        private ConveyorIODetailViewModel vm;
        public ConveyorIOView(bool IsPlayBack)
        {
            InitializeComponent();

            vm = new ConveyorIODetailViewModel(IsPlayBack);
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
