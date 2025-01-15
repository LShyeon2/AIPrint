using System;
using System.ComponentModel;

namespace BoxPrint.DataList
{
    /// <summary>
    /// STK 가 이적재 하는 캐리어 데이터 보관
    /// </summary>
    public class CarrierItem : INotifyPropertyChanged, IDisposable
    {
        #region  Property Change Event 
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        #endregion

        private string _CarrierID;
        public string CarrierID
        {
            get => _CarrierID;
            set
            {
                if (_CarrierID != value)
                {
                    _CarrierID = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("CarrierID"));
                }
            }
        }

        private eProductEmpty _ProductEmpty;
        public eProductEmpty ProductEmpty
        {
            get => _ProductEmpty;
            set
            {
                if (_ProductEmpty != value)
                {
                    _ProductEmpty = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ProductEmpty"));
                }

            }
        }

        private ePolarity _Polarity;
        public ePolarity Polarity
        {
            get => _Polarity;
            set
            {
                if (_Polarity != value)
                {
                    _Polarity = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Polarity"));
                }
            }
        }

        private eWinderDirection _WinderDirection;
        public eWinderDirection WinderDirection
        {
            get => _WinderDirection;
            set
            {
                if (_WinderDirection != value)
                {
                    _WinderDirection = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("WinderDirection"));
                }
            }
        }

        private int _ProductQuantity;
        public int ProductQuantity
        {
            get => _ProductQuantity;
            set
            {
                if (_ProductQuantity != value)
                {
                    _ProductQuantity = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ProductQuantity"));
                }
            }
        }

        private string _FinalLoc;
        public string FinalLoc
        {
            get => _FinalLoc;
            set
            {
                if (_FinalLoc != value)
                {
                    _FinalLoc = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("FinalLoc"));
                }
            }
        }
        public short SV_FinalLoc //Short Value FinalLoc
        {
            get
            {
                bool bParcingSuccess = short.TryParse(FinalLoc, out short sValue);
                if (bParcingSuccess)
                {
                    return sValue;
                }
                else
                {
                    return 0;
                }
            }
        }

        private eInnerTrayType _InnerTrayType;
        public eInnerTrayType InnerTrayType
        {
            get => _InnerTrayType;
            set
            {
                if (_InnerTrayType != value)
                {
                    _InnerTrayType = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("InnerTrayType"));
                }
            }
        }

        private string _Destination;
        public string Destination
        {
            get => _Destination;
            set
            {
                if (_Destination != value)
                {
                    _Destination = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Destination"));
                }
            }
        }
        public short DestCpu
        {
            get
            {
                bool bParcingSuccess = int.TryParse(Destination, out int destValue);
                if (bParcingSuccess)
                {
                    return (short)(destValue / 1000);
                }
                else
                {
                    return 0;
                }

            }
        }
        public short DestTrack
        {
            get
            {
                bool bParcingSuccess = int.TryParse(Destination, out int destValue);
                if (bParcingSuccess)
                {
                    return (short)(destValue % 1000);
                }
                else
                {
                    return 0;
                }
            }
        }

        private ePalletSize _PalletSize;
        public ePalletSize PalletSize
        {
            get => _PalletSize;
            set
            {
                if (_PalletSize != value)
                {
                    _PalletSize = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("PalletSize"));
                    OnPropertyChanged(new PropertyChangedEventArgs("CarrierSize"));
                }
            }
        }
        /// <summary>
        /// 230418 RGJ HAMS 1.5 버전 사양 번경으로 추가. PalletSize 아스키 64 로 변경해서 보고
        /// </summary>
        public string HSMSPalletSize
        {
            get
            {
                string Size;
                switch(PalletSize)
                {
                    case ePalletSize.NONE:
                    case ePalletSize.Raw_Material://원자재는 보고 빈값으로 올림 230830 정연동매니저 문의사항 대답.
                        Size = "";
                        break;
                    case ePalletSize.Cell_Short:
                    case ePalletSize.ModuleProduct_Short:
                    case ePalletSize.CellProduct_Short:
                    case ePalletSize.CathodeReel:
                    case ePalletSize.AnodeReel:
                        Size = "SHORT";
                        break;
                    case ePalletSize.Cell_Long:
                    case ePalletSize.CellProduct_Long:
                    case ePalletSize.ModuleProduct_Long:
                        Size = "LONG";
                        break;
                    default:
                        Size = "SHORT";
                        break;
                }
                return Size;
            }
        }

        private string _TrayStackCount;
        public string TrayStackCount
        {
            get => _TrayStackCount;
            set
            {
                if (_TrayStackCount != value)
                {
                    _TrayStackCount = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("TrayStackCount"));
                }
            }
        }
        public short SV_TrayStackCount //Short Value TrayStackCount
        {
            get
            {
                bool bParcingSuccess = short.TryParse(TrayStackCount, out short sValue);
                if (bParcingSuccess)
                {
                    return sValue;
                }
                else
                {
                    return 0;
                }
            }
        }

        private eTrayType _TrayType;
        public eTrayType TrayType
        {
            get => _TrayType;
            set
            {
                if (_TrayType != value)
                {
                    _TrayType = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("TrayType"));
                }
            }
        }

        private eUnCoatedPart _UncoatedPart;
        public eUnCoatedPart UncoatedPart
        {
            get => _UncoatedPart;
            set
            {
                if (_UncoatedPart != value)
                {
                    _UncoatedPart = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("UncoatedPart"));
                }
            }
        }

        private eCoreType _CoreType;
        public eCoreType CoreType//220523 조숭진 추후 엑셀 사양서 배포되면 enum으로 정의 예정.
        {
            get => _CoreType;
            set
            {
                if (_CoreType != value)
                {
                    _CoreType = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("CoreType"));
                }
            }
        }

        private string _ValidationNG;
        public string ValidationNG
        {
            get => _ValidationNG;
            set
            {
                if (_ValidationNG != value)
                {
                    _ValidationNG = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ValidationNG"));
                }
            }
        }
        public short SV_ValidationNG //Short Value ValidationNG
        {
            get
            {
                bool bParcingSuccess = short.TryParse(ValidationNG, out short sValue);
                if (bParcingSuccess)
                {
                    return sValue;
                }
                else
                {
                    return 0;
                }
            }
        }

        private eProductEnd _ProductEnd;
        public eProductEnd ProductEnd
        {
            get => _ProductEnd;
            set
            {
                if (_ProductEnd != value)
                {
                    _ProductEnd = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ProductEnd"));
                }
            }
        }

        private string _CarrierType;
        public string CarrierType
        {
            get => _CarrierType;
            set
            {
                if (_CarrierType != value)
                {
                    _CarrierType = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("CarrierType"));
                }
            }
        }

        //private eCarrierSize _CarrierSize;
        public eCarrierSize CarrierSize
        {
            get
            {
                return ConvertCarrierSize(PalletSize);
            }
            set
            {
                //일단 의미 없는 Set 유지

                //if (_CarrierSize != value)
                //{
                //    _CarrierSize = value;
                //    OnPropertyChanged(new PropertyChangedEventArgs("CarrierSize"));
                //}

            }
        }

        private eCarrierState _CarrierState = eCarrierState.NONE;
        public eCarrierState CarrierState
        {
            get
            {
                return _CarrierState;
            }
            set
            {
                //220823 조숭진 캐리어 상태 조건 추가
                if (_CarrierState != value && _CarrierState != eCarrierState.NONE && value != eCarrierState.DELETE) //디비에서 가져 올때 재보고를 막기 위해
                {
                    _CarrierState = value;
                    GlobalData.Current.DBManager.DbSetProcedureCarrierInfo(this, false); //캐리어 상태 변경할때 마다 디비 갱신
                }

                if (value == eCarrierState.WAIT_IN && _bFirstTime == true)
                {
                    _CarrierState = value;
                    GlobalData.Current.DBManager.DbSetProcedureCarrierInfo(this, false); //캐리어 상태 변경할때 마다 디비 갱신
                }
                _CarrierState = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CarrierState"));
            }
        }

        //20220622 조숭진 carrierstate waitin때 db에 들어가지 않아 추가.
        private bool _bFirstTime = false;
        public bool bFirstTime
        {
            get
            {
                return _bFirstTime;
            }
            set
            {
                if (_bFirstTime != value)
                {
                    _bFirstTime = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("CarrierState"));
                }
            }
        }

        private string _LotID;
        public string LotID
        {
            get => _LotID;
            set
            {
                if (_LotID != value)
                {
                    _LotID = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("LotID"));
                }
            }
        }

        private string _First_Lot;
        public string First_Lot
        {
            get => _First_Lot;
            set
            {
                if (_First_Lot != value)
                {
                    _First_Lot = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("First_Lot"));
                }
            }
        }

        private string _Second_Lot;
        public string Second_Lot
        {
            get => _Second_Lot;
            set
            {
                if (_Second_Lot != value)
                {
                    _Second_Lot = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Second_Lot"));
                }
            }
        }

        private string _Third_Lot;
        public string Third_Lot
        {
            get => _Third_Lot;
            set
            {
                if (_Third_Lot != value)
                {
                    _Third_Lot = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Third_Lot"));
                }
            }
        }

        private string _Fourth_Lot;
        public string Fourth_Lot
        {
            get => _Fourth_Lot;
            set
            {
                if (_Fourth_Lot != value)
                {
                    _Fourth_Lot = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Fourth_Lot"));
                }
            }
        }

        private string _Fifth_Lot;
        public string Fifth_Lot
        {
            get => _Fifth_Lot;
            set
            {
                if (_Fifth_Lot != value)
                {
                    _Fifth_Lot = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Fifth_Lot"));
                }
            }
        }

        private string _Sixth_Lot;
        public string Sixth_Lot
        {
            get => _Sixth_Lot;
            set
            {
                if (_Sixth_Lot != value)
                {
                    _Sixth_Lot = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Sixth_Lot"));
                }
            }
        }

        /// <summary>
        /// 20230206 RGJ 캐리어 높이 사양 추가.
        /// </summary>
        private eCarrierHeight _CarrierHeight;
        public eCarrierHeight CarrierHeight
        {
            get => _CarrierHeight;
            set
            {
                if (_CarrierHeight != value)
                {
                    _CarrierHeight = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("CarrierHeight"));
                }
            }
        }


        //20220526 조숭진 hsms 사양에 맞게 string으로 변경 yyyyMMddHHmmssfff
        //public DateTime CarryInTime;
        //public DateTime CarryOutTime;
        public string CarryInTime;
        public string CarryOutTime;
        private bool disposedValue;

        private eIDReadStatus _LastReadResult;
        public eIDReadStatus LastReadResult
        {
            get => _LastReadResult;
            set
            {
                if (_LastReadResult != value)
                {
                    _LastReadResult = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("LastReadResult"));
                }
            }
        }
        private string _CarrierLocation = string.Empty;
        public string CarrierLocation
        {
            get
            {
                return _CarrierLocation;
            }
            set
            {
                _CarrierLocation = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CarrierLocation"));
            }
        }
        public string CarrierZoneName
        {
            get
            {
                var CarrierLoc = GlobalData.Current.GetGlobalCarrierStoreAbleObject(CarrierLocation);
                if (CarrierLoc != null)
                {
                    return CarrierLoc.iZoneName;
                }
                return string.Empty;
            }
        }


        public CarrierItem()
        {
            CarrierID = string.Empty;
            CarrierLocation = string.Empty;
            FinalLoc = string.Empty;
            TrayStackCount = string.Empty;
            ValidationNG = string.Empty;
            CarrierType = string.Empty;
            LotID = string.Empty;
            First_Lot = string.Empty;
            Second_Lot = string.Empty;
            Third_Lot = string.Empty;
            Fourth_Lot = string.Empty;
            Fifth_Lot = string.Empty;
            Sixth_Lot = string.Empty;
        }
        public bool IsUnknownCarrier()
        {
            if (string.IsNullOrEmpty(CarrierID) || CarrierID.StartsWith("UNK"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool UpdateTagID(string TagData)
        {
            return true;
        }
        public void UnsubcribeEvent()
        {
            if (PropertyChanged != null)
            {//SuHwan_20220802 : 
                foreach (var d in PropertyChanged.GetInvocationList())
                    PropertyChanged -= d as PropertyChangedEventHandler;
            }
        }
        public static eCarrierSize ConvertCarrierSize(ePalletSize pSize)
        {
            eCarrierSize cSize = eCarrierSize.Unknown;
            switch (pSize)
            {
                case ePalletSize.Cell_Long: //장폭
                case ePalletSize.CellProduct_Long:
                case ePalletSize.ModuleProduct_Long:
                    cSize = eCarrierSize.Long;
                    break;
                case ePalletSize.Cell_Short: //단폭
                case ePalletSize.CellProduct_Short:
                case ePalletSize.ModuleProduct_Short:
                case ePalletSize.Raw_Material:
                    cSize = eCarrierSize.Short;
                    break;
                case ePalletSize.AnodeReel: //일단 사양이 없으므로 Short 으로 간주
                case ePalletSize.CathodeReel:
                    cSize = eCarrierSize.Short;
                    break;
            }
            return cSize;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
                }

                this.CarrierState = eCarrierState.DELETE;

                UnsubcribeEvent();
                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedValue = true;
            }
        }

        // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        ~CarrierItem()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
