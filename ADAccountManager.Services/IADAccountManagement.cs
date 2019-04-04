using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ADAccountManage.Model;

namespace ADAccountManage.Services
{
    public interface IADAccountManagement
    {
        IList<ADUser> GetADUsers();
        ADUser GetUser(string name);
        IResult CreateUser(ADUser user);
        IResult UpdateUser(ADUser user);
        IResult ChangePassword(string userName, string oldPassword, string newPassword);
        IResult ResetPassword(string userName, string answer, string newPassword);
        void CreateGroup(string name);
        void CreateOU(string name);

        AccountResult LogOn(string name, string password);
    }
}
