using Newtonsoft.Json;
using BoxPrint.DataBase;
using BoxPrint.DataList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TranslationByMarkupExtension;

namespace BoxPrint.Modules.User
{
    public class UserManager : SafeObservableCollection<User>
    {
        public delegate void EventHandler_LoginUserChange(User usr);
        public event EventHandler_LoginUserChange OnLoginUserChange;

        private List<User> dbuser;          //220610 조숭진 user db 추가.
        public static User UserData;
        //이름은 중복될 여지가 있음. 아이디는 고유하게 사용하도록 한다.
        private User _CurrentUser;
        public User CurrentUser
        {
            get { return _CurrentUser; }
            set
            {
                if (_CurrentUser == null || _CurrentUser != value)
                {
                    //유저변경 이벤트를 줘야하는가?
                    _CurrentUser = value;
                }
            }
        }

        bool bFirstLoad = true;
        bool bFirstLogin = false;

        public UserManager(bool DbGet = false)
        {
            //220610 조숭진 user db 추가.
            //DB에서 유저 정보를 가져온다.
            if (DbGet)
            {
                //dbuser = GlobalData.Current.DBManager.DbGetUserInfo();
                dbuser = GlobalData.Current.DBManager.DbGetProcedureUserInfo();

                foreach (var item in dbuser)
                {
                    //2024.09.08 lim, 로그아웃 시간 디폴트 값으로 들어감
                    User usr = new User(item.UserName, item.UserID, item.UserPassword, item.UserLevel, item.UserUse, true);
                    usr.SetAutoLogoutMinute(item.AutoLogoutMinute);
                    this.Add(usr);
                    //this.Add(new User(item.UserName, item.UserID, item.UserPassword, item.UserLevel, item.UserUse, true));
                }
            }
            else
            {
                var adminUser = new User("Administrator", "admin", "toptec135", eUserLevel.Admin, true);
                adminUser.SetAutoLogoutMinute(0);
                this.Add(adminUser);
            }

            //가져온 DB정보에서 admin이 없다면 admin은 강제로 생성해주고 DB에 저장해준다.
            if (GetUserByID("admin") == null)
            {
                var adminUser = new User("Administrator", "admin", "toptec135", eUserLevel.Admin, true);
                adminUser.SetAutoLogoutMinute(180);
                this.Add(adminUser);
                OracleDBManager.Current.DbSetProcedureUserInfo(adminUser);
            }

            if (bFirstLoad && DbGet)
            {
                GlobalData.Current.userlistrefresh += Current_userlistrefresh;
            }

        }

        private void Current_userlistrefresh()
        {
            this.Clear(); //어차피 DB 에서 유저 정보 가져오므로 클리어 시킴.

            if (dbuser.Count != 0)
            {
                dbuser.Clear();
            }

            dbuser = GlobalData.Current.DBManager.DbGetProcedureUserInfo();

            foreach (var item in dbuser)
            {
                //2024.09.08 lim, 로그아웃 시간 디폴트 값으로 들어감
                User usr = new User(item.UserName, item.UserID, item.UserPassword, item.UserLevel, item.UserUse, true);
                usr.SetAutoLogoutMinute(item.AutoLogoutMinute);
                this.Add(usr);
                //this.Add(new User(item.UserName, item.UserID, item.UserPassword, item.UserLevel, item.UserUse, true));
            }
            string currentUserID = MainWindow.GetMainWindowInstance().LoginUserID;
            var CurrentUser = this.Where(u => u.UserName == currentUserID).FirstOrDefault();
            if(CurrentUser != null)
            {
                CurrentUser.UpdateActivity();
            }

            //가져온 DB정보에서 admin이 없다면 admin은 강제로 생성해주고 DB에 저장해준다.
            if (GetUserByID("admin") == null)
            {
                var adminUser = new User("Administrator", "admin", "toptec135", eUserLevel.Admin, true);
                adminUser.SetAutoLogoutMinute(180);
                this.Add(adminUser);
                //GlobalData.Current.DBManager.DbSetProcedureUserInfo(adminUser);
            }
        }

        //아이디로 유저 검색
        public User GetUserByID(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                return null;
            }
            //대소문자 구분이 없게 사용하며, 검색시에는 upper case로 변환하여 검색한다.
            return this.Where(r => r.UserID.ToUpper().Equals(id.ToUpper())).FirstOrDefault();
        }
        //이름으로 유저 검색
        public SafeObservableCollection<User> GetUserByName(string name)
        {
            //대소문자 구분이 없게 사용하며, 검색시에는 upper case로 변환하여 검색한다.
            List<User> dmylist = this.Where(r => r.UserName.ToUpper().Equals(name.ToUpper())).ToList();

            if (dmylist == null)
                return null;

            SafeObservableCollection<User> ret = new SafeObservableCollection<User>();

            foreach (User user in dmylist)
            {
                ret.Add(user);
            }

            return ret;
        }
        //레벨(권한)으로 유저 검색
        public SafeObservableCollection<User> GetUsersByLevel(eUserLevel lvl)
        {
            List<User> dmylist = this.Where(r => r.UserLevel.Equals(lvl)).ToList();

            if (dmylist == null)
                return null;

            SafeObservableCollection<User> ret = new SafeObservableCollection<User>();

            foreach (User user in dmylist)
            {
                ret.Add(user);
            }

            return ret;
        }

        public ResultUserControl AddUser(User user)
        {
            try
            {
                //현재 유저가 유저 추가 / 제거가 가능한지.
                if (!PossibleUserControl())
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = TranslationManager.Instance.Translate("Current User cannot Create/Delete User. (Manager Only)").ToString(),
                    };
                }
                //ID중복 확인
                if (GetUserByID(user.UserID) != null)
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Overlap").ToString(), user.UserID)
                    };
                }

                //bool bsuccess = GlobalData.Current.DBManager.DbSetUserInfo(user);     //220610 조숭진 user db 추가. 나중에 사용여부도 추가되어야 함.
                bool bsuccess = false;

                //if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                //{
                    bsuccess = GlobalData.Current.DBManager.DbSetProcedureUserInfo(user);
                //}
                //else
                //{
                //    //이미 클라이언트에서 추가가 완료되었기때문에 무조건 add 시키면된다.
                //    bsuccess = true;
                //}

                if (bsuccess)
                {
                    this.Add(user);

                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        ClientSetProcedure(user, "ADD");

                        return new ResultUserControl()
                        {
                            bResult = true,
                            strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Create Complete").ToString(), user.UserID)
                        };
                    }
                    else
                    {
                        Current_userlistrefresh();
                        return null;
                    }
                }
                else
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = string.Format("ID {0} " +TranslationManager.Instance.Translate("DB Set Fail").ToString(), user.UserID)
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultUserControl()
                {
                    bResult = false,
                    strResult = ex.ToString(),
                };
            }
        }

        public ResultUserControl DeleteUser(User user)
        {
            try
            {
                if (user == null)
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = TranslationManager.Instance.Translate("Delete User is Null").ToString(),
                    };
                }

                //현재 유저가 유저 추가 / 제거가 가능한지.
                if (!PossibleUserControl())
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = TranslationManager.Instance.Translate("Current User cannot Create/Delete User. (Manager Only)").ToString(),
                    };
                }
                //삭제하려는 유저가 현재 로그인 유저인지
                //현재 유저는 삭제가 되지않기에, 매니저 하나를 생성하여 놓으면 해당 매니저는 삭제할 수 없음.
                if (CurrentUser.UserID.Equals(user.UserID))
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Current User").ToString(), user.UserID)
                    };
                }
                //해당 ID의 유저가 있는지 확인
                //리스트항목에서 선택하여 삭제가 되기에 이 조건은 없어도 되지만, 추후 어찌 변경될지 모르기에 혹시나해서 넣어놓음.
                if (GetUserByID(user.UserID) == null)
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Not Exist").ToString(), user.UserID)
                    };
                }

                //this.Remove(user);
                //bool bsuccess = GlobalData.Current.DBManager.DbSetUserInfo(user, true);     //220610 조숭진 user db 추가. 나중에 사용여부도 추가되어야 함.
                bool bsuccess = false;
                //if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                //{
                    bsuccess = GlobalData.Current.DBManager.DbSetProcedureUserInfo(user, true);
                //}
                //else
                //{
                //    //이미 클라이언트에서 추가가 완료되었기때문에 무조건 remove 시키면된다.
                //    bsuccess = true;
                //}

                if (bsuccess)
                {
                    this.Remove(user);

                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        ClientSetProcedure(user, "DELETE");

                        return new ResultUserControl()
                        {
                            bResult = true,
                            strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Remove Complete").ToString(), user.UserID)
                        };
                    }
                    else
                    {
                        Current_userlistrefresh();
                        return null;
                    }
                }
                else
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Remove Fail").ToString(), user.UserID)
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultUserControl()
                {
                    bResult = false,
                    strResult = ex.ToString(),
                };
            }
        }

        public ResultUserControl UpdateUser(User user)
        {
            try
            {
                if (user == null)
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = TranslationManager.Instance.Translate("Update User is Null").ToString(),
                    };
                }
                //현재 유저가 유저 추가 / 제거가 가능한지.
                if (!PossibleUserControl())
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = TranslationManager.Instance.Translate("Current User cannot Create/Delete User. (Manager Only)").ToString(),
                    };
                }
                //업데이트 하려는 유저가 현재 로그인 유저인지
                if (CurrentUser.UserID.Equals(user.UserID))
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Current User").ToString(), user.UserID)
                    };
                }
                //해당 ID의 유저가 있는지 확인
                //리스트항목에서 선택하여 업데이트 되기에 이 조건은 없어도 되지만, 추후 어찌 변경될지 모르기에 혹시나해서 넣어놓음.
                if (GetUserByID(user.UserID) == null)
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Not Exist").ToString(), user.UserID)
                    };
                }

                User tmp = GetUserByID(user.UserID);
                int icnt = this.IndexOf(tmp);

                if (!icnt.Equals(-1))
                {
                    //GlobalData.Current.DBManager.DbSetUserInfo(user);     //220610 조숭진 user db 추가. 나중에 사용여부도 추가되어야 함.
                    bool bsuccess = false;
                    //if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    //{
                        bsuccess = GlobalData.Current.DBManager.DbSetProcedureUserInfo(user);
                    //}
                    //else
                    //{
                    //    bsuccess = true;
                    //}

                    if (bsuccess)
                    {
                        this[icnt] = user;

                        if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                        {
                            ClientSetProcedure(user, "UPDATE");

                            return new ResultUserControl()
                            {
                                bResult = true,
                                strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Update Complete").ToString(), user.UserID)
                            };
                        }
                        else
                        {
                            Current_userlistrefresh();
                            return null;
                        }
                    }
                    else
                    {
                        return new ResultUserControl()
                        {
                            bResult = false,
                            strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Update Fail").ToString(), user.UserID)
                        };
                    }
                }
                else
                {
                    return new ResultUserControl()
                    {
                        bResult = false,
                        strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is no Result").ToString(), user.UserID)
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultUserControl()
                {
                    bResult = false,
                    strResult = ex.ToString(),
                };
            }
        }

        public ResultUserControl Login(string id, string pw)
        {
            User user = GetUserByID(id);

            if (user == null)
            {
                return new ResultUserControl()
                {
                    bResult = false,
                    strResult = string.Format("ID {0} " + TranslationManager.Instance.Translate("is Not Exist").ToString(), id)
                };
            }

            if (!user.CheckPW(pw))
            {
                return new ResultUserControl()
                {
                    bResult = false,
                    strResult = TranslationManager.Instance.Translate("Password is Wrong").ToString(),
                };
            }

            //220823 HHJ SCS 개선     //- Not Use User LogIn 방지 추가
            if (!user.UserUse)
            {
                return new ResultUserControl()
                {
                    bResult = false,
                    strResult = TranslationManager.Instance.Translate("Not Use User").ToString(),
                };
            }

            #region 로그인 유저 권한 부여
            //230103 LoginUserAuthority TEST //SuHwan_20230711 유저 매니저에서 권한 처리 하자

            #region XML 기반 로직
            //GlobalData.Current.LoginUserAuthority.Clear();
            //if (user != null)
            //{
            //    XmlDocument xdoc = new XmlDocument();
            //    //xdoc.Load(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
            //    FileInfo File = new FileInfo(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
            //    if (!File.Exists)
            //    {
            //        xdoc.Load(GlobalData.Current.FilePathChange(GlobalData.Current.CurrentFilePaths(""), GlobalData.Current.AuthorityFilePath));
            //    }
            //    else
            //    {
            //        xdoc.Load(GlobalData.Current.CurrentFilePaths("") + GlobalData.Current.AuthorityFilePath);
            //    }
            //    XmlNodeList nodes = xdoc.SelectNodes("/AuthorityLevels/AuthorityLevel");

            //    foreach (XmlNode item_AuthorityLevel in nodes)
            //    {
            //        if (user.UserLevel.ToString() == item_AuthorityLevel.Attributes["Level"].Value)
            //        {
            //            foreach (XmlNode item_Authority in item_AuthorityLevel)
            //            {
            //                //보기 권한 저장
            //                if (Convert.ToBoolean(item_Authority.Attributes["Read"].Value))
            //                {
            //                    string LoginUserAuthority = "Read" + item_Authority.Attributes["Name"].Value;
            //                    GlobalData.Current.LoginUserAuthority.Add(LoginUserAuthority);
            //                }
            //                //수정 권한 저장
            //                if (Convert.ToBoolean(item_Authority.Attributes["Modify"].Value))
            //                {
            //                    string LoginUserAuthority = "Modify" + item_Authority.Attributes["Name"].Value;
            //                    GlobalData.Current.LoginUserAuthority.Add(LoginUserAuthority);
            //                }
            //            }
            //        }
            //    }
            //}
            #endregion

            #region DB 기반 로직
            if (user != null)
            {
                GlobalData.Current.AuthorityMng.UpdateLoginUserAuthority(user.UserLevel, GlobalData.Current.LoginUserAuthority);
            }
            #endregion
            #endregion


            CurrentUser = user;
            CurrentUser.SetLoginTime();
            OnLoginUserChange(CurrentUser);     //220406 HHJ SCS 개선     //- Login Event 추가
            bFirstLogin = true;
            return new ResultUserControl()
            {
                bResult = true,
                strResult = TranslationManager.Instance.Translate("Success Login").ToString(),
            };
        }
        //220406 HHJ SCS 개선     //- Login Event 추가
        public ResultUserControl Logout()
        {
            CurrentUser = null;
            OnLoginUserChange(CurrentUser);

            return new ResultUserControl()
            {
                bResult = true,
                strResult = TranslationManager.Instance.Translate("Success Logout").ToString(),
            };
        }
        /// <summary>
        /// 유저 추가 제거 진행 가능여부
        /// 유저 추가 제거는 Level이 매니저, 관리자(어드민)만 가능하도록 한다.
        /// </summary>
        /// <returns></returns>
        public bool PossibleUserControl()
        {
            return CurrentUser == null ?
                false :
                (CurrentUser.UserLevel.Equals(eUserLevel.Admin) || CurrentUser.UserLevel.Equals(eUserLevel.Manager));
        }

        private void ClientSetProcedure(User user, string DataValue)
        {
            ClientReqList reqBuffer = new ClientReqList
            {
                EQPID = GlobalData.Current.EQPID,
                CMDType = DataValue,
                Target = "USER",
                TargetID = string.Empty,
                TargetValue = JsonConvert.SerializeObject(user),
                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Requester = eServerClientType.Client,
                JobID = string.Empty,
            };

            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);
        }

        public bool CheckFirstUILoginReady()
        {
            int EventSubscriptor = 6;//권한이 필요한 UI 요소획득이 어렵기에 나이브하게 하드코딩해둔다.
            bool Ready = (EventSubscriptor == OnLoginUserChange.GetInvocationList().Count() && !bFirstLogin);
            return Ready;
        }
    }

    public class ResultUserControl
    {
        public bool bResult;
        public string strResult;
    }
}
