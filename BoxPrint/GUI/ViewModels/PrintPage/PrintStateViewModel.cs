using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels.BindingCommand;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.Print;
using Microsoft.Office.Interop.Excel;
using PLCProtocol.DataClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ViewModels.PrintPage
{
    public class PrintStateViewModel : ViewModelBase
    {


        private bool RefreshCheckPass = false;//SuHwan_20230207 : 피엘씨 쓰기 추가
        private object SelectUnitLock = new object();
        private DateTime ViewInitTime = DateTime.Now;
        private eClientProcedureUnitType procedureUnitType;     //230207 추가 [ServerClient]
        public bool LoginState = false;

        #region Binding Item
        #region Data, Checked Binding

        //private CV_BaseModule module = null;
        private INK_SQUID print;
        
        private string _ViewTitle;
        public string ViewTitle
        {
            get { return _ViewTitle; }
            set => Set("ViewTitle", ref _ViewTitle, value);
        }
        private string _Connection;
        public string Connection
        {
            get { return _Connection; }
            set => Set("Connection", ref _Connection, value);
        }

        private string _ConnectCheck;
        public string ConnectCheck
        {
            get { return _ConnectCheck; }
            set => Set("ConnectCheck", ref _ConnectCheck, value);
        }

        private string _PrintName;
        public string PrintName
        {
            get { return _PrintName; }
            set => Set("PrintName", ref _PrintName, value);
        }
        private string _IP;
        public string IP
        {
            get { return _IP; }
            set => Set("IP", ref _IP, value);
        }
        private string _Port;
        public string Port
        {
            get { return _Port; }
            set => Set("Port", ref _Port, value);
        }

        protected int _UIFontSize_Large = 14;  //큰폰트
        public int UIFontSize_Large
        {
            get => _UIFontSize_Large;
            set => Set("UIFontSize_Large", ref _UIFontSize_Large, value);
        }
        protected int _UIFontSize_Medium = 12; //중간폰트
        public int UIFontSize_Medium
        {
            get => _UIFontSize_Medium;
            set => Set("UIFontSize_Medium", ref _UIFontSize_Medium, value);
        }
        protected int _UIFontSize_Small = 10;  //작은폰트
        public int UIFontSize_Small
        {
            get => _UIFontSize_Small;
            set => Set("UIFontSize_Small", ref _UIFontSize_Small, value);
        }

        protected double _BodyTextSize = 12;
        public double BodyTextSize
        {
            get => _BodyTextSize;
            set => Set("BodyTextSize", ref _BodyTextSize, value);
        }

        //RefreshChecked
        private bool _RefreshChecked;
        public bool RefreshChecked
        {
            get => _RefreshChecked;
            set => Set("RefreshChecked", ref _RefreshChecked, value);
        }

        #endregion

        #region Command Button
        //221230 HHJ SCS 개선
        public ICommand ButtonCommand { get; private set; }
        //230102 HHJ SCS 개선
    
        private eUnitCommandProperty _EnableState;
        public eUnitCommandProperty EnableState
        {
            get => _EnableState;
            set
            {
                EnableContent = TranslationManager.Instance.Translate(value.ToString()).ToString();
                Set("EnableState", ref _EnableState, value);
            }
        }
        private string _EnableContent;
        public string EnableContent
        {
            get => _EnableContent;
            set => Set("EnableContent", ref _EnableContent, value);
        }

        private bool _EnableBTNommand;
        public bool EnableBTNCommand
        {
            get => _EnableBTNommand;
            set => Set("EnableBTNCommand", ref _EnableBTNommand, value);
        }

        private bool _EnableStateCommand;
        public bool EnableStateCommand
        {
            get => _EnableStateCommand;
            set => Set("EnableKeyInCommand", ref _EnableStateCommand, value);
        }

        #endregion


        protected Visibility _CommandButtonVisible = Visibility.Visible;
        public Visibility CommandButtonVisible
        {
            get => _CommandButtonVisible;
            set => Set("CommandButtonVisible", ref _CommandButtonVisible, value);
        }
        #endregion


        public PrintStateViewModel()
        {

            //미사용일때는 Update 방지를 위해 Checked 막아준다.
            RefreshChecked = true;


            ButtonCommand = new BindingDelegateCommand<string>(ButtonCommandAction);      //221230 HHJ SCS 개선
            //UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }

        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            EnableContent = TranslationManager.Instance.Translate(EnableState.ToString()).ToString();
        }

        public void SetPrintModule(INK_SQUID p)
        {
            //module = CVModule;
            print = p;

            EnableBTNCommand = true;
            EnableStateCommand = true;
        }

        public INK_SQUID GetPrintModule(int id)
        {
            if (print != null)
                return print;
            else
                return null; 
        }

        public void DisableViewmodel()
        {
            //미사용일때는 Update 방지를 위해 Checked 막아준다.
            RefreshChecked = false;
        }

        protected override void ViewModelTimer()
        {
            while (true)
            {
                Thread.Sleep(500);

                if (CloseThread) return;

                if (!RefreshChecked)
                {
                    if (RefreshCheckPass)//패스로 한번만 지나가서 데이타 갱신
                        RefreshCheckPass = false;
                    else
                        continue;
                }
                try
                {
                    lock (SelectUnitLock)
                    {
                        
                        {
                            //var unit = module.CVUnitModule as INK_SQUID;
                            if (print != null)
                            {
                                Connection = print.CheckConnection().ToString();
                                ConnectCheck = print.IsFirstConnectedRecived.ToString();
                                this.PrintName = print.ModuleName;
                                this.IP = print.IPAddress;
                                this.Port = print.PortNumber;
                            }

                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        //221230 HHJ SCS 개선
        private void ButtonCommandAction(string cmd)
        {
            string buttonname = string.Empty;
            bool bcommand = true;

            try
            {
                //INK_SQUID um = GetUNIT_Module(1);

                MessageBoxPopupView msgbox = null;
                string msg = string.Empty;
                CustomMessageBoxResult mBoxResult = null;

                if (print == null)
                    return;

                //if (module == null)
                //    return;

                //if (!module.CheckRFID_Connection())
                //{

                //    msg = string.Format(TranslationManager.Instance.Translate("Conveyor") + "[{0}]\n" +
                //                               "PrintName DisConnect" + "?",
                //                               um.ControlName);
                //    //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                //    msgbox = new MessageBoxPopupView(msg, "컨베이어 BCR요청 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                //        um.ControlName, "", "", true);
                //    return;
                //}

                ePrintCommand cmdProperty = (ePrintCommand)Enum.Parse(typeof(ePrintCommand), cmd);
                eUnitCommandProperty changeProperty = eUnitCommandProperty.Active;
                switch (cmdProperty)
                {
                    case ePrintCommand.ReadAutoDataState:
                        buttonname = "ReadAutoDataState";

                        LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", print.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                            print.ControlName, buttonname);

                        msg = string.Format(TranslationManager.Instance.Translate("Conveyor") + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("BCR Read").ToString() + "?",
                                                print.ControlName);
                        //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        msgbox = new MessageBoxPopupView(msg, "컨베이어 BCR요청 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                            print.ControlName, "", "", true);

                        mBoxResult = msgbox.ShowResult();

                        if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        {
                            bool bcrresult = true;
                            GlobalData.Current.PrinterMng.GetPrintModule().MakeDataMessage(ePrintCommand.ReadAutoDataState.ToString());

                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("BCR Result").ToString() + "\n{1}",
                                                print.ControlName, bcrresult);
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "컨베이어 BCR완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                print.ControlName, "BCR Result", bcrresult.ToString(), true);
                            
                        }
                        break;
                }

                RefreshCheckPass = true;
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public void SetData()
        {
            Connection      = print.CheckConnection().ToString();
            PrintName       = print.ModuleName;
            IP              = print.IPAddress;
            Port            = print.PortNumber;
            ConnectCheck    = print.IsFirstConnectedRecived.ToString();
        }

        public void CloseView()
        {
            CloseThread = true;
            viewModelthread.Join();
        }
    }
}
