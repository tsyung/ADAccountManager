using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ADAccountManage.Model
{
    /// <summary>
    /// 定义一组用于表示执行结果的属性
    /// </summary>
    public interface IResult<T>
    {
        T ResultStatus { get; set; }
        string ResultMessage { get; set; }
        Exception ResultException { get; set; }
    }

    /// <summary>
    /// 定义一个返回逻辑结果的执行结果
    /// </summary>
    public interface IResult : IResult<bool>
    {
    }

    /// <summary>
    /// 提供一个通用的执行结果对象
    /// </summary>
    public class GeneralResult : IResult
    {
        public bool ResultStatus { get; set; }
        public string ResultMessage { get; set; }
        public Exception ResultException { get; set; }
    }

    public class AccountResult : IResult<AccountResultEnum>
    {
        public AccountResultEnum ResultStatus { get; set; }
        public string ResultMessage { get; set; }
        public Exception ResultException { get; set; }
        public IList<string> Roles { get; set; }
    }

    public enum AccountResultEnum
    {
        UserNotExist,           // 用户不存在
        PasswordValid,          // 密码错误
        UserNameEmpty,          // 用户名为空
        PasswordIsNUll,         // 密码为空
        OK,                     // 登录成功
        Error
    }

}
