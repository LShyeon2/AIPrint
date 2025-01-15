using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using OSG.Com.HSMS.Common;
using BoxPrint.DataBase;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.ViewModels.BindingCommand;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
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
    public class ViewModelStoredCarrierView : ViewModelBase
    {
        #region Variable
        #region Field
        private OracleDBManager_Log ForLogDBManager;
        //private System.Data.DataTable LogList;
        private bool bSearching = false;
        #endregion

        #region Binding
        private ObservableCollection<StoredCarrierViewModelData> _CarrierInShelfData;
        public ObservableCollection<StoredCarrierViewModelData> CarrierInShelfData
        {
            get => _CarrierInShelfData;
            set => Set("CarrierInShelfData", ref _CarrierInShelfData, value);
        }

        //2024.08.17 lim, 조범석 매니저 요청으로 추가
        private string _SearchCarrierCount;
        public string SearchCarrierCount
        {
            get => _SearchCarrierCount;
            set => Set("SearchCarrierCount", ref _SearchCarrierCount, value);
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
        public ICommand CarrierSearchCommand { get; private set; }
        private async void ExcuteSearch()
        {
            bool bSearchLogZero = false;

            try
            {
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
                    {
                        bSearching = true;
                    }
                    ObservableCollection<StoredCarrierViewModelData> tmp = new ObservableCollection<StoredCarrierViewModelData>();
                    
                    foreach (var shelfItem in GlobalData.Current.ShelfMgr.AllData)
                    {
                        if(shelfItem.CheckCarrierExist())
                        {
                            if (string.IsNullOrEmpty(CarrierID) || shelfItem.CarrierID.Contains(CarrierID.ToUpper()))
                            {
                                tmp.Add(new StoredCarrierViewModelData()
                                {
                                    EQPID = GlobalData.Current.EQPID,
                                    CarrierID = shelfItem.CarrierID,
                                    CarrierProductEmpty = shelfItem.ProductEmpty.ToString(),
                                    CarrierSize = shelfItem.CarrierSize.ToString(),
                                    CarrierLoc = shelfItem.iLocName.ToString(),
                                    Status = shelfItem.CarrierState.ToString(),
                                    Recode_DTTM = shelfItem.GetCarrierInTime(),
                                });
                            }
                           
                        }

                    }
                    //2024.07.31 lim, port 추가
                    foreach (var cvItem in GlobalData.Current.PortManager.AllCVList)
                    {
                        if (cvItem.CheckCarrierExist())
                        {
                            if (string.IsNullOrEmpty(CarrierID) || cvItem.GetCarrierID().Contains(CarrierID.ToUpper()))
                            {
                                DataList.CarrierItem CItem = cvItem.InSlotCarrier;
                                if (CItem != null)
                                {
                                    tmp.Add(new StoredCarrierViewModelData()
                                    {
                                        EQPID = GlobalData.Current.EQPID,
                                        CarrierID = CItem.CarrierID,
                                        CarrierProductEmpty = CItem.ProductEmpty.ToString(),
                                        CarrierSize = CItem.CarrierSize.ToString(),
                                        CarrierLoc = cvItem.iLocName.ToString(),
                                        Status = CItem.CarrierState.ToString(),
                                        Recode_DTTM = CItem.CarryInTime,
                                    });
                                }
                            }
                        }
                    }

                    //2024.07.31 lim, rm 추가
                    var RModule = GlobalData.Current.mRMManager.FirstRM;
                    DataList.CarrierItem RItem = GlobalData.Current.mRMManager.FirstRM.InSlotCarrier;
                    if (RItem != null)
                    {
                        tmp.Add(new StoredCarrierViewModelData()
                        {
                            EQPID = GlobalData.Current.EQPID,
                            CarrierID = RItem.CarrierID,
                            CarrierProductEmpty = RItem.ProductEmpty.ToString(),
                            CarrierSize = RItem.CarrierSize.ToString(),
                            CarrierLoc = RModule.iLocName.ToString(),
                            Status = RItem.CarrierState.ToString(),
                            Recode_DTTM = RItem.CarryInTime,
                        });
                    }

                    if (GlobalData.Current.SCSType == eSCSType.Dual)
                    {
                        var RModule2 = GlobalData.Current.mRMManager.SecondRM;
                        DataList.CarrierItem RItem2 = GlobalData.Current.mRMManager.SecondRM.InSlotCarrier;
                        if (RItem2 != null)
                        {
                            tmp.Add(new StoredCarrierViewModelData()
                            {
                                EQPID = GlobalData.Current.EQPID,
                                CarrierID = RItem2.CarrierID,
                                CarrierProductEmpty = RItem2.ProductEmpty.ToString(),
                                CarrierSize = RItem2.CarrierSize.ToString(),
                                CarrierLoc = RModule2.iLocName.ToString(),
                                Status = RItem2.CarrierState.ToString(),
                                Recode_DTTM = RItem2.CarryInTime,
                            });
                        }
                    }

                    if (tmp.Count == 0)
                    {
                        bSearchLogZero = true;
                    }

                    if (ForLogDBManager.bLogSearchCancel)
                    {
                        ForLogDBManager.bLogSearchCancel = false;
                    }
                    CarrierInShelfData = tmp;
                    SearchCarrierCount = string.Format("{0} : {1}", TranslationManager.Instance.Translate("Carrier").ToString(), CarrierInShelfData.Count);

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
                    CarrierInShelfData = new ObservableCollection<StoredCarrierViewModelData>();
                    SearchCarrierCount = string.Format("{0} : {1}", TranslationManager.Instance.Translate("Carrier").ToString(), 0);

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
                await Task.Run(() =>
                {
                    if (CarrierInShelfData.Count == 0 || CarrierInShelfData == null)
                    {
                        return;
                    }

                    //파일 저장 다이얼로그 오픈.
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = string.Format("{0}-{1}-{2}", GlobalData.Current.EQPID, "StoredCarrier", DateTime.Now.ToString("yyMMddHHmmss"));
                    saveFileDialog.Filter = "CSV Document|*.csv";
                    saveFileDialog.Title = "Save a CSV File";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    if (saveFileDialog.ShowDialog() == false)
                    {
                        return;
                    }


                    try
                    {
                        StringBuilder sbExportContents = new StringBuilder();
                        sbExportContents.AppendLine("EQPID,CARRIER_ID,CARRIER_LOCATION,PRODUCTEMPTY,SIZE,STATUS,DTTM"); // 헤더 추가

                        foreach (var carrier in CarrierInShelfData)
                        {
                            sbExportContents.AppendLine($"{carrier.EQPID},{carrier.CarrierID},{carrier.CarrierLoc},{carrier.CarrierProductEmpty},{carrier.CarrierSize},{carrier.Status},{carrier.Recode_DTTM}");
                        }

                        File.WriteAllText(saveFileDialog.FileName, sbExportContents.ToString());

                        string msg = string.Format("Data exported to {0}.", saveFileDialog.FileName);
                        MessageBox.Show(msg,"Saved" ,MessageBoxButton.OK,MessageBoxImage.Information);

                    }
                    catch (Exception ex)
                    {
                        Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                        string msg = string.Format("Failed exported to {0}.", saveFileDialog.FileName);
                        MessageBox.Show(msg, "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
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
        public ViewModelStoredCarrierView()
        {
            bool dbopenstate = false;

            SearchCarrierCount = string.Format("{0} : {1}", TranslationManager.Instance.Translate("Carrier").ToString(), 0);
            CarrierInShelfData = new ObservableCollection<StoredCarrierViewModelData>();
            CarrierSearchCommand = new BindingDelegateCommand(ExcuteSearch);
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

    public class StoredCarrierViewModelData
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

        private string _CarrierID;
        public string CarrierID
        {
            get => _CarrierID;
            set
            {
                _CarrierID = value;
            }
        }

        private string _CarrierProductEmpty;
        public string CarrierProductEmpty
        {
            get => _CarrierProductEmpty;
            set
            {
                _CarrierProductEmpty = value;
            }
        }

        private string _CarrierSize;
        public string CarrierSize
        {
            get => _CarrierSize;
            set
            {
                _CarrierSize = value;
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


        private string _CarrierLoc;
        public string CarrierLoc
        {
            get => _CarrierLoc;
            set
            {
                _CarrierLoc = value;
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
