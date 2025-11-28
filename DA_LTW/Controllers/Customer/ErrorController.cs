using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DA_LTW.Controllers.Customer
{
    public class ErrorController : Controller
    {
        public ActionResult Page403()
        {
            Response.StatusCode = 403; // Set status code cho chuẩn SEO/Browser
            return View();
        }
    }
}