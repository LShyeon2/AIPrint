using BoxPrint.Modules;          //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using WCF_LBS.Commands;

namespace BoxPrint.DataList
{
    public class SortingRecClass : INotifyPropertyChanged
    {
        string _no { set; get; }
        public string no
        {
            set
            {
                _no = value;
                Notify("No");
            }
            get
            {
                return _no;
            }
        }

        string _Item1 { set; get; }
        public string Item1
        {
            set
            {
                _Item1 = value;
                Notify("Name");
            }
            get
            {
                return _Item1;
            }
        }

        string _Item2 { set; get; }
        public string Item2
        {
            set
            {
                _Item2 = value;
                Notify("Item2");
            }
            get
            {
                return _Item2;
            }
        }

        string _Item3 { set; get; }
        public string Item3
        {
            set
            {
                _Item3 = value;
                Notify("Item3");
            }
            get
            {
                return _Item3;
            }
        }

        string _Item4 { set; get; }
        public string Item4
        {
            set
            {
                _Item4 = value;
                Notify("Item4");
            }
            get
            {
                return _Item4;
            }
        }

        string _Item5 { set; get; }
        public string Item5
        {
            set
            {
                _Item5 = value;
                Notify("Item5");
            }
            get
            {
                return _Item5;
            }
        }

        string _Item6 { set; get; }
        public string Item6
        {
            set
            {
                _Item6 = value;
                Notify("Item6");
            }
            get
            {
                return _Item6;
            }
        }

        string _Item7 { set; get; }
        public string Item7
        {
            set
            {
                _Item7 = value;
                Notify("Item7");
            }
            get
            {
                return _Item7;
            }
        }

        string _Item8 { set; get; }
        public string Item8
        {
            set
            {
                _Item8 = value;
                Notify("Item8");
            }
            get
            {
                return _Item8;
            }
        }

        string _Item9 { set; get; }
        public string Item9
        {
            set
            {
                _Item9 = value;
                Notify("Item9");
            }
            get
            {
                return _Item9;
            }
        }

        string _Item10 { set; get; }
        public string Item10
        {
            set
            {
                _Item10 = value;
                Notify("Item10");
            }
            get
            {
                return _Item10;
            }
        }

        string _Item11 { set; get; }
        public string Item11
        {
            set
            {
                _Item11 = value;
                Notify("Item11");
            }
            get
            {
                return _Item11;
            }
        }

        string _Item12 { set; get; }
        public string Item12
        {
            set
            {
                _Item12 = value;
                Notify("Item12");
            }
            get
            {
                return _Item12;
            }
        }

        string _Item13 { set; get; }
        public string Item13
        {
            set
            {
                _Item13 = value;
                Notify("Item13");
            }
            get
            {
                return _Item13;
            }
        }

        string _Item14 { set; get; }
        public string Item14
        {
            set
            {
                _Item14 = value;
                Notify("Item14");
            }
            get
            {
                return _Item14;
            }
        }

        string _Item15 { set; get; }
        public string Item15
        {
            set
            {
                _Item15 = value;
                Notify("Item15");
            }
            get
            {
                return _Item15;
            }
        }
        //200428 HHJ MaskProject    //이력조회 추가
        string _Item16 { set; get; }
        public string Item16
        {
            set
            {
                _Item16 = value;
                Notify("Item16");
            }
            get
            {
                return _Item16;
            }
        }

        //200429 HHJ MaskProject    //숫자 단위수 조정 추가
        public SortingRecClass(string _no, object[] _items, bool bUseSort, int iSortGrid, List<string> sortList)
        {
            decimal dRead = -1;
            string strread = string.Empty;

            strread = _no.ToString().Trim();

            if (decimal.TryParse(strread, out dRead))
            {
                strread = string.Format("{0:N0}", dRead);
            }

            this.no = string.Format("{0:N0}", strread);

            eNotifyItems iReadCnt = eNotifyItems.Item1;
            foreach (var vread in _items)
            {
                bool bSortChange = false;

                strread = vread.ToString().Trim();

                if (decimal.TryParse(strread, out dRead))
                {
                    strread = string.Format("{0:N0}", dRead);
                    bSortChange = true;
                }

                //구분을 사용한다면 해당 구분의 문구를 띄워줘야한다.
                if (bUseSort)
                {
                    if (iSortGrid == (int)iReadCnt)
                    {
                        if (bSortChange)
                        {
                            int iread = (int)dRead;

                            iread++;

                            strread = sortList[iread];
                        }
                    }
                }

                switch (iReadCnt)
                {
                    case eNotifyItems.Item1:
                        this.Item1 = strread;
                        break;

                    case eNotifyItems.Item2:
                        this.Item2 = strread;
                        break;

                    case eNotifyItems.Item3:
                        this.Item3 = strread;
                        break;

                    case eNotifyItems.Item4:
                        this.Item4 = strread;
                        break;

                    case eNotifyItems.Item5:
                        this.Item5 = strread;
                        break;

                    case eNotifyItems.Item6:
                        this.Item6 = strread;
                        break;

                    case eNotifyItems.Item7:
                        this.Item7 = strread;
                        break;

                    case eNotifyItems.Item8:
                        this.Item8 = strread;
                        break;

                    case eNotifyItems.Item9:
                        this.Item9 = strread;
                        break;

                    case eNotifyItems.Item10:
                        this.Item10 = strread;
                        break;

                    case eNotifyItems.Item11:
                        this.Item11 = strread;
                        break;

                    case eNotifyItems.Item12:
                        this.Item12 = strread;
                        break;

                    case eNotifyItems.Item13:
                        this.Item13 = strread;
                        break;

                    case eNotifyItems.Item14:
                        this.Item14 = strread;
                        break;

                    case eNotifyItems.Item15:
                        this.Item15 = strread;
                        break;

                    case eNotifyItems.Item16:
                        this.Item16 = strread;
                        break;
                }
                iReadCnt++;
            }
        }

        public SortingRecClass(string _no, PMacDataListItem _items, bool bUseSort, int iSortGrid, List<string> sortList)
        {
            this._no = _no;
            this.Item1 = _items.TagName.ToString();
            this.Item2 = _items.Definition.ToString();
            this.Item3 = _items.Description.ToString();
            this.Item4 = _items.Note.ToString();
            this.Item5 = _items.DataType.ToString();

        }
        // 2020.09.16 // MainView 로딩
        public SortingRecClass(ShelfItem _items, string RMnumber)
        {
            this.Item1 = RMnumber;
            this.Item2 = _items.TagName.ToString();
            //220524 HHJ SCS 개선     //- Shelf Xml제거
            //this.Item3 = _items.P_AxisT_Address.ToString();
            //this.Item4 = _items.P_AxisZ_Address.ToString();
            //this.Item5 = _items.P_Drive_Address.ToString();
            //this.Item6 = _items.P_Fork_Address.ToString();
        }

        public SortingRecClass(CraneCommand _items)
        {
            if (_items == null)
            {
                return;
            }
            this.Item1 = _items.Command.ToString();
            this.Item2 = _items.TargetType.ToString();
            this.Item3 = _items.TargetBank.ToString();
            this.Item4 = _items.TargetBay.ToString();
            this.Item5 = _items.TargetLevel.ToString();
            this.Item6 = _items.TargetTagID.ToString();
            this.Item7 = _items.TargetCarrierID.ToString();
        }

        /// 2020.11.18 Port Command Display
        //public SortingRecClass(PortCommand _items)
        //{
        //    this.Item1 = _items.PortCommandType.ToString();
        //    this.Item2 = _items.TargetTagID.ToString();
        //    this.Item3 = _items.TargetUnitID.ToString();
        //}

        /// 2020.11.18 Port Command Display
        public SortingRecClass(TowerLampCommand _items)
        {
            this.Item1 = _items.Green.ToString();
            this.Item2 = _items.Yellow.ToString();
            this.Item3 = _items.Red.ToString();
            this.Item4 = _items.Buzzer.ToString();
            this.Item5 = _items.MuteMode.ToString();
        }


        public SortingRecClass(string RmName, decimal postion1, decimal postion2, decimal postion3, decimal postion4, decimal postion5)
        {
            this.Item1 = RmName;
            this.Item2 = postion1.ToString();
            this.Item3 = postion2.ToString();
            this.Item4 = postion3.ToString();
            this.Item5 = postion4.ToString();
            this.Item6 = postion5.ToString();
        }


        public SortingRecClass(string Tag)
        {
            try
            {
                if (Tag != string.Empty)
                {
                    this.Item1 = Tag;
                    //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
                    //this.Item2 = Tag.Substring(0, 1);
                    //this.Item3 = Tag.Substring(4, 3);
                    //this.Item4 = Tag.Substring(1, 3);
                    this.Item2 = ShelfTagHelper.GetBank(Tag).ToString();
                    this.Item3 = ShelfTagHelper.GetBay(Tag).ToString();
                    this.Item4 = ShelfTagHelper.GetLevel(Tag).ToString();
                }
            }
            catch (Exception)
            {

            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void Notify(string propName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
