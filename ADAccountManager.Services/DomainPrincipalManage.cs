using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADAccountManage.Model;

namespace ADAccountManage.Services
{
    public class DomainPrincipalManage : IADAccountManagement
    {
        string _server;
        string _administratorAccount;
        string _adminPassword;
        string _container;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        /// <param name="server">AD服务器地址，可以是IP地址、Domain Name等</param>
        /// <param name="administrator">管理员帐号</param>
        /// <param name="password">管理员密码</param>
        public DomainPrincipalManage(string server, string container, string administrator, string password)
        {
            _server = server;
            if (!string.IsNullOrWhiteSpace(container))
                this._container = container;
            if (!string.IsNullOrWhiteSpace(administrator) && !string.IsNullOrWhiteSpace(password))
            {
                _administratorAccount = administrator;
                _adminPassword = password;
            }
        }

        private PrincipalContext CreatePrincipalContext()
        {
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain, _server, _container, _administratorAccount, _adminPassword);
            return ctx;
        }

        public AccountResult LogOn(string name, string password)
        {
            AccountResult result = new AccountResult();


            return result;
        }

        public IList<ADUser> GetADUsers()
        {
            using (PrincipalContext ctx = CreatePrincipalContext())
            {
                GroupPrincipal gp = GroupPrincipal.FindByIdentity(ctx, "Domain Users");

                PrincipalCollection col = gp.Members;
                List<ADUser> list = new List<ADUser>();
                foreach (UserPrincipal r in col)
                    list.Add(PopulationADUser(r));

                return list;

                /*UserPrincipal userQuery = new UserPrincipal(ctx);
                PrincipalSearcher search = new PrincipalSearcher(userQuery);
                PrincipalSearchResult<Principal> result = search.FindAll();
                var users = from r in result
                            select PopulationADUser((UserPrincipal)r);
                search.Dispose();
                return users.ToList();*/
            }
        }

        public ADUser GetUser(string name)
        {
            using (PrincipalContext ctx = CreatePrincipalContext())
            {
                UserPrincipal usr = UserPrincipal.FindByIdentity(ctx, name);
                if (usr != null)
                {
                    return PopulationADUser(usr);
                }
                return null;
            }
        }

        private ADUser PopulationADUser(UserPrincipal user)
        {
            ADUser adUser = new ADUser();
            adUser.Name = user.Name;
            adUser.SamAccountName = user.SamAccountName;
            adUser.Surname = user.Surname;
            adUser.GivenName = user.GivenName;
            adUser.MiddleName = user.MiddleName;
            adUser.DisplayName = user.DisplayName;
            adUser.Description = user.Description;
            adUser.EmailAddress = user.EmailAddress;
            adUser.DistinguishedName = user.DistinguishedName;
            adUser.LastLogon = user.LastLogon;
            if (user.GetUnderlyingObjectType() == typeof(DirectoryEntry))
            {
                DirectoryEntry entry = (DirectoryEntry)user.GetUnderlyingObject();
                adUser.SecrecyQuestion = Convert.ToString(entry.Properties["SecrecyQuestion"].Value);
                adUser.SecrecyAnswer = Convert.ToString(entry.Properties["SecrecyAnswer"].Value);
            }
            return adUser;
        }

        public IResult CreateUser(ADUser user)
        {
            using (PrincipalContext ctx = CreatePrincipalContext())
            {
                IResult result = new GeneralResult();

                UserPrincipal usr = UserPrincipal.FindByIdentity(ctx, user.SamAccountName);
                if (usr != null)
                {
                    result.ResultStatus = false;
                    result.ResultMessage = "'" + user.SamAccountName + "' already exists. Please use a different User Logon Name.";
                    return result;
                }
                return SaveUserPrincipal(true, user, new UserPrincipal(ctx));
            }
        }

        public IResult UpdateUser(ADUser user)
        {
            using (PrincipalContext ctx = CreatePrincipalContext())
            {
                IResult result = new GeneralResult();

                UserPrincipal usr = UserPrincipal.FindByIdentity(ctx, user.SamAccountName);
                if (usr == null)
                {
                    result.ResultStatus = false;
                    result.ResultMessage = "Can not find User'" + user.SamAccountName + "'";
                    return result;
                }
                return SaveUserPrincipal(false, user, usr);
            }
        }

        private IResult SaveUserPrincipal(bool isCreate, ADUser user, UserPrincipal usr)
        {
            IResult result = new GeneralResult();
            usr.Surname = user.Surname;
            usr.GivenName = user.GivenName;
            usr.MiddleName = user.MiddleName;
            usr.DisplayName = user.DisplayName;
            usr.Description = user.Description;
            usr.EmailAddress = user.EmailAddress;
            if (isCreate)
            {
                usr.Name = user.Name;
                usr.SamAccountName = user.SamAccountName;
                usr.SetPassword(user.Password);
                usr.Enabled = true;
                usr.PasswordNeverExpires = false;
            }
            usr.UnlockAccount();
            usr.PasswordNeverExpires = true;
            //usr.ExtensionSet("otherHomePhone", value);
            var oprateKey = isCreate ? "create" : "update";
            try
            {
                usr.Save();

                // 扩展属性
                if (usr.GetUnderlyingObjectType() == typeof(DirectoryEntry))
                {
                    DirectoryEntry entry = (DirectoryEntry)usr.GetUnderlyingObject();
                    if (!string.IsNullOrWhiteSpace(user.SecrecyQuestion))
                        entry.Properties["SecrecyQuestion"].Value = user.SecrecyQuestion;
                    if (!string.IsNullOrWhiteSpace(user.SecrecyAnswer))
                        entry.Properties["SecrecyAnswer"].Value = user.SecrecyAnswer;

                    entry.CommitChanges();
                }

                result.ResultStatus = true;
                result.ResultMessage = oprateKey + " user successful!";
            }
            catch (Exception e)
            {
                result.ResultStatus = false;
                result.ResultMessage = "Exception occurred while " + oprateKey + " user: " + e.Message;
                result.ResultException = e;
            }

            usr.Dispose();

            return result;
        }

        public IResult ChangePassword(string userName, string oldPassword, string newPassword)
        {
            IResult result = new GeneralResult();
            using (PrincipalContext ctx = CreatePrincipalContext())
            {
                try
                {
                    UserPrincipal usr = UserPrincipal.FindByIdentity(ctx, userName);
                    if (usr != null)
                    {
                        usr.ChangePassword(oldPassword, newPassword);
                        usr.Save();
                        result.ResultStatus = true;
                        result.ResultMessage = "Change Password Successful!";
                    }
                    else
                    {
                        result.ResultStatus = false;
                        result.ResultMessage = "Can not find User'" + userName + "'";
                    }
                }
                catch (Exception e)
                {
                    result.ResultStatus = false;
                    result.ResultMessage = "Exception occurred while changing password: " + e.Message;
                    result.ResultException = e;
                }
            }

            return result;
        }

        public IResult ResetPassword(string userName, string answer, string newPassword)
        {
            IResult result = new GeneralResult();
            using (PrincipalContext ctx = CreatePrincipalContext())
            {
                try
                {
                    UserPrincipal usr = UserPrincipal.FindByIdentity(ctx, userName);
                    if (usr != null)
                    {
                        string validAnswer = "";
                        if (string.Equals(validAnswer, answer))
                        {
                            usr.SetPassword(newPassword);
                            usr.Save();
                            result.ResultStatus = true;
                            result.ResultMessage = "Reset Password Successful!";
                        }
                        else
                        {
                            result.ResultStatus = false;
                            result.ResultMessage = "Reset password failes, answer is invalid!";
                        }
                    }
                    else
                    {
                        result.ResultStatus = false;
                        result.ResultMessage = "Can not find user'" + userName + "'.";
                    }
                }
                catch (Exception e)
                {
                    result.ResultStatus = false;
                    result.ResultMessage = "Exception occurred while reset password: " + e.Message;
                    result.ResultException = e;
                }
            }

            return result;
        }

        public void CreateGroup(string name)
        {
            throw new NotImplementedException();
        }

        public void CreateOU(string name)
        {
            throw new NotImplementedException();
        }

    }
}
