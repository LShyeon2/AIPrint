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
    /// ShelfDataView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ShelfDataView : UserControl
    {

        private string SelectWriteItem = string.Empty;
        private bool IsPlayBackControl = false;
        public ShelfDataViewModel vm { get; set; }
        public ShelfDataView(bool IsPlayBack)
        {
            InitializeComponent();

            vm = new ShelfDataViewModel(IsPlayBack);

            //SuHwan_20230110 : [1차 UI검수]
            {
                vm.UIFontSize_Large = 14;
                vm.UIFontSize_Medium = 12;
                vm.UIFontSize_Small = 10;
            }

            DataContext = vm;
            IsPlayBackControl = IsPlayBack;

            GlobalData.Current.UserMng.OnLoginUserChange += UserMng_OnLoginUserChange;
        }

        private void UserMng_OnLoginUserChange(User usr)
        {
            if (!(usr is null))
            {
                if (GlobalData.Current.LoginUserAuthority.Contains("ModifyJobControl"))
                {
                    btnEnableContent.IsEnabled = true;
                    btnInstallContent.IsEnabled = true;
                    btnShelfTypeContent.IsEnabled = true;
                    btnInform.IsEnabled = true;
                    btnStatus.IsEnabled = true;
                }
                else
                {
                    btnEnableContent.IsEnabled = false;
                    btnInstallContent.IsEnabled = false;
                    btnShelfTypeContent.IsEnabled = false;
                    btnInform.IsEnabled = false;
                    btnStatus.IsEnabled = false;
                }
            }
            else
            {
                btnEnableContent.IsEnabled = false;
                btnInstallContent.IsEnabled = false;
                btnShelfTypeContent.IsEnabled = false;
                btnInform.IsEnabled = false;
                btnStatus.IsEnabled = false;
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
                            vm.setSelectUnitData(PopupIOName.Text.ToString(), StringMenu_TextBox.Text.ToString());
                            break;
                        case "SaveBTN_ComboBox":
                            if (string.IsNullOrEmpty(ComboBoxMenu_ComboBox.Text.ToString()))
                            {
                                MessageBoxPopupView.Show(TranslationManager.Instance.Translate("데이터가 비어 있습니다. 확인해 주세요").ToString(), MessageBoxButton.OK, MessageBoxImage.Stop, false);
                                return;
                            }
                            vm.setSelectUnitData(PopupIOName.Text.ToString(), ComboBoxMenu_ComboBox.Text.ToString());
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
                PopupIOName.Text = senderBuffer.Tag.ToString();

                switch (senderBuffer.Tag.ToString())
                {
                    case "CarrierID":
                        return; //240805 RGJ SHELF 에서 CARRIER ID 변경 불가능하게 함.삭제하고 인스톨 하도록...
                        //popupWriteMenu.Tag = "TextBox";
                        //StringMenu_TextBox.Text = senderBuffer.Text;
                        //break;
                    case "CarrierSize":
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = null;
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(eCarrierSize));
                        break;

                    case "ShelfType":
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = null;
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(eShelfType));
                        break;

                    case "PalletSize":
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = null;
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(ePalletSize)); // 필요시 강제로 바꾸는 기능이므로 목록 원복함 
                        break;
                    case "ProductEmpty":
                        popupWriteMenu.Tag = "ComboBox";
                        ComboBoxMenu_ComboBox.ItemsSource = null;
                        ComboBoxMenu_ComboBox.ItemsSource = Enum.GetValues(typeof(eProductEmpty));
                        break;
                }

                popupWriteMenu.Width = senderBuffer.ActualWidth < 200 ? 200 : senderBuffer.ActualWidth;
                popupWriteMenu.HorizontalOffset = (senderBuffer.ActualWidth / 2) - (popupWriteMenu.Width / 2) + 5;
                popupWriteMenu.PlacementTarget = senderBuffer;
                popupWriteMenu.IsOpen = true;
            }
        }
    }
}
