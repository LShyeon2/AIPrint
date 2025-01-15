using BoxPrint.GUI.ETC;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels;
using BoxPrint.Modules.User;
using System;
using System.Windows;
using System.Windows.Controls;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.UserControls
{
    /// <summary>
    /// ConveyorDataView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ConveyorDataView : UserControl
    {
        private bool IsPlayBackControl = false;
        public ConveyorDataViewModel vm { get; set; }
        public ConveyorDataView(bool IsPlayBack)
        {
            InitializeComponent();

            vm = new ConveyorDataViewModel(IsPlayBack);
            vm.UIFontSize_Large = 14;
            vm.UIFontSize_Medium = 12;
            vm.UIFontSize_Small = 10;

            DataContext = vm;
            IsPlayBackControl = IsPlayBack;
            if(IsPlayBack) btnDetail.IsEnabled = true;

            GlobalData.Current.UserMng.OnLoginUserChange += UserMng_OnLoginUserChange;
        }

        private void UserMng_OnLoginUserChange(User usr)
        {
            if (!(usr is null))
            {
                 if (GlobalData.Current.LoginUserAuthority.Contains("ModifyJobControl"))
                {
                    btnPortAccessContent.IsEnabled = true;
                    btnAutoManualContent.IsEnabled = true;
                    btnEnableContent.IsEnabled = true;
                    btnTrackPauseContent.IsEnabled = true;
                    btnInstallContent.IsEnabled = true;
                    btnWrite.IsEnabled = true;
                    //btnKeyIn.IsEnabled = true;
                    btnDetail.IsEnabled = true;
                    vm.LoginState = true;
                    vm.CheckCVLoginState();
                }
                else
                {
                    btnPortAccessContent.IsEnabled = false;
                    btnAutoManualContent.IsEnabled = false;
                    btnEnableContent.IsEnabled = false;
                    btnTrackPauseContent.IsEnabled = false;
                    btnInstallContent.IsEnabled = false;
                    btnWrite.IsEnabled = false;
                    //btnKeyIn.IsEnabled = false;
                    btnDetail.IsEnabled = false;
                    vm.LoginState = false;
                    vm.CheckCVLoginState();
                }
            }
            else
            {
                btnPortAccessContent.IsEnabled = false;
                btnAutoManualContent.IsEnabled = false;
                btnEnableContent.IsEnabled = false;
                btnTrackPauseContent.IsEnabled = false;
                btnInstallContent.IsEnabled = false;
                btnWrite.IsEnabled = false;
                //btnKeyIn.IsEnabled = false;
                btnDetail.IsEnabled = false;
                vm.LoginState = false;
                vm.CheckCVLoginState();
            }
        }

        //SuHwan_20230404 : 데이타변경 팝업추가
        /// <summary>
        /// 세이브 버튼 클릭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SK_ButtonControl_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (IsPlayBackControl)
                    return;

                if (sender is SK_ButtonControl senderBuffer)
                {
                    switch (senderBuffer.Tag.ToString())
                    {
                        case "SaveBTN_TextBox":
                            if (string.IsNullOrEmpty(StringMenu_TextBox.Text.ToString()))
                            {
                                MessageBoxPopupView.Show(TranslationManager.Instance.Translate("데이터가 비어 있습니다. 확인해 주세요").ToString(), MessageBoxButton.OK, MessageBoxImage.Stop, false);
                                return;
                            }

                            vm.SetIOData(StringMenu_TextBox.Text.ToString());
                            break;
                        case "SaveBTN_ComboBox":
                            if (string.IsNullOrEmpty(ComboBoxMenu_ComboBox.Text.ToString()))
                            {
                                MessageBoxPopupView.Show(TranslationManager.Instance.Translate("데이터가 비어 있습니다. 확인해 주세요").ToString(), MessageBoxButton.OK, MessageBoxImage.Stop, false);
                                return;
                            }

                            vm.SetIOData(ComboBoxMenu_ComboBox.SelectedIndex.ToString(), ComboBoxMenu_ComboBox.Text.ToString());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        //SuHwan_20230404 : 데이타변경 팝업추가
        /// <summary>
        /// 쓰기할 아이탬 마우스 다운
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WriteItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsPlayBackControl)
                return;

            popupWriteMenu.IsOpen = false;

            if (sender is TextBox senderBuffer)
            {

                var plcDataItemBuffer = vm.GetPLCDataItem(senderBuffer.Tag.ToString());

                if (plcDataItemBuffer == null)
                    return;

                PopupIOName.Text = string.Format("{0}({1})", plcDataItemBuffer.ItemName, senderBuffer.Tag.ToString());
                ComboBoxMenu_ComboBox.ItemsSource = null;

                switch (senderBuffer.Tag.ToString())
                {
                    case "PCtoPLC_0_0"://PalletInfo
                        popupWriteMenu.Tag = "TextBox";
                        StringMenu_TextBox.Text = senderBuffer.Text;
                        break;
                    case "PCtoPLC_30_0"://CarrierSize -> PalletSize
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(ePalletSize)); // 필요시 강제로 바꾸는 기능이므로 목록 원복함 
                        break;

                    case "PCtoPLC_25_0"://ProductEmpty
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(eProductEmpty));
                        break;

                    case "PCtoPLC_24_0"://Polarity
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(ePolarity));
                        break;

                    case "PCtoPLC_26_0"://WinderDirection
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(eWinderDirection));
                        break;

                    case "PCtoPLC_29_0"://InnerTrayType
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(eInnerTrayType));
                        break;

                    case "PCtoPLC_22_0"://TrayType
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(eTrayType));
                        break;

                    case "PLCtoPC_48_0"://PortInOutType
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(ePortInOutType));
                        break;
                }

                popupWriteMenu.Width = senderBuffer.ActualWidth < 200 ? 200 : senderBuffer.ActualWidth;
                //popupWriteMenu.Width = senderBuffer.ActualWidth;
                popupWriteMenu.HorizontalOffset = (senderBuffer.ActualWidth / 2) - (popupWriteMenu.Width / 2) + 5;
                popupWriteMenu.PlacementTarget = senderBuffer;
                popupWriteMenu.IsOpen = true;
            }
        }
    }
}
