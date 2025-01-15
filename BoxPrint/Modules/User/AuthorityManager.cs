using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BoxPrint.Modules.User
{

    /// <summary>
    /// User 의 권한을 XML 관리대신 DB 에서 관리
    /// 현재 XML 체계에서는 권한 변경해도 전파 안됨.
    /// 직접 DB 에 쓰고 가져와서 권한을 입힌다.
    /// </summary>
    public class AuthorityManager
    {
        public AuthorityManager()
        {
            UpdateFullAuthorityFromDB();
        }

        private List<AuthorityItem> AList = new List<AuthorityItem>();
        /// <summary>
        /// DB에서 권한정보를 가져온다.
        /// </summary>
        public void UpdateFullAuthorityFromDB()
        {
            var temp = new List<AuthorityItem>();

            temp = GlobalData.Current.DBManager.DbGetProcedureAuthorityInfo();
            
            

            if (temp.Count > 0)  //권한을 가져왔는데 비었으면 기존값 유지
            {
                AList.Clear();
                AList = temp;
            }

        }
        /// <summary>
        /// 로긴 UserLevel 에 따른 권한을 업데이트한다.
        /// </summary>
        /// <param name="UserLevel"></param>
        /// <param name="AuthorityList"></param>
        public void UpdateLoginUserAuthority(eUserLevel UserLevel , List<string> AuthorityList)
        {
            if(AuthorityList == null)
            {
                return;
            }
            UpdateFullAuthorityFromDB();//로그인 할때마다 디비에서 가져와서 전체 권한 리스트 갱신
            AuthorityList.Clear();
            var LevelList = AList.Where(a => a.Authority_Level == UserLevel);
            foreach(var AItem in LevelList)
            {
                if(AItem != null && AItem.ReadAccess)
                {
                    AuthorityList.Add("Read" + AItem.Authority_Name);
                }
                if (AItem != null && AItem.ModifyAccess)
                {
                    AuthorityList.Add("Modify" + AItem.Authority_Name);
                }
            }

        }

        /// <summary>
        /// 해당 레벨에 해당하는 권한 목록 복사본을 가져온다.
        /// </summary>
        /// <param name="Level"></param>
        /// <returns></returns>
        public List<AuthorityItem> GetAuthorityItemsList(string Level)
        {
            List<AuthorityItem> LevelList;
            LevelList = AList.Where(a => a.Authority_Level.ToString() == Level).ToList();
            return LevelList;
        }

        public bool UpdateAuthorityToDB(AuthorityItem AItem)
        {
           bool Result =  GlobalData.Current.DBManager.DbSetProcedureAuthorityInfo(AItem);
           return Result;
        }

    }
}
