using System;

namespace BoxPrint.DataList.MCS
{
    public class Job_Result
    {
        public McsJob MJob;
        public string JobID;
        public eJob_Result JobResult;
        public string message;
        public DateTime ResultTime;
        public Job_Result(McsJob Job, string ID, eJob_Result result, string msg)
        {
            MJob = Job;
            JobID = ID;
            JobResult = result;
            message = msg;
            ResultTime = DateTime.Now;
        }
    }
}
