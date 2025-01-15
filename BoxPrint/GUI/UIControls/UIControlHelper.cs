using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BoxPrint.GUI.UIControls
{
    internal class BindingHelper
    {
        internal static void MaterialExistBind(FrameworkElement element, DependencyProperty dp, ControlBase collection)
        {
            if (collection.Capacity == 2)
            {
                Binding binding = new Binding(string.Format("MaterialExist"));
                binding.Mode = BindingMode.OneWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                element.SetBinding(dp, binding);

                Binding binding2 = new Binding(string.Format("MaterialExist"));
                binding2.Mode = BindingMode.OneWay;
                binding2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                element.SetBinding(dp, binding2);
            }
            else
            {
                Binding binding = new Binding(string.Format("MaterialExist"));
                binding.Mode = BindingMode.OneWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                element.SetBinding(dp, binding);
            }
        }
        internal static void ElementBind(FrameworkElement element, DependencyProperty dp, string bindingPath)
        {
            Binding binding = new Binding(string.Format(bindingPath));
            binding.Mode = BindingMode.OneWay;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            element.SetBinding(dp, binding);
        }
    }

    public class EnumToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Type objValueType = value.GetType();
            string resourceName = string.Format("{0}.{1}", objValueType.Name, value);

            Brush brush = Application.Current.Resources[resourceName] as Brush;
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// 변수명을 기준으로 값에 따른 비지블 결과가 다르기에 구분시킴.
    /// 결과값이 true일때 Visible해야하는 경우 PositiveVisibleConverter
    /// 결과값이 true일때 Unvisible해야하는 경우 NegativeVisibleConverter
    /// </summary>
    public class PositiveVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class NegativeVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class StringCodeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string resourceName = string.Format("{0}", value);

            Brush brush = Application.Current.Resources[resourceName] as Brush;
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class CraneForkPositionToYPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            decimal dret = 0;
            decimal dtmp = (decimal)value;
            decimal dtmpMax = 1000;
            bool bFrontBankPosition = true;

            //-가 들어오면 +로 변환이 되어야하고, +가 들어오면 -로 변환이 되어야한다.
            //실물은 1번뱅크가 +방향이지만 프로그램상으로는 1번뱅크가 -방향임.
            //현 포지션이 음수라면 RearBankPosition이다.
            if (dtmp < 0)
                bFrontBankPosition = false;

            //현 포지션이 최대 스트로크보다 크다면 최대 스트로크로 계산을 해야한다.
            if (dtmp > dtmpMax)
                dtmp = dtmpMax;

            //계산 편의를 위한 절대값 변경
            dtmp = Math.Abs(dtmp);

            //(실물 Fork Position / 실물 Fork Max Stroke) * UI 최대 Fork 구동거리(25 고정) = UI Fork Position ->  20 변경
            dret = (dtmp / dtmpMax) * 20;

            //FrontBank라면 음수로 변경해서 나가야한다.
            if (bFrontBankPosition)
                dret = -dret;

            return dret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class FireSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var a = ((double)value * 65) / 100;
            return (double)a;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    //SuHwan_20230405 : ture -> false, false -> true로 반환
    public class ReversalBoolTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? false : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    //230405 HHJ SCS 개선     //- Memo 기능 추가
    public class InformMemoExistVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value.ToString()))
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class UnkonwnCarrierCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //null이어도 UNK로 간주
            if (value is null)
            {
                return true;
            }

            string str = value.ToString().ToUpper();

            //empty, null이어도 UNK로 간주
            if (string.IsNullOrEmpty(str))
            {
                return true;
            }
            //아이디 시작이 unk로 되면 UNK로 간주
            if (str.StartsWith("UNK"))
            {
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }
    }
}
