using BoxPrint.Config;
using PLCProtocol.DataClass;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BoxPrint.GlobalData;

namespace BoxPrint.GUI.ViewModels.PrintPage
{
    public class RecipeModifyViewModel : ViewModelBase
    {
        private ObservableCollection<RecipeData> _SelectedRecipeDataList;
        public ObservableCollection<RecipeData> SelectedRecipeDataList
        {
            get => _SelectedRecipeDataList;
            set => Set("SelectedRecipeDataList", ref _SelectedRecipeDataList, value);

        }

        private int _CurrRecipeNo;
        public int CurrRecipeNo 
        {
            get => _CurrRecipeNo; 
            set => Set("CurrRecipeNo", ref _CurrRecipeNo, value);
        }
        private string _CurrRecipeName;
        public string CurrRecipeName
        {
            get => _CurrRecipeName;
            set => Set("CurrRecipeName", ref _CurrRecipeName, value);
        }

        private RecipeData _SelValue;
        public RecipeData SelValue
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

        public RecipeModifyViewModel()
        {
            SelectedRecipeDataList = new ObservableCollection<RecipeData>();
            OnRecipeDataRefreshed();



            GlobalData.Current.recipedatarefresh += OnRecipeDataRefreshed;
        }

        private void OnRecipeDataRefreshed()
        {

            //if (GlobalData.Current.ServerClientType == eServerClientType.Client &&
            //    (bInit || IsTimeOut(DateTime.Now)))
            Task.Run(() =>
            {
                ObservableCollection<RecipeData> RecipeListRefreshData = new ObservableCollection<RecipeData>();

                //dataSet = GlobalData.Current.DBManager.DbGetProcedureGlobalConfigInfo('1');
                dataSet = GlobalData.Current.Recipe_Manager.GetSelectedDataSet();

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        //if (string.IsNullOrEmpty(dr["CONFIG_NO"].ToString()))
                        //    continue;
                        if (!int.TryParse(dr["Recipe_No"].ToString(), out int reNo))
                            reNo = 0;

                        RecipeListRefreshData.Add(
                            new RecipeData()
                            {
                                Recipe_No   = reNo,
                                Order       = dr["Order"].ToString(),
                                DataType    = dr["DataType"].ToString(),
                                Config_NM   = dr["Name"].ToString(),
                                Config_Val  = dr["Value"].ToString(),
                            });
                    }
                }

                CurrRecipeNo = GlobalData.Current.Recipe_Manager.GetCurrentRecipeNo();
                CurrRecipeName = GlobalData.Current.Recipe_Manager.GetRecipeName(CurrRecipeNo);

                SelectedRecipeDataList = RecipeListRefreshData;
            });
        }

        public void SelectedDataListUp()
        {
            if (SelValue == null)
                return;

            string Reno = CurrRecipeNo.ToString();
            int idx = SelValue.iOrder;

            if (idx <= 1)
                return;

            var curr = SelectedRecipeDataList.Where(r => r.iOrder == idx).FirstOrDefault();
            var trgt = SelectedRecipeDataList.Where(r => r.iOrder == idx - 1).FirstOrDefault();

            if (curr != null && trgt != null)
            {
                curr.iOrder = idx - 1;
                trgt.iOrder = idx;

                // 순서를 재정렬
                SelectedRecipeDataList = new ObservableCollection<RecipeData>(
                    SelectedRecipeDataList.OrderBy(r => r.iOrder));
            }
        }

        public void SelectedDataListDown()
        {
            if (SelValue == null)
                return;

            string Reno = CurrRecipeNo.ToString();
            int idx = SelValue.iOrder;

            if (idx >= SelectedRecipeDataList.Count || idx <= 0)
                return;

            var curr = SelectedRecipeDataList.Where(r => r.iOrder == idx).FirstOrDefault();
            var trgt = SelectedRecipeDataList.Where(r => r.iOrder == idx + 1).FirstOrDefault();

            if (curr != null && trgt != null)
            {
                curr.iOrder = idx + 1;
                trgt.iOrder = idx;

                // 순서를 재정렬
                SelectedRecipeDataList = new ObservableCollection<RecipeData>(
                    SelectedRecipeDataList.OrderBy(r => r.iOrder));
            }

        }

        public void DataAdd()
        {
            int Reno = CurrRecipeNo;
            int select = SelectedRecipeDataList.Count + 1;

            SelectedRecipeDataList.Add(
                new RecipeData()
                {
                    Recipe_No = Reno,
                    iOrder = select,
                }
                );

            SelValue = SelectedRecipeDataList.Where(r => r.iOrder == select).FirstOrDefault();
        }


        public void DataDel()
        {
            if (SelValue == null)
                return;

            int select = SelValue.iOrder;
            int newOrder = select;

            bool isDel = SelectedRecipeDataList.Remove(SelValue);
           
            if (isDel)
            {
                foreach (RecipeData r in SelectedRecipeDataList)
                {
                    if (r.iOrder.Equals(newOrder + 1))
                    {
                        r.iOrder = newOrder;
                        newOrder++;
                    }
                }
            }

            // 순서를 재정렬
            SelectedRecipeDataList = new ObservableCollection<RecipeData>(
                SelectedRecipeDataList.OrderBy(r => r.iOrder));

            if (select > SelectedRecipeDataList.Count)
                select = SelectedRecipeDataList.Count;

            SelValue = SelectedRecipeDataList.Where(r => r.iOrder == select).FirstOrDefault();
        }

        public void SaveData()
        {

            var UpdateData = SelectedRecipeDataList.Where(r => r.Order == SelValue.Order).FirstOrDefault();

            if (UpdateData != null)
            {
                UpdateData = SelValue;
            }

            GlobalData.Current.Recipe_Manager.CurrentRecipeDataSave(SelectedRecipeDataList);

            GlobalData.Current.Recipe_Manager.CurrentRecipeReLoad();


            // 순서를 재정렬
            SelectedRecipeDataList = new ObservableCollection<RecipeData>(
                SelectedRecipeDataList.OrderBy(r => r.iOrder));
        }

        public int GetDataMaxCount()
        {
            int iMax = 0;
            foreach ( var item in SelectedRecipeDataList )
            {
                if (item.DataType != null && item.DataType.ToUpper() != eRecipeDataType.BASE.ToString())
                {
                    string temp = item.Config_NM.ToString().Substring(2);
                    
                    if(int.TryParse(temp, out int num) )
                    {
                        if (iMax  < num)
                            iMax = num;
                    }
                }
            }

            return iMax;
        }
    }
}
