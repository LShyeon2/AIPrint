using BoxPrint.GUI.Windows.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BoxPrint.GUI.Windows.Views
{
    /// <summary>
    /// PLCStateView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PLCStateView : Window
    {
        #region Variable
        private PLCStateViewModel vm;
        #endregion

        public PLCStateView()
        {
            InitializeComponent();

            vm = new PLCStateViewModel();
            DataContext = vm;
        }

        public void DisposeView()
        {
            vm.DisposeViewModel();
        }
    }
}
