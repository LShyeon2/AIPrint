using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.Views;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BoxPrint.GUI.ViewModels
{
    public class ViewModelCraneManualCommand : ViewModelBase
    {
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

        private string _SelectCrane;
        public string SelectCrane
        {
            get => _SelectCrane;
            set => Set("SelectCrane", ref _SelectCrane, value);
        }

        private RMModuleBase SelectRM = null;

        private LayOutView layOut;

        public ViewModelCraneManualCommand(LayOutView layout)
        {
            layOut = layout;
            DestPort = string.Empty;
            DestShelf = string.Empty;

            PortList = new ObservableCollection<string>(GlobalData.Current.PortManager.GetRobotIFCV());
            BankList = new ObservableCollection<string>() { "1", "2" };
            BayList = new ObservableCollection<string>(GetNumList(GlobalData.Current.ShelfMgr.FrontData.MaxBay));
            LevelList = new ObservableCollection<string>(GetNumList(GlobalData.Current.ShelfMgr.FrontData.MaxLevel));

            HeaderTextSize = 15;
            BodyTextSize = 12;

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
                    }
                }
                else if (control is ShelfItem shelf)
                {
                    SelectShelf = true;
                    DestBank = shelf.iBank.ToString();
                    DestBay = shelf.iBay.ToString();
                    DestLevel = shelf.iLevel.ToString();
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        public void DeleteChanged()
        {
            layOut.vm.OnControlSelectionChanged -= LayoutSelectionChanged;
        }
        public void SetRM(string rm)
        {
            SelectCrane = rm;

            try
            {
                if (GlobalData.Current.mRMManager.ModuleList.ContainsKey(SelectCrane))
                    SelectRM = GlobalData.Current.mRMManager[SelectCrane];
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        public RMModuleBase GetSelectRM()
        {
            return SelectRM;
        }
    }
}
