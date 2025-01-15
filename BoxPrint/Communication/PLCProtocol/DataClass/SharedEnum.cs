namespace PLCProtocol.DataClass
{
    public enum ePLCSeries
    {
        MxCom,      //MxComponent
        Q,          //Q Series
        R,          //R Series
    }
    public enum eCommunicationType
    {
        None,
        Ascii,
        Binary,
    }
    public enum ePLCFrame
    {
        None,
        Frame_4E,
        Frame_3E,
    }
    public enum eAreaType
    {
        None,
        PCtoPLC,
        PLCtoPC,
    }
    public enum eDataType
    {
        Bool,
        Short,
        Int32,
        String,
        Raw,
    }
    //사용하는 영역만 선언하여 놓고 나머지는 전부 주석처리한다.
    public enum eDevice
    {
        B,
        W,
        D,
        //X,
        //Y,
        //M,
        //L,
        //F,
        //V,
        //SB,
        //SW,
        //S,
        //DX,
        //DY,
        //Z,
        //R,
        //ZR
    }

    public enum ePLCStateDataState
    {
        Unknown,
        Connect,
        DisConnect,
    }
}
