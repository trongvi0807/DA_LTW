using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DA_LTW.Controllers.Customer
{
    public class HomeCustomerController : Controller
    {
        // GET: Home
        private db_cnpmEntities data = new db_cnpmEntities();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Profile()
        {
            var result = data.users.FirstOrDefault();
            return View(result);
        }
    }
}