using BoxPrint.DataList;

using System;
using System.ComponentModel;

namespace BoxPrint.GUI.UIControls
{
    public class SlotEventArgs : EventArgs
    {
        public int SlotNum { get; private set; }
        public string MaterialName { get; private set; }
        public string BeforeMaterialName { get; private set; }

        public bool MaterialExist { get; private set; }
        public bool BeforeMaterialExist { get; private set; }

        public SlotEventArgs(int slotnum, string Name, string bName, bool Exist, bool bExist)
        {
            SlotNum = slotnum;
            MaterialName = Name;
            BeforeMaterialName = bName;
            MaterialExist = Exist;
            BeforeMaterialExist = bExist;
        }
    }

    public class Slot : INotifyPropertyChanged
    {
        public event EventHandler<SlotEventArgs> SlotStateChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        private static object objLock = new object();
        protected void RaisePropertyChanged(string propertyName)
        {
            lock (objLock)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private int _SlotNum;
        public int SlotNum
        {
            get { return _SlotNum; }
            private set
            {
                _SlotNum = value;
                RaisePropertyChanged("SlotNum");
            }
        }
        public string _SlotLocation;
        public string SlotLocation
        {
            get => _SlotLocation;
            private set => _SlotLocation = value;
        }
        private string _BeforeMaterialName; //이전 캐리어ID 를 저장
        private string _MaterialName;
        public string MaterialName
        {
            get { return _MaterialName; }
            private set
            {
                _BeforeMaterialName = _MaterialName;
                _MaterialName = value;
                RaisePropertyChanged("MaterialName");
            }
        }
        private bool _BeforeMaterialExist; //이전 유무 상태를 저장
        private bool _MaterialExist;
        public bool MaterialExist
        {
            get { return _MaterialExist; }
            private set
            {
                _BeforeMaterialExist = _MaterialExist;
                _MaterialExist = value;
                RaisePropertyChanged("MaterialExist");
            }
        }
        //240719 HoN ProductEmpty 구분 추가
        private eProductEmpty _ProductEmpty;
        public eProductEmpty ProductEmpty
        {
            get => _ProductEmpty;
            private set
            {
                _ProductEmpty = value;
                RaisePropertyChanged("ProductEmpty");
            }
        }

        //220519 HHJ SCS 개선     //- 기자재 종류 바인딩 추가
        private eCarrierSize _MaterialType;
        public eCarrierSize MaterialType
        {
            get { return _MaterialType; }
            private set
            {
                _MaterialType = value;
                RaisePropertyChanged("MaterialType");
            }
        }

        public CarrierItem InSlotCarrier
        {
            get
            {
                return CarrierStorage.Instance.GetCarrierItem(MaterialName);
            }
        }

        public Slot(int slotnum, string SlotLoc)
        {
            SlotNum = slotnum;
            SlotLocation = SlotLoc;
            MaterialName = string.Empty;
            MaterialExist = false;
        }

        //221014 HHJ SCS 개선     //- C/V CarrierExist 실시간 반영
        public void SetCarrierExist(bool bexist)
        {
            MaterialExist = bexist;
        }

        public void SetCarrierData(string materialname, bool bUpdate = true)
        {
            //220524 HHJ SCS 개선     //- Shelf Xml제거
            //캐리어 스토리지에 해당 이름을 가진 캐리어가 없다면 없다면 업데이트가 되면 안됨.
            #region 이전
            //MaterialName = materialname;
            //MaterialExist = !string.IsNullOrEmpty(materialname);
            //if(!string.IsNullOrEmpty(MaterialName))
            //{
            //    CarrierItem cItem = CarrierStorage.Instance.GetCarrierItem(materialname);

            //    if (cItem != null)
            //    {
            //        cItem.CarrierLocation = SlotLocation;
            //        MaterialType = cItem.CarrierSize;
            //    }
            //}
            #endregion
            if (CarrierStorage.Instance.GetCarrierItem(materialname) is CarrierItem cItem)
            {
                cItem.CarrierLocation = SlotLocation;
                if (bUpdate)
                {
                    cItem.CarryInTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");     //20220526 조숭진 hsms 사양에 맞게 string으로 변경 yyyyMMddHHmmssfff
                }


                MaterialName = cItem.CarrierID;
                MaterialExist = !string.IsNullOrEmpty(cItem.CarrierID);
                MaterialType = cItem.CarrierSize;
                ProductEmpty = cItem.ProductEmpty;      //240719 HoN ProductEmpty 구분 추가

                OnSlotStateChanged();

                if (bUpdate)
                {
                    if (SlotLocation.StartsWith("01") || SlotLocation.StartsWith("02"))
                    {
                        Log.LogManager.WriteConsoleLog(eLogLevel.Info, "DbSetProcedureCarrierInfo Call! {0}", cItem.CarrierID);
                        GlobalData.Current.DBManager.DbSetProcedureCarrierInfo(cItem, false);
                        bool CrossCheck = GlobalData.Current.DBManager.DbGetCarrierIDExistInCarrierTable(cItem.CarrierID);
                        if(CrossCheck == false)
                        {
                            Log.LogManager.WriteConsoleLog(eLogLevel.Info, "DbSetProcedureCarrierInfo Call Fail! {0}", cItem.CarrierID);
                            //231127 SetCarrierData 쉘프에서 간혹 누락 되는 현상 정확한 원인 알기 전까지 리트라이 해본다.
                            GlobalData.Current.DBManager.DbSetProcedureCarrierInfo(cItem, false);
                            bool RetryCheck = GlobalData.Current.DBManager.DbGetCarrierIDExistInCarrierTable(cItem.CarrierID);
                            if(RetryCheck)
                            {
                                Log.LogManager.WriteConsoleLog(eLogLevel.Info, "DbSetProcedureCarrierInfo DB Check Retry OK! {0}", cItem.CarrierID);
                            }
                            else
                            {
                                Log.LogManager.WriteConsoleLog(eLogLevel.Info, "DbSetProcedureCarrierInfo Call Retry Fail!!!!! {0}", cItem.CarrierID);
                            }
                        }
                        else
                        {
                            Log.LogManager.WriteConsoleLog(eLogLevel.Info, "DbSetProcedureCarrierInfo DB Check OK! {0}", cItem.CarrierID);
                        }
                    }
                    else
                    {
                        GlobalData.Current.DBManager.DbSetProcedureCarrierInfo(cItem, false);
                    }
                }
                //GlobalData.Current.DBManager.DbSetCarrierInfo(cItem, false);
            }
        }
        //플레이백에서 사용할 CarrierSet
        public void UpdatePlayBackData(string CarrierID, eCarrierSize cSize)
        {
            MaterialName = CarrierID;
            MaterialExist = !string.IsNullOrEmpty(CarrierID);
            MaterialType = cSize;
            OnSlotStateChanged();
        }
        //241202 HoN PlayBack ProductEmpty 추가
        public void UpdatePlayBackData(string CarrierID, eShelfStatus shelfStatus, eProductEmpty productEmpty)
        {
            MaterialName = CarrierID;
            //일단 EMPTY만 Exist False로 ProductEmpty체크용으로만 사용할거니까..
            MaterialExist = shelfStatus.Equals(eShelfStatus.EMPTY) ? false : true;      
            ProductEmpty = productEmpty;
            OnSlotStateChanged();
        }

        //20220531 조숭진 이미 db에서는 지웠는데 지우려해서 insert가 된다. 그래서 인자값 추가.
        public void DeleteSlotCarrier(bool dbUpdate = true)
        {
            //220523 HHJ SCS 개선     //- ShelfSetterControl 신규 추가
            //현재 캐리어 정보를 가져와서 해당 캐리어의 로케이션 정보를 비워주고 자기 자신의 정보를 초기화 해준다.
            if (CarrierStorage.Instance.GetCarrierItem(MaterialName) is CarrierItem cItem)
            {
                if (cItem.CarrierLocation == this.SlotLocation) //230905 RGJ 캐리어가 자기자신 슬롯위치에 있을때만 캐리어 로케이션 삭제 다른쪽에서 삭제가 되면 현재 위치도 삭제됨.
                {
                    cItem.CarrierLocation = string.Empty;
                }
                if (dbUpdate) //Slot Location 바꾸고 업데이트
                {
                    //GlobalData.Current.DBManager.DbSetProcedureCarrierInfo(cItem, true); //주석처리 완전히 삭제될때만 True 준다.
                    GlobalData.Current.DBManager.DbSetProcedureCarrierInfo(cItem, false);
                }

                MaterialName = string.Empty;
                MaterialExist = false;
                MaterialType = eCarrierSize.Unknown;
                OnSlotStateChanged();
            }
            else
            {
                MaterialName = string.Empty;
                MaterialExist = false;
                MaterialType = eCarrierSize.Unknown;
                OnSlotStateChanged();
            }
        }
        protected virtual void OnSlotStateChanged()
        {
            SlotEventArgs args = new SlotEventArgs(SlotNum, _MaterialName, _BeforeMaterialName, _MaterialExist, _BeforeMaterialExist);
            SlotStateChanged?.Invoke(this, args);
        }

    }


}
