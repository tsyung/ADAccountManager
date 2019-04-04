using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ADAccountManage.Model
{
    /// <summary>
    /// 提供一个表示AD帐号的实体。
    /// 为什么不继承自UserPrincipal？UserPrincipal开放了多个AD操作的入口，而业务层应该向外界封闭这些入口。
    /// </summary>
    public class ADUser
    {
        /// <summary>
        /// 获取或设置此主体的名称。
        /// </summary>
        [DisplayName("名称")]
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置此主体的 SAM 帐户名。
        /// </summary>
        [DisplayName("帐户名")]
        [Required]
        public string SamAccountName { get; set; }

        /// <summary>
        /// 帐号密码。不能从AD中获取用户密码，这个属性通常用于创建帐号、修改密码时使用。
        /// </summary>
        [DisplayName("帐户密码")]
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DisplayName("确认密码")]
        //[Compare("Password", ErrorMessage = "密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; }

        /// <summary>
        /// 密保问题
        /// </summary>
        [DisplayName("密保问题")]
        [Required(ErrorMessage = "请选择一个密保问题")]
        public string SecrecyQuestion { get; set; }

        /// <summary>
        /// 密保问题的答案
        /// </summary>
        [DisplayName("密保问题答案")]
        [Required]
        public string SecrecyAnswer { get; set; }

        /// <summary>
        /// 获取或设置用户主体的姓氏。
        /// </summary>
        [DisplayName("姓氏")]
        public string Surname { get; set; }

        /// <summary>
        /// 获取或设置用户主体的名字。
        /// </summary>
        [DisplayName("名字")]
        public string GivenName { get; set; }

        /// <summary>
        /// 获取或设置用户主体的中间名。
        /// </summary>
        [DisplayName("中间名")]
        public string MiddleName { get; set; }

        /// <summary>
        /// 获取或设置此主体的显示名称。
        /// </summary>
        [DisplayName("显示名称")]
        public string DisplayName { get; set; }
        
        /// <summary>
        /// 获取或设置用户的描述。
        /// </summary>
        [DisplayName("用户描述")]
        public string Description { get; set; }

        [DisplayName("邮箱")]
        [RegularExpression(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9._]+\.[A-Za-z]{2,4}")]
        public string EmailAddress { get; set; }

        /// <summary>
        /// 获取此主体的可分辨名称 (DN)。
        /// </summary>
        [DisplayName("可分辨名称 (DN)")]
        public string DistinguishedName { get; set; }

        [DisplayName("是否启用")]
        public bool? Enabled { get; set; }
        /// <summary>
        /// 获取或设置一个可以为 null 的 DateTime，用于指定最后一次登录此帐户的日期和时间。
        /// </summary>
        [DisplayName("最后登录于")]
        public DateTime? LastLogon { get; set; }

    }
}
