namespace WCF_LBS.Commands
{
    public class TowerLampCommand
    {
        public string Visible = "1";
        public string Green = "0";
        public string Yellow = "0";
        public string Red = "0";
        public string Buzzer = "0";
        public string MuteMode = "0";

        public TowerLampCommand(string G, string Y, string R, string Buzzer, string MuteMode)
        {
            this.Green = G;
            this.Yellow = Y;
            this.Red = R;
            this.Buzzer = Buzzer;
            this.MuteMode = MuteMode;
        }
    }
}
