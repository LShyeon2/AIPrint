using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// ucNumericUpDown.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ucNumericUpDown : UserControl
    {
        //Max의 디폴트는 999로 설정해준다.
        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(int), typeof(ucNumericUpDown), new PropertyMetadata(999));
        public int MaxValue
        {
            get { return (int)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }
        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(int), typeof(ucNumericUpDown), new PropertyMetadata(0));
        public int MinValue
        {
            get { return (int)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(ucNumericUpDown), new PropertyMetadata(0));
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set
            {
                SetValue(ValueProperty, value);
                PART_TextBox.Text = value.ToString();
                OnNumericUpdownChange?.Invoke(value);       //230321 HHJ SCS 개선     //- CraneOrder Window 추가
            }
        }

        //230321 HHJ SCS 개선     //- CraneOrder Window 추가
        public delegate void NumericUpdownChange(int changedValue);
        public event NumericUpdownChange OnNumericUpdownChange;

        public ucNumericUpDown()
        {
            InitializeComponent();
        }

        private void PART_TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void PART_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is TextBox txt)
                {
                    if (int.TryParse(txt.Text, out int iValue))
                    {
                        if (iValue > MaxValue)
                        {
                            Value = MaxValue;
                            txt.Text = Value.ToString();
                        }
                        else if (iValue < MinValue)
                        {
                            Value = MinValue;
                            txt.Text = Value.ToString();
                        }
                        else
                            Value = iValue;
                    }
                    else
                    {
                        txt.Text = "0";
                        Value = 0;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void IncreaseDecrease_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Border btn)
                {
                    if (btn.Tag.ToString().Equals("Increase"))
                        PART_TextBox.Text = (Value + 1).ToString();
                    else
                        PART_TextBox.Text = (Value - 1).ToString();
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        public void InitNumericUpDown(int numValue)
        {
            PART_TextBox.Text = numValue.ToString();
        }
    }
}
