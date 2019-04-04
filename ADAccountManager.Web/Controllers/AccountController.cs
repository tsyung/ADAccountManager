using ADAccountManage.Model;
using ADAccountManage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace ADAccountManage.Web.Controllers
{
    public class AccountController : Controller
    {

        public ActionResult LogOut()
        {
            Response.Cookies.Remove(FormsAuthentication.FormsCookieName);
            FormsAuthentication.SignOut();
            return Redirect("~/");
        }
        //
        // GET: /Account/LogOn

        public ActionResult LogOn()
        {
            if (!this.Request.IsAuthenticated)
            {
                return View();
            }
            return Redirect("~/");
        }

        [HttpPost]
        public ActionResult LogOn(string name, string password, bool isCookiePersistent = false)
        {
            if (!this.Request.IsAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Message = "用户名或密码不能为空";
                    return View();
                }

                IADAccountManagement adManage = ServiceProxy.CreateADManager();
                AccountResult result = adManage.LogOn(name, password);
                if (result != null && result.ResultStatus == AccountResultEnum.OK)
                {
                    //Create the ticket, and add the groups.
                    string userGroups = string.Join("|", result.Roles.ToArray());

                    FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(1, name, DateTime.Now,
                        DateTime.Now.AddMinutes(FormsAuthentication.Timeout.TotalMinutes), isCookiePersistent, userGroups);

                    //Encrypt the ticket.
                    string encryptedTicket = FormsAuthentication.Encrypt(authTicket);

                    //Create a cookie, and then add the encrypted ticket to the cookie as data.
                    HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);

                    if (true == isCookiePersistent)
                        authCookie.Expires = authTicket.Expiration;

                    //Add the cookie to the outgoing cookies collection.
                    Response.Cookies.Add(authCookie);

                    //You can redirect now.
                    return Redirect(FormsAuthentication.GetRedirectUrl(name, false));

                }
                else
                {
                    string message = "登录失败";
                    switch (result.ResultStatus)
                    {
                        case AccountResultEnum.UserNameEmpty:
                        case AccountResultEnum.PasswordIsNUll:
                            message += ", 用户名或密码不能为空";
                            break;
                        case AccountResultEnum.UserNotExist:
                            message += ", 没有找到与用户名‘" + name + "’ 匹配的用户";
                            break;
                        case AccountResultEnum.PasswordValid:
                            message += ", 指定的用户名和密码不匹配";
                            break;
                        default:
                            break;
                    }
                    ViewBag.Message = message;
                    if (result.ResultException != null)
                    {
                        ViewBag.ErrorMessage = result.ResultException.Message + "<br/>" + result.ResultException.StackTrace;
                    }
                    return View();
                }
            }

            return Redirect("~/");
        }

        [Authorize]
        public ActionResult Details()
        {
            string name = HttpContext.User.Identity.Name;
            IADAccountManagement adManage = ServiceProxy.CreateADManager();
            var user = adManage.GetUser(name);
            if (user == null)
                ViewBag.ErrorMessage = "User Not Found.";
            else
            {
                SecrecyQuestion qs = HtmlExtensions.GetSecrecyAnswer(name);
                if (qs != null)
                {
                    user.SecrecyQuestion = qs.Question;
                    user.SecrecyAnswer = qs.Answer;
                }
            }
            return View(user);
        }

        //
        // GET: /Home/Edit/5
        [Authorize]
        public ActionResult Edit()
        {
            string name = HttpContext.User.Identity.Name;
            ViewBag.Title = "编辑我的资料";
            ViewBag.IsCreate = false;
            IADAccountManagement adManage = ServiceProxy.CreateADManager();
            var user = adManage.GetUser(name);
            if (user == null)
            {
                ViewBag.ErrorMessage = "User Not Found.";
            }
            else
            {
                SecrecyQuestion sq = HtmlExtensions.GetSecrecyAnswer(user.Name);
                if (sq != null)
                {
                    user.SecrecyQuestion = sq.Question;
                    user.SecrecyAnswer = sq.Answer;
                }
            }
            return View("Create", user);
        }

        //
        // POST: /Home/Edit/5
        [HttpPost]
        [Authorize]
        public ActionResult Edit(ADUser user)
        {
            try
            {
                string name = HttpContext.User.Identity.Name;
                IADAccountManagement ad = ServiceProxy.CreateADManager();
                user.Name = name;
                IResult result = ad.UpdateUser(user);
                ViewBag.Message = result.ResultMessage;
                if (result.ResultException != null)
                    ViewBag.ErrorMessage = (result.ResultException.Message + "\r\n" + result.ResultException.StackTrace);

                if (result.ResultStatus)
                {
                    HtmlExtensions.SaveSecrecyQuestion(name, user.SecrecyQuestion, user.SecrecyAnswer);
                    return RedirectToAction("Details", new { name = name });
                }
                else
                {
                    return View("Create", user);
                }
            }
            catch (Exception e)
            {
                ViewBag.Message = e.Message;
                ViewBag.ErrorMessage = e.StackTrace;
                return View("Create");
            }
        }

        [Authorize]
        public ActionResult ChangePassword()
        {
            IADAccountManagement ad = ServiceProxy.CreateADManager();
            string name = HttpContext.User.Identity.Name;
            ADUser user = ad.GetUser(name);
            if (user == null)
                ViewBag.ErrorMessage = "User Not Found.";
            if (user != null)
            {
                ViewBag.UserName = user.Name;
            }
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            string name = HttpContext.User.Identity.Name;
            var routeData = new { name = name, oldPassword = oldPassword, newPassword = newPassword };
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Message = "原密码和新密码不能为空";
                return View(routeData);
            }
            if (!string.Equals(newPassword, confirmPassword))
            {
                ViewBag.Message = "新密码和确认新密码不匹配";
                return View(routeData);
            }
            try
            {
                IADAccountManagement ad = ServiceProxy.CreateADManager();
                IResult result = ad.ChangePassword(name, oldPassword, newPassword);
                //ViewBag.Message = result.ResultMessage;
                //if (result.ResultException != null)
                //    ViewBag.ErrorMessage = (result.ResultException.Message + "\r\n" + result.ResultException.StackTrace);
                SetMessageBag(ViewBag, result.ResultMessage, result.ResultException);

                if (result.ResultStatus)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return View(routeData);
                }
            }
            catch (Exception e)
            {
                //string message = e.Message;
                //string stackTrace = e.StackTrace;
                //if (e.InnerException != null)
                //{
                //    message += (" -> " + e.InnerException.Message);
                //    stackTrace += ("\r\n\t" + e.InnerException.StackTrace);
                //}
                //ViewBag.Message = message;
                //ViewBag.ErrorMessage = e.StackTrace;
                SetMessageBag(ViewBag, null, e);
                return View(routeData);
            }
        }

        static void SetMessageBag(dynamic viewBag, string message, Exception e)
        {
            if (e != null)
            {
                string expMessage = e.Message;
                string stackTrace = e.StackTrace;
                if (e.InnerException != null)
                {
                    expMessage += (" -> " + e.InnerException.Message);
                    stackTrace += ("\r\n\t" + e.InnerException.StackTrace);
                }
                viewBag.ErrorMessage = expMessage + "\r\n" + stackTrace;
            }
            else if(!string.IsNullOrWhiteSpace(message))
                viewBag.Message = message;
        }
    }
}
