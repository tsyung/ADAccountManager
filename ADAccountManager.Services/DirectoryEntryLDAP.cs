using ADAccountManage.Model;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;

namespace ADAccountManage.Services
{
    public class DirectoryEntryLDAP : IADAccountManagement
    {
        string _server;
        string _administratorAccount;
        string _adminPassword;
        string _path;

        public DirectoryEntryLDAP(string server, string path, string adminAccount, string adminPassword)
        {
            this._server = server;
            this._path = path;
            this._administratorAccount = adminAccount;
            this._adminPassword = adminPassword;
        }

        private DirectoryEntry CreateDirectoryEntry()
        {
            // create and return new LDAP connection with desired settings  
            DirectoryEntry ldapConnection = new DirectoryEntry(this._server, this._administratorAccount, this._adminPassword, AuthenticationTypes.Secure);
            if (!string.IsNullOrWhiteSpace(this._path))
                ldapConnection.Path = this._path;
            ldapConnection.AuthenticationType = AuthenticationTypes.Secure;
            return ldapConnection;
        }


        public AccountResult LogOn(string name, string password)
        {
            AccountResult result = new AccountResult();

            // 首先用管理员帐号查找该用户，获取Path
            DirectoryEntry rootAD = CreateDirectoryEntry();

            try
            {

                DirectorySearcher search = new DirectorySearcher(rootAD);
                search.Filter = "(&(cn=" + name + "))";
                SearchResult sResult = search.FindOne();

                if (null == sResult)
                {
                    result.ResultStatus = AccountResultEnum.UserNotExist;
                    return result;
                }
                //string domain = sResult.Properties["domain"][0].ToString();

                DirectoryEntry userEntry = new DirectoryEntry(sResult.Path, name, password);

                DirectorySearcher usreSearch = new DirectorySearcher(userEntry);

                usreSearch.Filter = "(SAMAccountName=" + name + ")";
                usreSearch.PropertiesToLoad.Add("cn");
                SearchResult userResult = usreSearch.FindOne();

                if (null == userResult)
                {
                    result.ResultStatus = AccountResultEnum.PasswordValid;
                    return result;
                }
                string filterAttribute = (string)userResult.Properties["cn"][0];
                result.ResultStatus = AccountResultEnum.OK;
                result.Roles = GetUserGroups(result, userResult.Path, filterAttribute); 
            }
            catch (Exception ex)
            {
                result.ResultStatus = AccountResultEnum.Error;
                result.ResultException = ex;
                //throw new Exception("Error authenticating user. " + ex.Message);
            }

            return result;
        }

        public IList<string> GetUserGroups<T>(IResult<T> actionResult, string path, string filterAttribute)
        {
            DirectorySearcher search = new DirectorySearcher(path);
            search.Filter = "(cn=" + filterAttribute + ")";
            search.PropertiesToLoad.Add("memberOf");
            List<string> groupNames = new List<string>();

            try
            {
                SearchResult result = search.FindOne();
                int propertyCount = result.Properties["memberOf"].Count;
                string dn;
                int equalsIndex, commaIndex;

                for (int propertyCounter = 0; propertyCounter < propertyCount; propertyCounter++)
                {
                    dn = (string)result.Properties["memberOf"][propertyCounter];
                    equalsIndex = dn.IndexOf("=", 1);
                    commaIndex = dn.IndexOf(",", 1);
                    if (-1 == equalsIndex)
                    {
                        return null;
                    }
                    groupNames.Add(dn.Substring((equalsIndex + 1), (commaIndex - equalsIndex) - 1));
                }
            }
            catch (Exception ex)
            {
                if (actionResult != null)
                    actionResult.ResultException = ex;
                //throw new Exception("Error obtaining group names. " + ex.Message);
            }
            return groupNames;
        }

        private ADUser PopulationADUser(SearchResult result)
        {
            ADUser user = null;
            if (result != null)
            {
                Func<string, string> getResult = name =>
                {
                    if (result.Properties[name].Count > 0)
                    {
                        return result.Properties[name][0].ToString();
                    }
                    return null;
                };
                user = new ADUser();
                user.Name = getResult("Name");
                user.SamAccountName = getResult("SamAccountName");
                user.Surname = getResult("sn");
                user.GivenName = getResult("GivenName");
                user.MiddleName = getResult("MiddleName");
                user.DisplayName = getResult("DisplayName");
                user.Description = getResult("description");
                user.EmailAddress = getResult("mail");
                user.DistinguishedName = getResult("distinguishedname");
                if (result.Properties["LastLogon"].Count > 0)
                {
                    try
                    {
                        long ticks = Convert.ToInt64(getResult("LastLogon"));
                        if (ticks > 0)
                            user.LastLogon = (new DateTime(ticks)).ToLocalTime();
                    }
                    catch { }
                }
            }
            return user;
        }

        public IList<ADUser> GetADUsers()
        {
            List<ADUser> list = new List<ADUser>();
            try
            {
                DirectoryEntry rootAD = CreateDirectoryEntry();
                DirectorySearcher search = new DirectorySearcher();
                search.SearchRoot = rootAD;
                search.Filter = "(&(objectCategory=Person)(objectClass=user))";

                SearchResultCollection allUsers = search.FindAll();
                foreach (SearchResult result in allUsers)
                {
                    list.Add(this.PopulationADUser(result));
                    //if (result.Properties["cn"].Count > 0 && result.Properties[property].Count > 0)
                    //{
                    //    Console.WriteLine(String.Format("{0,-20} : {1}",
                    //                  result.Properties["cn"][0].ToString(),
                    //                  result.Properties[property][0].ToString()));
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught:\n\n" + e.ToString());
            }
            return list;
        }

        public Model.ADUser GetUser(string name)
        {
            DirectoryEntry rootAD = CreateDirectoryEntry();
            DirectorySearcher search = new DirectorySearcher(rootAD);
            search.Filter = "(&(cn=" + name + "))";
            SearchResult result = search.FindOne();
            ADUser user = this.PopulationADUser(result);
            return user;
        }

        public Model.IResult CreateUser(Model.ADUser user)
        {
            IResult returnResult = new GeneralResult();
            if (user == null || string.IsNullOrWhiteSpace(user.Name))
            {
                returnResult.ResultStatus = false;
                returnResult.ResultMessage = "User can not be null";
                return returnResult;
            }
            try
            {
                DirectoryEntry rootAD = CreateDirectoryEntry();
                // if exists
                DirectorySearcher deSearch = new DirectorySearcher();
                deSearch.SearchRoot = rootAD;
                deSearch.Filter = "(&(objectClass=user) (cn=" + user.Name + "))";
                SearchResultCollection results = deSearch.FindAll();
                if (results.Count > 0)
                {
                    returnResult.ResultStatus = false;
                    returnResult.ResultMessage = "User named '" + user.Name + "' is exists!";
                    return returnResult;
                }

                DirectoryEntry entryToCreate = rootAD.Children.Add("CN=" + user.Name, "user");

                // User name (domain based)   
                //entryToCreate.Properties["userprincipalname"].Add(user.Name + "@" + domain);

                // User name (older systems)  
                entryToCreate.Properties["samaccountname"].Add(user.SamAccountName);
                entryToCreate.Properties["sn"].Value = user.Surname;
                entryToCreate.Properties["GivenName"].Value = user.GivenName;
                entryToCreate.Properties["MiddleName"].Value = user.MiddleName;
                entryToCreate.Properties["DisplayName"].Value = user.DisplayName;
                entryToCreate.Properties["Description"].Value = user.Description;
                entryToCreate.Properties["mail"].Value = user.EmailAddress;

                entryToCreate.CommitChanges();

                // Set password
                object[] password = new object[] { user.Password };
                entryToCreate.Invoke("SetPassword", password);

                // enable account if requested (see http://support.microsoft.com/kb/305144 for other codes)   
                if (user.Enabled.HasValue && user.Enabled.Value)
                    entryToCreate.Invoke("Put", new object[] { "userAccountControl", "512" });

                // add user to specified groups  
                /*foreach (String thisGroup in groups)
                {
                    DirectoryEntry newGroup = myLdapConnection.Parent.Children.Find("CN=" + thisGroup, "group");
                    if (newGroup != null)
                        newGroup.Invoke("Add", new object[] { user.Path.ToString() });
                }*/


                /*
                 * 
                 * see: http://www.ianatkinson.net/computing/adcsharp.htm#ex5
                // set permissions on folder, we loop this because if the program  
                // tries to set the permissions straight away an exception will be  
                // thrown as the brand new user does not seem to be available, it takes  
                // a second or so for it to appear and it can then be used in ACLs  
                // and set as the owner  

                bool folderCreated = false;

                while (!folderCreated)
                {
                    try
                    {
                        // get current ACL  

                        DirectoryInfo dInfo = new DirectoryInfo(homeDir);
                        DirectorySecurity dSecurity = dInfo.GetAccessControl();

                        // Add full control for the user and set owner to them  

                        IdentityReference newUser = new NTAccount(domain + @"\" + username);

                        dSecurity.SetOwner(newUser);

                        FileSystemAccessRule permissions =
                           new FileSystemAccessRule(newUser, FileSystemRights.FullControl,
                                                    AccessControlType.Allow);

                        dSecurity.AddAccessRule(permissions);

                        // Set the new access settings.  

                        dInfo.SetAccessControl(dSecurity);
                        folderCreated = true;
                    }

                    catch (System.Security.Principal.IdentityNotMappedException)
                    {
                        Console.Write(".");
                    }

                    catch (Exception ex)
                    {
                        // other exception caught so not problem with user delay as   
                        // commented above  

                        Console.WriteLine("Exception caught:" + ex.ToString());
                        return 1;
                    }
                }  */

                entryToCreate.CommitChanges();
                entryToCreate.Dispose();

                returnResult.ResultStatus = true;
                returnResult.ResultMessage = "Update successful!";
                return returnResult;

            }
            catch (Exception e)
            {
                returnResult.ResultException = e;
                returnResult.ResultStatus = false;
                returnResult.ResultMessage = "Exception occurred while udpate user info: " + e.Message;
            }
            return returnResult;
        }

        public Model.IResult UpdateUser(Model.ADUser user)
        {
            IResult returnResult = new GeneralResult();
            if (user == null)
            {
                returnResult.ResultStatus = false;
                returnResult.ResultMessage = "User can not be null";
                return returnResult;
            }
            try
            {
                DirectoryEntry rootAD = CreateDirectoryEntry();
                DirectorySearcher search = new DirectorySearcher(rootAD);
                search.Filter = "(&(cn=" + user.Name + "))";
                SearchResult result = search.FindOne();
                if (result != null)
                {
                    DirectoryEntry entryToUpdate = result.GetDirectoryEntry();
                    if (entryToUpdate != null)
                    {
                        entryToUpdate.Properties["sn"].Value = user.Surname;
                        entryToUpdate.Properties["GivenName"].Value = user.GivenName;
                        entryToUpdate.Properties["MiddleName"].Value = user.MiddleName;
                        entryToUpdate.Properties["DisplayName"].Value = user.DisplayName;
                        entryToUpdate.Properties["Description"].Value = user.Description;
                        entryToUpdate.Properties["mail"].Value = user.EmailAddress;
                        entryToUpdate.CommitChanges();
                        entryToUpdate.Dispose();

                        returnResult.ResultStatus = true;
                        returnResult.ResultMessage = "Update successful!";
                        return returnResult;
                    }
                }
                returnResult.ResultStatus = false;
                returnResult.ResultMessage = "User can not be null";

            }
            catch (Exception e)
            {
                returnResult.ResultException = e;
                returnResult.ResultStatus = false;
                returnResult.ResultMessage = "Exception occurred while udpate user info: " + e.Message;
            }
            return returnResult;
        }

        public Model.IResult ChangePassword(string userName, string oldPassword, string newPassword)
        {

            IResult returnResult = new GeneralResult();
            try
            {
                DirectoryEntry rootAD = CreateDirectoryEntry();
                DirectorySearcher search = new DirectorySearcher(rootAD);
                search.Filter = "(&(cn=" + userName + "))";
                SearchResult result = search.FindOne();
                if (result != null)
                {
                    DirectoryEntry entryToUpdate = result.GetDirectoryEntry();
                    if (entryToUpdate != null)
                    {
                        object[] password = new object[] { oldPassword, newPassword };
                        entryToUpdate.Invoke("ChangePassword", password);
                        entryToUpdate.Properties["LockOutTime"].Value = 0;

                        entryToUpdate.CommitChanges();
                        entryToUpdate.Close();

                        returnResult.ResultStatus = true;
                        returnResult.ResultMessage = "Update successful!";
                        return returnResult;
                    }
                }
                returnResult.ResultStatus = false;
                returnResult.ResultMessage = "User can not be null";
            }
            catch (Exception e)
            {
                returnResult.ResultStatus = false;
                returnResult.ResultMessage = "Exception occurred while change password: " + e.Message;
                returnResult.ResultException = e;
            }
            return returnResult;
        }

        public Model.IResult ResetPassword(string userName, string answer, string newPassword)
        {
            IResult returnResult = new GeneralResult();
            try
            {
                DirectoryEntry rootAD = CreateDirectoryEntry();
                DirectorySearcher search = new DirectorySearcher(rootAD);
                search.Filter = "(&(cn=" + userName + "))";
                SearchResult result = search.FindOne();
                if (result != null)
                {
                    DirectoryEntry entryToUpdate = result.GetDirectoryEntry();
                    if (entryToUpdate != null)
                    {
                        object[] password = new object[] { newPassword };
                        entryToUpdate.Invoke("SetPassword", password);
                        entryToUpdate.Properties["LockOutTime"].Value = 0;

                        entryToUpdate.CommitChanges();
                        entryToUpdate.Close();

                        returnResult.ResultStatus = true;
                        returnResult.ResultMessage = "Update successful!";
                        return returnResult;
                    }
                }
                returnResult.ResultStatus = false;
                returnResult.ResultMessage = "User can not be null";
            }
            catch (Exception e)
            {
                returnResult.ResultStatus = false;
                returnResult.ResultMessage = "Exception occurred while reset password: " + e.Message;
                returnResult.ResultException = e;
            }
            return returnResult;
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
