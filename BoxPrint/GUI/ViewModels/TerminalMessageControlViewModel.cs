using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BoxPrint.GUI.ViewModels
{
    public class TerminalMessageItem
    {
        public DateTime MessageTime { get; set; }
        public string Direction { get; set; }
        public string Message { get; set; }
    }
    public class TerminalMessageControlViewModel : ViewModelBase
    {
        private ObservableCollection<TerminalMessageItem> _TerminalMessageItems;
        public ObservableCollection<TerminalMessageItem> TerminalMessageItems
        {
            get => _TerminalMessageItems;
            set => Set("TerminalMessageItems", ref _TerminalMessageItems, value);
        }

        public TerminalMessageControlViewModel()
        {
            TerminalMessageItems = new ObservableCollection<TerminalMessageItem>();
            GlobalData.Current.OnTerminalMessageChanged += OnTerminalMessageChanged;
            GlobalData.Current.OnTerminalMessageRefreshed += OnTerminalMessageRefreshed;
        }

        public void OnTerminalMessageRefreshed()
        {
            TerminalMessageItems = new ObservableCollection<TerminalMessageItem>();
            GlobalData.Current.DBManager.DbGetProcedureTerminalMSG();
        }

        public void OnTerminalMessageChanged(DateTime dt, eHostMessageDirection direction, string msg, bool init)
        {
            //터미널메세지 아이템에 있는 시간이 같은게 있으면 똑같은 메세지로 간주하여 return함.
            if (TerminalMessageItems.Where(d => d.MessageTime.ToString("yyyy-MM-dd HH:mm:ss") == dt.ToString("yyyy-MM-dd HH:mm:ss")).Count() != 0)
            {
                return;
            }
            
            TerminalMessageItems.Add(
                new TerminalMessageItem()
                {
                    MessageTime = dt,
                    Direction = direction.Equals(eHostMessageDirection.eHostToEq) ? "[H -> E]" : "[E -> H]",
                    Message = msg
                });

            if (!init)
            {
                GlobalData.Current.DBManager.DbSetProcedureTerminalMSG(string.Empty, string.Empty, msg, dt);
                TerminalMessageSort();
            }
            else
            {
                TerminalMessageSort();
            }
               
        }

        public void TerminalMessageSort()
        {

            ObservableCollection<TerminalMessageItem> tmpMsgItems = new ObservableCollection<TerminalMessageItem>();
            ObservableCollection<TerminalMessageItem> tmpItems = new ObservableCollection<TerminalMessageItem>();
            for (int i = 0; i < TerminalMessageItems.Count; i++)
            {
                tmpMsgItems.Add(TerminalMessageItems[i]);
            }

            List<TerminalMessageItem> listTerminalMsg = tmpMsgItems.OrderByDescending(i => i.MessageTime).ToList();
            listTerminalMsg.ForEach(x => tmpItems.Add(x));

            tmpMsgItems = tmpItems;
            TerminalMessageItems = tmpMsgItems;
        }
    }
}
