using BoxPrint.GUI.ViewModels;
using BoxPrint.Modules.Shelf;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// ModeChangeShelfTypePopupView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModeChangeShelfTypePopupView : Window
    {
        private bool bResult = false;
        private ShelfItem curShelf;
        private ModeChangeShelfTypePopupViewModel vm;

        public ModeChangeShelfTypePopupView(ShelfItem shelf, bool bNotUseBoth)
        {
            InitializeComponent();

            curShelf = shelf;

            vm = new ModeChangeShelfTypePopupViewModel(curShelf);
            DataContext = vm;

            //변화하는 데이터가 아닌경우 View단에서 처리해준다.
            if (bNotUseBoth)
                BothRadio.Visibility = Visibility.Collapsed;
            else
                BothRadio.Visibility = Visibility.Visible;
        }

        public eShelfType ResultShelfType()
        {
            ShowDialog();

            if (bResult)
                return GetCurrentSelectMode();
            else
                return eShelfType.Unknown;
        }
        private eShelfType GetCurrentSelectMode()
        {
            if (vm.ShelfTypeBoth)
                return eShelfType.Both;
            else if (vm.ShelfTypeLong)
                return eShelfType.Long;
            else
                return eShelfType.Short;
        }
        //https://stackoverflow.com/questions/4376475/wpf-mvvm-how-to-close-a-window
        //이걸 ViewModel로 해당 코드를 넘길 수 있을것같은데 시간이 없으니 일단 기존처럼 진행한다.
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                string msg = string.Empty;

                if (sender is Button btn)
                {
                    if (btn.Tag.Equals("Yes"))
                    {
                        //셋중 하나는 선택되어 있어야한다.
                        if (!(vm.ShelfTypeShort || vm.ShelfTypeLong || vm.ShelfTypeBoth))
                        {
                            msg = TranslationByMarkupExtension.TranslationManager.Instance.Translate("Please Select Mode").ToString();
                            MessageBoxPopupView.Show(msg, MessageBoxImage.Stop, false);
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
