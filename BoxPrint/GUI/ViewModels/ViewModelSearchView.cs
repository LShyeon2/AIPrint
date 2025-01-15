using BoxPrint.DataList;
//220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.Views;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BoxPrint.GUI.ViewModels
{
    public class ViewModelSearchView : ViewModelBase
    {
        private double _HeaderTextSize;
        public double HeaderTextSize
        {
            get => _HeaderTextSize;
            set => Set("HeaderTextSize", ref _HeaderTextSize, value);
        }
        private double _BodyTextSize;
        public double BodyTextSize
        {
            get => _BodyTextSize;
            set => Set("BodyTextSize", ref _BodyTextSize, value);
        }

        private ObservableCollection<SearchViewModelData> _CarrierListData;
        public ObservableCollection<SearchViewModelData> CarrierListData
        {
            get => _CarrierListData;
            set => Set("CarrierListData", ref _CarrierListData, value);
        }

        private ObservableCollection<SearchViewModelData> _SearchListData;
        public ObservableCollection<SearchViewModelData> SearchListData
        {
            get => _SearchListData;
            set => Set("SearchListData", ref _SearchListData, value);
        }

        private ObservableCollection<string> _BankList;
        public ObservableCollection<string> BankList
        {
            get => _BankList;
            set => Set("BankList", ref _BankList, value);
        }

        private ObservableCollection<string> _BayList;
        public ObservableCollection<string> BayList
        {
            get => _BayList;
            set => Set("BayList", ref _BayList, value);
        }

        private ObservableCollection<string> _LevelList;
        public ObservableCollection<string> LevelList
        {
            get => _LevelList;
            set => Set("LevelList", ref _LevelList, value);
        }

        private ObservableCollection<string> _PortList;
        public ObservableCollection<string> PortList
        {
            get => _PortList;
            set => Set("PortList", ref _PortList, value);
        }

        private string _SearchText;
        public string SearchText
        {
            get => _SearchText;
            set => Set("SearchText", ref _SearchText, value);
        }

        private string _Destination;
        public string Destination
        {
            get => _Destination;
            set => Set("DestPort", ref _Destination, value);
        }

        private string _DestPort;
        public string DestPort
        {
            get => _DestPort;
            set => Set("DestPort", ref _DestPort, value);
        }

        private string _DestShelf;
        public string DestShelf
        {
            get => _DestShelf;
            set => Set("DestShelf", ref _DestShelf, value);
        }

        private string _DestBank;
        public string DestBank
        {
            get => _DestBank;
            set => Set("DestBank", ref _DestBank, value);
        }

        private string _DestBay;
        public string DestBay
        {
            get => _DestBay;
            set => Set("DestBay", ref _DestBay, value);
        }

        private string _DestLevel;
        public string DestLevel
        {
            get => _DestLevel;
            set => Set("DestLevel", ref _DestLevel, value);
        }

        private SearchViewModelData _SelValue;
        public SearchViewModelData SelValue
        {
            get => _SelValue;
            set => Set("SelValue", ref _SelValue, value);
        }
        //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
        private bool _SelectShelf;
        public bool SelectShelf
        {
            get => _SelectShelf;
            set => Set("SelectShelf", ref _SelectShelf, value);
        }
        private bool _SelectPort;
        public bool SelectPort
        {
            get => _SelectPort;
            set => Set("SelectPort", ref _SelectPort, value);
        }

        private LayOutView layOut;      //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
        //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
        //public ViewModelSearchView()
        public ViewModelSearchView(LayOutView layout)
        {
            SearchText = string.Empty;
            DestPort = string.Empty;
            DestShelf = string.Empty;

            CarrierListData = new ObservableCollection<SearchViewModelData>();
            SearchListData = new ObservableCollection<SearchViewModelData>();


            PortList = new ObservableCollection<string>(GlobalData.Current.PortManager.GetRobotIFCV());
            BankList = new ObservableCollection<string>() { GlobalData.Current.FrontBankNum.ToString(), GlobalData.Current.RearBankNum.ToString() };
            BayList = new ObservableCollection<string>(GetNumList(GlobalData.Current.ShelfMgr.FrontData.MaxBay));
            LevelList = new ObservableCollection<string>(GetNumList(GlobalData.Current.ShelfMgr.FrontData.MaxLevel));

            HeaderTextSize = 15;
            BodyTextSize = 12;

            //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
            Task.Factory.StartNew(() => { CarrierChanged(GlobalData.Current.CarrierStore.GetAlltoList()); });
            GlobalData.Current.CarrierStore.OnCarrierStoreChanged += CarrierChanged;

            layOut = layout;
            layOut.vm.OnControlSelectionChanged += LayoutSelectionChanged;
        }

        private List<string> GetNumList(int iMax)
        {
            List<string> listNum = new List<string>();
            try
            {
                for (int i = 1; i <= iMax; i++)
                {
                    listNum.Add(i.ToString());
                }
            }
            catch (Exception)
            {
                listNum = new List<string>();
            }

            return listNum;
        }
        //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
        //지속적으로 쓰레드로 Carrier 정보 리딩 및 변환작업으로 인해 CPU 부하가 높아짐 이벤트 처리로 변경.
        #region 미사용 주석
        //public bool CompareListData(ObservableCollection<SearchViewModelData> cur, ObservableCollection<SearchViewModelData> past)
        //{
        //    bool bvalue = false;

        //    //211102 HHJ VOC 개발     //- VOC 사항 추가
        //    if (cur == null)
        //        return false;

        //    foreach (SearchViewModelData c in cur)
        //    {
        //        foreach (SearchViewModelData p in past)
        //        {
        //            string ctmp = c.DataSerialize();
        //            string ptmp = p.DataSerialize();

        //            bvalue = ctmp.Equals(ptmp);

        //            if (bvalue) break;
        //        }

        //        //검색하지 못했으면 나가야함.
        //        if (!bvalue) break;
        //    }

        //    return bvalue;
        //}

        //protected override void ViewModelTimer()
        //{
        //    while(EnableTimer)
        //    {
        //        Thread.Sleep(500);
        //        try
        //        {
        //            ObservableCollection<SearchViewModelData> tmp = new ObservableCollection<SearchViewModelData>();
        //            List<CarrierItem> tmp1 = GlobalData.Current.CarrierStore.GetAlltoList();

        //            int no = 1;
        //            foreach (CarrierItem c in tmp1)
        //            {
        //                SearchViewModelData v = new SearchViewModelData() { No = no++, CarrierID = c.CarrierID, Location = c.CarrierLocation, Status = c.CarrierState, ZoneID = "" };
        //                tmp.Add(v);
        //            }

        //            if (!CompareListData(CarrierListData, tmp))
        //            {
        //                CarrierListData = tmp;
        //            }
        //        }
        //        catch (Exception)
        //        {

        //        }
        //    }
        //}
        #endregion
        private void CarrierChanged(List<CarrierItem> changed)
        {
            try
            {
                ObservableCollection<SearchViewModelData> tmp = new ObservableCollection<SearchViewModelData>();

                int no = 1;
                foreach (CarrierItem c in changed)
                {
                    //2024.05.06 lim, SKOn 요청 사항으로 ProductEmpty 추가
                    SearchViewModelData v = new SearchViewModelData() { No = no++, CarrierID = c.CarrierID, Location = c.CarrierLocation, Status = c.CarrierState, ProductEmpty = c.ProductEmpty, ZoneID = c.CarrierZoneName };
                    tmp.Add(v);
                }

                CarrierListData = tmp;
            }
            catch (Exception)
            {

            }
        }
        public void DeleteChanged()
        {
            GlobalData.Current.CarrierStore.OnCarrierStoreChanged -= CarrierChanged;
            layOut.vm.OnControlSelectionChanged -= LayoutSelectionChanged;
        }
        //Manual Move 기능 추가
        private void LayoutSelectionChanged(ControlBase control)
        {
            try
            {
                if (control is CV_BaseModule)
                {
                    SelectPort = true;

                    if (PortList.Contains(control.ControlName))
                    {
                        DestPort = control.ControlName;
                        Destination = DestPort;
                    }
                }
                else if (control is ShelfItem shelf)
                {
                    SelectShelf = true;
                    DestShelf = shelf.TagName;
                    DestBank = shelf.iBank.ToString();
                    DestBay = shelf.iBay.ToString();
                    DestLevel = shelf.iLevel.ToString();
                    Destination = DestShelf;
                }
                else if( control is RMModuleBase rm)
                {
                    Destination = rm.ModuleName;
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }

    public class SearchViewModelData : ViewModelBase
    {
        private object _No;
        public object No
        {
            get => _No;
            set => Set("No", ref _No, value);
        }

        private object _CarrierID;
        public object CarrierID
        {
            get => _CarrierID;
            set => Set("CarrierID", ref _CarrierID, value);
        }

        private object _Location;
        public object Location
        {
            get => _Location;
            set => Set("Location", ref _Location, value);
        }

        private object _Status;
        public object Status
        {
            get => _Status;
            set => Set("Status", ref _Status, value);
        }

        //2024.05.06 lim, SKOn 요청 사항으로 ProductEmpty 추가
        private object _ProductEmpty;
        public object ProductEmpty
        {
            get => _ProductEmpty;
            set => Set("ProductEmpty", ref _ProductEmpty, value);
        }

        private object _ZoneID;
        public object ZoneID
        {
            get => _ZoneID;
            set => Set("ZoneID", ref _ZoneID, value);
        }
    }
}
