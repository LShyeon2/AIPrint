using BoxPrint.Config;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;


namespace BoxPrint.DataList
{
    /// <summary>
    /// Carrier 데이터를 관리보관 하는 클래스 
    /// 
    /// </summary>
    public class CarrierStorage : SingletonBase<CarrierStorage>
    {
        //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
        //지속적으로 쓰레드로 Carrier 정보 리딩 및 변환작업으로 인해 CPU 부하가 높아짐 이벤트 처리로 변경.
        public delegate void CarrierStoreChanged(List<CarrierItem> changed);
        public event CarrierStoreChanged OnCarrierStoreChanged;

        private object SimulLock = new object();
        private ConcurrentDictionary<string, CarrierItem> CarrierDic;

        private int UNKIDIndex = 0;

        public CarrierStorage()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Creating CarrierStorage......");
            CarrierDic = new ConcurrentDictionary<string, CarrierItem>();
            GetCarrierItemsFromDB();
            LogManager.WriteConsoleLog(eLogLevel.Info, "CarrierStorage has been created!");
        }

        public int GetTotalCarriersCount()
        {
            return CarrierDic.Count;
        }
        //220620 HHJ SCS 개선     //- Search Page 추가
        public List<CarrierItem> GetAlltoList()
        {
            return CarrierDic.Select(x => x.Value).ToList();
        }
        /// <summary>
        /// Carrier 데이터를 DB에서 가져온다.
        /// </summary>
        /// <returns></returns>
        public bool GetCarrierItemsFromDB()
        {
            //CarrierDic = GlobalData.Current.DBManager.GetDBCarrierInfo();       //20220526 조숭진
            
            CarrierDic = GlobalData.Current.DBManager.DbGetProcedureCarrierInfo();
       

            //var a = GlobalData.Current.DBManager.DbGetProcedureCarrierInfo();
            //foreach(var item in GlobalData.Current.DBManager.DbGetProcedureCarrierInfo())
            //{
            //    if (CarrierDic.ContainsKey(item.Key))
            //        CarrierDic[item.Key] = item.Value;
            //    else
            //        CarrierDic.TryAdd(item.Key, item.Value);


            //}




            return true;
        }

        public bool CarrierContain(string CarrierID)
        {
            return CarrierDic.ContainsKey(CarrierID);
        }

        public CarrierItem GetCarrierItem(string CarrierID)
        {
            if (string.IsNullOrEmpty(CarrierID))
            {
                return null;
            }
            CarrierDic.TryGetValue(CarrierID, out CarrierItem CItem);
            return CItem;
        }

        //20240415 조숭진 캐리어아이템 가져올때 아이디와 슬롯로케이션을 같이보게한다.
        //OY 충돌발생내역
        //--> 1. 포트에서 크레인 겟 완료후 크레인에 있는 캐리어를 포트자재감지센서가 감지하여 동일아이디 생성됨.
        //--> 2. 포트가 CarrierGetOutAction 진입 후 자재감지센서가 off되어 자재데이터를 지울 때, 하기 GetCarrierItem 함수를 통해 쉘프에 있는 자재가 검색됨.
        //--> 3. 2번으로 인해 쉘프 자재데이터가 삭제되었고, 크레인 이중입고센서도 제대로 감지되지 않아 충돌 발생함.
        public CarrierItem GetCarrierItem(string CarrierID, string CarrierLoc)
        {
            CarrierItem ReturnCItem = new CarrierItem();
            KeyValuePair<string, CarrierItem> temp = new KeyValuePair<string, CarrierItem>();

            if (string.IsNullOrEmpty(CarrierID) || string.IsNullOrEmpty(CarrierLoc))
            {
                return null;
            }

            try
            {
                temp = CarrierDic.First(c => c.Value.CarrierID == CarrierID && c.Value.CarrierLocation == CarrierLoc);
                ReturnCItem = temp.Value;
            }
            catch       //first로 찾았을때 없으면 null로 반환.
            {
                ReturnCItem = null;
            }

            return ReturnCItem;
        }

        public CarrierItem GetInModuleCarrierItem(string ModuleID)
        {
            if (string.IsNullOrEmpty(ModuleID))
            {
                return null;
            }
            KeyValuePair<string, CarrierItem> temp = CarrierDic.FirstOrDefault(c => c.Value.CarrierLocation == ModuleID);
            return temp.Value;
        }

        /// <summary>
        /// 스토리지에 캐리어 추가
        /// 포트에서 신규 추가 되거나 오퍼레이터가 메뉴얼 추가할때 콜
        /// </summary>
        /// <param name="CItem"></param>
        /// <returns></returns>
        public bool InsertCarrier(CarrierItem CItem)
        {
            if (CItem != null)
            {
                bool addSuccess = CarrierDic.TryAdd(CItem.CarrierID, CItem);
                //DB 에 삽입 코드 나중에 추가.
                //slot.cs SetCarrierData 에서 db 삽입함.

                //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
                //지속적으로 쓰레드로 Carrier 정보 리딩 및 변환작업으로 인해 CPU 부하가 높아짐 이벤트 처리로 변경.
                if (addSuccess && OnCarrierStoreChanged != null)
                    OnCarrierStoreChanged?.Invoke(GetAlltoList());

                return addSuccess;
            }
            return false;
        }

        public void RefreshStorageUI()
        {
            OnCarrierStoreChanged?.Invoke(GetAlltoList());
        }

        /// <summary>
        /// 캐리어의 현재 위치를 강제로 업데이트 한다.
        /// </summary>
        /// <param name="CarrierID"></param>
        /// <param name="NewLocation"></param>
        public eCarrierLocationChangeResult ChangeCarrierLocation(string CarrierID, string NewLocation)
        {
            try
            {
                var carrier = GetCarrierItem(CarrierID);
                if(carrier == null)
                {
                    //해당 화물 없음.
                    return eCarrierLocationChangeResult.CARRIER_NOT_EXIST;
                }

                if (GetInModuleCarrierItem(NewLocation) != null)
                {
                    //이미 다른 화물이 점유하고 있는 위치로 업데이트 시도함.
                    return eCarrierLocationChangeResult.LOCATION_ALREADY_OCCUPIED;
                }
                var destLoc = GlobalData.Current.GetGlobalCarrierStoreAbleObject(NewLocation);
                if(destLoc != null)
                {
                    var sourceLoc = GlobalData.Current.GetGlobalCarrierStoreAbleObject(carrier.CarrierLocation);
                    if (sourceLoc != null)
                    {
                        if(destLoc.iLocName == sourceLoc.iLocName)
                        {
                            return eCarrierLocationChangeResult.LOCATION_EQUAL;
                        }
                        sourceLoc.ResetCarrierData();
                        if (sourceLoc is ShelfItem SItem)
                        {
                            SItem.NotifyShelfStatusChanged();
                        }
                    }
                    destLoc.UpdateCarrier(CarrierID);
                    return eCarrierLocationChangeResult.SUCCESS;
                }
                else
                {
                    return eCarrierLocationChangeResult.LOCATION_NOT_EXIST;
                }
            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return eCarrierLocationChangeResult.FAILED;
            }

        }
        /// <summary>
        /// 스토리지에 캐리어 제거
        /// 포트에서 배출 되거나 오퍼레이터가 매뉴얼 제거할때 콜
        /// </summary>
        /// <param name="CItem"></param>
        /// <returns></returns>
        public bool RemoveStorageCarrier(string CarrierID)
        {
            if (string.IsNullOrEmpty(CarrierID))
            {
                return false;
            }

            //SuHwan_20220810 : [Client] 추가
            var carrierBuffer = GetCarrierItem(CarrierID);
            if (carrierBuffer == null)
            {
                return false;
            }
            carrierBuffer.CarrierState = eCarrierState.DELETE;

            CarrierItem remove = null;
            bool bSuccess = CarrierDic.TryRemove(CarrierID, out remove);
            remove?.UnsubcribeEvent();
            //DB 에서 제거 코드 나중에 추가.
            //slot.cs RemoveCarrierData 에서 db 제거함.

            //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
            //지속적으로 쓰레드로 Carrier 정보 리딩 및 변환작업으로 인해 CPU 부하가 높아짐 이벤트 처리로 변경.
            if (bSuccess && OnCarrierStoreChanged != null)
                OnCarrierStoreChanged?.Invoke(GetAlltoList());

            GlobalData.Current.DBManager.DbSetProcedureCarrierInfo(carrierBuffer, true);
            return bSuccess;
        }
        public CarrierItem CreateInPortCarrier(CV_BaseModule CV, bool ReadFail = false)
        {
            if (CV == null)
            {
                return null;
            }
            if(ReadFail) //BCR Fail 인경우
            {
                CarrierItem cItem = CV.ReadTrackingData();
                if (ReadFail)
                {
                    cItem.CarrierID = GetNewPortUnknownCarrierID();//UNK-XXXX 로 임시 저장.
                    cItem.LastReadResult = eIDReadStatus.FAILURE;
                }
                cItem.CarrierLocation = CV.ModuleName;
                return cItem;
            }
            else
            {
                //230812 RGJ Port 캐리어 데이터 스토리지에 있으면 캐리어 추가 생성은 안함.
                CarrierItem AlreadyExistedCarrier = GetCarrierItem(CV.PC_CarrierID);// 이미 캐리어 스토리지에 존재함.        
                if (AlreadyExistedCarrier != null)
                {
                    CV.UpdateTrackingData(AlreadyExistedCarrier); //데이터를 PLC 에 들어있는 값으로 업데이트.
                    AlreadyExistedCarrier.CarrierLocation = CV.ModuleName;
                    return AlreadyExistedCarrier;
                }
                else //스토커 도메인에 없는 캐리어 투입됨. 이미 포트에 올라와 있으므로 도메인에 넣어야 한다.
                {
                    CarrierItem cItem = CV.ReadTrackingData();
                    cItem.CarrierLocation = CV.ModuleName;
                    if(string.IsNullOrEmpty(cItem.CarrierID)) //화물아이디 없으면 넣지는 않고 로그만 남김
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "{0} CreateInPortCarrier CarrierID Empty ", CV.ModuleName);
                    }
                    else
                    {
                        InsertCarrier(cItem); //240330 RGJ CreateInPortCarrier 스토리지 없는경우 Insert
                    }

                    return cItem;
                }
            }

        }
        /// <summary>
        /// 포트에서 캐리어 데이터 없이 넘어왔을경우 Unknown Carrier 생성
        /// </summary>
        /// <param name="CV"></param>
        /// <returns></returns>
        public CarrierItem CreatePortUnknownCarrier(CV_BaseModule CV)
        {
            if (CV == null)
            {
                return null;
            }
            CarrierItem cItem = CV.ReadTrackingData(); //일단 가져올수 있는건 가지고온다.
            cItem.CarrierLocation = CV.ModuleName;
            cItem.CarrierType = "Plate";
            cItem.LotID = "";
            cItem.PalletSize = CV.PalletSize;
            cItem.CarrierID = GetNewPortUnknownCarrierID();//UNK-XXXX 로 임시 저장.
            cItem.LastReadResult = eIDReadStatus.FAILURE;
            return cItem;
        }

        public CarrierItem CreateSimulCarrier(string ModuleID, eCarrierSize size)
        {
            CarrierItem cItem = new CarrierItem();
            cItem.CarrierID = GetNewSimulCarrierID();
            cItem.CarrierLocation = ModuleID;
            cItem.CarrierSize = size;
            cItem.CarrierType = "Plate";
            cItem.LotID = "SIMUL_LOT";
            cItem.ProductQuantity = 500;
            cItem.Polarity = ePolarity.ANODE;
            cItem.InnerTrayType = eInnerTrayType.NONE;
            cItem.ProductEmpty = eProductEmpty.FULL;
            cItem.UncoatedPart = eUnCoatedPart.NA;
            cItem.WinderDirection = eWinderDirection.NONE;
            //cItem.CarryInTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");     //20220526 조숭진 hsms 사양에 맞게 string으로 변경 yyyyMMddHHmmssfff
            return cItem;
        }
        public CarrierItem CreateInvalidCarrier(string CarrierID)
        {
            CarrierItem cItem = new CarrierItem();
            cItem.CarrierID = CarrierID;
            return cItem;
        }
        /// <summary>
        /// DOUBLE STORAGE 발생시 해당 위치에 임시캐리어 생성
        /// </summary>
        /// <param name="ModuleID"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public CarrierItem CreateDSUnknownCarrier(string ModuleID, string DSCarrierID, eCarrierSize size)
        {
            CarrierItem cItem = new CarrierItem();
            cItem.CarrierID = GetNewDSUnknownCarrierID(DSCarrierID);
            cItem.CarrierState = eCarrierState.COMPLETED;
            cItem.CarrierLocation = ModuleID;
            cItem.CarrierSize = size;
            cItem.CarrierType = "Plate";
            cItem.LotID = "UNK_LOT";
            cItem.ProductQuantity = 500;
            cItem.Polarity = ePolarity.ANODE;
            cItem.InnerTrayType = eInnerTrayType.NONE;
            cItem.ProductEmpty = eProductEmpty.FULL;
            cItem.UncoatedPart = eUnCoatedPart.NA;
            cItem.WinderDirection = eWinderDirection.NONE;
            return cItem;
        }

        /// <summary>
        /// BCR DUPLICATE 발생시 해당 위치에 임시캐리어 생성
        /// </summary>
        /// <param name="ModuleID"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public CarrierItem CreateDUPUnknownCarrier(string ModuleID, string CarrierID, eCarrierSize size)
        {
            CarrierItem cItem = new CarrierItem();
            cItem.CarrierID = GetNewDuplicateUnknownCarrierID(CarrierID);
            cItem.CarrierState = eCarrierState.COMPLETED;
            cItem.CarrierLocation = ModuleID;
            cItem.CarrierSize = size;
            cItem.CarrierType = "Plate";
            cItem.LotID = "UNK_LOT";
            cItem.ProductQuantity = 500;
            cItem.Polarity = ePolarity.ANODE;
            cItem.InnerTrayType = eInnerTrayType.NONE;
            cItem.ProductEmpty = eProductEmpty.FULL;
            cItem.UncoatedPart = eUnCoatedPart.NA;
            cItem.WinderDirection = eWinderDirection.NONE;
            return cItem;
        }

        /// <summary>
        /// 공출고 발생시 해당 위치에 임시캐리어 생성
        /// </summary>
        /// <param name="ModuleID"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public CarrierItem CreateSourceEmptyCarrier(string ModuleID, string EmptyCarrierID, ePalletSize Psize)
        {
            Random r = new Random();
            CarrierItem cItem = new CarrierItem();
            cItem.CarrierID = GetNewSEUnknownCarrierID(EmptyCarrierID);
            cItem.CarrierState = eCarrierState.COMPLETED;
            cItem.CarrierLocation = ModuleID;
            cItem.PalletSize = Psize;
            cItem.CarrierType = "Plate";
            cItem.LotID = "UNK_LOT";
            cItem.ProductQuantity = 500;
            cItem.Polarity = ePolarity.ANODE;
            cItem.InnerTrayType = eInnerTrayType.NONE;
            cItem.ProductEmpty = eProductEmpty.FULL;
            cItem.UncoatedPart = eUnCoatedPart.NA;
            cItem.WinderDirection = eWinderDirection.NONE;
            return cItem;
        }

        public CarrierItem CreateSimulCarrier(string ModuleID, ePortSize size)
        {
            switch (size)
            {
                case ePortSize.Short:
                    return CreateSimulCarrier(ModuleID, eCarrierSize.Short);
                case ePortSize.Long:
                    return CreateSimulCarrier(ModuleID, eCarrierSize.Long);
                case ePortSize.Both:
                    if (new Random().Next(0, 2) == 1)
                    {
                        return CreateSimulCarrier(ModuleID, eCarrierSize.Short);
                    }
                    else
                    {
                        return CreateSimulCarrier(ModuleID, eCarrierSize.Long);
                    }
                default:
                    return null;
            }
        }
        private string GetNewSimulCarrierID()
        {
            lock (SimulLock)
            {
                Random r = new Random();
                string carrierID;
                do
                {
                    carrierID = string.Format("SKB{0:D6}", r.Next(1, 100000));
                }
                while (CarrierDic.ContainsKey(carrierID)); //중복이면 다시 뽑는다.

                return carrierID;
            }

        }
        public string GetNewSimulCarrierID2()//LKJ public으로 추가
        {
            lock (SimulLock)
            {
                Random r = new Random();
                string carrierID;
                do
                {
                    carrierID = string.Format("SKB{0:D6}", r.Next(1, 100000));
                }
                while (CarrierDic.ContainsKey(carrierID)); //중복이면 다시 뽑는다.

                return carrierID;
            }

        }
        private string GetNewDSUnknownCarrierID(string oldid)
        {
            lock (SimulLock)
            {
                string carrierID = string.Empty;

                {
                    if (!string.IsNullOrEmpty(oldid) && oldid.Contains("UNK"))
                    {
                        oldid = JustGetID(oldid);
                    }

                    string indexno = UNKIDIndex.ToString();
                    UNKIDIndex++;
                    if (UNKIDIndex > 9)
                        UNKIDIndex = 0;

                    carrierID = string.Format("UNKDBS-{0}-{1}{2}", oldid, DateTime.Now.ToString("yyMMddHHmmss"), indexno);
                }
                if(carrierID.Length > 40)
                {
                    carrierID = carrierID.Substring(0, 40); //240619 RGJ UNK 생성시 CarrierID 40자 유지 추가.
                }
                return carrierID;
            }
        }

        private string GetNewSEUnknownCarrierID(string oldid)
        {
            lock (SimulLock)
            {
                string carrierID = string.Empty;

                
                    if (!string.IsNullOrEmpty(oldid) && oldid.Contains("UNK")) //oldid null 체크 추가.
                    {
                        oldid = JustGetID(oldid);
                    }

                    string indexno = UNKIDIndex.ToString();
                    UNKIDIndex++;
                    if (UNKIDIndex > 9)
                        UNKIDIndex = 0;

                    carrierID = string.Format("UNKEMP-{0}-{1}{2}", oldid, DateTime.Now.ToString("yyMMddHHmmss"), indexno);
                //}

                if (carrierID.Length > 40)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Created UNKID but size over 40.Extras will be removed. UNKID:{0}", carrierID);
                    carrierID = carrierID.Substring(0, 40); //240619 RGJ UNK 생성시 CarrierID 40자 유지 추가.
                }
                return carrierID;
            }

        }
        public string GetNewPortUnknownCarrierID()
        {
            lock (SimulLock)
            {
                string carrierID = string.Empty;

                    string indexno = UNKIDIndex.ToString();
                    UNKIDIndex++;
                    if (UNKIDIndex > 9)
                        UNKIDIndex = 0;

                    carrierID = string.Format("UNK-{0}{1}", DateTime.Now.ToString("yyMMddHHmmss"), indexno);
                //}
                return carrierID;
            }

        }

        public string GetNewDuplicateUnknownCarrierID(string oldid)
        {
            lock (SimulLock)
            {
                string carrierID = string.Empty;

                    if (oldid.Contains("UNK"))
                    {
                        oldid = JustGetID(oldid);
                    }

                    string indexno = UNKIDIndex.ToString();
                    UNKIDIndex++;
                    if (UNKIDIndex > 9)
                        UNKIDIndex = 0;

                    carrierID = string.Format("UNKDUP-{0}-{1}{2}", oldid, DateTime.Now.ToString("yyMMddHHmmss"), indexno);
                //}
                if (carrierID.Length > 40)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Created UNKID but size over 40.Extras will be removed. UNKID:{0}", carrierID);
                    carrierID = carrierID.Substring(0, 40); //240619 RGJ UNK 생성시 CarrierID 40자 유지 추가.
                }
                return carrierID;
            }
        }

        private string JustGetID(string unkid)
        {
            lock (SimulLock)
            {
                try
                {
                    int firstindex = 0;
                    int secondindex = 0;

                    firstindex = unkid.IndexOf('-');
                    secondindex = unkid.LastIndexOf('-');

                    string tempid = unkid.Substring(firstindex + 1, secondindex - firstindex - 1);
                    return tempid;
                }
                catch //예외 처리 추가.
                {
                    return "UNKNOWN";
                }

            }
        }
    }
}
