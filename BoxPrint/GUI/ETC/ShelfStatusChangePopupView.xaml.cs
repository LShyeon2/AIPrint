using Newtonsoft.Json;
using BoxPrint.DataList;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{
    // 2023 03 30 정인길
    /// <summary>
    /// ShelfStatusChangePopupView 팝업창 생성
    /// </summary>
    public partial class ShelfStatusChangePopupView : Window
    {
        private string baystart;
        private string bayend;
        private string levelstart;
        private string levelend;
        private string banklevel_1;
        private string banklevel_2;
        private int bank_level;


        public int UIFontSize_Medium { get; set; }

        private string CurLanguage;

        private List<string> DisableShelfZoneNameItems = new List<string>();

        /// <summary>
        /// 생성자
        /// </summary>
        public ShelfStatusChangePopupView()
        {
            InitializeComponent();
            UIFontSize_Medium = 15;

            // 생성하면서 체크박스가 체크되어 있는지 확인
            if (checkbox_Bank1.IsChecked == false)
            {
                checkbox_Bank1.IsChecked = true;
            }

            DataContext = this;

            CurLanguage = TranslationManager.Instance.CurrentLanguage.ToString();

            string banktrans = TranslationManager.Instance.Translate("BANK Tag").ToString();
            string banktooltiptrans = TranslationManager.Instance.Translate("Bank 선택").ToString();
            string strMessage = string.Format(banktrans,
                                       GlobalData.Current.FrontBankNum.ToString());
            string strtootip = string.Format(banktooltiptrans,
                                        GlobalData.Current.FrontBankNum.ToString());
            checkbox_Bank1.Content = strMessage;
            checkbox_Bank1.ToolTip = strtootip;
            strMessage = string.Format(banktrans,
                           GlobalData.Current.RearBankNum.ToString());
            strtootip = string.Format(banktooltiptrans,
                            GlobalData.Current.RearBankNum.ToString());
            checkbox_Bank2.Content = strMessage;
            checkbox_Bank2.ToolTip = strtootip;
        }

        /// <summary>
        /// 버튼 클릭 이벤트 모음
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is System.Windows.Controls.Button sendBuffer)
                {
                    baystart = ComboBox_BayStart.Text.ToString();    // bay 시작 값
                    bayend = ComboBox_BayEnd.Text.ToString();      // bay 끝 값
                    levelstart = ComboBox_LevelStart.Text.ToString();  // level 시작 값
                    levelend = ComboBox_LevelEnd.Text.ToString();    // level 끝 값
                    banklevel_1 = checkbox_Bank1.Content.ToString();    // bank 1
                    banklevel_2 = checkbox_Bank2.Content.ToString();    // bank 2
                    string msg = string.Empty;
                    if (checkbox_Bank1.IsChecked == true)
                    {
                        //bank_level = 1;
                        bank_level = GlobalData.Current.FrontBankNum;
                    }
                    else if (checkbox_Bank2.IsChecked == true)
                    {
                        //bank_level = 2;
                        bank_level = GlobalData.Current.RearBankNum;
                    }

                    if (Convert.ToInt32(baystart) > Convert.ToInt32(bayend) || Convert.ToInt32(levelstart) > Convert.ToInt32(levelend)) // start의 값이 end값보다 작아야함 or levelstart의 값이 levelend 값보다 작아야함
                    {
                        MessageBoxPopupView msgbox_item = new MessageBoxPopupView(TranslationManager.Instance.Translate("Bay 또는 Level의 시작 값이 끝 값보다 작아야 합니다").ToString()); // 시작 item의 값이 끝값보다 클시 출력
                        CustomMessageBoxResult mBoxResult_item = msgbox_item.ShowResult();
                    }
                    else if (Convert.ToInt32(baystart) <= Convert.ToInt32(bayend) && Convert.ToInt32(levelstart) <= Convert.ToInt32(levelend)) // baystart의 값이 bayend 값보다 작거나 같아야함 and levelstart의 값이 levelend 값보다 작거나 같아야함
                    {
                        string usemsg = string.Empty;
                        if (sendBuffer.Name == "Enable_Btn")
                        {
                            usemsg = "Enable";
                        }
                        else
                        {
                            usemsg = "Disable";
                        }

                        msg = string.Format(TranslationManager.Instance.Translate("Bank").ToString() + "   :   {0}\n" +
                                            TranslationManager.Instance.Translate("Bay").ToString() + "   :   {1}~{2}\n" +
                                            TranslationManager.Instance.Translate("Level").ToString() + "   :   {3}~{4}\n" +
                                            TranslationManager.Instance.Translate(usemsg).ToString() + "\n" +
                                             TranslationManager.Instance.Translate("변경사항을 적용 시키겠습니까?").ToString(),
                                            bank_level, baystart, bayend, levelstart, levelend);

                        var startshelf = GlobalData.Current.ShelfMgr.GetShelf(bank_level, Convert.ToInt32(baystart), Convert.ToInt32(levelstart));
                        var endshelf = GlobalData.Current.ShelfMgr.GetShelf(bank_level, Convert.ToInt32(bayend), Convert.ToInt32(levelend));
                        string btnname = sendBuffer.Name;

                        MessageBoxPopupView msgbox = new MessageBoxPopupView(msg, "쉘프 구간 데이터 변경 메시지", "", "", MessageBoxButton.YesNo, MessageBoxImage.Question,
                            startshelf.TagName, endshelf.TagName, btnname == "Enable_Btn" ? "Enable" : "Disable", true);
                        //MessageBoxPopupView msgbox = new MessageBoxPopupView(msg, "", TranslationManager.Instance.Translate("변경사항을 적용 시키겠습니까?").ToString(), "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        CustomMessageBoxResult mBoxResult = msgbox.ShowResult(); // 메세지박스 통합

                        // 메세지박스 예를 눌렀을 때
                        if (mBoxResult.Result == MessageBoxResult.Yes)
                        {
                            if (sendBuffer.Name.ToString() == "Enable_Btn") // shelf 사용
                            {
                                //for (int i = Convert.ToInt32(baystart); i <= Convert.ToInt32(bayend); i++)
                                //{
                                //    for (int j = Convert.ToInt32(levelstart); j <= Convert.ToInt32(levelend); j++)
                                //    {
                                //        //쉘프를 받아온다
                                //        var bbl = GlobalData.Current.ShelfMgr.GetShelf(bank_level, i, j); // bank , bay , level

                                //        //1.쉘프를 변경할때 클라이언트인지 서버인지 확인을 한다
                                //        if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                //        {
                                //            //2.클라이언트면 자제적으로 쉘프의 사용을 변경하는게 아니고 디비에 클라이언트 오더에 넣어 서버에서 변경하도록 한다
                                //            GlobalData.Current.DBManager.DbSetProcedureClientReq(
                                //                GlobalData.Current.EQPID,//EQPID, 
                                //                eUnitCommandProperty.Enable.ToString(),//CMDType, Enable
                                //                eClientProcedureUnitType.Shelf.ToString(),//Target, 
                                //                bbl.TagName,//TargetID, 
                                //                string.Empty,//TargetValue, 
                                //                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),//ReqTime, 
                                //                eServerClientType.Client);//Requester
                                //        }
                                //        else
                                //        {
                                //            //3.서버이면 바로 변경하고 디비에 업데이트 한다
                                //            bbl.SHELFUSE = true;
                                //            GlobalData.Current.ShelfMgr.SaveShelfData(bbl);
                                //        }
                                //    }
                                //}
                                ClientShelfStatusCmd(eUnitCommandProperty.Enable.ToString(), Convert.ToInt32(baystart), Convert.ToInt32(bayend), Convert.ToInt32(levelstart), Convert.ToInt32(levelend));

                                msg = string.Format(TranslationManager.Instance.Translate("Bank").ToString() + "   :   {0}\n" +
                                    TranslationManager.Instance.Translate("Bay").ToString() + "   :   {1}~{2}\n" +
                                    TranslationManager.Instance.Translate("Level").ToString() + "   :   {3}~{4}",
                                    bank_level, baystart, bayend, levelstart, levelend);

                                MessageBoxPopupView.Show(msg, "쉘프 구간 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                    startshelf.TagName, endshelf.TagName, btnname == "Enable_Btn" ? "Enable" : "Disable", true);
                            }
                            else if (sendBuffer.Name.ToString() == "Disable_Btn") // shelf 사용x  
                            {
                                //for (int i = Convert.ToInt32(baystart); i <= Convert.ToInt32(bayend); i++)
                                //{
                                //    for (int j = Convert.ToInt32(levelstart); j <= Convert.ToInt32(levelend); j++)
                                //    {
                                //        // 쉘프를 받아온다
                                //        var bbl = GlobalData.Current.ShelfMgr.GetShelf(bank_level, i, j); // bank , bay , level
                                //                                                                          //1.쉘플를 변경할때 클라이언트인지 서버인지 확인을 한다
                                //        if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                //        {
                                //            //2.클라이언트면 자제적으로 쉘프의 사용을 변경하는게 아니고 디비에 클라이언트 오더에 넣어 서버에서 변경하도록 한다
                                //            GlobalData.Current.DBManager.DbSetProcedureClientReq(
                                //                GlobalData.Current.EQPID,//EQPID, 
                                //                eUnitCommandProperty.Disable.ToString(),//CMDType, Disable
                                //                eClientProcedureUnitType.Shelf.ToString(),//Target, 
                                //                bbl.TagName,//TargetID, 
                                //                string.Empty,//TargetValue, 
                                //                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),//ReqTime, 
                                //                eServerClientType.Client);//Requester
                                //        }
                                //        else
                                //        {
                                //            //3.서버이면 바로 변경하고 디비에 업데이트 한다
                                //            bbl.SHELFUSE = false;
                                //            GlobalData.Current.ShelfMgr.SaveShelfData(bbl);
                                //        }
                                //    }
                                //}
                                ClientShelfStatusCmd(eUnitCommandProperty.Disable.ToString(), Convert.ToInt32(baystart), Convert.ToInt32(bayend), Convert.ToInt32(levelstart), Convert.ToInt32(levelend));

                                msg = string.Format(TranslationManager.Instance.Translate("Bank").ToString() + "   :   {0}\n" +
                                    TranslationManager.Instance.Translate("Bay").ToString() + "   :   {1}~{2}\n" +
                                    TranslationManager.Instance.Translate("Level").ToString() + "   :   {3}~{4}",
                                    bank_level, baystart, bayend, levelstart, levelend);

                                MessageBoxPopupView.Show(msg, "쉘프 구간 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                    startshelf.TagName, endshelf.TagName, btnname == "Enable_Btn" ? "Enable" : "Disable", true);
                            }
                        }
                        else if (mBoxResult.Result == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        private async void ClientShelfStatusCmd(string cmd, int baystart, int bayend, int levelstart, int levelend)
        {
            await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
            {
                for (int i = baystart; i <= bayend; i++)
                {
                    for (int j = levelstart; j <= levelend; j++)
                    {
                        var bbl = GlobalData.Current.ShelfMgr.GetShelf(bank_level, i, j);

                        if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                        {
                            //2.클라이언트면 자제적으로 쉘프의 사용을 변경하는게 아니고 디비에 클라이언트 오더에 넣어 서버에서 변경하도록 한다
                            GlobalData.Current.DBManager.DbSetProcedureClientReq(
                                GlobalData.Current.EQPID,                           //EQPID, 
                                cmd,                                                //CMDType, Disable
                                "SHELF",                                            //Target, 
                                bbl.TagName,                                        //TargetID, 
                                string.Empty,                                       //TargetValue, 
                                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),   //ReqTime, 
                                eServerClientType.Client);//Requester
                        }
                        else
                        {
                            //3.서버이면 바로 변경하고 디비에 업데이트 한다
                            if (cmd == eUnitCommandProperty.Disable.ToString())
                            {
                                if (bbl.Scheduled == true)      //230922 enable상태여서 schedule이 이미 잡혔다면 해당 쉘프는 disable 건너뛴다.
                                {
                                    continue;
                                }
                                bbl.SHELFUSE = false;
                            }
                            else
                            {
                                bbl.SHELFUSE = true;
                            }

                            GlobalData.Current.ShelfMgr.SaveShelfData(bbl);
                        }

                        if (!DisableShelfZoneNameItems.Contains(bbl.iZoneName))
                        {
                            DisableShelfZoneNameItems.Add(bbl.iZoneName);
                        }
                    }
                }
                //230217 HHJ SCS 개선     //왜 PortAccess Mode와 Enable을 같이 처리하도록 되어있는지?
                //cvLine.ChangeAllPortUseType(!cmdProperty.Equals(eUnitCommandProperty.Enable));
            });

        }

        /// <summary>
        /// 체크박스 체크되었을 때 콤보박스 아이템 값 읽어오기
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

            if (sender is System.Windows.Controls.CheckBox senderBuufer)
            {
                int maxBay = 0, MaxLevel = 0;

                if (senderBuufer.Tag.ToString() == "Bank1")
                // 체크박스 태그의 이름이 bank1 일시 체크를 풀수 없도록  + bank2도 동일
                {
                    if (checkbox_Bank2.IsChecked == true)
                    {
                        checkbox_Bank2.IsChecked = false;

                    }

                    maxBay = GlobalData.Current.ShelfMgr.FrontData.MaxBay;
                    MaxLevel = GlobalData.Current.ShelfMgr.FrontData.MaxLevel;
                }
                else if (senderBuufer.Tag.ToString() == "Bank2")
                {
                    if (checkbox_Bank1.IsChecked == true)
                    {
                        checkbox_Bank1.IsChecked = false;

                    }

                    maxBay = GlobalData.Current.ShelfMgr.RearData.MaxBay;
                    MaxLevel = GlobalData.Current.ShelfMgr.RearData.MaxLevel;
                }


                // 콤보박스의 아이템 초기화 진행
                ComboBox_BayStart.Items.Clear();
                ComboBox_BayEnd.Items.Clear();
                ComboBox_LevelStart.Items.Clear();
                ComboBox_LevelEnd.Items.Clear();


                // 베이의 최대값까지 콤보박스에 아이템 추가
                for (int i = 1; i <= maxBay; i++)
                {
                    ComboBox_BayStart.Items.Add(i);
                    ComboBox_BayEnd.Items.Add(i);
                }

                // 레벨의 최대값까지 콤보박스에 아이템 추가
                for (int i = 1; i <= MaxLevel; i++)
                {
                    ComboBox_LevelStart.Items.Add(i);
                    ComboBox_LevelEnd.Items.Add(i);
                }

                // 체크박스 선택시 디폴트 값 설정
                if (checkbox_Bank1.IsChecked == true)
                {
                    ComboBox_BayStart.SelectedIndex = 0;
                    ComboBox_BayEnd.SelectedIndex = 0;
                    ComboBox_LevelStart.SelectedIndex = 0;
                    ComboBox_LevelEnd.SelectedIndex = 0;
                }
                else if (checkbox_Bank2.IsChecked == true)
                {
                    ComboBox_BayStart.SelectedIndex = 0;
                    ComboBox_BayEnd.SelectedIndex = 0;
                    ComboBox_LevelStart.SelectedIndex = 0;
                    ComboBox_LevelEnd.SelectedIndex = 0;
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
            // 윈도우 창 드래그 가능하게
        }

        private void SK_ButtonControl_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                this.Close();
                //버튼클릭시 창닫기
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

    }
}
