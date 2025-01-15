using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using BoxPrint.DataBase;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.ViewModels.BindingCommand;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ViewModels
{
    public class ViewModelAlarmLogView : ViewModelBase
    {
        #region Variable
        #region Field
        private OracleDBManager_Log ForLogDBManager;
        private System.Data.DataTable LogList;
        private bool bSearching = false;
        private ObservableCollection<AlarmLogViewModelData> prevAlarmListData;
        #endregion

        #region Binding
        private ObservableCollection<AlarmLogViewModelData> _AlarmListData;
        public ObservableCollection<AlarmLogViewModelData> AlarmListData
        {
            get => _AlarmListData;
            set => Set("AlarmListData", ref _AlarmListData, value);
        }
        private DateTime _SearchStart;
        public DateTime SearchStart
        {
            get => _SearchStart;
            set => Set("SearchStart", ref _SearchStart, value);
        }
        private DateTime _SearchEnd;
        public DateTime SearchEnd
        {
            get => _SearchEnd;
            set => Set("SearchEnd", ref _SearchEnd, value);
        }
        private string _UnitID;
        public string UnitID
        {
            get => _UnitID;
            set => Set("UnitID", ref _UnitID, value);
        }
        private string _ErrorCode;
        public string ErrorCode
        {
            get => _ErrorCode;
            set => Set("ErrorCode", ref _ErrorCode, value);
        }
        private bool _SpinnerVisible;
        public bool SpinnerVisible
        {
            get => _SpinnerVisible;
            set => Set("SpinnerVisible", ref _SpinnerVisible, value);
        }
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
        #endregion

        #region Command
        public ICommand LogFindCommand { get; private set; }
        private async void ExcuteLogFind()
        {
            bool bSearchLogZero = false;

            try
            {
                if (SearchEnd <= SearchStart)
                {
                    MessageBoxPopupView msgbox_item = new MessageBoxPopupView(
                        TranslationManager.Instance.Translate("검색 종료일시가 시작일시와 같거나 작습니다.\n 다시 설정하세요.").ToString(),
                        MessageBoxButton.OK, MessageBoxImage.Stop, false);
                    msgbox_item.Show();
                    return;
                }

                await Task.Run(() =>
                {
                    SpinnerVisible = true;
                    if (bSearching)
                    {
                        SpinnerVisible = false;
                        bSearching = false;
                        if (!ForLogDBManager.bLogSearchCancel)
                        {
                            ForLogDBManager.bLogSearchCancel = true;
                        }
                        return;
                    }
                    else
                        bSearching = true;

                    if (AlarmListData.Count != 0)
                    {
                        prevAlarmListData = AlarmListData;
                        AlarmListData = new ObservableCollection<AlarmLogViewModelData>();
                    }

                    ObservableCollection<AlarmLogViewModelData> tmp = new ObservableCollection<AlarmLogViewModelData>();

                    LogList = ForLogDBManager.DbGetProcedureLogListInfo("ALARM", SearchStart, SearchEnd, UnitID, ErrorCode);

                    if (LogList.Rows.Count == 0)
                    {
                        bSearchLogZero = true;
                    }

                    uint No = 1; //신규 로그 사양 추가.
                    foreach (DataRow dr in LogList.Rows)
                    {
                        if (bool.TryParse(dr["COL_8"].ToString(), out bool lightalarm))
                        {
                            var AlarmViewObject = new AlarmLogViewModelData()
                            {
                                NumberOrder = No++,
                                EQPID = dr["SCS_CD"].ToString(),
                                UnitID = dr["COL_1"].ToString(),
                                AlarmCode = dr["COL_2"].ToString(),
                                Message = dr["COL_3"].ToString(),
                                CraneNumber = dr["COL_4"].ToString(),
                                EQPType = dr["COL_5"].ToString(),
                                LightAlarm = lightalarm ? "Warning" : "Alarm",
                                StartDttm = dr["COL_6"].ToString(),
                                EndDttm = dr["COL_7"].ToString(),
                                CarrierID = dr["COL_9"].ToString(),
                                Description_KOR = dr["COL_10"].ToString(),
                                Description_ENG = dr["COL_11"].ToString(),
                                Description_CHN = dr["COL_12"].ToString(),
                                Description_HUN = dr["COL_13"].ToString(),
                            };
                            tmp.Add(AlarmViewObject);
                        }
                    }

                    if (ForLogDBManager.bLogSearchCancel)
                    {
                        //AlarmListData = prevAlarmListData;
                        ForLogDBManager.bLogSearchCancel = false;
                    }
                    //else
                    //{
                    //    AlarmListData = tmp;
                    //}
                    AlarmListData = tmp;

                    bSearching = false;
                    SpinnerVisible = false;
                });

                if (bSearchLogZero)
                {
                    MessageBoxPopupView msgbox_item = new MessageBoxPopupView(
                        TranslationManager.Instance.Translate("검색값이 없습니다.").ToString(),
                        MessageBoxButton.OK, MessageBoxImage.Stop, false);
                    msgbox_item.Show();
                }
            }
            catch (Exception)
            {
                bSearching = false;
                SpinnerVisible = false;
            }
        }
        public ICommand LogInitCommand { get; private set; }
        public async void ExcuteLogInit()
        {
            try
            {
                await Task.Run(() =>
                {
                    ErrorCode = string.Empty;
                    UnitID = string.Empty;
                    AlarmListData = new ObservableCollection<AlarmLogViewModelData>();
                });
            }
            catch (Exception)
            {

            }
        }
        public ICommand LogExportCommand { get; private set; }
        private async void ExcuteLogExport()
        {
            try
            {
                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    return; //export 는 일단 클라이언트에서만 실행.
                }
                await Task.Run(() =>
                {
                    if (AlarmListData.Count == 0 || AlarmListData == null)
                        return;

                    //20240112 RGJ Excel Export 시 파일로 바로저장. 조범석 매니저 요청
                    //파일 저장 다이얼로그 오픈.
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = string.Format("{0}-{1}-{2}", GlobalData.Current.EQPID, "AlarmLog", DateTime.Now.ToString("yyMMddHHmmss"));
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
                    xlWorkBook = xlexcel.Workbooks.Add(misValue);
                    xlWorkSheet = (Worksheet)xlWorkBook.Worksheets.get_Item(1);

                    try
                    {
                        Type AlarmListDataType = AlarmListData[0].GetType();
                        PropertyInfo[] props = AlarmListDataType.GetProperties();

                        for (int j = 0; j < props.Length; j++)
                        {
                            xlWorkSheet.Cells[1, j + 1].Font.Bold = true;
                            xlWorkSheet.Cells[1, j + 1] = props[j].Name;
                            xlWorkSheet.Columns[j + 1].ColumnWidth = 22;
                        }

                        Range DateR = (Range)xlWorkSheet.get_Range("M1", string.Format("N{0}", AlarmListData.Count + 1)); //Start End 2열처리
                        DateR.NumberFormat = "yyyy/mm/dd hh:mm:ss";

                        Range CR = (Range)xlWorkSheet.get_Range("A1", string.Format("O{0}", AlarmListData.Count + 1));
                        object[,] only_data = (object[,])CR.get_Value();

                        int row = AlarmListData.Count;
                        int column = CR.Columns.Count;

                        object[,] data = new object[row, column];
                        data = only_data;

                        for (int i = 0; i < AlarmListData.Count; i++)
                        {
                            data[i + 2, 1] = AlarmListData[i].NumberOrder;
                            data[i + 2, 2] = AlarmListData[i].EQPID;
                            data[i + 2, 3] = AlarmListData[i].UnitID;
                            data[i + 2, 4] = AlarmListData[i].CraneNumber;
                            data[i + 2, 5] = AlarmListData[i].EQPType;
                            data[i + 2, 6] = AlarmListData[i].AlarmCode;
                            data[i + 2, 7] = AlarmListData[i].Message;
                            data[i + 2, 8] = AlarmListData[i].Description_KOR;
                            data[i + 2, 9] = AlarmListData[i].Description_ENG;
                            data[i + 2, 10] = AlarmListData[i].Description_CHN;
                            data[i + 2, 11] = AlarmListData[i].Description_HUN;
                            data[i + 2, 12] = AlarmListData[i].LightAlarm;
                            data[i + 2, 13] = AlarmListData[i].StartDttm;
                            data[i + 2, 14] = AlarmListData[i].EndDttm;
                            data[i + 2, 15] = AlarmListData[i].CarrierID;
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
            catch (Exception)
            {

            }
        }
        #endregion
        #endregion

        #region Methods
        #region Constructor
        public ViewModelAlarmLogView()
        {
            bool dbopenstate = false;

            AlarmListData = new ObservableCollection<AlarmLogViewModelData>();
            LogFindCommand = new BindingDelegateCommand(ExcuteLogFind);
            LogInitCommand = new BindingDelegateCommand(ExcuteLogInit);
            LogExportCommand = new BindingDelegateCommand(ExcuteLogExport);
            SpinnerVisible = false;

            ForLogDBManager = new OracleDBManager_Log(out dbopenstate, "Log");
        }
        #endregion
        #region Etc
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
        #endregion
        #endregion
    }

    public class AlarmLogViewModelData
    {
        private uint _NumberOrder;
        public uint NumberOrder
        {
            get => _NumberOrder;
            set
            {
                _NumberOrder = value;
            }
        }

        private string _EQPID;
        public string EQPID
        {
            get => _EQPID;
            set
            {
                _EQPID = value;
            }
        }

        private string _UnitID;
        public string UnitID
        {
            get => _UnitID;
            set
            {
                _UnitID = value;
            }
        }

        private string _CraneNumber;
        public string CraneNumber
        {
            get => _CraneNumber;
            set
            {
                _CraneNumber = value;
            }
        }
        private string _EQPType;
        public string EQPType
        {
            get => _EQPType;
            set
            {
                _EQPType = value;
            }
        }

        private string _AlarmCode;
        public string AlarmCode
        {
            get => _AlarmCode;
            set
            {
                _AlarmCode = value;
            }
        }

        private string _Message;
        public string Message
        {
            get => _Message;
            set
            {
                _Message = value;
            }
        }

        private string _Description_KOR;
        public string Description_KOR
        {
            get => _Description_KOR;
            set
            {
                _Description_KOR = value;
            }
        }

        private string _Description_ENG;
        public string Description_ENG
        {
            get => _Description_ENG;
            set
            {
                _Description_ENG = value;
            }
        }

        private string _Description_CHN;
        public string Description_CHN
        {
            get => _Description_CHN;
            set
            {
                _Description_CHN = value;
            }
        }

        private string _Description_HUN;
        public string Description_HUN
        {
            get => _Description_HUN;
            set
            {
                _Description_HUN = value;
            }
        }

        private string _LightAlarm;
        public string LightAlarm
        {
            get => _LightAlarm;
            set
            {
                _LightAlarm = value;
            }
        }

        private string _StartDttm;
        public string StartDttm
        {
            get => _StartDttm;
            set
            {
                _StartDttm = value;
            }
        }

        private string _EndDttm;
        public string EndDttm
        {
            get => _EndDttm;
            set
            {
                _EndDttm = value;
            }
        }

        private string _CarrierID;
        public string CarrierID
        {
            get => _CarrierID;
            set
            {
                _CarrierID = value;
            }
        }
    }
}
