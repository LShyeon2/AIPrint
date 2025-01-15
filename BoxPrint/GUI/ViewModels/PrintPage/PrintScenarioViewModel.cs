using BoxPrint.Config;
using BoxPrint.Config.Print;
using BoxPrint.Modules.Print;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoxPrint.GUI.ViewModels.PrintPage
{
    public class PrintScenarioViewModel : ViewModelBase
    {
        private ObservableCollection<ScenarioData> _ViewScenatrioList;
        public ObservableCollection<ScenarioData> ViewScenarioList
        {
            get => _ViewScenatrioList;
            set => Set("ViewScenatrioList", ref _ViewScenatrioList, value);

        }

        private ScenarioData _SelValue;
        public ScenarioData SelValue
        {
            get => _SelValue;
            set => Set("SelValue", ref _SelValue, value);
        }

        private int _CurrentStep;

        public int CurrentStep
        {
            get => _CurrentStep;
            set => Set("CurrentStep", ref _CurrentStep, value);
        }

        private ePrintScenarioState _CurrentState;
        public ePrintScenarioState CurrentState
        {
            get => _CurrentState;
            set => Set("CurrentState", ref _CurrentState, value);
        }

        private bool _UseRobot;
        public bool UseRobot
        {
            get => _UseRobot;
            set => Set("UseRobot", ref _UseRobot, value);
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

        public PrintScenarioViewModel()
        {
            ViewScenarioList = new ObservableCollection<ScenarioData>();
            OnScenarioListRefreshed();

            CurrentState = ePrintScenarioState.Stop;

            //이벤트 구독
            GlobalData.Current.scenarioListrefresh += OnScenarioListRefreshed;
            GlobalData.Current.printScenarioStateChange += OnCurrentStateChanged;
        }


        private void OnScenarioListRefreshed()
        {

            Task.Run(() =>
            {
                //ObservableCollection<Recipe> RecipeListRefreshData = new ObservableCollection<Recipe>();

                //CurrRecipeNo = GlobalData.Current.Recipe_Manager.GetCurrentRecipeNo();
                //CurrRecipeName = GlobalData.Current.Recipe_Manager.GetRecipeName(CurrRecipeNo);

                ViewScenarioList = new ObservableCollection<ScenarioData>(
                    GlobalData.Current.PrintScenarioList.scenarioList.OrderBy(r => r.Step));
            });
        }

        private void OnCurrentStateChanged(ePrintScenarioState State)
        {
            CurrentState = State;
            //GlobalData.Current.PrintScenarioList.SetCurrentState(State);
        }

        public void SelectedDataListUp()
        {
            if (SelValue == null)
                return;

            int idx = SelValue.iStep;

            if (idx <= 1)
                return;

            var curr = ViewScenarioList.Where(r => r.iStep == idx).FirstOrDefault();
            var trgt = ViewScenarioList.Where(r => r.iStep == idx - 1).FirstOrDefault();

            if (curr != null && trgt != null)
            {
                curr.iStep = idx - 1;
                trgt.iStep = idx;

                // 순서를 재정렬
                ViewScenarioList = new ObservableCollection<ScenarioData>(
                    ViewScenarioList.OrderBy(r => r.iStep));
            }
        }

        public void SelectedDataListDown()
        {
            if (SelValue == null)
                return;

            int idx = SelValue.iStep;

            if (idx >= ViewScenarioList.Count || idx <= 0)
                return;

            var curr = ViewScenarioList.Where(r => r.iStep == idx).FirstOrDefault();
            var trgt = ViewScenarioList.Where(r => r.iStep == idx + 1).FirstOrDefault();

            if (curr != null && trgt != null)
            {
                curr.iStep = idx + 1;
                trgt.iStep = idx;

                // 순서를 재정렬
                ViewScenarioList = new ObservableCollection<ScenarioData>(
                    ViewScenarioList.OrderBy(r => r.iStep));
            }

        }

        internal void ScenarioAdd()
        {
            //int Reno = CurrRecipeNo;
            int select = ViewScenarioList.Count + 1;

            ViewScenarioList.Add(
                new ScenarioData()
                {
                    Step = select.ToString(),
                }
                );

            SelValue = ViewScenarioList.Where(r => r.iStep == select).FirstOrDefault();

        }

        internal void ScenarioDel()
        {
            if (SelValue == null)
                return;

            int select = SelValue.iStep;
            int newOrder = select;

            bool isDel = ViewScenarioList.Remove(SelValue);

            if (isDel)
            {
                foreach (ScenarioData r in ViewScenarioList)
                {
                    if (r.iStep.Equals(newOrder + 1))
                    {
                        r.iStep = newOrder;
                        newOrder++;
                    }
                }
            }

            // 순서를 재정렬
            ViewScenarioList = new ObservableCollection<ScenarioData>(
                ViewScenarioList.OrderBy(r => r.iStep));

            if (select > ViewScenarioList.Count)
                select = ViewScenarioList.Count;

            SelValue = ViewScenarioList.Where(r => r.iStep == select).FirstOrDefault();
        }

        internal void SaveData()
        {
            var UpdateData = ViewScenarioList.Where(r => r.Step == SelValue.Step).FirstOrDefault();

            if (UpdateData != null)
            {
                UpdateData = SelValue;
            }

            GlobalData.Current.PrintScenarioList.UpdateScenarioData(ViewScenarioList);

            //ScenarioList.Serialize(GlobalData.Current.CurrentFilePaths(FullPath) + GlobalData.Current.ScenarioFilePath, GlobalData.Current.PrintScenarioList);
            //GlobalData.Current.Recipe_Manager.CurrentRecipeReLoad();


            // 순서를 재정렬
            ViewScenarioList = new ObservableCollection<ScenarioData>(
                ViewScenarioList.OrderBy(r => r.iStep));
        }


        protected override void ViewModelTimer()
        {

            while (true)
            {
                Thread.Sleep(500);

                if (CloseThread) return;

                //상태 변경시 업데이트
                //if ()
            }
        }
    }
}
