using BoxPrint.GUI.UIControls;       //220509 HHJ SCS 개선     //- CraneControl 기존 소스에 적용
using System;
using System.Diagnostics;
namespace BoxPrint.Modules
{
    /// <summary>
    /// 동작 모듈의 기본 베이스
    /// </summary>
    //220509 HHJ SCS 개선     //- CraneControl 기존 소스에 적용
    //public abstract class ModuleBase
    public abstract class ModuleBase : ControlBase
    {
        public ModuleBase ParentModule
        {
            get;
            protected set;
        }
        public string ModuleName { get; protected set; }
        public bool SimulMode { get; set; }

        protected GlobalData GData { get; set; }
        //220509 HHJ SCS 개선     //- CraneControl 기존 소스에 적용
        //public ModuleBase(string mName, bool simul)
        public ModuleBase(string mName, bool simul) : base(mName, 1)
        {
            ModuleName = mName;
            SimulMode = simul;
            GData = GlobalData.Current;
            GlobalData.Current.InsertModuletoStore(this);
        }

        /// <summary>
        /// 모듈에 문제가 있는지 자가 체크
        /// </summary>
        /// <returns> true : 문제 발생 false : 정상
        /// </returns>
        public virtual bool DoAbnormalCheck()
        {
            throw new NotImplementedException("DoAbnormalCheck() 는 구현되지 않았습니다.");
        }

        /// <summary>
        /// 241105 RGJ TIMEOUT 시스템 시간 의존 개선.
        /// 시스템 시간 변경에도 Stopwatch 를 활용하여  올바르게 동작가능하게함
        /// Stopwatch stopwatch = Stopwatch.StartNew();
        /// IsTimeout(stopwatch, 10); // 10초 타임아웃 체크
        /// </summary>
        /// <param name="stopwatch"></param>
        /// <param name="nTimeoutValue"></param>
        /// <returns></returns>
        public bool IsTimeout_SW(Stopwatch stopwatch, int nTimeoutValue)
        {
            if(stopwatch == null)
            {
                return false;
            }
            // 지정한 초를 기준으로 Timeout 설정
            TimeSpan tLimit = TimeSpan.FromSeconds(nTimeoutValue);
            return stopwatch.Elapsed > tLimit;
        }
        public void SetParentModule(ModuleBase P)
        {
            ParentModule = P;
        }

        public bool CheckModuleHeavyAlarmExist()
        {
            return GlobalData.Current.Alarm_Manager.CheckModuleHeavyAlarmExist(this.ModuleName);
        }

        public string GetModuleLastAlarmCode()
        {
            return GlobalData.Current.Alarm_Manager.GetModule_LastAlarmCode(this.ModuleName);
        }

        public virtual void WriteCurrentState()
        {
            throw new NotImplementedException("WriteCurrentState() 는 구현되지 않았습니다.");
        }
        public virtual bool ReadLastState()
        {
            throw new NotImplementedException("WriteCurrentState() 는 구현되지 않았습니다.");
        }

        public override int GetUnitServiceState()
        {
            //SuHwan_20220525 : 임시 나중에 수정하자
            return (GlobalData.Current.MainBooth.SCState == eSCState.AUTO) ? 2 : 1;
        }
        public virtual bool CheckGetAble(string CarrierID)
        {
            return false;
        }
        public virtual bool CheckPutAble()
        {
            return false;
        }
    }
}
