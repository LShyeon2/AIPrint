using BoxPrint.DataList;
using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.ETC;
using BoxPrint.Log;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using TranslationByMarkupExtension;

////2020.09.11 UserControl => PangeView로 변경
namespace BoxPrint.GUI.Views
{
    /// <summary>
    /// ParaView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ParaView : Page
    {
        private object TokenLock = new object();

        private List<string> ListHeader = new List<string>();
        private string strtag = string.Empty;
        List<SortingRecClass> itemList = new List<SortingRecClass>();

        DispatcherTimer timer = new DispatcherTimer();    //객체생성

        private string SelectRMNumber = "RM1";
        private int SelectAxisNumber = 1;

        private int MaxPmacSendCount = 10;

        private delegate void D_Set_StringValue(string nValue);

        public GUIColorBase GUIColorMembers = new GUIColorBase();
        eThemeColor currentThemeColorName = eThemeColor.NONE;

        public ParaView()
        {
            InitializeComponent();
            GlobalData.Current.SendTagChange += Current_ReceiveEvent;

            cbbDbGet1.SelectionChanged += CbbDbGet_SelectionChanged;
            cbbDbGet2.SelectionChanged += CbbDbGet_SelectionChanged;

            sortGrid1.MouseDoubleClick += SortGrid1_MouseDoubleClick;

            GlobalData.Current.OnParaChangeEvent += Current_OnParaChangeEvent;

            //220330 seongwon 테마 색상 바인딩
            MainWindow._EventCall_ThemeColorChange += new MainWindow.EventHandler_ChangeThemeColor(this.eventGUIThemeColorChange);//테마 색상 이벤트
            GUIColorMembers = GlobalData.Current.GuiColor;

            // 2020.11.20 PMac에서 => 펌웨어 로 티칭 포인트 다운로드 
            //btnFrontRead.ToolTip = "Front Teaching 데이터를 Pmac으로 부터 읽어 옵니다.";
            //btnRearRead.ToolTip = "Rear Teaching 데이터를 Pmac으로 부터 읽어 옵니다.";
            //btnPortRead.ToolTip = "Port Teaching 데이터를 Pmac으로 부터 읽어 옵니다.";


            timer.Interval = TimeSpan.FromMilliseconds(1000);    //시간간격 설정
            timer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
            timer.Start();

        }


        private void SortGrid1_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (sender != null)
                {
                    DataGrid grid = sender as DataGrid;
                    if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                    {
                        DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                        SortingRecClass s = (SortingRecClass)grid.SelectedItems[0];

                        // RMParameter 만 변경 가능 나머지 것들은 변경 불가
                        if (strtag == "RMParameter")
                        {
                            RMParaChangePopupView kw = new RMParaChangePopupView(s, SelectRMNumber, strtag);
                            kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                            kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            kw.ShowDialog();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        private void Current_ReceiveEvent(object sender, EventArgs e)
        {
            string JInfo = (string)sender;
            this.Dispatcher.Invoke(new D_Set_StringValue(_DisplayChange), JInfo);
        }
        private void Current_OnParaChangeEvent()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                SetRMParaGird(SelectRMNumber, this.strtag);
            }));
        }

        private void _DisplayChange(string strtag)
        {
            try
            {
                this.strtag = strtag;
                initLoad(strtag);

                SetRMParaGird(SelectRMNumber, this.strtag);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (this.IsVisible)
            {
                switch (strtag)
                {
                    case "RMPval":
                        UpDatePval();
                        break;
                    case "AxisState":
                        UpDateAxisState();
                        break;
                    default:
                        break;
                }
            }
        }

        private void initLoad(string tag)
        {
            List<GridItemListItemInfo> lsititem = GlobalData.Current.GetGridItemList(tag);

            sortGrid1.Columns.Clear();

            foreach (var item in lsititem)
            {
                DataGridTextColumn addedcol = new DataGridTextColumn();

                addedcol.HeaderStyle = GetStyle(true);
                addedcol.CellStyle = GetStyle(false);

                if (item.GridItem.Contains("\\"))        //\ 있다면 \를 기준으로 띄워쓰기 해준다.
                {
                    addedcol.Header = item.GridItem.Replace("\\", "\n");
                }
                else
                    addedcol.Header = item.GridItem;

                addedcol.Binding = new Binding(item.BindingItem);
                addedcol.Width = item.GridWidth;
                addedcol.IsReadOnly = true;

                sortGrid1.Columns.Add(addedcol);

                ListHeader.Add(addedcol.Header.ToString());
            }


            grdTopDbGetData1.Visibility = Visibility.Hidden;
            grdTopDbGetData2.Visibility = Visibility.Hidden;
            grdTopDbGetData3.Visibility = Visibility.Hidden;

            grdTopSearch.Visibility = Visibility.Hidden;
            grdTopSort.Visibility = Visibility.Hidden;

            cbbDbGet1.Items.Clear();
            cbbDbGet2.Items.Clear();

            switch (tag)
            {
                case "RMParameter":
                case "RMPval":
                    grdTopDbGetData1.Visibility = Visibility.Visible;
                    DbGetName1.Text = TranslationManager.Instance.Translate("CRANE NUMBER").ToString();
                    //cbItemadd(cbbDbGet1, "RM{0}", GlobalData.Current.mRMManager.ModuleList.Count());

                    foreach (var item in GlobalData.Current.mRMManager.ModuleList)
                    {
                        cbbDbGet1.Items.Add(item.Key);
                    }

                    cbbDbGet1.SelectedIndex = 0;
                    //cbbDbGet1.SelectionChanged += CbbDbGet_SelectionChanged;
                    break;
                case "AxisState":
                    grdTopDbGetData1.Visibility = Visibility.Visible;
                    grdTopDbGetData2.Visibility = Visibility.Visible;

                    DbGetName1.Text = TranslationManager.Instance.Translate("CRANE NUMBER").ToString();
                    DbGetName2.Text = TranslationManager.Instance.Translate("Axis Number").ToString();

                    //cbItemadd(cbbDbGet1, "RM", GlobalData.Current.RMcount);
                    foreach (var item in GlobalData.Current.mRMManager.ModuleList)
                    {
                        cbbDbGet1.Items.Add(item.Key);
                    }

                    cbItemadd(cbbDbGet2, "Axis", 4);

                    cbbDbGet1.SelectedIndex = 0;
                    cbbDbGet2.SelectedIndex = 0;

                    break;
                default:
                    break;
            }
        }

        #region Evnet 관련
        private void CbbDbGet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ComboBox cbb = sender as ComboBox;

                if (cbb != null)
                {

                    switch (this.strtag)
                    {
                        case "RMParameter":
                        case "RMPval":
                            if (cbbDbGet1.SelectedValue != null)
                                SelectRMNumber = cbbDbGet1.SelectedValue.ToString();
                            break;
                        case "AxisState":
                            if (cbbDbGet1.SelectedValue != null)
                                SelectRMNumber = cbbDbGet1.SelectedValue.ToString();
                            if (cbbDbGet2.SelectedValue != null)
                                SelectAxisNumber = cbbDbGet2.SelectedIndex + 1;

                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ex.ToString()));
            }
        }
        #endregion

        private void cbItemadd(ComboBox c, string Name, int count)
        {
            c.Items.Clear();
            for (int i = 0; i < count; i++)
            {
                c.Items.Add(string.Format(Name + "{0}", i + 1));
            }
        }

        private void SetRMParaGird(string RMnumber, string Tag)
        {
            //int i = 1;

            itemList.Clear();

            //switch (Tag)
            //{
            //    case "RMParameter":
            //        foreach (var item in GlobalData.Current.mRMManager[RMnumber].nParameterList)
            //        {
            //            itemList.Add(new SortingRecClass((i).ToString(), item, false, 0, null));
            //            i++;
            //        }
            //        break;
            //    case "RMPval":
            //        foreach (var item in GlobalData.Current.mRMManager[RMnumber].nPValue)
            //        {
            //            itemList.Add(new SortingRecClass((i).ToString(), item, false, 0, null));
            //            i++;
            //        }
            //        break;
            //    case "AxisState":
            //        foreach (var item in GlobalData.Current.mRMManager[RMnumber].nAxisState)
            //        {
            //            itemList.Add(new SortingRecClass((i).ToString(), item, false, 0, null));
            //            i++;
            //        }
            //        break;
            //    default:
            //        break;
            //}

            if (itemList != null)
            {
                sortGrid1.ItemsSource = itemList;
                sortGrid1.Items.Refresh();
            }
        }

        private Style GetStyle(bool bHeader)
        {
            Style retStyle = new Style();

            try
            {

                if (bHeader)
                {
                    retStyle.Setters.Add(new Setter
                    {
                        Property = FontSizeProperty,
                        Value = 13.0
                    });

                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.HorizontalAlignmentProperty,
                        Value = HorizontalAlignment.Stretch
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.ForegroundProperty,
                        Value = Brushes.Green
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.HorizontalContentAlignmentProperty,
                        Value = HorizontalAlignment.Left
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.VerticalContentAlignmentProperty,
                        Value = VerticalAlignment.Center
                    });
                }
                else
                {
                    retStyle.Setters.Add(new Setter
                    {
                        Property = FontSizeProperty,
                        Value = 12.0
                    });

                    retStyle.Setters.Add(new Setter
                    {
                        Property = TextBlock.TextAlignmentProperty,
                        Value = TextAlignment.Left
                    });


                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.ForegroundProperty,
                        Value = Brushes.Black
                    });
                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.HorizontalAlignmentProperty,
                        Value = HorizontalAlignment.Stretch
                    });

                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.VerticalAlignmentProperty,
                        Value = VerticalAlignment.Stretch
                    });


                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.HorizontalContentAlignmentProperty,
                        Value = HorizontalAlignment.Center
                    });

                    retStyle.Setters.Add(new Setter
                    {
                        Property = System.Windows.Controls.Control.VerticalContentAlignmentProperty,
                        Value = VerticalAlignment.Center
                    });
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, System.Reflection.MethodBase.GetCurrentMethod().ToString() + "Fail");      //200509 HHJ MaskProject    //MainWindow Event 추가
                MessageBox.Show(string.Format(ex.ToString()));
            }

            return retStyle;
        }

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            switch (this.strtag)
            {
                case "RMParameter":
                case "RMPval":
                case "AxisState":

                    SetRMParaGird(SelectRMNumber, this.strtag);
                    break;
                default:
                    break;
            }
        }

        public IEnumerable<System.Windows.Controls.DataGridRow> GetDataGridRows(System.Windows.Controls.DataGrid grid)
        {
            var itemsSource = grid.ItemsSource as IEnumerable;
            if (null == itemsSource) yield return null;
            foreach (var item in itemsSource)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as System.Windows.Controls.DataGridRow;
                if (null != row) yield return row;
            }
        }



        private void UpDateAxisState()
        {
            try
            {

                string Cmd = string.Empty;
                string MotorName = string.Empty;
                string[] val;
                if (this.SelectAxisNumber > 0)
                {
                    MotorName = string.Format("Motor[{0}].", this.SelectAxisNumber);
                    #region Grid 업데이트

                    var rows1 = GetDataGridRows(sortGrid1);

                    foreach (DataGridRow r in rows1)
                    {
                        var pitem = (SortingRecClass)r.Item;

                        if (pitem.Item1 != "")
                        {
                            Cmd = MotorName + pitem.Item1; // TagName
                            val = GlobalData.Current.mRMManager[SelectRMNumber.ToString()].SetTextSend(Cmd);
                            pitem.Item2 = val[0];
                        }

                        if (pitem.Item2 != null)
                        {
                            if (pitem.Item2 != "0")
                                r.Background = Brushes.Beige;
                            else
                                r.Background = Brushes.White;
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        private void UpDatePval()
        {
            try
            {

                string Cmd = string.Empty;
                string MotorName = string.Empty;
                string[] val;
                if (this.SelectAxisNumber > 0)
                {
                    //MotorName = string.Format("Motor[{0}].", this.SelectAxisNumber);
                    #region Grid 업데이트

                    var rows1 = GetDataGridRows(sortGrid1);

                    foreach (DataGridRow r in rows1)
                    {
                        var pitem = (SortingRecClass)r.Item;

                        if (pitem.Item1 != "")
                        {
                            Cmd = pitem.Item1; // TagName
                            val = GlobalData.Current.mRMManager[SelectRMNumber.ToString()].SetTextSend(Cmd);
                            pitem.Item4 = val[0];
                        }

                        if (pitem.Item4 != null)
                        {
                            if (pitem.Item4 != "0")
                                r.Background = Brushes.Beige;
                            else
                                r.Background = Brushes.White;
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        /// <summary>
        ///  // 2020.11.20 PMac에서 => 펌웨어 로 티칭 포인트 다운로드 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            string Messange = string.Empty;
            Button btn = sender as Button;

            // PMac에서 => 펌웨어 로 다운로드
            Messange = TranslationManager.Instance.Translate("PMAC Reload 메시지").ToString();
            Messange = string.Format(Messange, cbbDbGet1.SelectedItem.ToString(), btn.Content.ToString());

            MessageBoxResult result = MessageBox.Show(Messange, TranslationManager.Instance.Translate("Confirmation").ToString(), MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                // Yes code here  
                lock (TokenLock)
                {
                    bool Result = false;
                    string tmpPath = string.Format("\\Data\\RM\\{0}{1}.xml", btn.Tag, cbbDbGet1.SelectedItem.ToString());

                    if (Result)
                    {
                        Messange = TranslationManager.Instance.Translate("PMAC Teaching Comp 메시지").ToString();
                        Messange = string.Format(Messange, cbbDbGet1.SelectedItem.ToString(), btn.Content.ToString());
                        MessageBox.Show(Messange, TranslationManager.Instance.Translate("Confirmation").ToString(), MessageBoxButton.OK);
                    }
                    else
                    {
                        Messange = TranslationManager.Instance.Translate("PMAC Teaching Fail 메시지").ToString();
                        Messange = string.Format(Messange, cbbDbGet1.SelectedItem.ToString(), btn.Content.ToString());
                        MessageBox.Show(Messange, TranslationManager.Instance.Translate("Confirmation").ToString(), MessageBoxButton.OK);
                    }

                }
            }
        }

        private bool ShelfDataRead(ShelfItemList shData, string RMID, eRMTarget Target)
        {
            // Yes code here  
            lock (TokenLock)
            {
                int s = 1;
                int LastCount = 1;
                List<ShelfItem> tmpLis = new List<ShelfItem>();
                foreach (var item in shData)
                {
                    tmpLis.Add(item);
                    if (s >= MaxPmacSendCount || LastCount >= shData.Count())
                    {
                        ShelfDataMarge(tmpLis, RMID, Target);
                        tmpLis.Clear();
                        s = 0;
                    }
                    s++;
                    LastCount++;
                }
                return true;
            }
        }
        private void ShelfDataMarge(List<ShelfItem> tmpListFirst, string RMID, eRMTarget Target)
        {
            try
            {
                //220524 HHJ SCS 개선     //- Shelf Xml제거
                //string space = " ";
                //string subSend = string.Empty;
                //string SendPDrive = string.Empty;
                //string SendPPork = string.Empty;
                //string SendPAxisT = string.Empty;
                //string SendPAxisZ = string.Empty;

                //ShelfItem sh = new ShelfItem();


                //foreach (var item in tmpListFirst)
                //{
                //    subSend = subSend + (SendPDrive + "P" + item.P_Drive_Address.ToString() + space +
                //                            SendPAxisZ + "P" + item.P_AxisZ_Address.ToString() + space +
                //                            SendPAxisT + "P" + item.P_AxisT_Address.ToString() + space +
                //                            SendPPork + "P" + item.P_Fork_Address.ToString() + space);
                //}

                //string[] rec = GlobalData.Current.mRMManager[RMID].SetTextSend(subSend);
                //GlobalData.Current.SendMessageEvent = rec[0];

                //int s = 0;
                //foreach (var item in tmpListFirst)
                //{
                //    if (rec[s] != "")
                //    {
                //        if (rec[s] != "")
                //            item.AxisDrive = decimal.Parse(rec[s]);
                //        s++;

                //        if (rec[s] != "")
                //            item.AxisZ = decimal.Parse(rec[s]);
                //        s++;

                //        if (rec[s] != "")
                //            item.AxisT = decimal.Parse(rec[s]);
                //        s++;

                //        if (rec[s] != "")
                //            item.AxisFork = decimal.Parse(rec[s]);
                //        s++;
                //    }



                //    switch (Target)
                //    {
                //        case eRMTarget.None:
                //            break;
                //        case eRMTarget.Front:
                //            sh = GlobalData.Current.mRMManager[RMID].FrontData.Where(r => r.TagName == item.TagName).FirstOrDefault();
                //            break;
                //        case eRMTarget.Rear:
                //            sh = GlobalData.Current.mRMManager[RMID].RearData.Where(r => r.TagName == item.TagName).FirstOrDefault();
                //            break;
                //        case eRMTarget.Port:
                //            sh = GlobalData.Current.mRMManager[RMID].PortData.Where(r => r.TagName == item.TagName).FirstOrDefault();
                //            break;
                //        default:
                //            break;
                //    }

                //    //ShelfItem sh = GlobalData.Current.mRMManager[RMID].FrontData.Where(r => r.TagName == item.TagName).FirstOrDefault();
                //    sh.AxisDrive = Math.Truncate(item.AxisDrive * 1000) / 1000;
                //    sh.AxisZ = Math.Truncate(item.AxisZ * 1000) / 1000;
                //    sh.AxisT = Math.Truncate(item.AxisT * 1000) / 1000;
                //    sh.AxisFork = Math.Truncate(item.AxisFork * 1000) / 1000;
                //}
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }

        }


        /// <summary>
        ///  // 2020.11.20 PMac에서 => 펌웨어 로 티칭 포인트 다운로드 
        /// </summary>
        /// <param name="tmpList"></param>
        /// <param name="RMName"></param>
        /// <returns></returns>
        private bool ShelfDataPmacRead(ShelfItemList tmpList, string RMName)
        {

            try
            {
                //220524 HHJ SCS 개선     //- Shelf Xml제거
                //string space = " ";
                //string subSend = string.Empty;
                //string SendPDrive = string.Empty;
                //string SendPPork = string.Empty;
                //string SendPAxisT = string.Empty;
                //string SendPAxisZ = string.Empty;

                //foreach (var item in tmpList)
                //{
                //    subSend = subSend + (SendPDrive + "P" + item.P_Drive_Address.ToString() + space +
                //                            SendPAxisZ + "P" + item.P_AxisZ_Address.ToString() + space +
                //                            SendPAxisT + "P" + item.P_AxisT_Address.ToString() + space +
                //                            SendPPork + "P" + item.P_Fork_Address.ToString() + space);
                //}

                //string[] rec = GlobalData.Current.mRMManager[RMName].SetTextSend(subSend);
                //GlobalData.Current.SendMessageEvent = rec[0];

                //string[] result = rec[0].Split(' ');
                //int s = 0;
                //foreach (var item in tmpList)
                //{
                //    if (result[s] != "")
                //    {
                //        if (result[s] != "")
                //            item.AxisDrive = decimal.Parse(result[s]);
                //        s++;

                //        if (result[s] != "")
                //            item.AxisZ = decimal.Parse(result[s]);
                //        s++;

                //        if (result[s] != "")
                //            item.AxisT = decimal.Parse(result[s]);
                //        s++;

                //        if (result[s] != "")
                //            item.AxisFork = decimal.Parse(result[s]);
                //        s++;
                //    }

                //    ShelfItem sh = GlobalData.Current.mRMManager[RMName].FrontData.Where(r => r.TagName == item.TagName).FirstOrDefault();
                //    sh.AxisDrive = item.AxisDrive;
                //    sh.AxisZ = item.AxisZ;
                //    sh.AxisT = item.AxisT;
                //    sh.AxisFork = item.AxisFork;
                //}
                return true;

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        //220330 seongwon 테마 색상 바인딩
        public void eventGUIThemeColorChange()
        {
            switch (GlobalData.Current.SendTagEvent)
            {
                case "RMParameter":
                    ParaviewTitleNametxt.Text = TranslationManager.Instance.Translate("RMParameter").ToString();
                    break;
                case "RMPval":
                    ParaviewTitleNametxt.Text = TranslationManager.Instance.Translate("RM P-val").ToString();
                    break;
                case "AxisState":
                    ParaviewTitleNametxt.Text = TranslationManager.Instance.Translate("Axis State").ToString();
                    break;
                default:
                    return;
            }

            setGUIThemeColorChange();
        }

        private void setGUIThemeColorChange()
        {

            if (currentThemeColorName == GUIColorMembers._currentThemeName)
                return;

            currentThemeColorName = GUIColorMembers._currentThemeName;

            colorBuffer_FirmwareLogViewMainBackground.Fill = GUIColorMembers.NormalBorderBackground;
            colorBuffer_FirmwareLogViewButtonBackground.Fill = GUIColorMembers.MainMenuButtonBackground;
            colorBuffer_FirmwareLogViewForeground.Fill = GUIColorMembers.MainMenuForeground;
            colorBuffer_FirmwareLogViewBorderBrush.Fill = GUIColorMembers.MainMenuButtonBorderBrush;
            colorBuffer_FirmwareLogViewButtonBackground_Enter.Fill = GUIColorMembers.NormalButtonBackground_Enter;
        }
    }
}
