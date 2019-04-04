using ADAccountManage.Model;
using ADAccountManage.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ADAccountManage.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrators")]
    public class UsersController : Controller
    {
        //
        // GET: /Admin/Users/
        public ActionResult Index()
        {
            IADAccountManagement adManage = ServiceProxy.CreateADManager();
            var model = adManage.GetADUsers();
            return View(model);
        }

        public ActionResult Details(string name)
        {
            IADAccountManagement adManage = ServiceProxy.CreateADManager();
            var user = adManage.GetUser(name);
            if (user == null)
                ViewBag.ErrorMessage = "User Not Found.";
            else
            {
                SecrecyQuestion qs = HtmlExtensions.GetSecrecyAnswer(user.Name);
                if (qs != null)
                {
                    user.SecrecyQuestion = qs.Question;
                    user.SecrecyAnswer = qs.Answer;
                }
            }
            return View(user);
        }

        //
        // GET: /Home/Create
        public ActionResult Create()
        {
            ViewBag.Title = "创建新用户";
            ViewBag.IsCreate = true;
            return View();
        }

        //
        // POST: /Home/Create
        [HttpPost]
        public ActionResult Create(ADUser user)
        {
            if (!ModelState.IsValid)
                return View(user);

            ViewBag.Title = "创建新用户";
            ViewBag.IsCreate = true;
            try
            {
                IADAccountManagement ad = ServiceProxy.CreateADManager();
                IResult result = ad.CreateUser(user);
                ViewBag.Message = result.ResultMessage;
                if (result.ResultException != null)
                    ViewBag.ErrorMessage = (result.ResultException.Message + "\r\n" + result.ResultException.StackTrace);

                if (result.ResultStatus)
                {
                    HtmlExtensions.SaveSecrecyQuestion(user.Name, user.SecrecyQuestion, user.SecrecyAnswer);
                    return RedirectToAction("Index");
                }
                else
                {
                    return View(user);
                }
            }
            catch (Exception e)
            {
                ViewBag.Message = e.Message;
                ViewBag.ErrorMessage = e.StackTrace;
                return View(user);
            }
        }

        //
        // GET: /Home/Edit/5
        public ActionResult Edit(string name)
        {
            ViewBag.Title = "编辑用户资料: " + name;
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
        public ActionResult Edit(string name, ADUser user)
        {
            try
            {
                IADAccountManagement ad = ServiceProxy.CreateADManager();
                IResult result = ad.UpdateUser(user);
                ViewBag.Message = result.ResultMessage;
                if (result.ResultException != null)
                    ViewBag.ErrorMessage = (result.ResultException.Message + "\r\n" + result.ResultException.StackTrace);

                if (result.ResultStatus)
                {
                    HtmlExtensions.SaveSecrecyQuestion(user.Name, user.SecrecyQuestion, user.SecrecyAnswer);
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

        public ActionResult ResetPassword(string name)
        {
            IADAccountManagement ad = ServiceProxy.CreateADManager();
            ADUser user = ad.GetUser(name);
            if (user == null)
                ViewBag.ErrorMessage = "User Not Found.";
            if (user != null)
            {
                ViewBag.UserName = user.Name;

                SecrecyQuestion qs = HtmlExtensions.GetSecrecyAnswer(user.Name);
                ViewBag.SecrecyQuestion = qs != null ? qs.Question : "";
            }
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(string name, string question, string answer, string newPassword)
        {
            try
            {
                ViewBag.UserName = name;
                ViewBag.SecrecyQuestion = question;

                // 验证密保和答案
                if (!QuestionAnswerValid(name, answer))
                {
                    ViewBag.ErrorMessage = "密保问题和答案不一致。";
                    return View();
                }
                IADAccountManagement ad = ServiceProxy.CreateADManager();
                IResult result = ad.ResetPassword(name, answer, newPassword);
                ViewBag.Message = result.ResultMessage;
                if (result.ResultException != null)
                    ViewBag.ErrorMessage = (result.ResultException.Message + "\r\n" + result.ResultException.StackTrace);

                if (result.ResultStatus)
                {
                    return RedirectToAction("Details", new { name = name });
                }
                else
                {
                    return View();
                }
            }
            catch (Exception e)
            {
                ViewBag.Message = e.Message;
                ViewBag.ErrorMessage = e.StackTrace;
                return View();
            }
        }

        private bool QuestionAnswerValid(string name, string answer)
        {
            SecrecyQuestion qs = HtmlExtensions.GetSecrecyAnswer(name);
            if (qs != null)
            {
                return qs.Answer == answer;
            }
            return false;
        }
    }
}
