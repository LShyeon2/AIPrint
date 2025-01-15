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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ViewModels
{
    public class ViewModelTransferLogView : ViewModelBase
    {
        #region Variable
        #region Field
        private OracleDBManager_Log ForLogDBManager;
        private System.Data.DataTable LogList;
        private bool bSearching = false;
        private ObservableCollection<TransferLogViewModelData> prevTransferListData;
        #endregion

        #region Binding
        private ObservableCollection<TransferLogViewModelData> _TransferListData;
        public ObservableCollection<TransferLogViewModelData> TransferListData
        {
            get => _TransferListData;
            set => Set("TransferListData", ref _TransferListData, value);
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

                    if (TransferListData.Count != 0)
                    {
                        prevTransferListData = TransferListData;
                        TransferListData = new ObservableCollection<TransferLogViewModelData>();
                    }

                    ObservableCollection<TransferLogViewModelData> tmp = new ObservableCollection<TransferLogViewModelData>();

                    LogList = ForLogDBManager.DbGetProcedureLogListInfo("TRANSFER", SearchStart, SearchEnd, CarrierID);

                    if (LogList.Rows.Count == 0)
                    {
                        bSearchLogZero = true;
                    }

                    foreach (DataRow dr in LogList.Rows)
                    {
                        tmp.Add(new TransferLogViewModelData()
                        {
                            EQPID = dr["SCS_CD"].ToString(),
                            CommandID = dr["COL_2"].ToString(),
                            Command = dr["COL_3"].ToString(),
                            Status = dr["COL_4"].ToString(),
                            CarrierID = dr["COL_5"].ToString(),
                            MacroSource = dr["COL_6"].ToString(),
                            MacroDest = dr["COL_7"].ToString(),
                            CarrierLoc = dr["COL_8"].ToString(),
                            Priority = dr["COL_9"].ToString(),
                            EndStatus = dr["COL_10"].ToString(),
                            Recode_DTTM = dr["RECODE_DTTM"].ToString(),
                        });
                    }

                    if (ForLogDBManager.bLogSearchCancel)
                    {
                        //TransferListData = prevTransferListData;
                        ForLogDBManager.bLogSearchCancel = false;
                    }
                    //else
                    //{
                    //    TransferListData = tmp;
                    //}
                    TransferListData = tmp;

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
                    TransferListData = new ObservableCollection<TransferLogViewModelData>();
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
                    if (TransferListData.Count == 0 || TransferListData == null)
                        return;

                    //20240112 RGJ Excel Export 시 파일로 바로저장. 조범석 매니저 요청
                    //파일 저장 다이얼로그 오픈.
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = string.Format("{0}-{1}-{2}", GlobalData.Current.EQPID, "TransferLog", DateTime.Now.ToString("yyMMddHHmmss"));
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
                        Type TransferListDataType = TransferListData[0].GetType();
                        PropertyInfo[] props = TransferListDataType.GetProperties();

                        for (int j = 0; j < props.Length; j++)
                        {
                            xlWorkSheet.Cells[1, j + 1].Font.Bold = true;
                            xlWorkSheet.Cells[1, j + 1] = props[j].Name;
                            xlWorkSheet.Columns[j + 1].ColumnWidth = 22;
                        }
                        Range DateR = (Range)xlWorkSheet.get_Range("K2", string.Format("K{0}", TransferListData.Count + 1)); //날짜 포맷 맞추기 위해서 따로 레인지 포맷 설정.
                        DateR.NumberFormat = "yyyy/mm/dd hh:mm:ss";

                        Range CR = (Range)xlWorkSheet.get_Range("A1", string.Format("K{0}", TransferListData.Count + 1));
    
                        object[,] only_data = (object[,])CR.get_Value();

                        int row = TransferListData.Count;
                        int column = CR.Columns.Count;

                        object[,] data = new object[row, column];
                        data = only_data;

                        for (int i = 0; i < TransferListData.Count; i++)
                        {
                            //xlWorkSheet.Cells[i + 2, 1] = TransferListData[i].EQPID;
                            //xlWorkSheet.Cells[i + 2, 2] = TransferListData[i].CommandID;
                            //xlWorkSheet.Cells[i + 2, 3] = TransferListData[i].Type;
                            //xlWorkSheet.Cells[i + 2, 4] = TransferListData[i].Status;
                            //xlWorkSheet.Cells[i + 2, 5] = TransferListData[i].CarrierID;
                            //xlWorkSheet.Cells[i + 2, 6] = TransferListData[i].MacroSource;
                            //xlWorkSheet.Cells[i + 2, 7] = TransferListData[i].MacroDest;
                            //xlWorkSheet.Cells[i + 2, 8] = TransferListData[i].CarrierLoc;
                            //xlWorkSheet.Cells[i + 2, 9] = TransferListData[i].EndStatus;
                            //xlWorkSheet.Cells[i + 2, 10] = TransferListData[i].Recode_DTTM;

                            data[i + 2, 1] = TransferListData[i].EQPID;
                            data[i + 2, 2] = TransferListData[i].CommandID;
                            data[i + 2, 3] = TransferListData[i].Command;
                            data[i + 2, 4] = TransferListData[i].Status;
                            data[i + 2, 5] = TransferListData[i].CarrierID;
                            data[i + 2, 6] = TransferListData[i].MacroSource;
                            data[i + 2, 7] = TransferListData[i].MacroDest;
                            data[i + 2, 8] = TransferListData[i].CarrierLoc;
                            data[i + 2, 9] = TransferListData[i].EndStatus;
                            data[i + 2, 10] = TransferListData[i].Priority;
                            data[i + 2, 11] = TransferListData[i].Recode_DTTM;
                        }

                        CR.Value = data;
                        xlWorkSheet.SaveAs(saveFileDialog.FileName, XlFileFormat.xlOpenXMLWorkbook, null, null, false); //파일로 저장.
                         xlexcel.Quit();
                        string msg = string.Format("Data exported.");
                        MessageBox.Show(msg,"Saved" ,MessageBoxButton.OK,MessageBoxImage.Information);

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
        public ViewModelTransferLogView()
        {
            bool dbopenstate = false;

            TransferListData = new ObservableCollection<TransferLogViewModelData>();
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

    public class TransferLogViewModelData
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

        private string _CommandID;
        public string CommandID
        {
            get => _CommandID;
            set
            {
                _CommandID = value;
            }
        }
        //20230713 RGJ Transfer Log 수정.
        private string _Command;
        public string Command
        {
            get => _Command;
            set
            {
                _Command = value;
            }
        }

        private string _Status;
        public string Status
        {
            get => _Status;
            set
            {
                _Status = value;
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

        private string _MacroSource;
        public string MacroSource
        {
            get => _MacroSource;
            set
            {
                _MacroSource = value;
            }
        }

        private string _MacroDest;
        public string MacroDest
        {
            get => _MacroDest;
            set
            {
                _MacroDest = value;
            }
        }

        private string _CarrierLoc;
        public string CarrierLoc
        {
            get => _CarrierLoc;
            set
            {
                _CarrierLoc = value;
            }
        }

        private string _EndStatus;
        public string EndStatus
        {
            get => _EndStatus;
            set
            {
                _EndStatus = value;
            }
        }
        //20230713 RGJ Transfer Log 수정.
        private string _Priority;
        public string Priority
        {
            get => _Priority;
            set
            {
                _Priority = value;
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


    }
}
