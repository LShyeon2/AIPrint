using System.Security.Cryptography;
using System.Text;
using System;
using System.Diagnostics;

namespace BoxPrint.Modules.User
{
    public class User
    {
        public string UserID { get; set; }
        public string UserPassword { get; set; }
        public string UserName { get; set; }
        public eUserLevel UserLevel { get; set; }

        public string TeamName { get; set; }  //2024.06.08 lim, OY_Merge 

        public static readonly int DefalutAutoLogoutTime = 30;

        private int _AutoLogoutMinute = DefalutAutoLogoutTime;
        public int AutoLogoutMinute
        {
            get => _AutoLogoutMinute;
            set
            {
                if (value < 0) //0을 받아서 로그 오프 안하도록 함.
                    _AutoLogoutMinute = DefalutAutoLogoutTime; //자동 로그아웃 타임 설정값 실제값은 디비에서 받아와야 하지만 기본값은 30분으로 한다.
                else
                    _AutoLogoutMinute = value;
            }
        }
        private DateTime _LastActivetime;
        //최종 활동 시간. 최종 활동 시간에서 자동 로그아웃 타임 오버되면 로그아웃.
        public DateTime LastActivetime
        {
            get
            {
                return _LastActivetime;
            }
            private set
            {
                _LastActivetime = value;
                
                
                if(SW_LastActive == null)
                {
                    SW_LastActive = Stopwatch.StartNew();
                }
                else
                {
                    SW_LastActive.Restart();
                }
            }
        }
        public Stopwatch SW_LastActive = null; //241120 RGJ 스탑워치 사용.


        public bool UserUse { get; set; }

        private int _ListNo;
        public static int sListNo;
        public int ListNo
        {
            get { return _ListNo; }
            set
            {

                _ListNo = value;
                sListNo = value;
            }
        }//----------------------------------------------------------------------------220704

        public User(string name, string id, string pw, eUserLevel lvl, bool use, bool dbget = false)        //220610 조숭진 db에서 읽어올때..)
        {
            UserName = name;
            UserID = id;

            if (!dbget)
                UserPassword = CreateSHA512Password(pw);
            else
                UserPassword = pw;

            UserLevel = lvl;
            UserUse = use;
        }
        public void SetAutoLogoutMinute(int minute)
        {
            if(minute > 0) //로그아웃 시간 상한선은 추후 결정함.
            {
                AutoLogoutMinute = minute;
            }
            else
            {
                AutoLogoutMinute = 0;
            }
        }

        public User()
        {
            UserName = string.Empty;
            UserID = string.Empty;
            UserPassword = string.Empty;
            UserLevel = eUserLevel.Operator;
            UserUse = false;
            ChangeListNo();
        }

        private void ChangeListNo()
        {
            ListNo = sListNo;
            sListNo++;
        }

        private string CreateSHA512Password(string PlainText)
        {
            string hashData = HashGenerator.GetSHA512Hash(PlainText);
            if (hashData.Length == 128)
            {
                return hashData;
            }
            else
            {
                return "void password";
            }
        }

        public bool CheckPW(string pw)
        {
            return UserPassword.Equals(HashGenerator.GetSHA512Hash(pw));
        }

        //마지막 로그인 시간을 저장해둠
        public void SetLoginTime()
        {
            LastActivetime = DateTime.Now;
        }

        public void UpdateActivity()
        {
            LastActivetime = DateTime.Now;
        }
        public bool IsAutoLogoutTimeover()
        {
            if (AutoLogoutMinute <= 0)
            {
                return false;
            }
            else
            {
                return IsTimeout_SW(SW_LastActive, AutoLogoutMinute * 60);
            }
        }

        /// <summary>
        /// 241105 RGJ TIMEOUT 시스템 시간 의존 개선.
        /// 시스템 시간 변경에도 Stopwatch 를 활용하여  올바르게 동작가능하게함
        /// Stopwatch stopwatch = Stopwatch.StartNew();
        /// IsTimeout(stopwatch, 10); // 10초 타임아웃 체크
        /// </summary>
        /// <param name="stopwatch"></param>
        /// <param name="nTimeoutValue"></param>
        /// <returns></returns>
        public static bool IsTimeout_SW(Stopwatch stopwatch, int nTimeoutValue)
        {
            if (stopwatch == null)
            {
                return false;
            }
            // 지정한 초를 기준으로 Timeout 설정
            TimeSpan tLimit = TimeSpan.FromSeconds(nTimeoutValue);
            return stopwatch.Elapsed > tLimit;
        }

    }

    public class HashGenerator
    {
        public static string GetSHA512Hash(string rawData)
        {

            using (SHA512 sha512Hash = SHA512.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha512Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
