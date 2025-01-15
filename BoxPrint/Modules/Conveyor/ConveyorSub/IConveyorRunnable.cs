namespace BoxPrint.Modules.Conveyor
{
    public interface IConveyorRunnable
    {
        string GetRunnerName();
        bool CV_CheckFWD_Running();
        bool CV_CheckBWD_Running();
        bool CV_CheckError();
        bool CV_EMGStop();
        bool CV_Stop();
        bool CV_Reset();
        bool CV_ForwardRun(eCV_Speed spd);
        bool CV_BackwardRun(eCV_Speed spd);
        bool InitConveyorRunner();

        eCV_Speed CV_GetCurrentRunningSpeed();

    }
}
