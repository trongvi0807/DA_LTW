using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DA_LTW.Models;
using System.Data.Entity;

namespace DA_LTW.Controllers.Customer
{
    public class LoginController : Controller
    {
        private db_cnpmEntities db = new db_cnpmEntities();

        [HttpGet]
        public ActionResult Index()
        {
            if (Session["User"] != null)
            {
                return RedirectToAction("Index", "HomeCustomer");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // chống tấn công CSRF
        public ActionResult Login(string email, string password, string ReturnUrl)
        {
            if (ModelState.IsValid)
            {
                // 1. Tìm người dùng
                var user = db.users.FirstOrDefault(u => u.email.Equals(email));

                if (user != null)
                {
                    if (user.password.Equals(password)) 
                    {

                        var userRoleCode = db.user_roles
                                             .Where(ur => ur.user_id == user.id)
                                             .Select(ur => ur.role.code)
                                             .FirstOrDefault();

                        if (string.IsNullOrEmpty(userRoleCode))
                        {
                            ModelState.AddModelError("", "Tài khoản không được gán vai trò.");
                            return View("Index");
                        }

                        Session["User"] = user;
                        Session["UserId"] = user.id;
                        Session["FullName"] = user.full_name;
                        Session["RoleCode"] = userRoleCode; 


                        if (userRoleCode == "ADMIN")
                        {
                            return RedirectToAction("Index", "OrderManagement", new { area = "Admin" });
                        }
                        else
                        {
                            return RedirectToAction("Index", "HomeCustomer");
                        }

                    } 
                    else
                    {
                        ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                    }
                }
                else
                {
     
                    ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                }
            }

            return View("Index");
        }
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Login");
        }

    }
}