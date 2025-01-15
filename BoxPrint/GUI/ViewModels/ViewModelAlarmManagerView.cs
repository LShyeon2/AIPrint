using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using BoxPrint.Alarm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace BoxPrint.GUI.ViewModels
{
    public class ViewModelAlarmManagerView : ViewModelBase
    {
        private ObservableList<AlarmData> _CurrentAlarmList;
        public ObservableList<AlarmData> CurrentAlarmList
        {
            get => _CurrentAlarmList;
            set
            {
                _CurrentAlarmList = value;
            }
        }

        //SuHwan_20221226 : [1차 UI검수] 폰트 사이즈 설정
        protected int _UIFontSize_Large = 14;  //큰폰트
        public int UIFontSize_Large
        {
            get => _UIFontSize_Large;
            set
            {
                if (_UIFontSize_Large == value) return;
                _UIFontSize_Large = value;

                RaisePropertyChanged("UIFontSize_Large");
            }
        }

        protected int _UIFontSize_Medium = 12; //중간폰트
        public int UIFontSize_Medium
        {
            get => _UIFontSize_Medium;
            set
            {
                if (_UIFontSize_Medium == value) return;
                _UIFontSize_Medium = value;

                RaisePropertyChanged("UIFontSize_Medium");
            }
        }

        protected int _UIFontSize_Small = 10;  //작은폰트
        public int UIFontSize_Small
        {
            get => _UIFontSize_Small;
            set
            {
                if (_UIFontSize_Small == value) return;
                _UIFontSize_Small = value;

                RaisePropertyChanged("UIFontSize_Small");
            }
        }

        private string tempExcelPath = @"\Data\AlarmList_temp.xls";
        private string tempXmlPath = @"\Data\AlarmList_temp.xml";
        private string Excelpath;
        private string XmlPath;

        public ViewModelAlarmManagerView()
        {
            Excelpath = GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + tempExcelPath;
            XmlPath = GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + tempXmlPath;
            CurrentAlarmList = GlobalData.Current.Alarm_Manager.getAllAlarmList();
        }

        public async void ExcelImport()
        {
            string[] colname;
            string[] continuecolname;
            //XmlDocument xdoc = new XmlDocument();
            //XmlNode root;

            //root = xdoc.CreateElement("ArrayOfAlarmData");
            //xdoc.AppendChild(root);

            var sts = new XmlWriterSettings()
            {
                Indent = true,
            };

            if (System.IO.File.Exists(Excelpath))
            {
                await Task.Run(() =>
                {
                    Microsoft.Office.Interop.Excel.Application xlexcel;
                    xlexcel = new Microsoft.Office.Interop.Excel.Application();
                    Workbook xlWorkBook = xlexcel.Workbooks.Open(Excelpath);
                    Worksheet xlWorkSheet = (Worksheet)xlWorkBook.Worksheets.get_Item(1);

                    try
                    {
                        Range range = xlWorkSheet.UsedRange;

                        colname = new string[range.Columns.Count];
                        continuecolname = new string[range.Columns.Count];

                        using (XmlWriter wr = XmlWriter.Create(XmlPath, sts))
                        {
                            wr.WriteStartDocument();
                            wr.WriteStartElement("ArrayOfAlarmData");

                            for (int row = 1; row <= range.Rows.Count; row++) // 가져온 행 만큼 반복
                            {
                                if (row == 1)
                                {
                                    //int datanum = 0;
                                    int continuecolnum = 0;

                                    for (int i = 0; i < range.Columns.Count; i++)
                                    {
                                        var cellvalue = (range.Cells[row, i + 1] as Range).Value;
                                        string tempcolname = Convert.ToString(cellvalue);

                                        if (tempcolname == "Description_ENG" || tempcolname == "Description_CHN" || tempcolname == "Description_HUN" ||
                                            tempcolname == "Solution_ENG"    || tempcolname == "Solution_CHN"    || tempcolname == "Solution_HUN" ||
                                            tempcolname == "ListNo")
                                        {
                                            continuecolname[continuecolnum] = i.ToString();
                                            continuecolnum++;
                                            //continue;
                                        }

                                        if (!string.IsNullOrEmpty(tempcolname))
                                        {
                                            if (tempcolname == "Heavy Alarm")
                                            {
                                                colname[i] = "IsLightAlarm";
                                            }
                                            else
                                            {
                                                colname[i] = tempcolname;
                                            }
                                        }
                                        //datanum++;
                                    }

                                    //colname = colname.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                    continuecolname = continuecolname.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                                    //using (XmlWriter wr = XmlWriter.Create(XmlPath))
                                    //{
                                    //    wr.WriteStartDocument();
                                    //    wr.WriteStartElement("ArrayOfAlarmData");
                                    //    wr.WriteEndElement();
                                    //    wr.WriteEndDocument();
                                    //}
                                    //root = xdoc.CreateElement("ArrayOfAlarmData");
                                    //xdoc.AppendChild(root);
                                    continue;
                                }

                                //XmlNode alarmdatanode = xdoc.CreateElement("AlarmData");
                                wr.WriteStartElement("AlarmData");

                                for (int column = 1; column <= range.Columns.Count; column++) // 가져온 열 만큼 반복
                                {
                                    bool continueflag = false;
                                    for (int z = 0; z < continuecolname.Count(); z++)
                                    {
                                        if (continuecolname[z] == (column - 1).ToString())
                                        {
                                            continueflag = true;
                                            break;
                                        }
                                    }

                                    if (continueflag)
                                        continue;

                                    //using (XmlWriter wr = XmlWriter.Create(XmlPath))
                                    //{
                                    //    wr.WriteStartDocument();

                                    //    wr.WriteStartElement("AlarmData");

                                    //    var cellvalue = (range.Cells[row, column] as Range).Value;
                                    //    string temp = Convert.ToString(cellvalue);
                                    //    //if (!string.IsNullOrEmpty(temp))
                                    //    {
                                    //        //wr.WriteAttributeString("ID", );
                                    //    }
                                    //    //else

                                    //    wr.WriteEndElement();
                                    //    wr.WriteEndDocument();
                                    //}
                                    var cellvalue = (range.Cells[row, column] as Range).Value;
                                    string temp = Convert.ToString(cellvalue);

                                    if (colname[column - 1] == "IsLightAlarm")
                                    {
                                        temp = temp == "Y" ? "false" : "true";
                                    }
                                    else
                                    {
                                        temp = string.IsNullOrEmpty(temp) ? string.Empty : temp;
                                    }

                                    //XmlAttribute dataattribute = xdoc.CreateAttribute(colname[column - 1]);
                                    //if (colname[column - 1] == "IsLightAlarm")
                                    //{
                                    //    dataattribute.Value = temp == "Y" ? "true" : "false";
                                    //}
                                    //else
                                    //{
                                    //    dataattribute.Value = string.IsNullOrEmpty(temp) ? string.Empty : temp;
                                    //}
                                    //alarmdatanode.Attributes.Append(dataattribute);

                                    wr.WriteAttributeString(colname[column - 1], temp);
                                }
                                wr.WriteEndElement();

                                //root.AppendChild(alarmdatanode);
                                //xdoc.Save(XmlPath);
                            }

                            wr.WriteEndElement();
                            wr.WriteEndDocument();
                        }
                    }
                    finally
                    {
                        ReleaseObject(xlWorkSheet);
                        ReleaseObject(xlWorkBook);
                        ReleaseObject(xlexcel);
                    }
                });
            }
        }

        public async void ExcuteLogExport()
        {
            try
            {
                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    return; //export 는 일단 클라이언트에서만 실행.
                }
                await Task.Run(() =>
                {
                    if (_CurrentAlarmList.Count == 0 || _CurrentAlarmList == null)
                        return;

                    //20240112 RGJ Excel Export 시 파일로 바로저장. 조범석 매니저 요청
                    //파일 저장 다이얼로그 오픈.
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = string.Format("{0}-{1}-{2}", GlobalData.Current.EQPID, "AlarmList", DateTime.Now.ToString("yyMMddHHmmss"));
                    saveFileDialog.Filter = "Excel Document|*.xlsx";
                    saveFileDialog.Title = "Save an Excel File";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    if (saveFileDialog.ShowDialog() == false)
                    {
                        return;
                    }

                    Workbook xlWorkBook;
                    Worksheet xlWorkSheet;

                    object misValue = Missing.Value;
                    Microsoft.Office.Interop.Excel.Application xlexcel;
                    xlexcel = new Microsoft.Office.Interop.Excel.Application();
                    xlexcel.DisplayAlerts = false;
                    xlWorkBook = xlexcel.Workbooks.Add(misValue);
                    xlWorkSheet = (Worksheet)xlWorkBook.Worksheets.get_Item(1);

                    string tempPath = @"\Data\AlarmList_temp.xls";
                    string path = GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + tempPath;

                    try
                    {
                        Type AlarmListDataType = CurrentAlarmList[0].GetType();
                        PropertyInfo[] props = AlarmListDataType.GetProperties();

                        int d = 0;

                        for (int j = 0; j < props.Length; j++)
                        {
                            if (props[j].Name == "OccurDateTime" ||
                                props[j].Name == "ClearDateTime" ||
                                props[j].Name == "IsLightAlarm" ||
                                props[j].Name == "RecoveryOption" ||
                                props[j].Name == "AlarmRecoveryList" ||
                                props[j].Name == "ModuleName" ||
                                props[j].Name == "iAlarmID" ||
                                props[j].Name == "CarrierID")
                            {
                                continue;
                            }
                            else if (props[j].Name == "AlarmLevel")
                            {
                                xlWorkSheet.Cells[1, d + 1].Font.Bold = true;
                                xlWorkSheet.Cells[1, d + 1] = "Heavy Alarm";
                                xlWorkSheet.Columns[d + 1].ColumnWidth = 22;
                                d++;
                                continue;
                            }

                            xlWorkSheet.Cells[1, d + 1].Font.Bold = true;
                            xlWorkSheet.Cells[1, d + 1] = props[j].Name;
                            xlWorkSheet.Columns[d + 1].ColumnWidth = 22;
                            d++;
                        }

                        Range CR = (Range)xlWorkSheet.get_Range("A1", string.Format("M{0}", CurrentAlarmList.Count + 1));
                        object[,] only_data = (object[,])CR.get_Value();

                        int row = CurrentAlarmList.Count;
                        int column = CR.Columns.Count;

                        object[,] data = new object[row, column];
                        data = only_data;

                        for (int i = 0; i < CurrentAlarmList.Count; i++)
                        {
                            data[i + 2, 1]  = CurrentAlarmList[i].AlarmID;
                            data[i + 2, 2]  = CurrentAlarmList[i].AlarmLevel;
                            data[i + 2, 3]  = CurrentAlarmList[i].ModuleType;
                            data[i + 2, 4]  = CurrentAlarmList[i].AlarmName;
                            data[i + 2, 5]  = CurrentAlarmList[i].Description;
                            data[i + 2, 6]  = CurrentAlarmList[i].Description_ENG;
                            data[i + 2, 7]  = CurrentAlarmList[i].Description_CHN;
                            data[i + 2, 8]  = CurrentAlarmList[i].Description_HUN;
                            data[i + 2, 9]  = CurrentAlarmList[i].Solution;
                            data[i + 2, 10] = CurrentAlarmList[i].Solution_ENG;
                            data[i + 2, 11] = CurrentAlarmList[i].Solution_CHN;
                            data[i + 2, 12] = CurrentAlarmList[i].Solution_HUN;
                            data[i + 2, 13] = CurrentAlarmList[i].ListNo;
                        }

                        CR.Value = data;

                        xlWorkSheet.SaveAs(saveFileDialog.FileName, XlFileFormat.xlOpenXMLWorkbook, null, null, false); //파일로 저장.
                        xlexcel.Quit();
                        string msg = string.Format("Data exported.");
                        MessageBox.Show(msg, "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        ReleaseObject(xlWorkSheet);
                        ReleaseObject(xlWorkBook);
                        ReleaseObject(xlexcel);
                    }
                });
            }
            catch (Exception ex)
            {

            }
        }

        public void CurAlarmListRefresh()
        {
            CurrentAlarmList = GlobalData.Current.Alarm_Manager.getAllAlarmList();
        }

        static void ReleaseObject(object obj)
        {
            try
            {
                if (obj != null)
                {
                    Marshal.ReleaseComObject(obj); // 액셀 객체 해제
                    obj = null;
                }
            }
            catch (Exception ex)
            {
                obj = null;
                throw ex;
            }
            finally
            {
                GC.Collect(); // 가비지 수집
            }
        }
    }
}
