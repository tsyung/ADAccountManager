using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

namespace ADAccountManage.Web
{
    public static class HtmlExtensions
    {
        public static IEnumerable<SelectListItem> LoadSecrecyQuestion()
        {
            return new List<SelectListItem>()
            {
                new SelectListItem() { Text = "您最熟悉的童年好友名字是？", Value = "您最熟悉的童年好友名字是？" },
                new SelectListItem() { Text = "对您影响最大的人名字是？", Value = "对您影响最大的人名字是？" },
                new SelectListItem() { Text = "您的学号（或工号）是？", Value = "您的学号（或工号）是？" },
                new SelectListItem() { Text = "您的高中语文老师名字是？", Value = "您的高中语文老师名字是？" }
            };
        }

        //string cacheKey = "ADManageDemo.Web.HtmlExtensions.SecrecyQuestion";
        public static bool SaveSecrecyQuestion(string name, string question, string answer)
        {
            try
            {
                List<SecrecyQuestion> list = SecrecyQuestion.LoadSecrecyQuestion();
                if (list == null)
                    list = new List<SecrecyQuestion>();
                SecrecyQuestion current = list.Find(s => string.Equals(s.Name, name));
                if (current == null)
                {
                    list.Add(new SecrecyQuestion
                    {
                        Name = name,
                        Question = question,
                        Answer = answer
                    });
                }
                else
                {
                    current.Question = question;
                    current.Answer = answer;
                }
                SecrecyQuestion.SaveSecrecyQuestion(list);
                return true;
            }
            catch //(Exception e)
            {
                return false;
                //throw;
            }
        }

        public static SecrecyQuestion GetSecrecyAnswer(string name)
        {
            List<SecrecyQuestion> list = SecrecyQuestion.LoadSecrecyQuestion();
            var s = from u in list where u.Name == name select u;
            return s.FirstOrDefault();
        }
    }

    [Serializable]
    public class SecrecyQuestion
    {
        public string Name { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }

        static string filePath = "~/content/SecrecyAnswer.dat";
        internal static List<SecrecyQuestion> LoadSecrecyQuestion()
        {
            FileStream fs = new FileStream(HttpContext.Current.Server.MapPath(filePath), FileMode.OpenOrCreate);
            List<SecrecyQuestion> data = new List<SecrecyQuestion>();
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                var stream = bf.Deserialize(fs);

                if (stream != null)
                    data = (List<SecrecyQuestion>)stream;
            }
            catch
            {
            }
            finally
            {
                fs.Close();
            }
            return data;
        }

        internal static void SaveSecrecyQuestion(IList<SecrecyQuestion> data)
        {
            FileStream fs = new FileStream(HttpContext.Current.Server.MapPath(filePath), FileMode.Create);
            try
            {

                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, data);
            }
            catch
            {
            }
            finally
            {
                fs.Close();
            }
        }
    }
}