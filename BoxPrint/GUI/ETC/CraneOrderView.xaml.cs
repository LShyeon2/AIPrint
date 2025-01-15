using BoxPrint.GUI.ViewModels;
using BoxPrint.GUI.Views;
using System;
using System.Windows;
namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// CraneOrderView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CraneOrderView : Window
    {
        private CraneOrderViewModel vm { get; set; }
        public CraneOrderView(string craneID, LayOutView layout)
        {
            InitializeComponent();

            vm = new CraneOrderViewModel(craneID, layout);
            DataContext = vm;

            priorityUpDown.Value = priorityUpDown.MinValue;
            priorityUpDown.OnNumericUpdownChange += priorityUpDownChanged;

            cbox_MoveOnly.Checked += Cbox_MoveOnly_Checked;
            cbox_MoveOnly.Unchecked += Cbox_MoveOnly_Unchecked;
        }

        private void Cbox_MoveOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                vm.MoveOnly = false;
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        private void Cbox_MoveOnly_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                vm.MoveOnly = true;
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        private void priorityUpDownChanged(int ivalue)
        {
            try
            {
                vm.Priority = ivalue;
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            vm.DisableVoieModel();
        }

        public bool ChangeRM(string craneID)
        {
            try
            {
                if(vm.CraneID == craneID) //231115 RGJ 같은 크레인이 목적지를 크레인으로 선택한것.
                {
                    return true;
                }
                vm.ChangeRM(craneID);
                //231025 HHJ ManualOrder Open 상태에서 Crane 선택시 RMChange 진행
                if (vm.InitCommand.CanExecute(null))
                    vm.InitCommand.Execute(null);
            }
            catch (Exception ex)
            {
                _ = ex;
                return false;
            }

            return true;
        }
        //NumericUpDown의 Value가 Binding처리가 되지않아서 이부분만 Click Event사용해준다.
        private void InitClick(object sender, RoutedEventArgs e)
        {
            try
            {
                priorityUpDown.Value = priorityUpDown.MinValue;
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }
}
