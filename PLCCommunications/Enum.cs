
namespace PLCCommunications
{
    //PLC 통신 타입
    public enum ePlcNetType
    {
        None = 0,
        Ether = 1,
        UTL = 2,
        Simulation = 3,
    }

    //PLC Data Type
    public enum ePlcArea
    {
        Bit = 0,
        Word = 1,
    }

    //PLC Bit State 
    public enum eBitState
    {
        ERROR = -1,
        OFF = 0,
        ON = 1,
    }

    //PLC Message Type
    public enum ePLCMessageType
    {
        None = 0,
        Ascii,
        Binary,
        Bit,
    }

    //진행 로봇 타입
    public enum eRMType
    {
        PMAC,
        ARC,
        PLC_EHER,
        PLC_UTL,
        TPLC,
    }

    //로그 종류
    public enum eLogLevel
    {
        Debug,
        Error,
        Fatal,
        Info,
        Warn
    }
}
