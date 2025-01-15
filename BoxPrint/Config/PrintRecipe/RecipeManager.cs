using BoxPrint.Alarm;
using BoxPrint.Database;
using BoxPrint.GUI.ETC.LoadingPopup;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Serialization;

namespace BoxPrint.Config.PrintRecipe
{
    public class RecipeManager
    {
        private int SelectedRecipeNo = 0;

        public delegate void SeclectedRecipeChanged(RecipeData recipe, bool addList);
        public event SeclectedRecipeChanged OnSeclectedRecipeChanged;

        private string filePath = string.Empty;

        //private string[] _RecipeList = new string[100] ;
        
        private ObservableCollection<Recipe> _RecipeList = new ObservableCollection<Recipe>();
        public ObservableCollection<Recipe> RecipeList
        {
            get
            {
                return _RecipeList;
            }
        }

        private ObservableCollection<RecipeData> AllRecipeDataList = new ObservableCollection<RecipeData>();

        private ObservableCollection<RecipeData> _SelectedRecipeDataList;
        public ObservableCollection<RecipeData> SelectedRecipeDataList
        {
            get
            {
                return _SelectedRecipeDataList; 
            }
        }

        public List<RecipeData> getSelectedList()
        {
            List<RecipeData> tempList = _SelectedRecipeDataList.ToList();
            return tempList;
        }

        public DataSet GetSelectedDataSet()
        {
            //List<RecipeData> tempList = _SelectedRecipeDataList.ToList();
            DataSet dataSet = new DataSet();

            // DataTable 생성
            DataTable dataTable = new DataTable("SelectedRecipeData");

            // DataTable 컬럼 정의
            dataTable.Columns.Add("Recipe_No", typeof(string));
            dataTable.Columns.Add("Order", typeof(string));
            dataTable.Columns.Add("DataType", typeof(string));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Value", typeof(string));

            var selected = AllRecipeDataList.Where(rd => rd.Recipe_No == SelectedRecipeNo);

            foreach (var data in selected)
            {
                DataRow row = dataTable.NewRow();
                row["Recipe_No"] = data.Recipe_No;
                row["Order"] = data.Order;
                row["DataType"] = data.DataType;
                row["Name"] = data.Config_NM;
                row["Value"] = data.Config_Val;
                dataTable.Rows.Add(row);
            }

            // DataTable을 DataSet에 추가
            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        public RecipeManager(string listFilePath)
        {
            string LogPath = LogManager.GetLogRootPath();

            filePath = listFilePath;

            //LocalDatabase = new LocalDBManager(LogPath + DataBaseFileName);
            //LastAlarmID = LocalDatabase.GetAlarmLogCount();


            //AllAlarmList = GlobalData.Current.DBManager.DbGetProcedureAlarmInfo();

            //if (AllRecipeDataList.Count == 0)     //계정별로 테이블 생성되기때문에 없으면 새로 생성한다.
            //{
            _SelectedRecipeDataList = new ObservableCollection<RecipeData>();

            Deserialize(listFilePath);

            //}

            //foreach (var Adata in AllRecipeDataList)
            //{
            //    if (string.IsNullOrEmpty(Adata.RecoveryOption))
            //    {
            //        Adata.RecoveryOption = string.Empty;
            //    }

            //}


            LogManager.WriteConsoleLog(eLogLevel.Info, "Recipe Manager has been created.");
        }


        private bool Deserialize(String fileName)
        {
            bool bSuccess = false;
            //ObservableList<RecipeData> bindAlarmList = new ObservableList<RecipeData>();
            //List<RecipeData> alarmList = null;
            try
            {
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(fileName);
                XmlNodeList nodes = xdoc.SelectNodes("/ArrayOfRecipe/Recipe");

                foreach (XmlNode item_Recipe in nodes)
                {
                    Recipe recipe = new Recipe();
                    bool IsSelect = false;
                    int idx = 0;

                    if (!int.TryParse(item_Recipe.Attributes["Recipe_No"].Value, out idx))
                        continue;

                    //RecipeList.Add(item_Recipe.Attributes["Recipe_Name"].Value);
                    //RecipeList[idx] = item_Recipe.Attributes["Recipe_Name"].Value;

                    recipe.Recipe_No = idx;
                    recipe.Recipe_Name = item_Recipe.Attributes["Recipe_Name"].Value;
                    //recipe.ModifyDate = item_Recipe.Attributes["ModifyDate"].Value;
                    //recipe.CreateDate = item_Recipe.Attributes["CreateDate"].Value;

                    if (DateTime.TryParse(item_Recipe.Attributes["ModifyDate"]?.Value, out DateTime modifyDate))
                        recipe.ModifyDate = modifyDate;
                    else
                        recipe.ModifyDate = DateTime.Now; // 기본값

                    if (DateTime.TryParse(item_Recipe.Attributes["CreateDate"]?.Value, out DateTime CreateDate))
                        recipe.CreateDate = CreateDate;
                    else
                        recipe.CreateDate = DateTime.Now; // 기본값

                    RecipeList.Add(recipe);

                    if (bool.TryParse(item_Recipe.Attributes["IsSelected"].Value, out IsSelect))
                        recipe.IsSelected = IsSelect;

                    if (recipe.IsSelected)
                        SelectedRecipeNo = idx;

                    foreach (XmlNode item_RecipeData in item_Recipe)
                    {
                        RecipeData rd = new RecipeData();

                        if (int.TryParse(item_RecipeData.Attributes["Recipe_No"]?.Value, out int reNo))
                            rd.Recipe_No = reNo;

                        if (rd.Recipe_No != idx)
                            rd.Recipe_No = idx;

                        rd.Order = item_RecipeData.Attributes["Order"].Value;
                        rd.DataType = item_RecipeData.Attributes["DataType"].Value;
                        rd.Config_NM = item_RecipeData.Attributes["Name"].Value;
                        rd.Config_Val = item_RecipeData.Attributes["Value"].Value;

                        AllRecipeDataList.Add(rd);
                        if (IsSelect)
                            SelectedRecipeDataList.Add(rd);
                    }
                }

                bSuccess = true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, "== AlarmDataList.Deserialize() Exception : {0}", ex.ToString());
                bSuccess = false;
            }
            return bSuccess;
        }

        private bool Serialize(string fileName)
        {
            bool isSuccess = false;

            try
            {
                XmlDocument xdoc = new XmlDocument();

                // 루트 노드 생성
                XmlElement rootElement = xdoc.CreateElement("ArrayOfRecipe");
                xdoc.AppendChild(rootElement);

                //for (int recipeNo = 0; recipeNo < 100; recipeNo++)
                foreach (var recipe in RecipeList)
                {
                    // Recipe 노드 생성
                    //if (string.IsNullOrEmpty(RecipeList[recipeNo]))
                    if (string.IsNullOrEmpty(recipe.Recipe_Name))
                        continue;

                    XmlElement recipeElement = xdoc.CreateElement("Recipe");
                    recipeElement.SetAttribute("Recipe_No", recipe.Recipe_No.ToString());
                    recipeElement.SetAttribute("Recipe_Name", recipe.Recipe_Name);
                    recipeElement.SetAttribute("IsSelected", recipe.IsSelected.ToString());
                    recipeElement.SetAttribute("ModifyDate", recipe.ModifyDate.ToString("yyyy/MM/dd HH:mm:ss"));
                    recipeElement.SetAttribute("CreateDate", recipe.CreateDate.ToString("yyyy/MM/dd HH:mm:ss"));

                    // RecipeData 노드 추가
                    var relatedData = AllRecipeDataList.Where(rd => rd.Recipe_No == recipe.Recipe_No);
                    foreach (var recipeData in relatedData)
                    {
                        XmlElement recipeDataElement = xdoc.CreateElement("RecipeData");
                        recipeDataElement.SetAttribute("Recipe_No", recipeData.Recipe_No.ToString());
                        recipeDataElement.SetAttribute("Order", recipeData.Order);
                        recipeDataElement.SetAttribute("DataType", recipeData.DataType);
                        recipeDataElement.SetAttribute("Name", recipeData.Config_NM);
                        recipeDataElement.SetAttribute("Value", recipeData.Config_Val);

                        recipeElement.AppendChild(recipeDataElement);
                    }

                    rootElement.AppendChild(recipeElement);
                }

                // XML 파일 저장
                xdoc.Save(fileName);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, "== AlarmDataList.Serialize() Exception : {0}", ex.ToString());
            }

            return isSuccess;
        }

        public bool RecipeLoad()
        {
            return Deserialize(filePath);
        }

        public bool RecipeListSave(ObservableCollection<Recipe> list)
        {
            RecipeList.Clear();


            foreach (var recipe in list)
            {
                RecipeList.Add(recipe);

                //기존에 없는 레시피
                if (AllRecipeDataList.Where(r => r.Recipe_No == recipe.Recipe_No).Count() == 0)
                {
                    makeDefaultData(recipe.Recipe_No);
                }


                //var temp = RecipeList.Where(r => r.Recipe_No == recipe.Recipe_No).FirstOrDefault();
                //if (temp != null)
                //{
                //    if (!temp.Equals(recipe))
                //        temp = recipe;
                //}
                //else
                //{
                //    RecipeList.Add(recipe);
                //    AllRecipeDataList.Add(new RecipeData()
                //    {
                //        Recipe_No = recipe.Recipe_No,
                //        Order = "1",
                //        DataType = eRecipeDataType.BASE.ToString(),
                //        Config_NM = eRecipeBaseData.Filename.ToString(),
                //        Config_Val = "Default"
                //    });
                //    AllRecipeDataList.Add(new RecipeData()
                //    {
                //        Recipe_No = recipe.Recipe_No,
                //        Order = "2",
                //        DataType = eRecipeDataType.BASE.ToString(),
                //        Config_NM = eRecipeBaseData.Speed.ToString(),
                //        Config_Val = "15"
                //    });
                //    AllRecipeDataList.Add(new RecipeData()
                //    {
                //        Recipe_No = recipe.Recipe_No,
                //        Order = "3",
                //        DataType = eRecipeDataType.BASE.ToString(),
                //        Config_NM = eRecipeBaseData.Delay.ToString(),
                //        Config_Val = "10"
                //    });
                //    AllRecipeDataList.Add(new RecipeData()
                //    {
                //        Recipe_No = recipe.Recipe_No,
                //        Order = "4",
                //        DataType = eRecipeDataType.BASE.ToString(),
                //        Config_NM = eRecipeBaseData.Direct.ToString(),
                //        Config_Val = "R-L"
                //    });
                //}
            }

            return Serialize(filePath);
        }

        private void makeDefaultData(int reNo)
        {
            AllRecipeDataList.Add(new RecipeData()
            {
                Recipe_No = reNo,
                Order = "1",
                DataType = eRecipeDataType.BASE.ToString(),
                Config_NM = eRecipeBaseData.Filename.ToString(),
                Config_Val = "Default"
            });
            AllRecipeDataList.Add(new RecipeData()
            {
                Recipe_No = reNo,
                Order = "2",
                DataType = eRecipeDataType.BASE.ToString(),
                Config_NM = eRecipeBaseData.Speed.ToString(),
                Config_Val = "15"
            });
            AllRecipeDataList.Add(new RecipeData()
            {
                Recipe_No = reNo,
                Order = "3",
                DataType = eRecipeDataType.BASE.ToString(),
                Config_NM = eRecipeBaseData.Delay.ToString(),
                Config_Val = "10"
            });
            AllRecipeDataList.Add(new RecipeData()
            {
                Recipe_No = reNo,
                Order = "4",
                DataType = eRecipeDataType.BASE.ToString(),
                Config_NM = eRecipeBaseData.Direct.ToString(),
                Config_Val = "R-L"
            });
        }

        public bool CurrentRecipeDataSave(ObservableCollection<RecipeData> saveData)
        {
            int newDataCnt = saveData.Count;
            int currDataCnt = AllRecipeDataList.Where(r => r.Recipe_No == SelectedRecipeNo).Count();
            //int reNo = 0;

            if (newDataCnt > 0)
            {
                //reNo = SelectedRecipeNo;

                //data clear
                //var removeList = AllRecipeDataList.Where(r => r.Recipe_No == SelectedRecipeNo);
                //foreach (var  item in removeList)
                //    AllRecipeDataList.Remove(item);

                //foreach (var data in saveData)
                //{
                //    //var Target = AllRecipeDataList.Where(r => r.Recipe_No == reNo && r.Order == data.Order).FirstOrDefault();

                //    AllRecipeDataList.Add(data);
                //}

                int dataCnt = Math.Max(newDataCnt, currDataCnt);

                for (int i = 1; i <= dataCnt; i++)
                {
                    var Save   = saveData.Where(r => r.iOrder == i).FirstOrDefault();
                    var Target = AllRecipeDataList.Where(r => r.Recipe_No == SelectedRecipeNo && r.iOrder == i).FirstOrDefault();

                    if (Save != null && Target != null)
                    {
                        //Target = Save;
                        Target.Order = Save.Order;
                        Target.DataType = Save.DataType;
                        Target.Config_NM = Save.Config_NM;
                        Target.Config_Val = Save.Config_Val;
                    }
                    else if (Save != null)  //(Target == null)
                        AllRecipeDataList.Add(Save);
                    else if (Target != null) //(Save == null)
                        AllRecipeDataList.Remove(Target);
                
                }

                //변경 이력 기록
                RecipeList.Where(r => r.Recipe_No == SelectedRecipeNo).FirstOrDefault().ModifyDate = DateTime.Now;
            }

            return Serialize(filePath);
        }

        public void ChangeSelecteRecipe(int reNo)
        {
            var before = RecipeList.Where(r => r.Recipe_No == SelectedRecipeNo).FirstOrDefault();
            var target = RecipeList.Where(r => r.Recipe_No == reNo).FirstOrDefault();

            // RecipeData 노드 추가
            var relatedData = AllRecipeDataList.Where(rd => rd.Recipe_No == reNo);

            if (relatedData != null)
            {
                SelectedRecipeNo = reNo;

                _SelectedRecipeDataList.Clear();

                foreach (var recipeData in relatedData)
                {
                    RecipeData rd = new RecipeData();

                    rd.Recipe_No = recipeData.Recipe_No;
                    rd.Order = recipeData.Order;
                    rd.DataType = recipeData.DataType;
                    rd.Config_Val = recipeData.Config_Val;
                    rd.Config_NM = recipeData.Config_NM;

                    _SelectedRecipeDataList.Add(rd);
                }

                if (target != null && before != null)
                {
                    before.IsSelected = false;
                    target.IsSelected = true;
                }

                Serialize(filePath);

                CurrentRecipeReLoad();
            }
        }

        public void CurrentRecipeReLoad()
        {
            //Printer로 설정 전송
            if (GlobalData.Current.PrinterMng.GetFirstConnectedRecived())
            {
                if (Application.Current?.MainWindow == null)
                {
                    LodingPopup.Instance.Start();
                    LodingPopup.Instance.setProgressValue(20, "Recipe Changing....");
                }
                GlobalData.Current.PrinterMng.StartInitialize();
            }
        }

    
        public int GetCurrentRecipeNo()
        {
            return SelectedRecipeNo;
        }

        public string GetRecipeName(int RecipeNo)
        {
            return RecipeList.Where(r => r.Recipe_No == RecipeNo).FirstOrDefault().Recipe_Name;
        }

        public int RecipeDataAdd(int RecipeNo)
        {
            //int recipecnt = AllRecipeDataList.Where(r => r.Recipe_No == RecipeNo).Count();

            int Order = SelectedRecipeDataList.Count + 1;

            RecipeData rd = new RecipeData();

            rd.Recipe_No = RecipeNo;
            rd.iOrder = Order;

            SelectedRecipeDataList.Add(rd);

            return Order;
        }

        public ObservableCollection<Recipe> GetCurrentRecipeList()
        {
            return RecipeList;
        }
    }
}
