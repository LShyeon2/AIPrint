using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// InformMemoView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InformMemoView : Window
    {
        private bool bResult = false;

        public InformMemoView(string memo)
        {
            InitializeComponent();
            txtInformMemo.Text = memo;
            txtInformMemo.Focus();
            txtInformMemo.SelectAll();
        }

        public ResultInformMemo InformMemoString()
        {
            ShowDialog();

            if (bResult)
                return new ResultInformMemo(txtInformMemo.Text.Trim());
            else
                return new ResultInformMemo(eResultInformMemo.eCancel);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Button btn)
                {
                    if (btn.Tag.Equals("Yes"))
                    {
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key.Equals(Key.Enter)) //메모쓸때 엔터키 필요하므로 삭제
            //{
            //    Button_Click(Button_Yes, null); 
            //}
            //else 
            if (e.Key.Equals(Key.Escape))
            {
                if (Button_No.IsVisible)
                    Button_Click(Button_No, null);
            }
        }
    }

    public class ResultInformMemo
    {
        public string InformMemoString = string.Empty;
        public eResultInformMemo InformMemoResult = eResultInformMemo.eCancel;

        public ResultInformMemo(eResultInformMemo result)
        {
            InformMemoString = string.Empty;
            InformMemoResult = result;
        }
        public ResultInformMemo(string memo)
        {
            InformMemoString = memo;
            InformMemoResult = eResultInformMemo.eChange;
        }
    }
}
