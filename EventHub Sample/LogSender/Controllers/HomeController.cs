using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LogSender.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            Trace.Write(String.Format("Request on /Home/Index at {0}", Environment.MachineName));
            return View();
        }

        public ActionResult About()
        {
            Trace.Write(String.Format("Request on /Home/About at {0}", Environment.MachineName));
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            Trace.Write(String.Format("Request on /Home/Contact at {0}", Environment.MachineName));
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}