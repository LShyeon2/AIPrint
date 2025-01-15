using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using BoxPrint.DataBase;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.ViewModels.BindingCommand;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ViewModels
{
    public class ViewModelHSMSLogView : ViewModelBase
    {
        #region Variable
        #region Field
        private OracleDBManager_Log ForLogDBManager;
        private int SECS2LogLimit = 500; //로그 항목 너무 많이 선택시 행걸리므로 제한을 둔다. 
        private System.Data.DataTable LogList;
        private bool bSearching = false;
        private ObservableCollection<HSMSLogViewModelData> prevHSMSListData;
        #endregion

        #region Binding
        private ObservableCollection<HSMSLogViewModelData> _HSMSListData;
        public ObservableCollection<HSMSLogViewModelData> HSMSListData
        {
            get => _HSMSListData;
            set => Set("HSMSListData", ref _HSMSListData, value);
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
        private string _SysByte;
        public string SysByte
        {
            get => _SysByte;
            set => Set("SysByte", ref _SysByte, value);
        }
        private string _CEID;
        public string CEID
        {
            get => _CEID;
            set => Set("CEID", ref _CEID, value);
        }
        private string _CarrierID;
        public string CarrierID
        {
            get => _CarrierID;
            set => Set("CarrierID", ref _CarrierID, value);
        }
        private bool _SpinnerVisible;
        public bool SpinnerVisible
        {
            get => _SpinnerVisible;
            set => Set("SpinnerVisible", ref _SpinnerVisible, value);
        }
        private string _Secs2Log;
        public string Secs2Log
        {
            get => _Secs2Log;
            set => Set("Secs2Log", ref _Secs2Log, value);
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
                    Secs2Log = string.Empty;
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

                    if (HSMSListData.Count != 0)
                    {
                        prevHSMSListData = HSMSListData;
                        HSMSListData = new ObservableCollection<HSMSLogViewModelData>();
                    }

                    ObservableCollection<HSMSLogViewModelData> tmp = new ObservableCollection<HSMSLogViewModelData>();

                    LogList = ForLogDBManager.DbGetProcedureLogListInfo("HSMS", SearchStart, SearchEnd, SysByte, CEID, CarrierID);

                    if (LogList.Rows.Count == 0)
                    {
                        bSearchLogZero = true;
                    }

                    foreach (DataRow dr in LogList.Rows)
                    {
                        tmp.Add(new HSMSLogViewModelData()
                        {
                            EQPID = dr["SCS_CD"].ToString(), //EQPID 추가
                            Direction = dr["COL_1"].ToString(),
                            StreamFunction = dr["COL_2"].ToString(),
                            SysByte = dr["COL_3"].ToString(),
                            MSGNM = dr["COL_4"].ToString(),
                            UNITID = dr["COL_5"].ToString(),
                            CEID = dr["COL_6"].ToString(),
                            JobID = dr["COL_9"].ToString(),
                            CarrierID = dr["COL_10"].ToString(),
                            Recode_DTTM = dr["RECODE_DTTM"].ToString(),
                            SECS2 = dr["HIST_SECS2"].ToString(),
                        });
                    }

                    if (ForLogDBManager.bLogSearchCancel)
                    {
                        //HSMSListData = prevHSMSListData;
                        ForLogDBManager.bLogSearchCancel = false;
                    }
                    //else
                    //{
                    //    HSMSListData = tmp;
                    //}
                    HSMSListData = tmp;

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
                    CarrierID = string.Empty;
                    CEID = string.Empty;
                    SysByte = string.Empty;
                    Secs2Log = string.Empty;
                    HSMSListData = new ObservableCollection<HSMSLogViewModelData>();
                });
            }
            catch(Exception)
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
                    if (HSMSListData.Count == 0 || HSMSListData == null)
                        return;

                    //20240112 RGJ Excel Export 시 파일로 바로저장. 조범석 매니저 요청
                    //파일 저장 다이얼로그 오픈.
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = string.Format("{0}-{1}-{2}", GlobalData.Current.EQPID, "HSMSLog", DateTime.Now.ToString("yyMMddHHmmss"));
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
                        Type HSMSListDataType = HSMSListData[0].GetType();
                        PropertyInfo[] props = HSMSListDataType.GetProperties();

                        for (int j = 0; j < props.Length; j++)
                        {
                            xlWorkSheet.Cells[1, j + 1].Font.Bold = true;
                            xlWorkSheet.Cells[1, j + 1] = props[j].Name;
                            xlWorkSheet.Columns[j + 1].ColumnWidth = 22;
                        }
                        Range DateR = (Range)xlWorkSheet.get_Range("J2", string.Format("J{0}", HSMSListData.Count + 1)); //날짜 포맷 맞추기 위해서 따로 레인지 포맷 설정.
                        DateR.NumberFormat = "yyyy/mm/dd hh:mm:ss";

                        Range CR = (Range)xlWorkSheet.get_Range("A1", string.Format("K{0}", HSMSListData.Count + 1));
                        object[,] only_data = (object[,])CR.get_Value();

                        int row = HSMSListData.Count;
                        int column = CR.Columns.Count;

                        object[,] data = new object[row, column];
                        data = only_data;

                        for (int i = 0; i < HSMSListData.Count; i++)
                        {
                            data[i + 2, 1] = HSMSListData[i].EQPID;
                            data[i + 2, 2] = HSMSListData[i].StreamFunction;
                            data[i + 2, 3] = HSMSListData[i].CEID;
                            data[i + 2, 4] = HSMSListData[i].Direction;
                            data[i + 2, 5] = HSMSListData[i].SysByte;
                            data[i + 2, 6] = HSMSListData[i].MSGNM;
                            data[i + 2, 7] = HSMSListData[i].UNITID;
                            data[i + 2, 8] = HSMSListData[i].JobID; //순서 잘못 배치된 부분 수정.
                            data[i + 2, 9] = HSMSListData[i].CarrierID;
                            data[i + 2, 10] = string.Format("{0:yyyy-MM-dd HH:mm:ss}", HSMSListData[i].Recode_DTTM);
                            data[i + 2, 11] = HSMSListData[i].SECS2; //Raw Log 추가.
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
        /// <summary>
        /// DataGrid 다중 선택 커맨드
        /// </summary>
        public ICommand DataGridSelectChangeCommand { get; private set; }
        /// <summary>
        /// DataGrid 다중 선택 커맨드 동작
        /// </summary>
        /// <param name="selectData">선택된 항목</param>
        private void ExcuteDataGridSelectChangeCommand(IList selectData)
        {
            Secs2Log = string.Empty;
            SetSecs2Log(selectData);
        }
        #endregion
        #endregion

        #region Methods
        #region Constructor
        public ViewModelHSMSLogView()
        {
            bool dbopenstate = false;

            HSMSListData = new ObservableCollection<HSMSLogViewModelData>();
            prevHSMSListData = new ObservableCollection<HSMSLogViewModelData>();
            LogFindCommand = new BindingDelegateCommand(ExcuteLogFind);
            LogInitCommand = new BindingDelegateCommand(ExcuteLogInit);
            LogExportCommand = new BindingDelegateCommand(ExcuteLogExport);
            DataGridSelectChangeCommand = new BindingDelegateCommand<IList>(ExcuteDataGridSelectChangeCommand);
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
        /// <summary>
        /// 선택된 항목을 리스트화 하여 SECS2 Log를 Text Box에 기입한다.
        /// </summary>
        /// <param name="selectData">선택된 항목들</param>
        private void SetSecs2Log(IList selectData)
        {
            try
            {
                int index = 0;
                StringBuilder SB = new StringBuilder();
                List<HSMSLogViewModelData> tmp = selectData.Cast<HSMSLogViewModelData>().ToList().OrderByDescending(r => r.Recode_DTTM).ToList();
                foreach (HSMSLogViewModelData data in tmp)
                {
                    if(index > SECS2LogLimit) //SECS2 리미트 제한
                    {
                        break;
                    }
                    //Secs2Log += data.SECS2;
                    //Secs2Log += '\n';
                    SB.AppendLine(data.SECS2);
                    index++;
                }
                Secs2Log = SB.ToString();
            }
            catch
            {
                Secs2Log = string.Empty;
            }
        }
        #endregion
        #endregion
    }

    public class HSMSLogViewModelData
    {
        private string _EQPID;
        public string EQPID
        {
            get => _EQPID;
            set
            {
                _EQPID = value;
            }
        }
        private string _StreamFunction;
        public string StreamFunction
        {
            get => _StreamFunction;
            set
            {
                _StreamFunction = value;
            }
        }

        private string _CEID;
        public string CEID
        {
            get => _CEID;
            set
            {
                _CEID = value;
            }
        }

        private string _Direction;
        public string Direction
        {
            get => _Direction;
            set
            {
                _Direction = value;
            }
        }
        private string _SysByte;
        public string SysByte
        {
            get => _SysByte;
            set
            {
                _SysByte = value;
            }
        }
        private string _MSGNM;
        public string MSGNM
        {
            get => _MSGNM;
            set
            {
                _MSGNM = value;
            }
        }

        private string _UNITID;
        public string UNITID
        {
            get => _UNITID;
            set
            {
                _UNITID = value;
            }
        }

        private string _JobID;
        public string JobID
        {
            get => _JobID;
            set
            {
                _JobID = value;
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

        private string _Recode_DTTM;
        public string Recode_DTTM
        {
            get => _Recode_DTTM;
            set
            {
                _Recode_DTTM = value;
            }
        }

        private string _SECS2;
        public string SECS2
        {
            get => _SECS2;
            set
            {
                _SECS2 = value;
            }
        }
    }
}
