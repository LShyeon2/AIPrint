using BoxPrint.GUI.UIControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxPrint.GUI.ExtensionCollection
{
    public static class GUIExtensionCollection
    {
        public static ControlBase GetUnit(UIControlBase uiControl, bool PlayBackControl)
        {
            ControlBase cBase = null;
            if (uiControl is UIControlShelf shelf)
            {
                if (!PlayBackControl)
                    cBase = GlobalData.Current.ShelfMgr.GetShelf(shelf.UnitName);
            }
            else if (uiControl is UIControlRM rm)
            {
                if (!PlayBackControl)
                    cBase = GlobalData.Current.mRMManager[rm.UnitName];
            }
            else if (uiControl is UIControlCV cv)
            {
                if (!PlayBackControl)
                    cBase = GlobalData.Current.PortManager.GetCVModule(cv.UnitName);
            }

            return cBase;
        }
    }
}
