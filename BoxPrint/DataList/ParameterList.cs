namespace BoxPrint.DataList
{
    /// <summary>
    /// 190812 Paramter List 추가 
    /// </summary>
    public class ParameterList
    {

        public int exist_axis { get; set; } // Axis 수량

        public int ShelfTiltCheckHeight { get; set; } // shelf Tilt Sensor 사용 높이 기준    2021.07.20 lim,

        public int RearXcount { get; set; } // X배열 수량 Shelf
        public int RearYcount { get; set; } // Y배열 수량 Shelf
        public int RearTotal { get; set; } // Rear total 수량

        public int FrontXcount { get; set; } // X배열 수량 Shelf
        public int FrontYcount { get; set; } // Y배열 수량 Shelf
        public int FrontTotal { get; set; } // Front total 수량

        public int AutoXcount { get; set; } // OHT Port X 수량
        public int AutoYcount { get; set; }  // OHT Port Y 수량
        public int AutoTotal { get; set; }  // OHT Port Total 수량

        public int ManualXcount { get; set; }  // manual  Port X 수량
        public int ManualYcount { get; set; }  // manual  Port Y 수량
        public int ManualTotal { get; set; }  // manual  Port Total  수량

        public decimal HomePosAxis1 { get; set; }
        public decimal HomePosAxis2 { get; set; }
        public decimal HomePosAxis3 { get; set; }
        public decimal HomePosAxis4 { get; set; }
        public decimal HomePosAxis5 { get; set; }
        public decimal HomePosAxis6 { get; set; }

        public decimal Appreach_Lower { get; set; } // Get Put 완료 이후 동작 완료 범위
        public decimal Appreach_Upper { get; set; } // Get Put 완료 이후 동작 완료 범위

        public decimal ArmActionCompeteRange { get; set; } // Get Put 완료 이후 동작 완료 범위

        public decimal ForkAxis_Allowablerange { get; set; } //Fork 완료 허용 범위
        public decimal XAxis_Allowablerange { get; set; } //X 완료 허용 범위

        public decimal ZAxis_Allowablerange { get; set; } //Z 완료 허용 범위

        public decimal TurnAxis_Allowablerange { get; set; } //Turn 완료 허용 범위


        public int MoveTimeout { get; set; } // Move 동작에 대한 time Out 설정
        public int InitTimeout { get; set; } // init 동작에 대한 time Out 설정

        public string IPAddress { get; set; } // Power P-Mac IP

        public string FrontZone1Xcount { get; set; }
        public string FrontZone1Ycount { get; set; }

        public string FrontZone2Xcount { get; set; }
        public string FrontZone2Ycount { get; set; }

        public string FrontZone3Xcount { get; set; }
        public string FrontZone3Ycount { get; set; }

        public string FrontZone4Xcount { get; set; }
        public string FrontZone4Ycount { get; set; }

        public string FrontZone5Xcount { get; set; }
        public string FrontZone5Ycount { get; set; }

        public string FrontZone6Xcount { get; set; }
        public string FrontZone6Ycount { get; set; }


        public string RearZone1Xcount { get; set; }
        public string RearZone1Ycount { get; set; }

        public string RearZone2Xcount { get; set; }
        public string RearZone2Ycount { get; set; }

        public string RearZone3Xcount { get; set; }
        public string RearZone3Ycount { get; set; }

        public string RearZone4Xcount { get; set; }
        public string RearZone4Ycount { get; set; }

        public string RearZone5Xcount { get; set; }
        public string RearZone5Ycount { get; set; }

        public string RearZone6Xcount { get; set; }
        public string RearZone6Ycount { get; set; }


        public int MaxZoneButtonX { get; set; } // Zone 버튼 수량 X
        public int MaxZoneButtonY { get; set; } // Zone 버튼 수량 Y
        public int MaxDatacount { get; set; } // Pmac 으로 한번에 보낼 수량

        public int ZAxisSoftLimitP { get; set; } // Pmac 으로 Z축 SoftLimit
        public int TurnAxisSoftLimitP { get; set; } // Pmac 으로 T축 SoftLimit

        public int ForkAxisSoftLimitP { get; set; } // Pmac 으로 T축 SoftLimit

        public int XAxisSoftLimitP { get; set; } // Pmac 으로 T축 SoftLimit

        public int ZAxisSoftLimitM { get; set; } // Pmac 으로 Z축 SoftLimit
        public int TurnAxisSoftLimitM { get; set; } // Pmac 으로 T축 SoftLimit

        public int ForkAxisSoftLimitM { get; set; } // Pmac 으로 T축 SoftLimit

        public int XAxisSoftLimitM { get; set; } // Pmac 으로 T축 SoftLimit


        public int X_MOVE_SPEED_NO_CARRIER { get; set; }
        public int X_MOVE_ACCEL_NO_CARRIER { get; set; }
        public int X_MOVE_DECEL_NO_CARRIER { get; set; }

        public int X_MOVE_SPEED { get; set; }
        public int X_MOVE_ACCEL { get; set; }
        public int X_MOVE_DECEL { get; set; }

        public int DRIVE_SPEED { get; set; }
        public int DRIVE_ACCEL { get; set; }
        public int DRIVE_DECEL { get; set; }

        public int Z_LOW_SPEED { get; set; }
        public int Z_LOW_ACCEL { get; set; }
        public int Z_LOW_DECEL { get; set; }

        public int TURN_SPEED { get; set; }
        public int TURN_ACCEL { get; set; }
        public int TURN_DECEL { get; set; }

        public int pVACTORSPEED { get; set; }
        public int pVACTTA { get; set; }
        public int pVACTTS { get; set; }
        public int pVACTTD { get; set; }

        public int pClamp_SPEED { get; set; }
        public int pClamp_ACCEL { get; set; }
        public int pClamp_DECEL { get; set; }

        public int mMachineType { get; set; }

        public int pGetPutSensorCheck { get; set; }

        public int pStandardTeaching { get; set; }

        public int TactLogSave { get; set; }

        public int InitDefaultSpeed { get; set; }


        // 2021.020.19 TrayHeight 인터락 추가
        public int TrayHeightInterLock { get; set; }


    }
}
