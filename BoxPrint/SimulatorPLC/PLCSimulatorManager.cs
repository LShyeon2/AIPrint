using System.Collections.Generic;
using System.Linq;

namespace BoxPrint.SimulatorPLC
{
    public class PLCSimulatorManager : SingletonBase<PLCSimulatorManager>
    {
        public List<BaseSimulator> PLCSimulList;
        public BoothSimulator BS;
        public CraneSimulator C1;
        public CraneSimulator C2;
        public PLCSimulatorManager()
        {
            PLCSimulList = new List<BaseSimulator>();
            CreateSimulPLCMoudules();// LKJ TEST

        }
        public void StartSimulPLCModules()
        {
            foreach (var pItem in PLCSimulList)
            {
                pItem.StartSimulPLC();
            }
        }
        public void StopSimulPLCModules()
        {
            foreach (var pItem in PLCSimulList)
            {
                pItem.StopSimulPLC();
            }
        }
        public BaseSimulator GetSimulator(string SPLCName)
        {
            return PLCSimulList.Where(s => s.SPLC_Name == SPLCName).FirstOrDefault();
        }
        public void CreateSimulPLCMoudules()
        {
            if (PLCSimulList.Count() > 0)
            {
                return;
            }
            //Booth PLC 생성
            BS = new BoothSimulator();
            BS.SPLC_Name = "PLC_BOOTH";
            BS.SetPLCAddress(0, GlobalData.Current.MainBooth.PLCReadOffset, GlobalData.Current.MainBooth.PLCWriteOffset);
            PLCSimulList.Add(BS);
            //Crane PLC 생성

            C1 = new CraneSimulator(1);
            C1.SPLC_Name = "PLC_CRANE1";
            C1.SetPLCAddress(GlobalData.Current.mRMManager.FirstRM.PLCNumber, GlobalData.Current.mRMManager.FirstRM.PLCReadOffset, GlobalData.Current.mRMManager.FirstRM.PLCWriteOffset);
            C1.SetInitPosition(1, 1, 1);
            PLCSimulList.Add(C1);



            if (GlobalData.Current.mRMManager.SecondRM != null)
            {
                C2 = new CraneSimulator(2);
                C2.SPLC_Name = "PLC_CRANE2";
                C2.SetPLCAddress(GlobalData.Current.mRMManager.SecondRM.PLCNumber, GlobalData.Current.mRMManager.SecondRM.PLCReadOffset, GlobalData.Current.mRMManager.SecondRM.PLCWriteOffset);
                C2.SetInitPosition(1, GlobalData.Current.ShelfMgr.AllData.MaxBay, 1);
                PLCSimulList.Add(C2);
            }

            //Port PLC 생성
            foreach (var cvItem in GlobalData.Current.PortManager.AllCVList)
            {
                PortSimulator temp = new PortSimulator();
                temp.SPLC_Name = "PLC_" + cvItem.ModuleName;
                temp.SetBase(cvItem);
                temp.SetSimulProperty(cvItem.CVModuleType, cvItem.PortType, cvItem.UseBCR, cvItem.CVModuleType == eCVType.Plain, cvItem.PortInOutType); //LKJ 추가
                temp.SetPLCAddress(cvItem.PLCNum, cvItem.BaseAddress, cvItem.BaseAddress);
                PLCSimulList.Add(temp);
            }
            foreach (var pItem in PLCSimulList)//다음 포트 설정
            {
                if (pItem is PortSimulator ps)
                {
                    ps.SetNextSimulPort(GetSimulator(ps.NextSimulPortName) as PortSimulator);
                    ps.SetPrevSimulPort(GetSimulator(ps.PrevSimulPortName) as PortSimulator);
                }
            }

        }
    }
}
