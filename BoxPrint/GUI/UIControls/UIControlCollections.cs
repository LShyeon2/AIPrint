using System.Windows;

namespace BoxPrint.GUI.UIControls
{
    public class UIControlRM : UIControlBase
    {
        protected override void SetBinding(object data)
        {
            if (this.DataContext is ControlBase collection)
            {
                BindingHelper.ElementBind(this, SlotListProperty, SlotListProperty.Name);
                BindingHelper.ElementBind(this, CraneArmStateProperty, CraneArmStateProperty.Name);
                //220610 HHJ SCS 개선     //- Crane UIControl 변경
                BindingHelper.ElementBind(this, CraneStateProperty, CraneStateProperty.Name);
                //241001 HDK Crane 작업가능상태 표시 개선 
                BindingHelper.ElementBind(this, CraneSCModeProperty, CraneSCModeProperty.Name);
                //220914 HHJ SCS 개선     //- RM Fork Position UI 연동
                //220919 HHJ SCS 개선     //- ForkAxisPosition Biding Item 변경
                //BindingHelper.ElementBind(this, UIForkAxisPositionProperty, UIForkAxisPositionProperty.Name);
                BindingHelper.ElementBind(this, ForkAxisPositionProperty, ForkAxisPositionProperty.Name);
                //230118 HHJ SCS 개선
                BindingHelper.ElementBind(this, SelectorProperty, SelectorProperty.Name);
                BindingHelper.ElementBind(this, SelectZIndexProperty, SelectZIndexProperty.Name);
                BindingHelper.ElementBind(this, SelectorProperty, SelectorProperty.Name);
                //230314 HHJ SCS 개선
                //BindingHelper.ElementBind(this, LayOutAngleProperty, LayOutAngleProperty.Name);

                BindingHelper.ElementBind(this, CraneSC_StateProperty, CraneSC_StateProperty.Name);
            }
        }

        protected override void ControlBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_isFirstTimeLoaded || GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                if (e.OriginalSource is UIControlRM uic)
                {
                    //230307 HHJ SCS 개선
                    ////220610 HHJ SCS 개선     //- 범례 추가
                    ////UnitName이 있는경우에만 매니저에서 가져오도록 한다.
                    ////DataContext = GlobalData.Current.mRMManager[uic.UnitName];
                    //if (!string.IsNullOrEmpty(uic.UnitName))
                    //    DataContext = GlobalData.Current.mRMManager[uic.UnitName];
                    //if (!uic.IsPlayBack)
                    {
                        if (!string.IsNullOrEmpty(uic.UnitName))
                            DataContext = GlobalData.Current.mRMManager[uic.UnitName];
                    }
                }

                this.DataContextChanged += Control_DataContextChanged;

                OnFirstTimeLoaded();

                m_isFirstTimeLoaded = false;
            }
        }
    }

    //220509 HHJ SCS 개선     //- ShelfControl 변경
    public class UIControlShelf : UIControlBase
    {
        protected override void SetBinding(object data)
        {
            if (this.DataContext is ControlBase collection)
            {
                BindingHelper.ElementBind(this, SlotListProperty, SlotListProperty.Name);
                BindingHelper.ElementBind(this, ShelfTypeProperty, ShelfTypeProperty.Name);
                BindingHelper.ElementBind(this, DeadZoneProperty, DeadZoneProperty.Name);
                BindingHelper.ElementBind(this, ShelfBusyRmProperty, ShelfBusyRmProperty.Name);
                BindingHelper.ElementBind(this, ShelfEnableProperty, ShelfEnableProperty.Name);
                //220609 HHJ SCS 개선     //- Shelf UIControl 변경
                BindingHelper.ElementBind(this, ShelfStatusProperty, ShelfStatusProperty.Name);
                //230118 HHJ SCS 개선
                BindingHelper.ElementBind(this, SelectorProperty, SelectorProperty.Name);
                BindingHelper.ElementBind(this, SelectZIndexProperty, SelectZIndexProperty.Name);
                //230405 HHJ SCS 개선     //- Memo 기능 추가
                BindingHelper.ElementBind(this, ShelfMemoProperty, ShelfMemoProperty.Name);
                //BindingHelper.ElementBind(this, LayOutAngleProperty, LayOutAngleProperty.Name);
                //231101 HHJ Shelf NG State 추가
                BindingHelper.ElementBind(this, ShelfNGStateProperty, ShelfNGStateProperty.Name);
            }
        }

        protected override void ControlBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_isFirstTimeLoaded || GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                if (e.OriginalSource is UIControlShelf uic)
                {
                    //230307 HHJ SCS 개선
                    //ShelfControl에서 가져와야한다.
                    //DataContext = GlobalData.Current.ShelfMgr.GetShelf(uic.UnitName);
                    //if (!uic.IsPlayBack)
                    {
                        //PlayBack이 아니라면 기존과 동일하게 각 Manager에서
                        DataContext = GlobalData.Current.ShelfMgr.GetShelf(uic.UnitName);
                    }
                }

                this.DataContextChanged += Control_DataContextChanged;

                OnFirstTimeLoaded();

                m_isFirstTimeLoaded = false;
            }
        }
    }

    //220610 HHJ SCS 개선     //- CV UIControl 추가
    public class UIControlCV : UIControlBase
    {
        protected override void SetBinding(object data)
        {
            if (this.DataContext is ControlBase collection)
            {
                BindingHelper.ElementBind(this, SlotListProperty, SlotListProperty.Name);
                BindingHelper.ElementBind(this, CVEnableProperty, CVEnableProperty.Name);       //220805 조숭진
                BindingHelper.ElementBind(this, PortInOutTypeProperty, PortInOutTypeProperty.Name);       //220902 HHJ SCS 개선     //- Direction 변경에 따른 UI 반응 추가
                                                                                                          //230118 HHJ SCS 개선
                BindingHelper.ElementBind(this, SelectorProperty, SelectorProperty.Name);
                BindingHelper.ElementBind(this, SelectZIndexProperty, SelectZIndexProperty.Name);

                BindingHelper.ElementBind(this, CVWayProperty, CVWayProperty.Name);     //230214 HHJ SCS 개선

                //230217 HHJ SCS 개선     //CV UI State 관련 추가
                BindingHelper.ElementBind(this, CVModuleTypeProperty, CVModuleTypeProperty.Name);
                BindingHelper.ElementBind(this, ConveyorUIStateProperty, ConveyorUIStateProperty.Name);
                BindingHelper.ElementBind(this, IsTrackPauseProperty, IsTrackPauseProperty.Name);
                BindingHelper.ElementBind(this, PortAccessModeProperty, PortAccessModeProperty.Name);
                //230314 HHJ SCS 개선
                //BindingHelper.ElementBind(this, LayOutAngleProperty, LayOutAngleProperty.Name);
                BindingHelper.ElementBind(this, PortBCRStateProperty, PortBCRStateProperty.Name);       //230517 HHJ SCS 개선     //- BCR Path 변경

                //241030 HoN 화재 관련 추가 수정        -. 화재수조 가용조건 변경 -> 화재수조는 적재조건, 완료조건 Carrier Sensor 무시 요청
                BindingHelper.ElementBind(this, WaterPoolExistProperty, WaterPoolExistProperty.Name);
            }
        }

        protected override void ControlBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_isFirstTimeLoaded || GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                if (e.OriginalSource is UIControlCV uic)
                {
                    //230307 HHJ SCS 개선
                    ////220902 HHJ SCS 개선     //- Direction 변경에 따른 UI 반응 추가   //이름 빈값 체크 조건 추가
                    //if (!string.IsNullOrEmpty(uic.UnitName))
                    //    DataContext = GlobalData.Current.PortManager.GetCVModule(uic.UnitName);
                    //if (!uic.IsPlayBack)
                    {
                        if (!string.IsNullOrEmpty(uic.UnitName))
                            DataContext = GlobalData.Current.PortManager.GetCVModule(uic.UnitName);
                    }
                }

                this.DataContextChanged += Control_DataContextChanged;

                OnFirstTimeLoaded();

                m_isFirstTimeLoaded = false;
            }
        }
    }

    public class UIControlRMRail : UIControlBase
    {
        protected override void SetBinding(object data)
        {
            if (this.DataContext is ControlBase collection)
            {
                BindingHelper.ElementBind(this, CraneSC_StateProperty, CraneSC_StateProperty.Name);
            }
        }

        protected override void ControlBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_isFirstTimeLoaded || GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                if (e.OriginalSource is UIControlRMRail uic)
                {
                    if (!uic.IsPlayBack)
                    {
                        if (!string.IsNullOrEmpty(uic.UnitName))
                            DataContext = GlobalData.Current.mRMManager[uic.UnitName];
                    }
                }

                this.DataContextChanged += Control_DataContextChanged;

                OnFirstTimeLoaded();

                m_isFirstTimeLoaded = false;
            }
        }
    }
}
