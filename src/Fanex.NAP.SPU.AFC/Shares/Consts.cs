namespace Fanex.NAP.SPU.AFC.Shares
{
    public static class Consts
    {
        public const string CONVERT_TO_SQL_DATETIME = "yyyy-MM-dd HH:mm:ss";

        public enum Category
        {
            Newmember = 201,
            NormalMember = 202,
            SmartPunter = 203,
            HighRishPunter = 204,
            VIP = 205,
            Probation = 206,
            VVIP = 213,
            Robot = 61
        };

        public enum SystemParameters
        {
            ProblemAccountLastScannedTime = 2,
            RobotAccountLastScannedTime = 3,
            MatchMonitorLastScannedTrans = 20
        };
    }
}