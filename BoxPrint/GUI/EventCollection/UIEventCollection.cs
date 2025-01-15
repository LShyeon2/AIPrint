using PLCProtocol.DataClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxPrint.GUI.EventCollection
{
    public class UIEventCollection : SingletonBase<UIEventCollection>
    {
        #region Event
        public delegate void CultureChanged(string cultureKey);
        public event CultureChanged OnCultureChanged;

        public delegate void RequestPlcState();
        public event RequestPlcState OnRequestPlcState;

        public delegate void ResponsePlcState(List<PLCStateData> stateDataList);
        public event ResponsePlcState OnResponsePlcState;

        public delegate void PlcStateChanged(PLCStateData stateData);
        public event PlcStateChanged OnPlcStateChanged;
        #endregion
        #region Methods
        public void InvokerControlSelectionChanged(string cultureKey)
        {
            OnCultureChanged?.Invoke(cultureKey);
        }

        public void InvokerRequestPlcState()
        {
            OnRequestPlcState?.Invoke();
        }

        public void InvokerResponsePlcState(List<PLCStateData> stateDataList)
        {
            OnResponsePlcState?.Invoke(stateDataList);
        }

        public void InvokerPlcStateChanged(PLCStateData stateData)
        {
            OnPlcStateChanged?.Invoke(stateData);
        }
        #endregion
    }
}
