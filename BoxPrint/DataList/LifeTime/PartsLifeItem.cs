using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stockerfirmware.DataList.LifeTime
{
    public class PartsLifeItem : INotifyPropertyChanged
    {

        private bool NeedUpdate = false; //불필요한 쿼리 방지 위해 업데이트 플래그
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public string ModuleName { get; set; } //파츠의 모듈네임

        public string PartsName { get; set; } //파츠 항목

        public string PartsModel { get; set; } //파츠 세부 모델

        public string PartsDesc { get; set; } //파츠 설명

        public string PartsMaker { get; set; } //파츠 설명


        public string MeasurementUnits { get; set; } //측정 유닛

        private double mCurrentValue = 0;
        public double CurrentValue
        {
            get
            {
                return mCurrentValue;
            }
            set
            {
                mCurrentValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CurrentValue"));
                NeedUpdate = true;
            }
        } //현재 값

        public double LifeTimeValue { get; set; } //수명 값

        //소모 퍼센티지
        public double LifePercent
        {
           get { return (CurrentValue / LifeTimeValue) * 100; }
        }

        public string LifePercentageString
        {
            get { return LifePercent.ToString("F2") + " %"; }
        }
        public bool LifeOver
        {
            get { return CurrentValue >= LifeTimeValue; }
        }

        public PartsLifeItem(string mModule ,string pName, string pModel, string pMaker , string pDesc,string mUnit,double currentValue,double lifeValue)
        {
            ModuleName = mModule;
            PartsName = pName;
            PartsModel = pModel;
            PartsMaker = pMaker;
            PartsDesc = pDesc;
            MeasurementUnits = mUnit;
            mCurrentValue = currentValue;
            LifeTimeValue = lifeValue;
        }
        public bool IsUpdateRequire()
        { 
            return NeedUpdate;
        }
        public void NotifyUpdateComplete()
        {
            NeedUpdate = false;
        }
    }
}
