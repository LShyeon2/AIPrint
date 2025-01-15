using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.UserControls;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using System.Windows.Controls;

namespace BoxPrint.GUI.ViewModels
{
    public class UnitIODetailViewModel : ViewModelBase
    {
        private ControlBase SelectUnit = null;

        private string _UnitID;
        public string UnitID
        {
            get => _UnitID;
            set => Set("UnitID", ref _UnitID, value);
        }

        private UserControl _DetailControl;
        public UserControl DetailControl
        {
            get => _DetailControl;
            set => Set("DetailControl", ref _DetailControl, value);
        }
        private ConveyorIODetailView conveyorDetail;
        private CraneIODetailView craneDetail;

        public UnitIODetailViewModel(bool IsPlayBack)
        {
            conveyorDetail = new ConveyorIODetailView(IsPlayBack);
            craneDetail = new CraneIODetailView(IsPlayBack);
        }

        public void AbleViewModel(ControlBase selectunit)
        {
            SelectUnit = selectunit;
            UnitID = selectunit.ControlName;

            //타입 체크
            if (SelectUnit is RMModuleBase)
            {
                craneDetail.AbleControl(SelectUnit);
                DetailControl = craneDetail;
            }
            else if (SelectUnit is CV_BaseModule)
            {
                conveyorDetail.AbleControl(SelectUnit);
                DetailControl = conveyorDetail;
            }
        }

        public void DisableViewmodel()
        {
            //타입 체크
            if (SelectUnit is RMModuleBase)
            {
                craneDetail.DisableControl();
            }
            else if (SelectUnit is CV_BaseModule)
            {
                conveyorDetail.DisableControl();
            }
        }
    }
}
