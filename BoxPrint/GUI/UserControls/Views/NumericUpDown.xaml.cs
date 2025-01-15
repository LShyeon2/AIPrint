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

namespace BoxPrint.GUI.UserControls.Views
{
    /// <summary>
    /// NumericUpDown.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        #region Variable
        #region Field
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public int HighestValue
        {
            get => (int)GetValue(HighestValueProperty);
            set => SetValue(HighestValueProperty, value);
        }
        public int LowestValue
        {
            get => (int)GetValue(LowestValueProperty);
            set => SetValue(LowestValueProperty, value);
        }
        #endregion

        #region DependencyProperties
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value",
            typeof(int), typeof(NumericUpDown), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));
        /// <summary>
        /// Property Change CallBack
        /// 값이 변경되면 내부 컨트롤 변경 진행
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDown nud)
            {
                nud.txtValue.Text = e.NewValue.ToString();
                nud.txtValue.Select(nud.txtValue.Text.Length, 0);
            }
        }
        public static readonly DependencyProperty HighestValueProperty = DependencyProperty.Register("HighestValue",
            typeof(int), typeof(NumericUpDown), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty LowestValueProperty = DependencyProperty.Register("LowestValue",
            typeof(int), typeof(NumericUpDown), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        #endregion
        #endregion

        #region Constructor
        public NumericUpDown()
        {
            InitializeComponent();
            Value = 0;
            txtValue.Text = Value.ToString();
        }
        #endregion

        #region Methods
        #region Event
        /// <summary>
        /// Border MouseDown
        /// Border Click시 값 변경
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border bd)
            {
                switch (bd.Tag.ToString())
                {
                    case "Increment":
                        ValueChange(true);
                        break;
                    case "Decrement":
                        ValueChange(false);
                        break;
                }
            }
        }
        /// <summary>
        /// Border Mouse Wheel
        /// Wheel을 이용하여 값 변경
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Border bd)
            {
                if (e.Delta > 0)
                {
                    ValueChange(true);
                }
                else
                {
                    ValueChange(false);
                }
            }
        }
        /// <summary>
        /// TextBox PreviewTextInput
        /// 입력받은 데이터 검증
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //e.Handled = IsTextNumeric(e.Text);

            //입력받은 문자가 숫자인지 확인
            if (!InputTextIsNumeric(e.Text))
                e.Handled = true;
        }
        /// <summary>
        /// TextBox TextChanged
        /// 입력되어있는 문자를 감지해서 비어있는 경우 0으로 변경
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox txt)
            {
                if (string.IsNullOrEmpty(txt.Text))
                    txt.Text = "0";

                if (!int.TryParse(txt.Text, out int parseValue))
                    return;

                Value = parseValue;

                if (parseValue > HighestValue)
                    Value = HighestValue;
                else if (parseValue < LowestValue)
                    Value = LowestValue;
            }
        }
        #endregion
        #region Etc
        /// <summary>
        /// 실제 Value 변경부
        /// </summary>
        /// <param name="increment"></param>
        private void ValueChange(bool increment)
        {
            if (increment)
            {
                if (Value + 1 > HighestValue)
                    return;
                Value++;
            }
            else
            {
                if (Value - 1 < LowestValue)
                    return;
                Value--;
            }
            txtValue.Text = Value.ToString();
        }
        /// <summary>
        /// 입력받은 Text가 숫자형식인지 체크한다.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>숫자면 true, 그 외 false</returns>
        private bool InputTextIsNumeric(string str)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("[0-9]");  //숫자 아닌걸 true로 내고싶으면 "[^0-9]"로 변경하면 됨.
            return reg.IsMatch(str);
        }
        #endregion
        #endregion
    }
}
