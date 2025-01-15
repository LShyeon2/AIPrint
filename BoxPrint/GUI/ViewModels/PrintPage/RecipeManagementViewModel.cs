using BoxPrint.Config;
using BoxPrint.GUI.ETC.LoadingPopup;
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
    public class RecipeManagementViewModel : ViewModelBase
    {
        private ObservableCollection<Recipe> _ViewRecipeList;
        public ObservableCollection<Recipe> ViewRecipeList
        {
            get => _ViewRecipeList;
            set => Set("ViewRecipeList", ref _ViewRecipeList, value);
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


        private Recipe _SelValue;
        public Recipe SelValue
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

        public RecipeManagementViewModel()
        {
            ViewRecipeList = new ObservableCollection<Recipe>();
            OnRecipeListRefreshed();

            GlobalData.Current.recipeListrefresh += OnRecipeListRefreshed;
        }

        private void OnRecipeListRefreshed()
        {

            //if (GlobalData.Current.ServerClientType == eServerClientType.Client &&
            //    (bInit || IsTimeOut(DateTime.Now)))
            Task.Run(() =>
            {
                //ObservableCollection<Recipe> RecipeListRefreshData = new ObservableCollection<Recipe>();

                //dataSet = GlobalData.Current.DBManager.DbGetProcedureGlobalConfigInfo('1');
                //dataSet = GlobalData.Current.Recipe_Manager.GetSelectedDataSet();

                //int table = dataSet.Tables.Count;
                //for (int i = 0; i < table; i++)// set the table value in list one by one
                //{
                //    foreach (DataRow dr in dataSet.Tables[i].Rows)
                //    {
                //        //if (string.IsNullOrEmpty(dr["CONFIG_NO"].ToString()))
                //        //    continue;
                //        RecipeListRefreshData.Add(
                //            new Recipe()
                //            {
                //                Recipe_No = (int)dr["Recipe_No"],
                //                Order = dr["Order"].ToString(),
                //                DataType = dr["DataType"].ToString(),
                //                Config_NM = dr["Name"].ToString(),
                //                Config_Val = dr["Value"].ToString(),
                //            });
                //    }
                //}

                CurrRecipeNo = GlobalData.Current.Recipe_Manager.GetCurrentRecipeNo();
                CurrRecipeName = GlobalData.Current.Recipe_Manager.GetRecipeName(CurrRecipeNo);

                ViewRecipeList = new ObservableCollection<Recipe>(
                    GlobalData.Current.Recipe_Manager.RecipeList.OrderBy(r => r.Recipe_No));
            });
        }

        public void RecipeAdd()
        {
            int Reno = CurrRecipeNo;
            //int select = SelectedRecipeDataList.Count + 1;

            ViewRecipeList.Add(
                new Recipe()
                {
                    ModifyDate = DateTime.Now,
                    CreateDate = DateTime.Now,
                }
                );

            SelValue = ViewRecipeList.Where(r => r.Recipe_No == 0).FirstOrDefault();

        }

        public void RecipeDel()
        {
            if (SelValue == null)
                return;

            int select = SelValue.Recipe_No;

            if (select == CurrRecipeNo)
                return;

            bool isDel = ViewRecipeList.Remove(SelValue);


            // 순서를 재정렬
            ViewRecipeList = new ObservableCollection<Recipe>(
                ViewRecipeList.OrderBy(r => r.Recipe_No));

            //SelValue = ViewRecipeList.Where(r => r.Recipe_No == 0).FirstOrDefault();

        }

        public bool CheckRecipeNo(int reNo)
        {
            var temp = ViewRecipeList.Where(r => r.Recipe_No == reNo).FirstOrDefault();

            if (temp != null)
            {
                if (string.IsNullOrEmpty(temp.Recipe_Name))
                    return false;
            
                return true;
            }

            return true;
        }

        public void SaveData()
        {
             var UpdateRecipe = _ViewRecipeList.Where(r => r.Recipe_No == SelValue?.Recipe_No).FirstOrDefault();

            if( UpdateRecipe != null )
            {
                UpdateRecipe = SelValue;
            }

            GlobalData.Current.Recipe_Manager.RecipeListSave(ViewRecipeList);

            // 순서를 재정렬
            ViewRecipeList = new ObservableCollection<Recipe>(
                ViewRecipeList.OrderBy(r => r.Recipe_No));

        }

        public bool RecipeChange()
        {
            int TargetNo = SelValue.Recipe_No;

            var recipe = ViewRecipeList.Where(r => r.Recipe_No == TargetNo).FirstOrDefault();

            if (recipe != null)
            {
                if (recipe.IsSelected)
                    return true;

                GlobalData.Current.PrinterMng.ChangingRecipe = true;

                GlobalData.Current.Recipe_Manager.ChangeSelecteRecipe(TargetNo);

                //OnRecipeListRefreshed();
            }

            return true;
        }
    }
}
