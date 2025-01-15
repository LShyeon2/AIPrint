using BoxPrint.DataList;
using BoxPrint.GUI.ViewModels;
using BoxPrint.Modules.Conveyor;
using System;
using System.Windows;
using System.Windows.Controls;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// Interaction logic for CarrierInstall.xaml
    /// </summary>
    public partial class CarrierKeyIn : Window
    {
        private bool bResult = false;
        private PortKeyInViewModel vm;

        public CarrierKeyIn(CV_BaseModule CPort)
        {
            InitializeComponent();

            vm = new PortKeyInViewModel(CPort);
            DataContext = vm;


            cbbPalletSize.ItemsSource = GlobalData.Current.GetPalletSizeList();
            cbbProductEmpty.ItemsSource = Enum.GetValues(typeof(eProductEmpty));

            //cbbPolarity.ItemsSource = Enum.GetValues(typeof(ePolarity));
            //cbbWinderDirection.ItemsSource = Enum.GetValues(typeof(eWinderDirection));
            //cbbInnerTrayType.ItemsSource = Enum.GetValues(typeof(eInnerTrayType));
            //cbbTrayType.ItemsSource = Enum.GetValues(typeof(eTrayType));
        }

        public CarrierItem ResultCarrierItem()
        {
            ShowDialog();

            if (bResult)
            {
                //Carrier Location 정보는 여기서 업데이트 해주지않음.
                //Carrier Data만 업데이트 진행.
                return vm.GetCarrier();
            }
            else
            {
                return null;
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Button btn)
                {
                    if (btn.Tag.Equals("OK"))
                    {
                        //우선 케리어 아이디만 입력되어있다면 넘어가도록 한다.
                        if (string.IsNullOrEmpty(vm.CarrierID) || vm.PalletSize == ePalletSize.NONE)
                        {
                            MessageBoxPopupView.Show(TranslationManager.Instance.Translate("CarrierID is Empty or PalletSize is None.").ToString(), MessageBoxImage.Stop, true);
                            return;
                        }

                        bResult = true;
                        Close();
                    }
                    else
                    {
                        bResult = false;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
    }
}
