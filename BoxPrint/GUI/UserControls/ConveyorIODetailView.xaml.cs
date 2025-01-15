using PLCProtocol.DataClass;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels;
using BoxPrint.Log;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace BoxPrint.GUI.UserControls
{
    /// <summary>
    /// ConveyorIODetailView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ConveyorIODetailView : UserControl
    {
        private ConveyorIODetailViewModel vm;
        private bool IsPlayBackControl = false;
        public ConveyorIODetailView(bool IsPlayBack)
        {
            InitializeComponent();
            IsPlayBackControl = IsPlayBack;
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

        /// <summary>
        /// 쓰기할 아이탬 마우스 다운
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WriteItem_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsPlayBackControl)
                return;

            popupWriteMenu.IsOpen = false;

            if (sender is TextBlock senderBuffer)
            {
                var plcDataItemBuffer = vm.GetPLCDataItem(senderBuffer.Tag.ToString());

                if (plcDataItemBuffer == null)
                    return;

                //240830 HoN MouseEnter Address Data 표시     //PLCtoPC는 WriteItem이 진행되면 안됨.
                if (plcDataItemBuffer.Area.Equals(eAreaType.PLCtoPC))
                    return;

                PopupIOName.Text = string.Format("{0}({1})", plcDataItemBuffer.ItemName, senderBuffer.Tag.ToString());

                switch (plcDataItemBuffer.DataType)
                {
                    case PLCProtocol.DataClass.eDataType.String:
                    case PLCProtocol.DataClass.eDataType.Short:
                    case PLCProtocol.DataClass.eDataType.Int32:
                    case PLCProtocol.DataClass.eDataType.Raw:
                        StringMenu.Visibility = System.Windows.Visibility.Visible;
                        ComboBoxMenu.Visibility = System.Windows.Visibility.Collapsed;
                        CheckBoxMenu.Visibility = System.Windows.Visibility.Collapsed;

                        StringMenu_TextBox.Text = senderBuffer.Text;
                        break;
                    case PLCProtocol.DataClass.eDataType.Bool:
                        StringMenu.Visibility = System.Windows.Visibility.Collapsed;
                        ComboBoxMenu.Visibility = System.Windows.Visibility.Visible;
                        CheckBoxMenu.Visibility = System.Windows.Visibility.Collapsed;

                        ComboBoxMenu_ComboBox.Items.Clear();
                        ComboBoxMenu_ComboBox.Items.Add("False");
                        ComboBoxMenu_ComboBox.Items.Add("True");
                        break;
                    default:
                        return;
                }

                popupWriteMenu.Width = senderBuffer.ActualWidth < 300 ? 300 : senderBuffer.ActualWidth;
                popupWriteMenu.HorizontalOffset = (senderBuffer.ActualWidth / 2) - (popupWriteMenu.Width / 2) + 5;
                popupWriteMenu.PlacementTarget = senderBuffer;
                popupWriteMenu.Placement = PlacementMode.Bottom;
                popupWriteMenu.IsOpen = true;
            }

            if (sender is ComboBox senderBuffer2)
            {

            }
        }

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
                        case "String":
                            vm.SetIOData(StringMenu_TextBox.Text);
                            break;
                        case "ComboBox":
                            vm.SetIOData(ComboBoxMenu_ComboBox.SelectedIndex.ToString());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        private void CheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (IsPlayBackControl)
                    return;

                if (sender is CheckBox cb)
                {
                    var plcDataItemBuffer = vm.GetPLCDataItem(cb.Tag.ToString());

                    if (plcDataItemBuffer == null)
                    {
                        return;
                    }

                    //240830 HoN MouseEnter Address Data 표시     //PLCtoPC는 WriteItem이 진행되면 안됨.
                    if (plcDataItemBuffer.Area.Equals(eAreaType.PLCtoPC))
                        return;

                    bool bToggle = (bool)cb.IsChecked;
                    vm.SetBitIOData(plcDataItemBuffer, bToggle);
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
        //240830 HoN MouseEnter Address Data 표시
        private void Element_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                string bindingPath = string.Empty;

                if (sender is FrameworkElement element)
                {
                    BindingExpression bindingExpression = null;
                    if (element is TextBlock tb)
                    {
                        bindingExpression = BindingOperations.GetBindingExpression(tb, TextBlock.TextProperty);

                        //바인딩 없다면 진행하지 않음
                        if (bindingExpression is null)
                            return;

                        bindingPath = bindingExpression.ResolvedSourcePropertyName;
                    }
                    else if (element is CheckBox cb)
                    {
                        bindingExpression = BindingOperations.GetBindingExpression(cb, CheckBox.IsCheckedProperty);

                        //바인딩 없다면 진행하지 않음
                        if (bindingExpression is null)
                            return;

                        bindingPath = bindingExpression.ResolvedSourcePropertyName;
                    }

                    //바인딩 Path 정보 검색 실패시 진행하지 않음
                    if (string.IsNullOrEmpty(bindingPath))
                        return;

                    PLCDataItem plcItem = vm.GetPLCDataItem(bindingPath);

                    //PLC Item 검색실패시 진행하지 않음
                    if (plcItem is null)
                    {
                        return;
                    }

                    //표기형식 DAddress(Offset)
                    if (plcItem.DataType.Equals(eDataType.Bool))
                    {
                        element.ToolTip = $"{plcItem.DeviceType.ToString()}{plcItem.ItemPLCAddress}.{plcItem.BitOffset.ToString("X")}({plcItem.AddressOffset}.{plcItem.BitOffset.ToString("X")})";
                    }
                    else
                    {
                        element.ToolTip = $"{plcItem.DeviceType.ToString()}{plcItem.ItemPLCAddress}({plcItem.AddressOffset})";
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
    }
}
