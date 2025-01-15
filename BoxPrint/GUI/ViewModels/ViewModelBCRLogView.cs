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
    public class ViewModelBCRLogView : ViewModelBase
    {
        #region Variable
        #region Field
        private OracleDBManager_Log ForLogDBManager;
        private System.Data.DataTable LogList;
        private bool bSearching = false;
        private ObservableCollection<BCRLogViewModelData> prevBCRListData;
        #endregion

        #region Binding
        private ObservableCollection<BCRLogViewModelData> _BCRListData;
        public ObservableCollection<BCRLogViewModelData> BCRListData
        {
            get => _BCRListData;
            set => Set("BCRListData", ref _BCRListData, value);
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
        private string _TrackID;
        public string TrackID
        {
            get => _TrackID;
            set => Set("TrackID", ref _TrackID, value);
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

                    if (BCRListData.Count != 0)
                    {
                        prevBCRListData = BCRListData;
                        BCRListData = new ObservableCollection<BCRLogViewModelData>();
                    }

                    ObservableCollection<BCRLogViewModelData> tmp = new ObservableCollection<BCRLogViewModelData>();

                    LogList = ForLogDBManager.DbGetProcedureLogListInfo("BCR", SearchStart, SearchEnd, TrackID);
                    
                    if (LogList.Rows.Count == 0)
                    {
                        bSearchLogZero = true;
                    }

                    uint No = 1; //신규 로그 사양 추가.
                    foreach (DataRow dr in LogList.Rows)
                    {
                        tmp.Add(new BCRLogViewModelData()
                        {
                            NumberOrder = No++,
                            EQPID = dr["SCS_CD"].ToString(),
                            TrackID = dr["COL_1"].ToString(),
                            TrackNumber = dr["COL_2"].ToString(),
                            BCRNo = dr["COL_3"].ToString(),
                            ReadData = dr["COL_4"].ToString(),
                            Recode_DTTM = dr["RECODE_DTTM"].ToString(),
                        });
                    }

                    if (ForLogDBManager.bLogSearchCancel)
                    {
                        //BCRListData = prevBCRListData;
                        ForLogDBManager.bLogSearchCancel = false;
                    }
                    //else
                    //{
                    //    BCRListData = tmp;
                    //}
                    BCRListData = tmp;

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
                    TrackID = string.Empty;
                    BCRListData = new ObservableCollection<BCRLogViewModelData>();
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
                    if (BCRListData.Count == 0 || BCRListData == null)
                        return;

                    //20240112 RGJ Excel Export 시 파일로 바로저장. 조범석 매니저 요청
                    //파일 저장 다이얼로그 오픈.
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = string.Format("{0}-{1}-{2}", GlobalData.Current.EQPID, "BCRLog", DateTime.Now.ToString("yyMMddHHmmss"));
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
                        Type BCRListDataType = BCRListData[0].GetType();
                        PropertyInfo[] props = BCRListDataType.GetProperties();

                        for (int j = 0; j < props.Length; j++)
                        {
                            xlWorkSheet.Cells[1, j + 1].Font.Bold = true;
                            xlWorkSheet.Cells[1, j + 1] = props[j].Name;
                            xlWorkSheet.Columns[j + 1].ColumnWidth = 22;
                        }
                        Range DateR = (Range)xlWorkSheet.get_Range("G2", string.Format("G{0}", BCRListData.Count + 1)); //날짜 포맷 맞추기 위해서 따로 레인지 포맷 설정.
                        DateR.NumberFormat = "yyyy/mm/dd hh:mm:ss";

                        Range CR = (Range)xlWorkSheet.get_Range("A1", string.Format("G{0}", BCRListData.Count + 1));
                        object[,] only_data = (object[,])CR.get_Value();

                        int row = BCRListData.Count;
                        int column = CR.Columns.Count;

                        object[,] data = new object[row, column];
                        data = only_data;

                        for (int i = 0; i < BCRListData.Count; i++)
                        {
                            //xlWorkSheet.Cells[i + 2, 1] = BCRListData[i].EQPID;
                            //xlWorkSheet.Cells[i + 2, 2] = BCRListData[i].TrackID;
                            //xlWorkSheet.Cells[i + 2, 3] = BCRListData[i].BCRNo;
                            //xlWorkSheet.Cells[i + 2, 4] = BCRListData[i].ReadData;
                            //xlWorkSheet.Cells[i + 2, 5] = BCRListData[i].Recode_DTTM;
                            data[i + 2, 1] = BCRListData[i].NumberOrder;
                            data[i + 2, 2] = BCRListData[i].EQPID;
                            data[i + 2, 3] = BCRListData[i].TrackID;
                            data[i + 2, 4] = BCRListData[i].TrackNumber;
                            data[i + 2, 5] = BCRListData[i].BCRNo;
                            data[i + 2, 6] = BCRListData[i].ReadData;
                            data[i + 2, 7] = BCRListData[i].Recode_DTTM;
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
        public ViewModelBCRLogView()
        {
            bool dbopenstate = false;
            BCRListData = new ObservableCollection<BCRLogViewModelData>();
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

    public class BCRLogViewModelData
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

        private string _TrackID;
        public string TrackID
        {
            get => _TrackID;
            set
            {
                _TrackID = value;
            }
        }

        private string _TrackNumber;
        public string TrackNumber
        {
            get => _TrackNumber;
            set
            {
                _TrackNumber = value;
            }
        }
        private string _BCRNo;
        public string BCRNo
        {
            get => _BCRNo;
            set
            {
                _BCRNo = value;
            }
        }

        private string _ReadData;
        public string ReadData
        {
            get => _ReadData;
            set
            {
                _ReadData = value;
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
