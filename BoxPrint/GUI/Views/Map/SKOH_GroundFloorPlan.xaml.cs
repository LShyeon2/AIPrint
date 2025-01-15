using BoxPrint.DataList;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views.Map
{
    /// <summary>
    /// Page1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SKOH_GroundFloorPlan : Page
    {
        Dictionary<string, MapGUIControl> dicEQPINFO;
        //List<EQPInfo> EQPList = new List<EQPInfo>();

        public SKOH_GroundFloorPlan()
        {
            InitializeComponent();

            //Storyboard storyboardBuffer = Resources["storyboardSelectkiosk"] as Storyboard;
            //storyboardBuffer.Begin();

            //MapView._EventHandler_EQPIDChange += SelectedRackMaster;

            //EQPList = eqplist;

            InitLoad();
        }

        public void InitLoad()
        {
            dicEQPINFO = new Dictionary<string, MapGUIControl>();

            //230403 리스트 변경
            //EQPList = GlobalData.Current.DBManager.EqpListForMap();
            //EQPList = GlobalData.Current.EQPList;

        }

        //YSW_221026 선택된 RM 표시
        private void SelectedRackMaster()
        {
            foreach (var item in dicEQPINFO.Values)
            {
                if (item.EQPID == GlobalData.Current.EQPID)
                    item.isSelect = true;
                else
                    item.isSelect = false;
            }
        }

        //YSW_221019 RM EQPID 값 초기화
        public void InitEQPID()
        {
            foreach (EQPInfo item in GlobalData.Current.EQPList)
            {
                if (dicEQPINFO.ContainsKey(item.EQPNumber))
                {
                    if (dicEQPINFO[item.EQPNumber].EQPID != item.EQPID)
                    {
                        dicEQPINFO[item.EQPNumber].EQPID = item.EQPID;
                    }

                    //221230 YSW Map View안에 각 SCS의 Tooltip에 IP 항목 추가 : DB에서 가져온 해당 EQPID IP를 해당 RM에 입력
                    if (dicEQPINFO[item.EQPNumber].SCSIP != item.SCSIP)
                    {
                        dicEQPINFO[item.EQPNumber].SCSIP = item.SCSIP;
                    }

                    if (dicEQPINFO[item.EQPNumber].DisplayName != TranslationManager.Instance.Translate(item.EQPName).ToString())
                    {
                        dicEQPINFO[item.EQPNumber].DisplayName = TranslationManager.Instance.Translate(item.EQPName).ToString();
                    }
                    SelectedRackMaster();
                }
            }
        }

        //YSW_221024 Dictionary에 RM Control 정보 입력
        private void RackMaster_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is MapGUIControl senderBuffer)
            {
                if (senderBuffer.Tag.ToString() == "RM")
                {
                    if (!dicEQPINFO.ContainsKey(senderBuffer.IndexNumber.ToString()))
                    {
                        dicEQPINFO.Add(senderBuffer.IndexNumber.ToString(), senderBuffer);
                    }
                }
            }

            InitEQPID();
        }
    }
}
