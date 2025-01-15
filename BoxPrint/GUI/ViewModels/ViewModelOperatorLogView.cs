using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using BoxPrint.DataBase;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.ViewModels.BindingCommand;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
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
    public class ViewModelOperatorLogView : ViewModelBase
    {
        #region Variable
        #region Field
        private OracleDBManager_Log ForLogDBManager;
        private System.Data.DataTable LogList;
        private bool bSearching = false;
        private ObservableCollection<OperatorLogViewModelData> prevOperatorListData;
        #endregion

        #region Binding
        private ObservableCollection<OperatorLogViewModelData> _OperatorListData;
        public ObservableCollection<OperatorLogViewModelData> OperatorListData
        {
            get => _OperatorListData;
            set => Set("OperatorListData", ref _OperatorListData, value);
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
        private string _PCIP;
        public string PCIP
        {
            get => _PCIP;
            set => Set("PCIP", ref _PCIP, value);
        }
        private string _UserID;
        public string UserID
        {
            get => _UserID;
            set => Set("UserID", ref _UserID, value);
        }
        private string _Action;
        public string Action
        {
            get => _Action;
            set => Set("Action", ref _Action, value);
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

        private Dictionary<int, string> KorMessagedic;
        private Dictionary<int, string> EngMessagedic;
        private Dictionary<int, string> ChnMessagedic;
        private Dictionary<int, string> HunMessagedic;
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

                    if (OperatorListData.Count != 0)
                    {
                        prevOperatorListData = OperatorListData;
                        OperatorListData = new ObservableCollection<OperatorLogViewModelData>();
                    }

                    ObservableCollection<OperatorLogViewModelData> tmp = new ObservableCollection<OperatorLogViewModelData>();

                    if (Action == "ALL")
                        Action = string.Empty;

                    //LogList = GlobalData.Current.DBManager.DbGetProcedureLogListInfo("OPERATOR", SearchStart, SearchEnd.AddHours(24), PCIP, UserID, Action);
                    LogList = ForLogDBManager.DbGetProcedureLogListInfo("OPERATOR", SearchStart, SearchEnd, PCIP, UserID, Action);

                    if (LogList.Rows.Count == 0)
                    {
                        bSearchLogZero = true;
                    }

                    uint No = 1; //신규 로그 사양 추가.
                    string tempmsg; //번역
                    foreach (DataRow dr in LogList.Rows)
                    {//추가수정 필요
                        OperatorLogViewModelData item = new OperatorLogViewModelData();

                        item.NumberOrder = No++;
                        item.SaveDttm = dr["RECODE_DTTM"].ToString();
                        item.UserID = dr["COL_7"].ToString();
                        //item.Action = dr["COL_2"].ToString();
                        //item.Category = dr["COL_3"].ToString();
                        item.PCIP = dr["COL_4"].ToString();
                        item.PCLoc = dr["COL_5"].ToString();
                        item.PCName = dr["COL_6"].ToString();
                        if (!string.IsNullOrEmpty(dr["COL_8"].ToString()))
                        {
                            item.MessageType = Convert.ToInt32(dr["COL_8"]);
                            string Lang_buf = TranslationManager.Instance.CurrentLanguage.ToString();
                            CultureInfo changeCulture = new CultureInfo(Lang_buf);
                            if (KorMessagedic.TryGetValue(item.MessageType, out tempmsg))
                            {
                                changeCulture = new CultureInfo("ko-KR");
                                TranslationManager.Instance.CurrentLanguage = changeCulture;
                                item.KRAction = TranslationManager.Instance.Translate_Log(dr["COL_2"].ToString()).ToString();
                                item.KRCategory = TranslationManager.Instance.Translate_Log(dr["COL_3"].ToString()).ToString();

                                if (string.IsNullOrEmpty(dr["COL_13"].ToString()))
                                {
                                    item.KRMessage = string.Format(tempmsg,
                                        TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                }
                                else
                                {
                                    if (dr["COL_9"].ToString() == "Info Message")
                                    {
                                        string strMessage = TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString();
                                        strMessage = string.Format(strMessage,
                                                                   TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                                                   TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString());

                                        item.KRMessage = string.Format(tempmsg,
                                            TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString(),
                                            strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                    }
                                    else
                                    {
                                        string strMessage = TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString();
                                        strMessage = string.Format(strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString(),
                                            TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                            TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString());

                                        item.KRMessage = string.Format(tempmsg, strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                    }
                                }

                                if (string.IsNullOrEmpty(item.KRMessage))
                                {
                                    if (item.KRMessage.Contains("\n"))
                                    {
                                        item.KRMessage = item.KRMessage.Replace("\n", "");
                                    }
                                }
                            }
                            if (EngMessagedic.TryGetValue(item.MessageType, out tempmsg))
                            {
                                changeCulture = new CultureInfo("en-US");
                                TranslationManager.Instance.CurrentLanguage = changeCulture;
                                item.USAction = TranslationManager.Instance.Translate_Log(dr["COL_2"].ToString()).ToString();
                                item.USCategory = TranslationManager.Instance.Translate_Log(dr["COL_3"].ToString()).ToString();

                                if (string.IsNullOrEmpty(dr["COL_13"].ToString()))
                                {
                                    item.USMessage = string.Format(tempmsg,
                                        TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                }
                                else
                                {
                                    if (dr["COL_9"].ToString() == "Info Message")
                                    {
                                        string strMessage = TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString();
                                        strMessage = string.Format(strMessage,
                                                                   TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                                                   TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString());

                                        item.USMessage = string.Format(tempmsg,
                                            TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString(),
                                            strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                    }
                                    else
                                    {
                                        string strMessage = TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString();
                                        strMessage = string.Format(strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString(),
                                            TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                            TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString());

                                        item.USMessage = string.Format(tempmsg, strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                    }
                                }

                                if (string.IsNullOrEmpty(item.USMessage))
                                {
                                    if (item.USMessage.Contains("\n"))
                                    {
                                        item.USMessage = item.USMessage.Replace("\n", "");
                                    }
                                }
                            }
                            if (ChnMessagedic.TryGetValue(item.MessageType, out tempmsg))
                            {
                                changeCulture = new CultureInfo("zh-CN");
                                TranslationManager.Instance.CurrentLanguage = changeCulture;
                                item.CNAction = TranslationManager.Instance.Translate_Log(dr["COL_2"].ToString()).ToString();
                                item.CNCategory = TranslationManager.Instance.Translate_Log(dr["COL_3"].ToString()).ToString();

                                if (string.IsNullOrEmpty(dr["COL_13"].ToString()))
                                {
                                    item.CNMessage = string.Format(tempmsg,
                                        TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                }
                                else
                                {
                                    if (dr["COL_9"].ToString() == "Info Message")
                                    {
                                        string strMessage = TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString();
                                        strMessage = string.Format(strMessage,
                                                                   TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                                                   TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString());

                                        item.CNMessage = string.Format(tempmsg,
                                            TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString(),
                                            strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                    }
                                    else
                                    {
                                        string strMessage = TranslationManager.Instance.Translate(dr["COL_9"].ToString()).ToString();
                                        strMessage = string.Format(strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString(),
                                            TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                            TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString());

                                        item.CNMessage = string.Format(tempmsg, strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                    }
                                }

                                if (string.IsNullOrEmpty(item.CNMessage))
                                {
                                    if (item.CNMessage.Contains("\n"))
                                    {
                                        item.CNMessage = item.CNMessage.Replace("\n", "");
                                    }
                                }
                            }
                            if (HunMessagedic.TryGetValue(item.MessageType, out tempmsg))
                            {
                                changeCulture = new CultureInfo("hu-HU");
                                TranslationManager.Instance.CurrentLanguage = changeCulture;
                                item.HUAction = TranslationManager.Instance.Translate_Log(dr["COL_2"].ToString()).ToString();
                                item.HUCategory = TranslationManager.Instance.Translate_Log(dr["COL_3"].ToString()).ToString();

                                if (string.IsNullOrEmpty(dr["COL_13"].ToString()))
                                {
                                    item.HUMessage = string.Format(tempmsg,
                                        TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString(),
                                        TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                }
                                else
                                {
                                    if (dr["COL_9"].ToString() == "Info Message")
                                    {
                                        string strMessage = TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString();
                                        strMessage = string.Format(strMessage,
                                                                   TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                                                   TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString());

                                        item.HUMessage = string.Format(tempmsg,
                                            TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString(),
                                            strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                    }
                                    else
                                    {
                                        string strMessage = TranslationManager.Instance.Translate_Log(dr["COL_9"].ToString()).ToString();
                                        strMessage = string.Format(strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_10"].ToString()).ToString(),
                                            TranslationManager.Instance.Translate_Log(dr["COL_11"].ToString()).ToString(),
                                            TranslationManager.Instance.Translate_Log(dr["COL_12"].ToString()).ToString());

                                        item.HUMessage = string.Format(tempmsg, strMessage,
                                            TranslationManager.Instance.Translate_Log(dr["COL_13"].ToString()).ToString());
                                    }
                                }

                                if (string.IsNullOrEmpty(item.HUMessage))
                                {
                                    if (item.HUMessage.Contains("\n"))
                                    {
                                        item.HUMessage = item.HUMessage.Replace("\n", "");
                                    }
                                }
                            }
                            changeCulture = new CultureInfo(Lang_buf);
                            TranslationManager.Instance.CurrentLanguage = changeCulture;
                            tmp.Add(item);
                        }
                        //tmp.Add(new OperatorLogViewModelData()
                        //{
                        //    NumberOrder = No++,
                        //    SaveDttm = dr["RECODE_DTTM"].ToString(),
                        //    UserID = dr["COL_7"].ToString(),
                        //    Action = dr["COL_2"].ToString(),
                        //    Category = dr["COL_3"].ToString(),
                        //    //Message = dr["COL_1"].ToString(),
                        //    PCIP = dr["COL_4"].ToString(),
                        //    PCLoc = dr["COL_5"].ToString(),
                        //    PCName = dr["COL_6"].ToString(),
                        //    MessageType = Convert.ToInt32(dr["COL_8"]),
                        //}) ;
                    }
                    
                    if (ForLogDBManager.bLogSearchCancel)
                    {
                        //OperatorListData = prevOperatorListData;
                        ForLogDBManager.bLogSearchCancel = false;
                    }
                    //else
                    //{
                    //    OperatorListData = tmp;
                    //}
                    OperatorListData = tmp;

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
                    PCIP = string.Empty;
                    UserID = string.Empty;
                    Action = string.Empty;
                    OperatorListData = new ObservableCollection<OperatorLogViewModelData>();
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
                    if (OperatorListData.Count == 0 || OperatorListData == null)
                        return;

                    //20240112 RGJ Excel Export 시 파일로 바로저장. 조범석 매니저 요청
                    //파일 저장 다이얼로그 오픈.
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = string.Format("{0}-{1}-{2}", GlobalData.Current.EQPID, "OperatorLog", DateTime.Now.ToString("yyMMddHHmmss"));
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
                        Type OperatorListDataType = OperatorListData[0].GetType();
                        PropertyInfo[] props = OperatorListDataType.GetProperties();

                        for (int j = 0; j < props.Length; j++)
                        {
                            xlWorkSheet.Cells[1, j + 1].Font.Bold = true;
                            xlWorkSheet.Cells[1, j + 1] = props[j].Name;
                            xlWorkSheet.Columns[j + 1].ColumnWidth = 22;
                        }
                        Range DateR = (Range)xlWorkSheet.get_Range("B2", string.Format("B{0}", OperatorListData.Count + 1)); //날짜 포맷 맞추기 위해서 따로 레인지 포맷 설정.
                        DateR.NumberFormat = "yyyy/mm/dd hh:mm:ss";

                        Range CR = (Range)xlWorkSheet.get_Range("A1", string.Format("R{0}", OperatorListData.Count + 1));
                        object[,] only_data = (object[,])CR.get_Value();

                        int row = OperatorListData.Count;
                        int column = CR.Columns.Count;

                        object[,] data = new object[row, column];
                        data = only_data;

                        for (int i = 0; i < OperatorListData.Count; i++)
                        {
                            data[i + 2, 1] = OperatorListData[i].NumberOrder;
                            data[i + 2, 2] = OperatorListData[i].SaveDttm;
                            data[i + 2, 3] = OperatorListData[i].UserID;
                            data[i + 2, 4] = OperatorListData[i].KRAction;
                            data[i + 2, 5] = OperatorListData[i].USAction;
                            data[i + 2, 6] = OperatorListData[i].CNAction;
                            data[i + 2, 7] = OperatorListData[i].HUAction;
                            data[i + 2, 8] = OperatorListData[i].KRCategory;
                            data[i + 2, 9] = OperatorListData[i].USCategory;
                            data[i + 2, 10] = OperatorListData[i].CNCategory;
                            data[i + 2, 11] = OperatorListData[i].HUCategory;
                            data[i + 2, 12] = OperatorListData[i].KRMessage;
                            data[i + 2, 13] = OperatorListData[i].USMessage;
                            data[i + 2, 14] = OperatorListData[i].CNMessage;
                            data[i + 2, 15] = OperatorListData[i].HUMessage;
                            data[i + 2, 16] = OperatorListData[i].PCIP;
                            data[i + 2, 17] = OperatorListData[i].PCLoc;
                            data[i + 2, 18] = OperatorListData[i].PCName;
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
        public ViewModelOperatorLogView()
        {
            bool dbopenstate = false;
            OperatorListData = new ObservableCollection<OperatorLogViewModelData>();
            LogFindCommand = new BindingDelegateCommand(ExcuteLogFind);
            LogInitCommand = new BindingDelegateCommand(ExcuteLogInit);
            LogExportCommand = new BindingDelegateCommand(ExcuteLogExport);
            SpinnerVisible = false;

            KorMessagedic = new Dictionary<int, string>()
            {
                {1, "사용자가 {0} 화면을 Open하였습니다."},
                {2, "사용자가 {0} 화면에서 {1} 을/를 Click하였습니다. {2}"},
                {3, "사용자가 {0} 화면에서 {1} 을/를 Click하였습니다." },
                {4, "사용자가 {0} {1} 을/를 Click하였습니다."},
                {5, "사용자가 {0} 에서 {1} 을/를 Click하였습니다." },
                {6, "사용자가 {0} {1} {2} 을/를 Click하였습니다." },
                {7, "사용자가 {0}의 {1} 을/를 Click하였습니다." },
                {8, "사용자가 {0}의 {1} 을/를 Right Click하였습니다." },
                {9, "사용자가 {0}의 수동지시 상세 {1} 을/를 Click하였습니다." },
                {10, "사용자가 {0} {1}에 {2} 을/를 Write하였습니다." },
                {11, "사용자가 Playback 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다." },
                {12, "사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다." },
                {13, "사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다." },
                {14, "사용자가 Playback Crane {0}의 수동지시 {1} 을/를 Click하였습니다." },
                {15, "사용자가 Shelf {0}의 수동지시 {1} 을/를 Click하였습니다." },
                {16, "사용자가 {0} {1} 을/를 {2} Click하였습니다." },
                {17, "사용자가 알람 {0} 을/를 {1} Click하였습니다." },
                {18, "사용자가 {0} 의 {1} Menu Open하였습니다." },
                {19, "사용자가 언어를 {0} 으로 변경하였습니다." },
                {20, "사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Write하였습니다." },
                {21, "사용자가 Crane {0}의 수동지시 {1} 을/를 Write하였습니다." },
                {22, "사용자가 Shelf {0}의 수동지시 {1} 을/를 Write하였습니다." },
            };

            EngMessagedic = new Dictionary<int, string>()
            {
                {1, "Operator Opened {0} Page." },
                {2, "Operator Clicked {1} on {0} Page. {2}" },
                {3, "Operator Clicked {1} on {0} Page." },
                {4, "Operator Clicked {1} {0}." },
                {5, "Operator Clicked {1} on {0}." },
                {6, "Operator Clicked {2} {0} {1}." },
                {7, "Operator Clicked {1} of {0}." },
                {8, "Operator Right Clicked {1} of {0}." },
                {9, "Operator Clicked Manual Command Detail {1} of {0}." },
                {10, "Operator Wrote {2} on {0} {1}." },
                {11, "Operator Clicked Manual Command {1} of Playback Conveyor {0}." },
                {12, "Operator Clicked Manual Command {1} of Conveyor {0}." },
                {13, "Operator Clicked Manual Command {1} of Crane {0}." },
                {14, "Operator Clicked Manual Command {1} of Playback Crane {0}." },
                {15, "Operator Clicked Manual Command {1} of Shelf {0}." },
                {16, "Operator Clicked {2} for {0} {1}." },
                {17, "Operator Clicked {1} for Alarm {0}." },
                {18, "Operator Opened {1} Menu of {0}." },
                {19, "Operator Changed Language to {0}." },
                {20, "Operator Wrote Manual Command {1} of Conveyor {0}." },
                {21, "Operator Wrote Manual Command {1} of Crane {0}." },
                {22, "Operator Wrote Manual Command {1} of Shelf {0}." },
            };

            ChnMessagedic = new Dictionary<int, string>()
            {
                {1, "操作員打開{0}頁。"},
                {2, "操作員在{0}頁上點擊{1}。{2}"},
                {3, "操作員在{0}頁上點擊{1}。" },
                {4, "操作員點擊{1}{0}。"},
                {5, "操作員在{0}上點擊{1}。" },
                {6, "操作員點擊{2}{0}{1}。" },
                {7, "操作員點擊{0}的{1}。" },
                {8, "操作員右鍵點擊{0}的{1}。" },
                {9, "操作員在{0}上點擊手動命令細節{1}。" },
                {10, "操作員在{0}{1}上寫{2}。" },
                {11, "操作員在回放輸送帶{0}上點擊手動命令{1}。" },
                {12, "操作員在輸送帶{0}上點擊手動命令{1}。" },
                {13, "操作員在吊車{0}上點擊手動命令{1}。" },
                {14, "操作員在回放吊車{0}上點擊手動命令{1}。" },
                {15, "操作員在架子{0}上點擊手動命令{1}。" },
                {16, "操作員點擊{0}{1}的{2}。" },
                {17, "操作員爲警報{0}點擊{1}。" },
                {18, "操作員打開{0}的{1}菜單。" },
                {19, "操作員把語言改爲{0}。" },
                {20, "操作員寫輸送帶{0}的手動命令{1}。" },
                {21, "操作員寫吊車{0}的手動命令{1}。" },
                {22, "操作員寫架子{0}的手動命令{1}。" },
            };

            HunMessagedic = new Dictionary<int, string>()
            {
                {1, "Operátor Megnyitotta {0} Oldalt." },
                {2, "Operátor a {1} elemre Kattintott a {0} Oldalon. {2}" },
                {3, "Operátor a {1} elemre Kattintott a {0} Oldalon." },
                {4, "Operátor Kattintott {1} {0}." },
                {5, "Operátor Kattintott {1} a {0}-re." },
                {6, "Operátor Kattintott {2} {0} {1}." },
                {7, "Operátor Kattintott {1} / {0}." },
                {8, "Operátor Jobb Kattintott {1} / {0}." },
                {9, "Operátor Kattintott Kézi Parancs Részlet {1} / {0}." },
                {10, "Operátor {2}-t írt {0} {1}-re." },
                {11, "Operátor Kattintott Kézi Parancs {1} / Lejatszas Szállítószalag {0}." },
                {12, "Operátor Kattintott Kézi Parancs {1} / Szállítószalag {0}." },
                {13, "Operátor Kattintott Kézi Parancs {1} / Daru {0}." },
                {14, "Operátor Kattintott Kézi Parancs {1} / Lejatszas Daru {0}." },
                {15, "Operátor Kattintott Kézi Parancs {1} / Polc {0}." },
                {16, "Operátor Kattintott {2} a következőhöz {0} {1}." },
                {17, "Operátor Kattintott {1} a következőhöz Riadó {0}." },
                {18, "Operator Megnyitotta a(z) {0} {1} Menüjét." },
                {19, "Operátor Nyelvváltoztatás {0}-re." },
                {20, "Operátor a {0} Szállítószalag Kézi Parancsát {1} Írta." },
                {21, "Operátor a {0} Daru Kézi Parancsát {1} Írta." },
                {22, "Operátor a {0} Polc Kézi Parancsát {1} Írta." },
            };

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

    public class OperatorLogViewModelData
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

        private string _SaveDttm;
        public string SaveDttm
        {
            get => _SaveDttm;
            set
            {
                _SaveDttm = value;
            }
        }

        private string _UserID;
        public string UserID
        {
            get => _UserID;
            set
            {
                _UserID = value;
            }
        }

        private string _KRAction;
        public string KRAction
        {
            get => _KRAction;
            set
            {
                _KRAction = value;
            }
        }

        private string _USAction;
        public string USAction
        {
            get => _USAction;
            set
            {
                _USAction = value;
            }
        }

        private string _CNAction;
        public string CNAction
        {
            get => _CNAction;
            set
            {
                _CNAction = value;
            }
        }

        private string _HUAction;
        public string HUAction
        {
            get => _HUAction;
            set
            {
                _HUAction = value;
            }
        }

        private string _KRCategory;
        public string KRCategory
        {
            get => _KRCategory;
            set
            {
                _KRCategory = value;
            }
        }

        private string _USCategory;
        public string USCategory
        {
            get => _USCategory;
            set
            {
                _USCategory = value;
            }
        }

        private string _CNCategory;
        public string CNCategory
        {
            get => _CNCategory;
            set
            {
                _CNCategory = value;
            }
        }

        private string _HUCategory;
        public string HUCategory
        {
            get => _HUCategory;
            set
            {
                _HUCategory = value;
            }
        }

        private string _KRMessage;
        public string KRMessage
        {
            get => _KRMessage;
            set
            {
                _KRMessage = value;
            }
        }

        private string _USMessage;
        public string USMessage
        {
            get => _USMessage;
            set
            {
                _USMessage = value;
            }
        }

        private string _CNMessage;
        public string CNMessage
        {
            get => _CNMessage;
            set
            {
                _CNMessage = value;
            }
        }

        private string _HUMessage;
        public string HUMessage
        {
            get => _HUMessage;
            set
            {
                _HUMessage = value;
            }
        }

        private string _PCIP;
        public string PCIP
        {
            get => _PCIP;
            set
            {
                _PCIP = value;
            }
        }

        private string _PCLoc;
        public string PCLoc
        {
            get => _PCLoc;
            set
            {
                _PCLoc = value;
            }
        }

        private string _PCName;
        public string PCName
        {
            get => _PCName;
            set
            {
                _PCName = value;
            }
        }

        private int _MessageType;
        public int MessageType
        {
            get => _MessageType;
            set
            {
                _MessageType = value;
            }
        }
    }
}
