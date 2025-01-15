using BoxPrint.DataList;

namespace BoxPrint.GUI.ViewModels
{
    public class CarrierInstallViewModel : ViewModelBase
    {
        #region Bind Property
        /// <summary>
        /// 우선 CarrierItem에 있는 데이터만 업데이트 진행함. 그 외는 전부 string형으로 우선 선언 해놓음.
        /// </summary>
        private string _UnitName = string.Empty;
        public string UnitName
        {
            get => _UnitName;
            set
            {
                if (!_UnitName.Equals(value))
                {
                    _UnitName = value;
                    RaisePropertyChanged("UnitName");
                }
            }
        }

        private string _CarrierID = string.Empty;
        public string CarrierID
        {
            get => _CarrierID;
            set
            {
                if (!_CarrierID.Equals(value))
                {
                    _CarrierID = value;
                    RaisePropertyChanged("CarrierID");
                }
            }
        }

        private eProductEmpty _ProductEmpty;
        public eProductEmpty ProductEmpty
        {
            get => _ProductEmpty;
            set
            {
                if (!_ProductEmpty.Equals(value))
                {
                    _ProductEmpty = value;
                    RaisePropertyChanged("ProductEmpty");
                }
            }
        }

        private ePolarity _Polarity;
        public ePolarity Polarity
        {
            get => _Polarity;
            set
            {
                if (!_Polarity.Equals(value))
                {
                    _Polarity = value;
                    RaisePropertyChanged("Polarity");
                }
            }
        }

        private eWinderDirection _WinderDirection;
        public eWinderDirection WinderDirection
        {
            get => _WinderDirection;
            set
            {
                if (!_WinderDirection.Equals(value))
                {
                    _WinderDirection = value;
                    RaisePropertyChanged("WinderDirection");
                }
            }
        }

        private int _ProductQuantity;
        public int ProductQuantity
        {
            get => _ProductQuantity;
            set
            {
                if (!_ProductQuantity.Equals(value))
                {
                    _ProductQuantity = value;
                    RaisePropertyChanged("ProductQuantity");
                }
            }
        }

        private eInnerTrayType _InnerTrayType;
        public eInnerTrayType InnerTrayType
        {
            get => _InnerTrayType;
            set
            {
                if (!_InnerTrayType.Equals(value))
                {
                    _InnerTrayType = value;
                    RaisePropertyChanged("InnerTrayType");
                }
            }
        }
        private ePalletSize _PalletSize;
        public ePalletSize PalletSize
        {
            get => _PalletSize;
            set
            {
                if (!_TrayType.Equals(value))
                {
                    _PalletSize = value;
                    RaisePropertyChanged("_PalletSize");
                }
            }
        }



        private eTrayType _TrayType;
        public eTrayType TrayType
        {
            get => _TrayType;
            set
            {
                if (!_TrayType.Equals(value))
                {
                    _TrayType = value;
                    RaisePropertyChanged("TrayType");
                }
            }
        }

        //캐리어 아이템에 없는내용들.
        private int _TraySlotQuantity;
        public int TraySlotQuantity
        {
            get => _TraySlotQuantity;
            set
            {
                if (!_TraySlotQuantity.Equals(value))
                {
                    _TraySlotQuantity = value;
                    RaisePropertyChanged("TraySlotQuantity");
                }
            }
        }

        private eUnCoatedPart _UncoordinatedDirection = eUnCoatedPart.NA;
        public eUnCoatedPart UncoordinatedDirection
        {
            get => _UncoordinatedDirection;
            set
            {
                if (!_UncoordinatedDirection.Equals(value))
                {
                    _UncoordinatedDirection = value;
                    RaisePropertyChanged("UncoordinatedDirection");
                }
            }
        }

        private string _CoreType = string.Empty;
        public string CoreType
        {
            get => _CoreType;
            set
            {
                if (!_CoreType.Equals(value))
                {
                    _CoreType = value;
                    RaisePropertyChanged("CoreType");
                }
            }
        }

        private string _ValidationNG = string.Empty;
        public string ValidationNG
        {
            get => _ValidationNG;
            set
            {
                if (!_ValidationNG.Equals(value))
                {
                    _ValidationNG = value;
                    RaisePropertyChanged("ValidationNG");
                }
            }
        }

        private eProductEnd _ProductEnd = eProductEnd.NONE;
        public eProductEnd ProductEnd
        {
            get => _ProductEnd;
            set
            {
                if (!_ProductEnd.Equals(value))
                {
                    _ProductEnd = value;
                    RaisePropertyChanged("ProductEnd");
                }
            }
        }
        #endregion

        public CarrierInstallViewModel(string unitName)
        {
            UnitName = unitName;
        }

        public CarrierItem GetCarrier()
        {
            return new CarrierItem()
            {
                CarrierID = CarrierID.Trim(), //사용자 캐리어 아이디 입력값의 앞뒤 공백을 제거한다.
                TrayType = TrayType,
                TrayStackCount = TraySlotQuantity.ToString(),
                Polarity = Polarity,
                ProductEmpty = ProductEmpty,
                WinderDirection = WinderDirection,
                ProductQuantity = ProductQuantity,
                FinalLoc = string.Empty,
                InnerTrayType = InnerTrayType,
                PalletSize = PalletSize,
                CarrierSize = CarrierItem.ConvertCarrierSize(PalletSize),
                UncoatedPart = UncoordinatedDirection,
                CoreType = eCoreType.NONE, //전극 타입 모듈에서는 None 기본값 사용 요청.
                ProductEnd = ProductEnd,
                ValidationNG = ValidationNG,
            };
        }
    }
}
