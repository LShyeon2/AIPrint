using BoxPrint.Config;
using BoxPrint.Config.Print;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace BoxPrint.Modules.Print
{
    [XmlRoot("ScenarioList")]
    public class ScenarioList
    {

        private ObservableCollection<ScenarioData> _scenarioList = null;

        [XmlElement("ScenarioStep")]
        public ObservableCollection<ScenarioData> scenarioList
        {
            get
            {
                return _scenarioList;
            }
            set
            {
                //if (_scenarioList.Equals(value))
                {
                    _scenarioList = value;
                }
            }
        }

        public ePrintScenarioState CurrentState { get; set; }
        public int CurrentStep { get; set; }


        public string ScenarioFilePath = string.Empty;

        private static Object thisLock = new Object();

        public ScenarioList()
        {
            CurrentState = ePrintScenarioState.Stop;
            CurrentStep = 0;

        }

        public void UpdateScenarioData(ObservableCollection<ScenarioData> list)
        {
            _scenarioList.Clear();
            foreach (var item in list)
            {
                _scenarioList.Add(item);
            }

            ScenarioList.Serialize(ScenarioFilePath, this);
        }

        public ScenarioData GetCurrentScenario()
        {
            return scenarioList.Where(r => r.iStep == CurrentStep).FirstOrDefault();
        }

        public bool SetNextStep()
        {
            int last = scenarioList.Last().iStep;

            if (CurrentStep == last)
            {
                ScenarioStop();
                return false;
            }

            return true;
        }

        public void ScenarioStart()
        {
            CurrentState = ePrintScenarioState.Run;
            CurrentStep = 1;
        }

        public void ScenarioStop()
        {
            CurrentState = ePrintScenarioState.Stop;
            CurrentStep = 0;
        }

        public void ScenarioPause()
        {
            CurrentState = ePrintScenarioState.Paused;
        }

        #region Serialize & Deserialize
        /// <summary>
        /// 현재 List를 xml 파일로 저장한다
        /// </summary>
        /// <param name="fileName">파일 경로</param>
        /// <param name="ScenarioData">저장할 List</param>
        public static void Serialize(string fileName, ScenarioList data)
        {
            try
            {
                lock (thisLock)
                {
                    string path = System.IO.Path.GetDirectoryName(fileName);
                    if (!System.IO.Directory.Exists(path))
                        System.IO.Directory.CreateDirectory(path);

                    using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        XmlSerializer xmlSer = new XmlSerializer(typeof(ScenarioList));
                        xmlSer.Serialize(fs, data);
                        xmlSer = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Serialize - Exception:{0}", ex);
                throw;
            }
        }

        /// <summary>
        /// xml 파일로 부터 List를 가져온다
        /// </summary>
        /// <param name="fileName">파일 경로</param>
        /// <returns>list</returns>
        public static ScenarioList Deserialize(string fileName)
        {
            try
            {
                ScenarioList data = null;
                lock (thisLock)
                {
                    if (System.IO.File.Exists(fileName))
                    {
                        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            XmlSerializer xmlSer = new XmlSerializer(typeof(ScenarioList));
                            data = (ScenarioList)xmlSer.Deserialize(fs);
                            xmlSer = null;
                        }
                    }
                    else
                    {
                        data = new ScenarioList();
                    }
                }
                data.ScenarioFilePath = fileName;
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Deserialize - Exception:{0}", ex);
                throw;
            }
        }
        #endregion



    }
}
