using System;       //230911 HHJ enum Value string으로 변경
using System.Linq;

namespace BoxPrint
{
    #region SCS 기준정보
    public enum eSCSType
    {
        Single = 1,
        Dual = 2,
    }
    public enum eLineSite
    {
        None,
        TOP_POC,
    }
    public enum eRMType
    {
        PMAC,
        ARC, //아진제어기
        PLC_EHER, //2021년 5월 18일 화요일 오후 5:23:39 - Editted by 수환 : PLC Robot 추가 
        PLC_UTL,
        TPLC, //Toptec PLC RM
    }
    public enum eCVLineType
    {
        None = 0,
        MaunalIn = 1,
        MaunalOut = 2,
        AutoIn = 3,
        AutoOut = 4,
    }

    #endregion

    #region Log 관련
    public enum eLogLevel
    {
        Debug,
        Error,
        Fatal,
        Info,
        Warn
    }

    public enum eModuleList
    {
        None = 0,
        SYS,
        RM,
        CV,
        BCR,
        Print,

    }
    #endregion

    #region I/O 관련
    public enum eIOGroup
    {
        Booth,
        Robot,
        Port,
        OHT,
        Safety,
    }

    public enum eIODirectionTypeList
    {
        In = 0,
        Out = 1,
        None = 2,
    }

    #endregion

    #region SAFETY 관련
    public enum eSafetyComType
    {
        None,
        TCP_IP,
        PORT,
    }
    #endregion

    #region Booth 관련
    public enum eEqpState
    {
        None = 0,
        Normal,
        Fault,
        PM,
    }

    public enum eProcState
    {
        None = 0,
        Init,
        Idle,
        Execute,
        Pause,
    }
    public enum eTowerLampMode
    {
        OFF = 0,
        ON = 1,
        Blink = 2
    }

    public enum eBuzzerSoundType
    {
        None = 0,
        Sound1 = 1, //아직 부저 사양 확정안되서 임시로 추가
        Sound2 = 2,
        Sound3 = 3,
        Sound4 = 4,
    }
    public enum eBuzzerControlMode
    {
        BuzzerOFF = 0,
        BuzzerON = 1,
        ValidationNG = 9,
    }


    #endregion

    #region Shelf 관련
    //220321 HHJ SCS 개발     //- ShelfData UI 연동
    public enum eShelfType
    {
        Unknown = -1,
        Short = 0,          //단폭
        Long,               //장폭
        Both,               //혼용
    }
    //220509 HHJ SCS 개선     //- ShelfControl 변경
    public enum eShelfBusyRm
    {
        Unknown,
        RM1,
        RM2,
    }
    #endregion

    #region Conveyor Port 관련
    public enum eCVLineCommand
    {
        None,
        Init,
        AutoJobTask,
        ManualTask,
    }
    public enum ePortSize
    {
        Unknown = -1,
        Short = 0,          //단폭
        Long,               //장폭
        Both,               //혼용 //실제 혼용 운용을 하지는 않는다.
    }
    public enum eCVAutoManualState
    {
        None,
        AutoRun = 1,   //오토 온라인 상태
        ManualRun = 2, //메뉴얼 상태

        //Error = 3, //에러
        //Pause = 4,  //일시 정지 상태
    }

    public enum eCVType
    {
        Simulator = -1,
        Plain = 0,
        Turn,
        RobotIF,
        OHTIF,
        EQIF,
        OperatorIF,
        EQRobot,
        TurnEQIF,
        Stacker,
        StackerBox,
        StackBuffer,
        TurnOHTIF,      //2021.05.21 lim,
        TurnBridge,      //2021.06.15 lim,
        OHTRobot,
        GantryIF,
        ShuttleTurn,
        SlavePlain,
        Manual,
        WaterPool, //방화 수조포트
        Print,
    }

    public enum eCVCommandType
    {
        None,
        Normal,
        Recovery,
    }
    public enum eCVCommand
    {
        None,
        Initialize,     //초기화 동작
        AutoAction,     //컨베이어 오토 동작
        WaitCranePut,      //Crane 으로부터 Carrier Put 동작 대기
        WaitCraneGet,     //Crane 으로부터 Carrier Get 동작 대기
        ReceiveCarrier,    //전컨베이어로 부터 캐리어 받기.
        SendCarrier,       //다음컨베이어로  캐리어 보내기.
        WaitCarrierLoad,   //메뉴얼 포트에서 작업자 캐리어 투입 대기
        WaitCarrierRemove, //메뉴얼 포트에서 작업자 캐리어 제거 대기
        ErrorHandling,  //에러 발생후 조치 대기

    }
    /// <summary>
    /// 포트 방향 정의
    /// </summary>
    public enum ePortInOutType
    {
        //아래는 SKOH2
        //0 : N/A 　 　 　 　
        //1 : INPUT
        //2 : OUTPUT
        //3 : BOTH

        //아래는 SKOY
        //0 : N/A 　 　 　 　
        //2 : INPUT
        //4 : OUTPUT
        //6 : BOTH

        Unknown = 0,
        INPUT = 1,    //부스 방향           //In Port
        OUTPUT = 2,   //부스 반대 방향      //Out Port
        BOTH = 3,     //AGV Only            //Both Port
    }

    /// <summary>
    /// Port AccessMode
    /// </summary>
    public enum ePortAceessMode
    {
        Unknown = 0,
        AUTO = 1,   // AGV 투입,오토 투입
        MANUAL = 2, // 작업자 메뉴얼 투입
    }
    public enum eCV_Speed
    {
        None = 0,
        Low,
        Mid,
        High,
    }

    public enum eCV_RunControlType
    {
        None = 0,
        Inverter = 1,
        Servo = 2,
    }
    public enum eCV_ActionResult
    {
        Complete = 0,
        ErrorOccured = 1,
        Aborted = 2,
        TimeOut = 3,
    }
    public enum eJob_Result
    {
        Complete = 0,
        ErrorOccured = 1,
        Aborted = 2,
        Paused = 3,
        TimeOut = 4,
    }
    public enum eCV_TurnState
    {
        Unknown = 0,
        Turn,
        Return,
    }
    public enum eCV_StopperState
    {
        None,//스톱퍼 없음
        Unknown,//스톱퍼 상태 이상
        Up,  //업[클로즈]
        Down,//다운[오픈]
    }
    public enum eCV_StopperPosition
    {
        None,
        BoothSide,
        AwaySide,
        BothSide,
    }

    public enum eCV_DoorState
    {
        Unknown = 0,
        Open,
        Close,
    }
    public enum eCV_ChuckState
    {
        Unknown = 0,
        Chuck,
        Unchuck,
    }

    public enum eCV_StackerHoldState
    {
        Unknown = 0,
        Hold,
        Release,
    }

    public enum eCV_TrayPadState
    {
        Unknown = 0,
        PadUp,
        PadDown,
    }

    public enum eTurnType
    {
        AirCylinder = 0,
        Servo = 1,
        ServoIO = 2,
        ServeIO_Dellta = 3
    }
    public enum eTurnCommand
    {
        Return = 0,         //0 도
        Turn_Side,          //90 도
        Turn,               //180 도
        Turn_ReverseSide    //270 도
    }
    public enum eDirection
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
    }
    #endregion

    #region Print 관련
    public enum eRecipeDataType
    {
        BASE,
        AUTODATA,
        COUNT,
    }
    public enum eRecipeBaseData
    {
        Filename,
        Speed,
        Delay,
        Direct,
    }
    public enum eDataCount
    {
        No1,
        No2,
        No3,
        No4,
        No5,
        No6,
        No7,
        No8,
        No9,
    }

    public enum ePrintScenarioState
    {
        Stop,
        Run,
        Paused,
    }

    #endregion

    #region Unit 관련
    public enum eRFIDComType
    {
        None,
        TCP_IP,
        RS_232,
    }

    public enum eUnitComType
    {
        None,
        TCP_IP,
        RS_232,
    }

    public enum eUnitConnection
    {
        Disconnect,
        Connect,
    }

    public enum ePrintCommand
    {
        EnablePrintComplete,    //Print Complete 회신 기능
        ReadAutoDataState,
        GetAutoDataString,
        WriteAutoDataRedord,    //데이터 전송
        ClearAutoDataQueue,
        ReadInkLevel,
        Build,                  //출력 이미지 변경
        GetPrintDirection,      //방향
        SetPrintDirection,
        GetPrintDelay,          //시작 위치
        SetPrintDelay,
        GetManualSpeed,         //속도
        SetManualSpeed,
        ReadSystemDateTime,     //컨트롤 시간 
        WriteSystemDateTime,
    }

    public enum ePrintStep
    {
        None,
        Initialize,     //초기화 동작
        AutoAction,     //컨베이어 오토 동작
        WaitCranePut,      //Crane 으로부터 Carrier Put 동작 대기
        WaitCraneGet,     //Crane 으로부터 Carrier Get 동작 대기
        ReceiveCarrier,    //전컨베이어로 부터 캐리어 받기.
        SendCarrier,       //다음컨베이어로  캐리어 보내기.
        WaitCarrierLoad,   //메뉴얼 포트에서 작업자 캐리어 투입 대기
        WaitCarrierRemove, //메뉴얼 포트에서 작업자 캐리어 제거 대기
        ErrorHandling,  //에러 발생후 조치 대기

    }

    public enum AutoDataResponseEnum
    {
        XON, //프린트 대기열 공간이 있을때
        XOFF, //프린트 대기열 공간이 없을때
    }

    public enum WriteAutoDataResponseEnum
    {
        Received, //프린트 대기열 공간이 있을때
        XOFF, //프린트 대기열 공간이 없을때
        QueueCleared //다른 데이터도 없고 끝에 틸드(~)도 없을때
    }

    public enum DirectionEnum
    {
        Up,
        Down
    }

    public enum TypeEnum
    {
        Numeric,
        Alpha,
        Alphanumeric,
    }

    public enum BcrResultEnum
    {
        OK = 0,
        Reject = 1,
    }

    #endregion

    #region OHT Interface
    public enum eOHT_PIOResult
    {
        Complete = 0,
        ErrorOccured = 1,
        Aborted = 2,
        TimeOut = 3,
    }
    public enum eOHTPIORecoveryOption
    {
        None,
        PIO_ForceComplete,
        PIO_Restart, //미사용
    }
    public enum eOHTPIOStep
    {
        Unknown,
        LoadStep00_Start,
        LoadStep01_Wait_TRReq,
        LoadStep02_Wait_BusyOn,
        LoadStep03_Wait_OHTComplete,
        LoadStep04_Wait_AllOff,
        LoadErrorRecovery_Start,


        UnloadStep00_Start,
        UnloadStep01_Wait_TRReq,
        UnloadStep02_Wait_BusyOn,
        UnloadStep03_Wait_OHTComplete,
        UnloadStep04_Wait_AllOff,
        UnloadErrorRecovery_Start,

        Step_Done,

    }

    #endregion

    #region AGV Interface
    //2021.05.21 lim, AGV PIO 추가 미사용 삭제 확인필요
    public enum eAGV_PIOResult
    {
        Complete = 0,
        ErrorOccured = 1,
        Aborted = 2,
        TimeOut = 3,
    }
    public enum eAGVPIORecoveryOption
    {
        None,
        PIO_ForceComplete,
        PIO_Restart, //미사용
    }
    public enum eAGVPIOStep
    {
        Unknown,
        LoadStep00_Start,
        LoadStep01_Wait_TRReq,
        LoadStep02_Wait_BusyOn,
        LoadStep03_Wait_AGVComplete,
        LoadStep04_Wait_AllOff,
        LoadErrorRecovery_Start,

        UnloadStep00_Start,
        UnloadStep01_Wait_TRReq,
        UnloadStep02_Wait_BusyOn,
        UnloadStep03_Wait_AGVComplete,
        UnloadStep04_Wait_AllOff,
        UnloadErrorRecovery_Start,

        Step_Done,
    }
    #endregion

    #region 셔틀 관련
    public enum eShuttlePosition
    {
        Unknown,
        Master,
        Slave,
    }
    public enum eShuttleRequestResponse
    {
        NoResponse, //응답없음
        Accepted, //셔틀 동작 승낙
        Refused,  //셔틀 동작 거절
    }
    #endregion

    #region RM Crane 관련
    public enum eRMPmacState
    {
        Initialize_Not_Done = 0,
        Initializing = 1,
        Initialized_Idle = 2,
        Moving = 3,
        Transfering = 4,
        Done = 5,
        Error = 9,
        Auto_Teaching_Running = 11,
        Auto_Teaching_Complete = 12,
        Jog_Running = 21,
        Jog_Complete = 22,
        Unknown = -1,
    }
    /// <summary>
    /// 기상반 동작 모드
    /// </summary>
    public enum eCraneSCMode
    {
        OFFLINE = 0,
        AUTO_RUN = 1,
        MANUAL_RUN = 2,
        ONE_RACK_PAUSE = 3,
    }
    public enum eCraneSCState
    {
        OFFLINE = 0,
        IDLE = 1,              //대기 상태
        BUSY = 2,              //동작상태
        ERROR = 4,             //에러상태
    }
    /// <summary>
    /// 가동률 eCraneSCMode eCraneSCState 를 조합해서 5가지를 저장해야함.
    /// </summary>
    public enum eCraneUtilState
    {
        NONE,
        RUN, //Auto 이고 Job 있을시
        IDLE,  //Auto 이고 Job 없을시
        ERROR, //에러상태
        MANUAL, //메뉴얼 모드 상태
        REMOVE //One Rack 모드 상태.
    }

    /// <summary>
    /// 지상반 동작 모드
    /// </summary>
    public enum eCraneOnlineMode
    {
        OFFLINE = 0,
        LOCAL = 1,
        REMOTE = 2,
    }

    public enum eCraneJobState
    {
        Busy = 0,
        JobComplete_Fork1 = 1,
        JobComplete_Fork2 = 2,
        JobComplete_All = 3,
        JobCancel_Fork1 = 4,
        JobCancel_Fork2 = 5,
        JobCancel_All = 6,
    }

    public enum eRM_ForkErrorState
    {
        READY = 0,
        DOUBLE_STORAGE = 1,
        EMPTY_RETRIEVAL = 2,

    }
    public enum eCraneChuckState
    {
        Unknown = 0,
        Chuck,
        Unchuck,
    }
    public enum eJogAction
    {
        Stop = 0,
        Start = 1,
    }

    public enum eAutoTeachingMode
    {
        //0 : All (Front, Rear 모두 Auto Teaching 수행)
        //1 : Front 만 Auto Teaching 수행
        //2 : Rear 만 Auto Teaching 수행
        //11 : P7102 에 의해 지정된 Shelf 에서만 Auto Teaching 수행
        ALL = 0,
        Front = 1,
        Rear = 2,
        SelectShelf = 11,
        Unknow = -1,
    }
    public enum eShelfBank
    {
        Front = 1,
        Rear = 2,
    }

    public enum eRMTarget
    {
        None = 0,
        Front = 1,
        Rear = 2,
        Port = 3
    }

    public enum ePLCInitInitializeState
    {
        InitializeNotComplete = 0,
        InitializeComplete = 1,
    }

    public enum ePopUpActionName
    {
        PListDelete,
        PListADD,
        PListSave,
    }

    public enum eKeyboard
    {
        TAB,
    }

    public enum eJogDir
    {
        //CW ,
        //CCW,
        //Stop
        CCW = -1,
        CW = 1,
        Stop = 0,
    }
    public enum eAxisNumber
    {
        ForkAxis = 1,
        XAxis = 2,
        ZAxis = 3,
        TurnAxis = 4,
        Grip = 5, // 200302 Hand Grip 추가 변경 
    }

    public enum eJogMoveAxis
    {
        unknow = 0,
        ForkAxis = 1,
        XAxis = 2,
        ZAxis = 3,
        TurnAxis = 4,
    }
    public enum eMachineType
    {
        N2STK = 1,
        CPSLBS = 2
    }
    public enum eJogMoveType
    {
        JogMove = 1,
        IncMove = 2,
        AbsMove = 3,
    }

    public enum eNotifyItems
    {
        No = 1,
        Item1,
        Item2,
        Item3,
        Item4,
        Item5,
        Item6,
        Item7,
        Item8,
        Item9,
        Item10,
        Item11,
        Item12,
        Item13,
        Item14,
        Item15,
        Item16,
    }

    public enum eDbGetItem
    {
        eNone,
        eRMParameter,
    }
    //220506 HHJ SCS 개선     //- Crane Control 변경
    public enum eCraneArmState
    {
        Center = 0,
        ExtendFront = 1,
        ExtendRear = 2
    }
    public enum CrancActionStep
    {
        None,

        Get_Init, //Get 동작전 Crane 및 타겟 체크
        Get_PortPIO, //Get Port PIO  
        Get_SendCommand, //Crane 의 명령 전달
        Get_WaitActionComplete, //Crane 의 동작 완료 대기
        Get_Complete, //동작완료 후처리
        Get_ErrorHandling,//Get 동작중 에러 발생시 처리

        Put_Init, //Put 동작전 Crane 및 타겟 체크
        Put_PortPIO, //Put Port PIO  
        Put_SendCommand, //Crane 의 명령 전달
        Put_WaitActionComplete, //Crane 의 동작 완료 대기
        Put_Complete, //동작완료 후처리
        Put_ErrorHandling,//Put 동작중 에러 발생시 처리

        Move_Init, //Move 동작전 Crane 및 타겟 체크
        Move_SendCommand, //Crane 의 명령 전달
        Move_WaitActionComplete, //Crane 의 동작 완료 대기
        Move_Complete, //동작완료 후처리
        Move_ErrorHandling,//Move 동작중 에러 발생시 처리
    }
    #endregion

    #region Carrier 관련
    public enum eCarrierSize
    {
        Unknown = 0,
        Long = 1,            //장폭
        Short = 2,           //단폭
    }

    public enum eNotchingMode
    {
        DEFAULT = 1,
        REELCORE = 2,
        EMPTYTRAY = 3,
        REELTRAY = 4,
        UNCOATEDPART = 5,
    }
    public enum eUnCoatedPart
    {
        NA = 0,
        FRONT = 1,
        REAR = 2,

    }
    public enum eTrayType
    {
        NONE = 0,
        EMPTYEMPTY = 1,
        EMPTY = 2,
        FULL = 3,
    }

    //극성
    public enum ePolarity
    {
        NONE = 0,
        ANODE = 1, //양극
        CATHODE = 2, //음극
    }
    public enum eWinderDirection
    {
        NONE = 0,
        UP = 1,
        DOWN = 2,
    }
    public enum eInnerTrayType
    {
        NONE = 0,
        EMPTYEMPTY = 1,
        EMPTY = 2,
        FULL = 3
    }
    public enum eProductEmpty
    {
        NONE = 0,
        FULL = 1,
        EMPTY = 2
    }
    public enum ePalletSize
    {
        //아래는 SKOH2
        //0 : N/A 　 　 　 　
        //1 :셀버퍼 (장) 1530 *1130* 800 500KG
        //2 :셀버퍼 (단) 1100 *1200 *800 500KG
        //3 :셀완제품(장) 1530* 1130* 1600 1000KG
        //4 :셀완제품(단) 1200* 1100* 1600 1000KG
        //5 :원자재  1100* 1100* 1600 600KG
        //6 :완제품(단) 1226* 1150* 2024 2000KG
        //7 :완제품(장) 1530* 1130 *830 1000KG
        //8 : 음극 릴대차 (릴동간 PLT) (현재미사용 //221114)
        //9 : 양극 릴대차 (릴동간 PLT) (현재미사용 //221114)

        //아래는 SKOY
        //301 :셀버퍼 (장) 1530 *1130* 800 500KG
        //302 :셀버퍼 (단) 1100 *1200 *800 500KG
        //303 :셀완제품(장) 1530* 1130* 1600 1000KG
        //304 :셀완제품(단) 1200* 1100* 1600 1000KG
        //305 :원자재  1100* 1100* 1600 600KG
        //306 :완제품(단) 1226* 1150* 2024 2000KG
        //307 :완제품(장) 1530* 1130 *830 1000KG
        //308 : 음극 릴대차 (릴동간 PLT) (현재미사용 //221114)
        //309 : 양극 릴대차 (릴동간 PLT) (현재미사용 //221114)

        NONE = 0,
        Cell_Long = 1,
        Cell_Short = 2,
        CellProduct_Long = 3,
        CellProduct_Short = 4,
        Raw_Material = 5,
        ModuleProduct_Short = 6,
        ModuleProduct_Long = 7,
        CathodeReel = 8,
        AnodeReel = 9
    }

    /// <summary>
    /// Discriminator according to
    /// the height value of the tone bag.
    /// Below or above the standard
    /// value(ex. 1500mm)
    /// </summary>
    public enum eCarrierHeight
    {
        NONE = 0,
        Low = 1,  //높이 1500mm 이하
        High = 2, //높이 1500mm 초과
    }
    public enum eCoreType
    {
        NONE = 0,
        A = 1,
        B = 2,
    }

    public enum eProductEnd
    {
        NONE = 0,
        CELLBIZ = 1,
        PACKBIZ = 2,
    }

    public enum eHandoffType
    {
        MANUAL = 1, //작업자
        AUTO = 2,   //자동설비
    }

    public enum eCarrierLocationChangeResult
    {
        FAILED = 0,
        SUCCESS,
        CARRIER_NOT_EXIST,
        LOCATION_ALREADY_OCCUPIED,
        LOCATION_NOT_EXIST,
        LOCATION_EQUAL
    }

    #endregion

    #region WPS Monitoring 관련
    public enum eWPSMonitorType
    {
        None,
        Converter,
        Regulator,
    }
    public enum eWPSComType
    {
        None,
        TCP_IP,
        RS_232,
        RS_485,
    }
    public enum eWPSDataState
    {
        NoDataReceived,
        FormatError,
        CheckSumError,
        Normal,
    }
    public enum eWPSModuleState
    {
        UNKNOWN = -1,
        STOP = 0,
        RUN = 1,
        FAULT = 2,
        WARNING = 3,
        FAILOVER = 4,
    }

    #endregion

    #region  PLC 관련
    //2021년 5월 20일 목요일 오전 11:01:56 - Editted by 수환 : PLC enum 추가
    //PLC 통신 타입
    public enum ePlcNetType
    {
        None,
        Ether,
        UTL,
        Simulation,
    }

    //PLC Data Type
    public enum ePlcArea
    {
        Bit,
        Word,
    }

    //PLC Message Type
    public enum ePLCMessageType
    {
        Ascii,
        Binary,
        Bit,
    }

    //PLC Bit State 
    public enum eBitState
    {
        ERROR = -1,
        OFF = 0,
        ON = 1,
    }

    //Bit Interface Map
    public enum eBitIFMap
    {
        CIMAlive,
        Command,
        PLCAlive,
        Alarm,
        CommanReply,
        EMS,
        DoorOpen,
        LightCurtain1,
        LightCurtain2,
        LightCurtain3,
        LightCurtain4,
        StartSW,
        StopSW,
        ResetSW,
        AutoSW,
    }

    //PLC PIO 상태 
    public enum ePIOState
    {
        Idel,
        Executeing,
        Complete,
        Error,
    }

    public enum eDataChangeUnitType
    {
        eBooth,
        eCrane,
        ePort,      //(C/V)
    }


    #endregion

    #region 스케쥴러 관련
    public enum enumScheduleStep
    {
        None,
        ManualPortCall,
        ManualPortInput,
        PrepareHostJob,
        JobAssign,
        TargetInterlockCheck,
        SourceUnloadStart,
        SourceUnloadComplete,
        DestLoadStart,
        DestLoadComplete,
        PortGet,
        PortPut,
        MoveComplete,
        JobAbortComplete,
        CraneJobComplete,
        JobPause,
        CheckDestination,
        WaitHostAbortCommand, //RM 문제 발생시 Host Abort 명령 대기

        ErrorEmptyRetrieve,
        ErrorDoubleStorage,
        ErrorPortIF,


        //RM 제어 스텝 추가.
        RMError,
        //RMSecureArea, //삭제
        //RMSecurePushWait,
        RMGetAssign,
        RMGetCompleteWait,
        RMPutAssign,
        RMPutCompleteWait,
        RMMoveAssign,
        RMMoveCompleteWait,
        RMExtinguish,   //소화작업 추가.
        RMExtinguishWait,

    }

    /// <summary>
    ///     Transfer Command State
    ///     //상태 천이 테이블
    ///
    ///     //QUEUED => TRANSFERRING (반송 명령이 초기화 되면)
    ///
    ///     //QUEUED => CANCELING (HOST 가 CANCEL 명령 내리면)
    ///
    ///     //CANCELING => NONE (CANCEL 작업 완료되면)
    ///
    ///     //TRANSFERRING => PAUSED (반송불가능할 상태시 SCS 가 PAUSE 명령)
    ///
    ///     //PAUSED => TRANSFERRING (반송 가능할시 SCS RESUME)
    ///
    ///     //[TRANSFERRING OR PAUSED] => NONE (작업 명령이 성공 또는 실패시 Result Code  0 = Success   ETC = Fail)
    ///
    ///     //[TRANSFERRING OR PAUSED] => ABORTING (상위 ABORT 명령 내려올시)
    ///
    ///     //ABORTING => NONE (중단 작업 완료시)
    ///
    ///     //ABORTING => [TRANSFERRING OR PAUSED] (ABORT 작업 실패시)
    ///
    ///     //CANCELING => QUEUED (CANCEL 실패시) [비사용]
    /// </summary>
    public enum eTCState
    {
        NONE,
        QUEUED, //Cancel 가능
        TRANSFERRING, //Abort 가능
        PAUSED,       //Abort 가능
        CANCELING,
        ABORTING,
    }


    public enum eReserveAreaResult
    {
        ReserveOK,
        PushRequire,
        WaitRequire,
        WithdrawRequire,
        CannotReachAble, //목표지점에 도달할수 없음
    }

    public enum eScheduleJobFrom
    {
        Operator,
        HostMCS,
        EQItSelf
    }

    public enum eSubJobType
    {
        None, //해당 작업은 서브 작업이 아님
        Push, //작업 동선 확보를 위해 다른 RM 을 안전지대로 보낸다.
        HandOver, //전용 구역에서 전용 구역으로 갈때 임시 버퍼에 보낸다.
        AlterStore, //목표 상태 이상일때 임시 쉘프에 대체 보관한다.
    }
    public enum eSC_OperationMode
    {
        NormalMode,     //듀얼 랙 모드
        FirstRMOnly,    //첫번째 RM 만 가동
        SecondRMOnly    //두번째 RM 만 가동
    }

    public enum eCraneExZone
    {
        NoAccessAble,
        SharedZone,     //공용 Zone
        FirstCraneZone,    //첫번째 RM 전용
        SecondCraneZone    //두번째 RM 전용
    }
    public enum eSC_OperationModeResultCode
    {
        NotSupported = 0,  //해당 호기는 모드 변경 지원 안함.
        OK = 1, //정상변경
        NeedPauseState = 2,//현재 포즈 상태에서만 변경 가능
        CheckCranePosition = 3, //다운 크레인 위치가 양끝방향 위치가 아님
        ErrorOccurred = 4,  //모드 변경 도중 에러 발생.
    }
    public enum eManualJobCreateResult
    {
        Failed = 0,
        Success = 1,
        InvalidSource = 2,
        InvalidDestination = 3,
        InvalidCrane = 4,

        CarrierID_MisMatch = 5,
        CarrierSize_MisMatch = 6,
        CarrierNotExistInSTK = 7,
        CarrierNotExistInSource = 8,
        CarrierNotReady = 9,

        CraneNotReady = 10,
        CraneNotReachAble = 11,
        ShelfAlreadyReserved = 12,
        SameJobExist = 13,
        PriorityCheck = 14,

        SourceNotAvailable = 15,
        DestinationNotAvailable = 16,

        SourceAndDestinationEqual = 17,
        DestShelfProtected = 18,
    }

    #endregion

    #region GUI 관련
    public enum eSectionHeader
    {
        RMParameter,
        RMPval,
        AxisState,
        Main_Pval,
        AxisPosition,
        Main_BankByalevel,
        CraneCmd,
        PortCmd,
        TowerLampCmd,
        McsJob,

        User,   //220405 HHJ SCS 개선     //- User Page 추가
        GUser,  //220704

        State_PRT,
        Edit_IMG,
        Recipe_Modify,
        Recipe_Manage,
        Scenario_Manage,
    }

    //SuHwan_20220316 : GUI 테마 색상  
    public enum eThemeColor
    {
        NONE = 0,
        DARK, //어두운 테마
        LIGHT,  //밝은 테마
    }
    //220331 HHJ SCS UI 기능 추가       //- ShelfColor 변경 추가
    public enum eAnimationType
    {
        eRM1Job,
        eRM2Job,
    }
    public enum eRotateDirection
    {
        Horizontal = 0,         //가로방향 (0도 - 노말상태)
        Vertical = 90,          //세로방향 (90도 회전)
    }

    //221230 HHJ SCS 개선
    public enum eUnitCommandProperty
    {
        EmergencyStop,         //Fixed, Crane 전용
        Active,                //Fixed, Crane 전용
        Stop,                  //Fixed, Crane 전용
        ErrorReset,            //Fixed, Crane 전용
        Home,                  //Fixed, Crane 전용
        ManualJob,             //Fixed, Crane 전용
        Smoke,                 //Fixed, Crane 전용
        Fire1,                 //Fixed, Crane 전용
        Fire2,                 //Fixed, Crane 전용
        FireSignal,            //Fixed, Crane 전용

        Detail,                //Fixed, Crane, CV 공용

        //230217 HHJ SCS 개선  //Auto -> AutoRun, Manual -> ManualRun 이름 변경 및 AccessAGV, AccessOPER 추가
        AutoRun,               //NoneFixed(Auto, Manual 혼용), CV 전용
        ManualRun,             //NoneFixed(Auto, Manual 혼용), CV 전용

        AccessAGV,             //NoneFixed(Auto, Manual 혼용), CV 전용
        AccessOPER,            //NoneFixed(Auto, Manual 혼용), CV 전용

        Enable,                //NoneFixed(Enable, Disable 혼용), CV, Shelf 공용
        Disable,               //NoneFixed(Enable, Disable 혼용), CV, Shelf 공용

        TrackPause,            //NoneFixed(Pause, Resume 혼용), CV 전용
        TrackResume,           //NoneFixed(Pause, Resume 혼용), CV 전용

        Write,                 //Fixed, CV 전용
        BcrRead,               //Fixed, CV 전용
        KeyIN,                 //Fixed, CV 전용

        DirectionInMode,       //NoneFixed(DirectionInMode, DirectionOutMode, DirectionBothMode 혼용), CV 전용
        DirectionOutMode,      //NoneFixed(DirectionInMode, DirectionOutMode, DirectionBothMode 혼용), CV 전용
        DirectionBothMode,     //NoneFixed(DirectionInMode, DirectionOutMode, DirectionBothMode 혼용), CV 전용

        ShortMode,             //NoneFixed(ShortMode, LongMode, BothMode 혼용), Shelf 전용
        LongMode,              //NoneFixed(ShortMode, LongMode, BothMode 혼용), Shelf 전용
        BothMode,              //NoneFixed(ShortMode, LongMode, BothMode 혼용), Shelf 전용

        Install,               //NoneFixed(Install, Delete 혼용), 전체 공용
        Delete,                //NoneFixed(Install, Delete 혼용), 전체 공용

        CarrierID,              //230404
        CarrierSize,            //230404
        PalletSize,
        ProductEmpty,

        Inform,                //Fixed, 공용

        Status,                 //Shelf 상태 설정

        // Print Commad
        EnablePrintComplete,
        ReadAutoDataState,
        WriteAutoDataRedord,
        GetAutoDataString,
        ClearAutoDataQueue,
        ReadInkLevel,
        WriteWorkingFile,
        Build,
        CreatePrintBitmap,

    }

    //230103 HHJ SCS 개선
    public enum eDataChangeProperty
    {
        eNone,
        eIO_PLCtoPC,
        eIO_PCtoPLC,
        eUnitProperty,
    }

    public enum eOpenWindowName
    {
        eCraneManualJob,
        eUnitDetail,
        eCraneOrder,        //230321 HHJ SCS 개선     //- CraneOrder Window 추가
    }

    public enum eZoomCommandProperty
    {
        ePlus,
        eOrigin,
        eMinus,
        eRotate,        //230314 HHJ SCS 개선
    }

    //230214 HHJ SCS 개선
    public enum eCVWay
    {
        BottomToTop,        //crane기준으로 input port
        TopToBottom,        //crane기준으로 output port
        LeftToRight,        //crane기준으로 output port
        RightToLeft,        //crane기준으로 input port
    }
    //230215 HHJ SCS 개선
    public enum eScaleProperty
    {
        eValue,
        eTick,
        eMax,
        eMin,
        eChangeOrigin,
    }
    //230217 HHJ SCS 개선     //CV UI State 관련 추가
    public enum eConveyorUIState
    {
        None,
        Alarm,
        AlarmClear,
        Online,
        Manual,
    }
    //230314 HHJ SCS 개선
    public enum eLayOutAngle
    {
        eAngle0,
        eAngle90,
        eAngle180,
        eAngle270,
    }

    //230405 HHJ SCS 개선     //- Memo 기능 추가
    public enum eResultInformMemo
    {
        eCancel,
        eChange,
    }
    public enum eBCRState
    {
        NoBCR = 0, //BCR 없음
        OFFLine = 1, //BCR Offline 상태
        ManualBCR = 2, //메뉴얼 BCR (핸드스캐너)
        AutoBCROnline = 3, //고정 BCR TCP Trigger
    }

    public enum eUIJobRemoveResult
    {
        NoResult = 0,
        Cancel,
        CancelFail,
        Abort,
        AbortFail,
        AbortRequest,
        AbortAlreadyRequest,
        AbortJobForceComplete, //화물 Loc 없어서 화물 위치를 모를때 강제 작업 완료 처리한다.
    }
    #endregion

    #region User Management 관련

    //220405 HHJ SCS 개선     //- User Page 추가
    public enum eUserLevel
    {
        Admin,          //모든 권한 + 메인 레이아웃 설정 관리자 권한
        //Engineer,       //모든 권한
        Manager,        //오퍼레이터 추가만 가능
        Operator,       //메인화면의 기능만 부분 사용가능
        //Monitor,        //단순 모니터링
    }


    //230105 YSW SCS 권한부여enum
    public enum eUserLevelAuthority
    {
        AlarmManager,
        Config,
        //User,
        MapViewer,
        CarrierSearch,
        TerminalMessage,
        StoredCarrier,
        IOMonitor,
        SmokeDetect,
        AlarmLog,
        BCRLog,
        HSMSLog,
        OperatorLog,
        TransferLog,
        UtilizationLog,
        PlayBack,
        //20230711
        MCSState,
        SCSState, 
        PLCState, 
        BoothState,
        AlarmClear,
        JobControl,
        EXIT,
    }
    #endregion

    #region Play Back 관련
    public enum ePlayBackSource
    {
        LogFile,
        Local_DB,
        RemoteNetwork,
    }
    public enum ePB_Type
    {
        Playback_IO,
        Playback_Shelf,
        Playback_Port,
        Playback_Job,
        Playback_Alarm,
        Playback_CraneCommand,
    }
    public enum ePB_ItemType
    {
        IO_FullSnap, // I/O 전체 풀 스냅샷 
        IO_Changed,  // I/O 변경 이벤트

        Shelf_FullSnap, // 쉘프 상태 전체 풀 스냅샷
        Shelf_Changed,  // 쉘프 상태 변경 이벤트

        Port_FullSnap,  // 포트 상태 전체 풀 스냅샷
        Port_Changed,   // 포트 상태 변경 이벤트

        Job_FullSnap, // 작업 상태 전체 풀 스냅샷
        Job_Changed,   // 작업 상태 변경 이벤트

        CraneCommand_Changed, //크레인 커맨드 변경 이벤트

        Alarm_Changed //알람 발생 또는 클리어 이벤트

    }
    public enum ePB_DataEventType
    {
        Update,
        Add,
        Remove,
        Empty,
    }
    public enum ePBItem_State
    {
        NotProcessed,
        PreProcessed,
        Processed
    }
    #endregion

    #region MCS Spec 관련 
    public enum eOnlineState
    {
        Offline_EQ = 1,
        Offline_GOING = 2,
        Offline_HOST = 3,
        Remote = 5,
    }
    /// <summary>
    /// Establish Communication Request (S1F13) 상태 저장용 Enum 
    /// </summary>
    public enum eCommunicationEstablishState
    {
        NotEstablished = 0, //MCS 연결 확립 안됨
        EstablishReqSent = 1, //MCS 연결 요청 보냄
        Established = 2, //MCS  연결됨
    }
    //public enum eEQPControlState
    //{
    //    OFFLINE_EQPOFFLINE = 1,
    //    OFFLINE_GOINGOFFLINE = 2,
    //    OFFLINE_HOSTOFFLINE = 3,
    //    ONLINE_REMOTE = 5,
    //}
    public enum eTransferState
    {
        UNKNOWN = 0,
        QUEUED = 1,
        TRANSFERRING = 2,
        PAUSED = 3,
        CANCELING = 4,
        ABORTING = 5,
    }
    //public enum ePortState
    //{
    //    NONE,
    //    OUT_OF_SERVICE, //포트 서비스 중단 상태
    //    TRANSFER_BLOCK, //해당 포트로 현재 반송 불가능 상태
    //    READY_TO_LOAD, //크레인 -> Port 가능     [배출포트]
    //    READY_TO_UNLOAD, //Port -> 크레인가능    [투입포트]
    //}
    public enum eShelfState
    {
        NONE = 0,
        OUT_OF_SERVICE = 1,
        IN_SERVICE = 2,
    }
    public enum eShelfScheduleState
    {
        NONE = 0,
        GET_SCHEDULED = 1,
        PUT_SCHEDULED = 2,
    }

    public enum eShelfHSMSStatus
    {
        EMPTY = 0,          //대기
        OCCUPIED = 1,       //적재됨
        RESERVED = 2,   //예약
    }

    public enum eShelfStatus
    {
        EMPTY = 0,          //대기
        RESERVED_PUT = 1,   //입고예약
        RESERVED_GET = 2,   //출고예약
        OCCUPIED = 3,       //적재됨
        BLOCKED_PUT = 4,    //입고금지
        BLOCKED_GET = 5,    //출고금지
        NOT_USE = 6,        //사용금지
        DOUBLE_STORAGE = 7, //더블에러
        SOURCE_EMPTY = 8,   //공출고
        UNKSHELF = 9, //언노운 캐리어
    }
    public enum eCarrierState
    {
        NONE = 0,
        WAIT_IN = 1,
        TRANSFERRING = 2,
        COMPLETED = 3,
        ALTERNATE = 4,
        WAIT_OUT = 5,
        DELETE = 99,
    }
    

    public enum eCraneCommand
    {
        NONE = 0,
        //----------------------------------------------------------------------
        CYCLE1 = 1,                 //MOVE+PICKUP 
        CYCLE2 = 2,                 //MOVE+UNLOAD
        PORTTOPORT = 3,             //직출고(PortToPort) 
        RACKTORACK = 4,             //재배치(RacktoRack)
        LOCAL_HOME = 5,             //홈복귀
        DEST_ALT = 6,         //포트다운 재지정
        MOVE = 8,  //(해당 위치 이동)
        PICKUP = 9,   //(캐리어 취출 GET)
        UNLOAD = 10,   //(캐리어 수납 PUT)
        //----------------------------------------------------------------------
        REMOTE_CYCLE1 = 11,                 //MOVE+PICKUP 
        REMOTE_CYCLE2 = 12,                 //MOVE+UNLOAD
        REMOTE_PORTTOPORT = 13,             //직출고(PortToPort) (ONLINE)
        REMOTE_RACKTORACK = 14,             //재배치(RacktoRack) (ONLINE)
        REMOTE_HOME = 15,             //홈복귀 (ONLINE)
        REMOTE_DEST_ALT = 16,         //포트다운 재지정
        REMOTE_FIRE_UNLOADING = 17,         //화재출고 (ONLINE)
        REMOTE_MOVE = 18,  //(해당 위치 이동)
        REMOTE_PICKUP = 19,   //(캐리어 취출 GET)
        REMOTE_UNLOAD = 20,   //(캐리어 수납 PUT)
        //----------------------------------------------------------------------
        //## ONLINE MODE는 SCS(WCS)와 Online Mode일때 사용 (Nomal)
        // Remote (11~20)는 Local에서 사용
        //(21~28/30~41 은 제어단독사용)
        DELETE = 99, //SuHwan_20220819 : 삭제
    }
    public enum eCraneUIState
    {
        UNKNOWN = 0,
        ONLINE = 1,             //온라인 대기 상태
        OFFLINE = 2,            //오프라인 상태
        HOMING = 3,             //홈 복귀중
        PUTTING = 4,            //PUT 동작중
        GETTING = 5,            //GET 동작중
        MOVING = 6,             //이동 동작중
        RACK_TO_RACK = 7,       //랙투랙 동작중
        DIRECT_GETTING = 8,     //직출고 동작중
        FIRE_UNLOADING = 9,     //화재출고 동작중
        ERROR = 10,             //에러상태
    }

    public enum eCraneActiveState
    {
        INACTIVE = 0,            //중지 상태
        ACTIVE = 1,              //자동, NO ERROR, 최초 SET 후 유지
    }


    public enum enumCraneTarget
    {
        None,
        PORT = 1,
        SHELF = 2,
    }

    //220517 조숭진 hsms 메세지 추가
    public enum eSVCraneServiceState
    {
        NONE = 0,
        Out_OF_SERVICE = 1,
        IN_SERVICE = 2,
    }

    //220523 조숭진 hsms s2계열 메세지 추가 s
    public enum eHCACKCodeList
    {
        HCAckCode = 0,              //Confirmed, the command was excuted (the transfer system doesn't use this value. Confirmation is made using the number 4 values.)
        HCNAckCode1 = 1,            //Command does not exist
        HCNAckCode2 = 2,            //Currently not able to execute
        HCNAckCode3 = 3,            //At least one parameter is invalid
        HCAckCode4 = 4,             //Confirmed, the command will be executed and completion will be notified by an event
        HCNAckCode5 = 5,            //Rejected, already requested
        HCNAckCode6 = 6,            //object doesn't exist
        HCNAckCode7 = 7,            //from eq u_req not on.
        HCNAckCode8 = 8,            //to eq l_req not on
        HCNAckCode9 = 9,            //from and to either not on
        HCNAckCode10 = 10,          //mismatch cassette size
        HCNAckCode17 = 17,        //Specific Shelf Zone is Full (SKOJ2 Powder)
    }

    public enum eCPACKCodeList
    {
        CPAckCode = 0,              //no error
        CPNAckCode1 = 1,            //CPNAME doesn't exist
        CPNAckCode2 = 2,            //the incorrect value is specified in CPVAL
        CPNAckCode3 = 3,            //the incorrect format is specified in CPVAL
    }
    //220523 조숭진 hsms s2계열 메세지 추가 e

    public enum ePortTrasnferState
    {
        OUT_OF_SERVICE = 1,
        IN_SERVICE = 2,
        TRANSFER_BLOCKED = 3, //INLINE PORT
        READY_TO_LOAD = 4,    //INLINE PORT
        READY_TO_UNLOAD = 5,  //INLINE PORT
    }
    public enum eIDReadStatus
    {
        SUCCESS = 0,
        FAILURE = 1,
        DUPLICATE = 2,
        MISMATCH = 3,
        NO_CST = 4,
    }

    // 1.Input 2.Operation 3.Buffer
    public enum ePortType
    {
        LP = 1,     //LOADING_PORT      //라인 하나 짜리는 모두 LP사용
        OP = 2,     //OPERATION_PORT
        BP = 3,     //BUFFER_PORT
    }
    public enum eJobResultCode
    {
        SUCCESS = 0,
        OTHER_ERROR = 1,
        SHELF_ZONE_FULL = 2,
        DUPLICATE_ID = 3,
        MISMATCH_ID = 4,
        FAILURE_READ_ID = 5,
        EMPTY_RETRIEVAL = 6,
        DOUBLE_STORAGE = 7,
        DEST_INTERLOCK_NG = 11,
        SOURCE_INTERLOCK_NG = 12,
        PORT_NG = 13,
        MISMACTH_CST_SIZE = 14,
    }
    public enum eSCState
    {
        NONE = 0,
        INIT = 1,
        PAUSED = 2,
        AUTO = 3,
        PAUSING = 4,
    }
    public enum eZoneType
    {
        SHELF = 1,
        PORT = 2,
    }

    //Host Command Ack Code
    public enum eHCACKCode
    {
        Confirmed = 0,
        CommandNotExist = 1,
        CurrentlyNotExcuteAble = 2,
        InvalidParameter = 3,
        ConfirmedAndEventReport = 4,
        RejectedByAlreadyReq = 5,
        TargetObjectNotExist = 6,
        EQUnlaodReqNotExist = 7,
        EQLoadReqNotExist = 8,
        EQBothReqNotExist = 9,
        CSTSizeMismatch = 10,
        Hold = 11,
    }

    //221226 HHJ SCS 개선
    public enum eHostMessageDirection
    {
        eHostToEq,
        eEqToHost
    }
    #endregion

    #region 화재 관련
    public enum eFireMonitorMessageType
    {
        Unknown = 0,
        FireOccurred = 901,             //화재 발생 보고
        FireOccurredAck = 902,          //화재 발생 보고 응답
        FireReset = 905,                //화재 발생 리셋 보고
        FireResetAck = 906,             //화재 발생 리셋 보고 응답
        HeartBeat = 911,                //설비 상태 보고 요청
        HeartBeatAck = 912,             //설비 상태 보고 요청 응답
        ConfirmConnection = 998,        //HELLO 최초접속 확인 요청 (HELLO)
        ConfirmConnectionAck = 999,     //HELLO 접속 완료 응답(HI)
    }
    //220512 조숭진 화재감지 db추가
    public enum eFireEventInfo
    {
        NONE = 0,  
        SMOKE = 1, //연기 발생
        FIRE = 2,  //화재 발생
        ERROR = 3, //센서 통신 에러
        SENSOR_NOTHING = 4,   //특이사항 없음.
    }

    #endregion

    #region Server - Client 관련

    //SuHwan_20220719 : 클라이언트 추가
    public enum eSubscriptionCommand
    {
        Request_None,
        Request_AlarmList,
        Request_ActiveAlarmList,
        Request_AlarmClear,
        Request_BasicState,
        Request_SystemParameter,
        Request_ShelfData_Front,
        Request_ShelfData_Rear,
        Request_McsJobList,
        Request_CarrierItemList,
        Request_CVLineModuleList,
        Subscription_LayOutView,
    }
    public enum eCraneOPManualCommand
    {
        EMG_STOP,
        ACTIVE,
        STOP,
        ERROR_RESET,
        RETURN_HOME,
        MANUAL_COMMAND,
        SMOKE,
        FIRE1,
        FIRE2,
        FIRE_SIGNAL,
    }
    //220928 HHJ SCS 개선     //- 회전 기능 보완

    //SuHwan_20220928 : [ServerClient] 서버 클라 선택 이넘
    public enum eServerClientType
    {
        Server = 0,
        Client,
    }
    public enum eClientProcedureUnitType
    {
        Shelf,
        CV,     //230207 추가
        Crane,  //230207 추가
    }
    #endregion

}
//230911 HHJ enum Value string으로 변경
public static class EnumExtensionMethods
{
    public static bool GetEnumStringByEnumValue<T>(this object value, out object eData)
    {
        try
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToDictionary(t => (int)(object)t, t => t.ToString());

            //value가 int형이 아니면 Empty로 처리하고 true를 리턴한다.
            if (!int.TryParse(value.ToString(), out int ivalue))
            {
                eData = "Not Define";
                return true;
            }

            //Key 없으면 Empty로 처리하고 true를 리턴한다.
            if (!enumValues.ContainsKey(ivalue))
            {
                eData = "Not Define";
                return true;
            }

            eData = enumValues[ivalue];
            return true;
        }
        catch (Exception ex)
        {
            eData = "Exception!";
            throw new ArgumentException("CheckExistEnumValue Exception");
        }

        eData = "Not Define";
        return false;
    }
}