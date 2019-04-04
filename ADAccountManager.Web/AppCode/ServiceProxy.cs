using ADAccountManage.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ADAccountManage.Web
{
    public class ServiceProxy
    {
        internal static IADAccountManagement CreateADManager()
        {
            var appSetting = ConfigurationManager.AppSettings;
            IADAccountManagement adManage = new DirectoryEntryLDAP( //new DomainPrincipalManage(
                appSetting["ADServer"], //"192.168.3.101",
                appSetting["ADContainer"], //"DC=k29server",
                appSetting["AdminAccount"], //@"K29Server\Administrator", 
                appSetting["AdminPwd"] //"1qaz@wsx"
                );
            return adManage;
        }

    }
}