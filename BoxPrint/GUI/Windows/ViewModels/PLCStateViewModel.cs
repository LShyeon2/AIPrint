using PLCProtocol.DataClass;
using BoxPrint.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BoxPrint.GUI.Windows.ViewModels
{
    public class PLCStateViewModel : ViewModelBase
    {
        #region Variable
        #region Field
        #endregion
        #region Binding
        private ObservableCollection<PLCStateViewModelData> _DataList;
        public ObservableCollection<PLCStateViewModelData> DataList
        {
            get => _DataList;
            set => Set("DataList", ref _DataList, value);
        }
        #endregion
        #region Command
        public ICommand CloseCommand { get; private set; }
        #endregion
        #endregion

        #region Constructor
        public PLCStateViewModel()
        {
            DataList = new ObservableCollection<PLCStateViewModelData>();

            EventCollection.UIEventCollection.Instance.OnResponsePlcState += OnResponsePlcState;
            EventCollection.UIEventCollection.Instance.OnPlcStateChanged += OnPlcStateChanged;

            EventCollection.UIEventCollection.Instance.InvokerRequestPlcState();
        }
        #endregion

        #region Methods
        #region Event
        private void OnResponsePlcState(List<PLCStateData> stateDataList)
        {
            uint i = 1;
            foreach(PLCStateData stateData in stateDataList)
            {
                PLCStateViewModelData data = new PLCStateViewModelData();
                data.No = i++;
                data.PLCName = stateData.PLCName;
                data.ConnectionInfo = stateData.ConnectInfo;
                data.State = stateData.State.ToString();
                data.StateChangeTime = stateData.StateChangeTime;
                DataList.Add(data);
            }
        }
        private void OnPlcStateChanged(PLCStateData stateData)
        {
            PLCStateViewModelData state = DataList.Where(r => r.ConnectionInfo.Equals(stateData.ConnectInfo)).FirstOrDefault();

            if (state is null)
                return;

            //DataList.Remove(state);

            //PLCStateViewModelData data = new PLCStateViewModelData();
            //data.No = state.No;
            //data.ConnectionInfo = stateData.ConnectInfo;
            //data.State = stateData.State.ToString();
            //data.StateChangeTime = stateData.StateChangeTime;
            //DataList.Add(data);

            for (int i = 0; i < DataList.Count; i++)
            {
                if (DataList[i].ConnectionInfo == state.ConnectionInfo)
                {
                    DataList[i].State = stateData.State.ToString();
                    DataList[i].StateChangeTime = stateData.StateChangeTime;

                    //GlobalData.Current.DBManager.DbSetProcedurePLCInfo(DataList[i]);
                }
            }
        }
        #endregion
        #region Etc
        public void DisposeViewModel()
        {
            EventCollection.UIEventCollection.Instance.OnResponsePlcState -= OnResponsePlcState;
            EventCollection.UIEventCollection.Instance.OnPlcStateChanged -= OnPlcStateChanged;
        }
        #endregion
        #endregion
    }

    public class PLCStateViewModelData
    {
        private uint _No;
        public uint No
        {
            get => _No;
            set
            {
                _No = value;
            }
        }

        private string _PLCName;
        public string PLCName
        {
            get => _PLCName;
            set
            {
                _PLCName = value;
            }
        }

        private string _ConnectionInfo;
        public string ConnectionInfo
        {
            get => _ConnectionInfo;
            set
            {
                _ConnectionInfo = value;
            }
        }

        private string _State;
        public string State
        {
            get => _State;
            set
            {
                _State = value;
            }
        }

        private DateTime _StateChangeTime;
        public DateTime StateChangeTime
        {
            get => _StateChangeTime;
            set
            {
                _StateChangeTime = value;
            }
        }
    }
}
