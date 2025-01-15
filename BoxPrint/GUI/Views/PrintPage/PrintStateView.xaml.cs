using BoxPrint.GUI.ETC;
using BoxPrint.GUI.Views.UserPage;
using BoxPrint.Modules.User;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using TranslationByMarkupExtension;
using System.ComponentModel;
using BoxPrint.Log;
using BoxPrint.Modules.Print;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.CVLine;
using BoxPrint.GUI.ViewModels;
using BoxPrint.GUI.ViewModels.PrintPage;

namespace BoxPrint.GUI.Views.PrintPage
{
    /// <summary>
    /// PrintStateView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PrintStateView : Page
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static object objLock = new object();
        //protected virtual void RaisePropertyChanged(string propertyName)
        //{
        //    lock (objLock)
        //    {
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //    }
        //}

        public PrintStateViewModel vm { get; set; }

        //private CV_BaseModule module = null;

        private bool bResult = false;
        private bool bedit = false;

        public PrintStateView(int PrintID)
        {
            InitializeComponent();

            vm = new PrintStateViewModel();

            vm.ViewTitle = TranslationManager.Instance.Translate($"Print{PrintID}").ToString();

            var cvLine = GlobalData.Current.GetModuleByName("T0POC01_CV01") as CVLineModule;
            if (cvLine != null)
            {
                //vm.SetCVModule(cvLine.ModuleList.Where(r => r.ModuleName == "T0POC01_CV01").FirstOrDefault());
                vm.SetPrintModule(GlobalData.Current.PrinterMng.GetPrintModule());

                //var unit = vm.GetUNIT_Module(PrintID);

                vm.SetData();

                //if (unit != null)
                //{
                //    vm.Connection = unit.CheckConnection().ToString();
                //    vm.PrintName = unit.ModuleName;
                //    vm.IP = unit.IPAddress;
                //    vm.Port = unit.PortNumber;
                //    vm.ConnectCheck = unit.IsFirstConnectedRecived.ToString();
                //}
            }

            DataContext = vm;
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                User userdata;
                ResultUserControl userControlResult = null;
                if (sender is Button btn)
                {
                    string buttonname = string.Empty;

                    switch (btn.Tag.ToString())
                    {
                        case "Confirm":
                           
                            break;
                        case "Cancel":
                            bResult = false;
                            if (bedit)
                            {
                                buttonname = "User Edit";
                            }
                            else
                            {
                                buttonname = "User Create";
                            }
                            break;
                    }

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", buttonname, btn.Tag.ToString()),
                        "CLICK", btn.Tag.ToString().ToUpper(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                        buttonname, btn.Tag.ToString());

                    clear();
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }

        private void clear()
        {
        //    UserID_Textbox.Text = null;
        //    Password_Textbox.Text = null;
        //    UserNamge_Textbox.Text = null;
        //    cbbLevel.Text = string.Empty;
        //    cbbUse.Text = string.Empty;
        //    AutoLogoutTime_updown.Value = User.DefalutAutoLogoutTime;
        }

    }

    public class CustomUserCreatenEditResult1
    {
        public bool Result;
        public User ResultUser;
    }
}
