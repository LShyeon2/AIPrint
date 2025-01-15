using BoxPrint.DataList.MCS;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels;
using BoxPrint.GUI.ViewModels.BindingCommand;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BoxPrint.GUI.UserControls.ViewModels
{
    public class LayOutControlViewModel : ViewModelBase
    {
        #region Variable
        #region Event
        public delegate void LayOutUnitSelect(UIControlBase selectUnit, bool rightClick);
        public event LayOutUnitSelect OnLayOutUnitSelect;

        public delegate void LayOutScaleDataChange(eScaleProperty property, decimal value);
        public event LayOutScaleDataChange OnLayOutScaleDataChange;
        #endregion

        private bool PlayBackControl = false;
        private object SelectUnitLock = new object();

        private readonly decimal ScaleOriginValue = 1;

        public double _tmpMinBayCvWidth = 0;
        public double _tmpMaxBayCvWidth = 0;

        #region Binding
        private double _MinBayCvWidth;
        public double MinBayCvWidth
        {
            get => _MinBayCvWidth;
            set => Set("MinBayCvWidth", ref _MinBayCvWidth, value);
        }

        private double _MaxBayCvWidth;
        public double MaxBayCvWidth
        {
            get => _MaxBayCvWidth;
            set => Set("MaxBayCvWidth", ref _MaxBayCvWidth, value);
        }

        private string _SelectUnitID;
        public string SelectUnitID
        {
            get => _SelectUnitID;
            set => Set("SelectUnitID", ref _SelectUnitID, value);
        }

        private decimal _ScaleValue = 1;
        public decimal ScaleValue
        {
            get => _ScaleValue;
            set
            {
                if (!_ScaleValue.Equals(value))
                {
                    Set("ScaleValue", ref _ScaleValue, value);
                    OnLayOutScaleDataChange?.Invoke(eScaleProperty.eValue, value);
                }
            }
        }
        private decimal _ScaleTick = 1;
        public decimal ScaleTick
        {
            get => _ScaleTick;
            set
            {
                Set("ScaleTick", ref _ScaleTick, value);
                OnLayOutScaleDataChange?.Invoke(eScaleProperty.eTick, value);
            }
        }
        private decimal _ScaleMin = 1;
        public decimal ScaleMin
        {
            get => _ScaleMin;
            set
            { 
                Set("ScaleMin", ref _ScaleMin, value);
                OnLayOutScaleDataChange?.Invoke(eScaleProperty.eMin, value);
            }
        }
        private decimal _ScaleMax = 1;
        public decimal ScaleMax
        {
            get => _ScaleMax;
            set
            {
                Set("ScaleMax", ref _ScaleMax, value);
                OnLayOutScaleDataChange?.Invoke(eScaleProperty.eMax, value);
            }
        }
        private decimal _ChangeOriginValue = 0;
        public decimal ChangeOriginValue
        {
            get => _ChangeOriginValue;
            set
            {
                Set("ChangeOriginValue", ref _ChangeOriginValue, value);
                OnLayOutScaleDataChange?.Invoke(eScaleProperty.eChangeOrigin, value);
            }
        }
        //230314 HHJ SCS 개선
        private double _LayOutTextDegree;
        public double LayOutTextDegree
        {
            get => _LayOutTextDegree;
            set => Set("LayOutTextDegree", ref _LayOutTextDegree, value);
        }

        //20230602 JIG LayOutViewBox 마진 제어
        private Thickness _margins;
        public Thickness Margins
        {
            get => _margins; 
            set => Set("Margins", ref _margins, value);
        }

        #endregion
        #endregion

        #region Constructor
        public LayOutControlViewModel(bool playbackControl)
        {
            PlayBackControl = playbackControl;
        }
        #endregion

        #region Methods
        #region Etc
        /// <summary>
        /// 스케일 관련 변수 초기화
        /// </summary>
        public void SetDefaultScaleData()
        {
            ScaleTick = (decimal)0.05;
            ScaleMin = (decimal)0.1;
            ScaleMax = 2;

            ScaleValue = (ScaleOriginValue - ChangeOriginValue) - ScaleTick;
        }
        /// <summary>
        /// 유닛 선택
        /// </summary>
        /// <param name="select"></param>
        public void SetSelectUnit(UIControlBase select, bool rightClick)
        {
            try
            {
                lock (SelectUnitLock)
                {
                    SelectUnitID = select != null ? select.ControlName : string.Empty;

                    OnLayOutUnitSelect?.Invoke(select, rightClick);
                }
            }
            catch (Exception)
            {

            }
        }
        /// <summary>
        /// LayOutView에서 Zoom 관련 Event 발생시 Scale Value 변경
        /// </summary>
        /// <param name="select"></param>
        public void ScaleCommandAction(eZoomCommandProperty cmdProperty)
        {
            try
            {
                switch (cmdProperty)
                {
                    case eZoomCommandProperty.ePlus:
                        if (ScaleValue < ScaleMax)
                            ScaleValue += ScaleTick;
                        break;
                    case eZoomCommandProperty.eOrigin:
                        ScaleValue = (ScaleOriginValue - ChangeOriginValue) - ScaleTick;
                        //드래그로 LayOutViewBox 마진 초기화
                        Margins = new Thickness(0,0,0,0);
                        break;
                    case eZoomCommandProperty.eMinus:
                        if (ScaleValue > ScaleMin)
                            ScaleValue -= ScaleTick;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        //230911 HHJ 회전시 배율 자동 조정되도록 수정 Start
        /// <summary>
        /// Default Scale Value 리턴
        /// </summary>
        /// <returns></returns>
        public decimal GetScaleDefaultValue()
        {
            return ScaleOriginValue - ChangeOriginValue - ScaleTick;
        }
        //230911 HHJ 회전시 배율 자동 조정되도록 수정 End
        #endregion

        public void CloseView()
        {

        }
        #endregion
    }
}
