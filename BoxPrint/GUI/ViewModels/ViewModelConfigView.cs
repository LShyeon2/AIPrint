using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;

namespace BoxPrint.GUI.ViewModels
{
    public class ViewModelConfigView : ViewModelBase
    {
        private ObservableCollection<ConfigViewModelData> _ConfigListData;
        public ObservableCollection<ConfigViewModelData> ConfigListData
        {
            get => _ConfigListData;
            //set => Set("ConfigListData", ref _ConfigListData, value);
            set
            {
                if (!CompareListData(value, _ConfigListData))
                {
                    Set("ConfigListData", ref _ConfigListData, value);
                }
            }
        }

        private ConfigViewModelData _SelValue;
        public ConfigViewModelData SelValue
        {
            get => _SelValue;
            set => Set("SelValue", ref _SelValue, value);
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

        private DataSet dataSet;

        public ViewModelConfigView()
        {
            ConfigListData = new ObservableCollection<ConfigViewModelData>();
            OnConfigDataRefreshed();
            GlobalData.Current.configdatarefresh += OnConfigDataRefreshed;
        }

        public bool CompareListData(ObservableCollection<ConfigViewModelData> cur, ObservableCollection<ConfigViewModelData> past)
        {
            bool bvalue = false;

            //211102 HHJ VOC 개발     //- VOC 사항 추가
            if (cur == null)
                return false;

            foreach (ConfigViewModelData c in cur)
            {
                foreach (ConfigViewModelData p in past)
                {
                    string ctmp = c.DataSerialize();
                    string ptmp = p.DataSerialize();

                    bvalue = ctmp.Equals(ptmp);

                    if (bvalue) break;
                }

                //검색하지 못했으면 나가야함.
                if (!bvalue) break;
            }

            return bvalue;
        }

        private void OnConfigDataRefreshed()
        {

            //if (GlobalData.Current.ServerClientType == eServerClientType.Client &&
            //    (bInit || IsTimeOut(DateTime.Now)))
            Task.Run(() =>
            {
                ObservableCollection<ConfigViewModelData> ConfigListRefreshData = new ObservableCollection<ConfigViewModelData>();

                dataSet = GlobalData.Current.DBManager.DbGetProcedureGlobalConfigInfo('1');

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        //if (string.IsNullOrEmpty(dr["CONFIG_NO"].ToString()))
                        //    continue;
                        ConfigListRefreshData.Add(
                            new ConfigViewModelData()
                            {
                                ConfigNumber = dr["CONFIG_NO"].ToString(),
                                ConfigName = dr["CONFIG_NM"].ToString(),
                                ConfigType = dr["CONFIG_GB"].ToString(),
                                ConfigValue = dr["CONFIG_VAL"].ToString(),
                                ConfigDefaultValue = string.IsNullOrEmpty(dr["CONFIG_DEF"].ToString()) ? string.Empty : dr["CONFIG_DEF"].ToString(),
                                ConfigDescription = string.IsNullOrEmpty(dr["CONFIG_DES"].ToString()) ? string.Empty : dr["CONFIG_DES"].ToString()
                            });
                    }
                }

                ConfigListData = ConfigListRefreshData;
            });
            //else if (GlobalData.Current.ServerClientType == eServerClientType.Server)
            //{
            //    ObservableCollection<ConfigViewModelData> ConfigListRefreshData = new ObservableCollection<ConfigViewModelData>();

            //    dataSet = GlobalData.Current.DBManager.DbGetProcedureGlobalConfigInfo('1');

            //    int table = dataSet.Tables.Count;
            //    for (int i = 0; i < table; i++)// set the table value in list one by one
            //    {
            //        foreach (DataRow dr in dataSet.Tables[i].Rows)
            //        {
            //            //if (string.IsNullOrEmpty(dr["CONFIG_NO"].ToString()))
            //            //    continue;
            //            ConfigListRefreshData.Add(
            //                new ConfigViewModelData()
            //                {
            //                    ConfigNumber = dr["CONFIG_NO"].ToString(),
            //                    ConfigName = dr["CONFIG_NM"].ToString(),
            //                    ConfigType = dr["CONFIG_GB"].ToString(),
            //                    ConfigValue = dr["CONFIG_VAL"].ToString(),
            //                    ConfigDefaultValue = string.IsNullOrEmpty(dr["CONFIG_DEF"].ToString()) ? string.Empty : dr["CONFIG_DEF"].ToString(),
            //                    ConfigDescription = string.IsNullOrEmpty(dr["CONFIG_DES"].ToString()) ? string.Empty : dr["CONFIG_DES"].ToString()
            //                });
            //        }
            //    }

            //    vm.ConfigListData = ConfigListRefreshData;
            //}
        }
    }

    public class ConfigViewModelData
    {
        private object _ConfigNumber;
        public object ConfigNumber
        {
            get => _ConfigNumber;
            //set => Set("ConfigNumber", ref _ConfigNumber, value);
            set
            {
                _ConfigNumber = value;
            }
        }

        private object _ConfigName;
        public object ConfigName
        {
            get => _ConfigName;
            //set => Set("ConfigName", ref _ConfigName, value);
            set
            {
                _ConfigName = value;
            }
        }

        private object _ConfigType;
        public object ConfigType
        {
            get => _ConfigType;
            //set => Set("ConfigType", ref _ConfigType, value);
            set
            {
                _ConfigType = value;
            }
        }

        private object _ConfigValue;
        public object ConfigValue
        {
            get => _ConfigValue;
            //set => Set("ConfigValue", ref _ConfigValue, value);
            set
            {
                _ConfigValue = value;
            }
        }

        private object _ConfigDefaultValue;
        public object ConfigDefaultValue
        {
            get => _ConfigDefaultValue;
            //set => Set("ConfigDefaultValue", ref _ConfigDefaultValue, value);
            set
            {
                _ConfigDefaultValue = value;
            }
        }

        private object _ConfigDescription;
        public object ConfigDescription
        {
            get => _ConfigDescription;
            //set => Set("ConfigDescription", ref _ConfigDescription, value);
            set
            {
                _ConfigDescription = value;
            }
        }
    }
}
