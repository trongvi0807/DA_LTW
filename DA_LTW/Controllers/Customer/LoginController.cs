using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DA_LTW.Models;

namespace DA_LTW.Controllers.Customer
{
    public class LoginController : Controller
    {

        private db_cnpmEntities db = new db_cnpmEntities();

        [HttpGet]
        public ActionResult Index()
        {
            // Nếu người dùng đã đăng nhập rồi thì chuyển về trang chủ
            if (Session["User"] != null)
            {
                return RedirectToAction("Index", "HomeCustomer");
            }
            return View();
        }

        // Xử lí khi người dùng nhấn vào nút đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            if (ModelState.IsValid)
            {
                // 1. Tìm người dùng trong CSDL theo email
                var user = db.users.FirstOrDefault(u => u.email.Equals(email));
                if (user != null)
                {
                    if (user.password.Equals(password)) // Chỗ này phải so sánh mật khẩu đã mã hóa
                    {
                        // Đăng nhập thành công!
                        // Lưu thông tin người dùng vào Session
                        Session["User"] = user;
                        Session["UserId"] = user.id;
                        Session["FullName"] = user.full_name;

                        // Chuyển hướng đến trang chủ của khách hàng
                        return RedirectToAction("Index", "HomeCustomer");
                    }
                    else
                    {
                        // Sai mật khẩu
                        ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                    }
                }
                else
                {
                    // Không tìm thấy email
                    ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                }

            }
            // Nếu lỗi thì quay lại trang đăng nhập sử lí lỗi. 
            return View("Index");
        }
    }
}