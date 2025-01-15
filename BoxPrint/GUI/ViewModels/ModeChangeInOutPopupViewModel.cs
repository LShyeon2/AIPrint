using BoxPrint.Modules.Conveyor;

namespace BoxPrint.GUI.ViewModels
{
    public class ModeChangeInOutPopupViewModel : ViewModelBase
    {
        private CV_BaseModule curCV;
        private bool _PortModeIn;
        public bool PortModeIn
        {
            get => _PortModeIn;
            set => Set("PortModeIn", ref _PortModeIn, value);
        }
        private bool _PortModeOut;
        public bool PortModeOut
        {
            get => _PortModeOut;
            set => Set("PortModeOut", ref _PortModeOut, value);
        }
        private bool _PortModeBoth;
        public bool PortModeBoth
        {
            get => _PortModeBoth;
            set => Set("PortModeBoth", ref _PortModeBoth, value);
        }

        public ModeChangeInOutPopupViewModel(CV_BaseModule cv)
        {
            curCV = cv;
            switch (curCV.PortInOutType)
            {
                case ePortInOutType.INPUT:
                    PortModeIn = true;
                    break;
                case ePortInOutType.OUTPUT:
                    PortModeOut = true;
                    break;
                case ePortInOutType.BOTH:
                    PortModeBoth = true;
                    break;
            }
        }
    }
}
