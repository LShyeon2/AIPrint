namespace BoxPrint.Modules
{
    /// <summary>
    /// 캐리어를 수납할수 있는 모듈 인터페이스
    /// </summary>
    public interface ICarrierStoreAble
    {
        int iBank { get; }

        int iBay { get; }

        int iLevel { get; }

        short iWorkPlaceNumber { get; }

        string iGroup { get; }
        string iLocName { get; }
        string iZoneName { get; }

        //230306 조숭진 캐리어 사이즈 추가
        ePalletSize PalletSize { get; }


        bool UpdateCarrier(string CarrierID, bool DBUpdate = true, bool HostReq = false); //캐리어 위치 업데이트

        bool ResetCarrierData();   //캐리어 데이터 리셋 (스토리지는 유지)

        bool RemoveSCSCarrierData();   //캐리어 스토커 도메인에서 삭제 (스토리지까지 삭제)

        bool TransferCarrierData(ICarrierStoreAble To); //캐리어 데이터 전달 처리

        bool CheckCarrierExist();
        int CalcDistance(ICarrierStoreAble a, ICarrierStoreAble b);
        string GetCarrierID();
        string GetTagName();
        void NotifyScheduled(bool Reserved, bool init = false);     //221012 조숭진 init 인자 추가...

        bool CheckGetAble(string CarrierID);
        bool CheckPutAble();

        bool CheckCarrierSizeAcceptable(eCarrierSize Size);

        int GetUnitServiceState();

    }
}
