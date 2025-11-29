using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DA_LTW.Controllers.Customer
{
    public class RegisterController : Controller
    {

        private db_cnpmEntities db = new db_cnpmEntities();

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra xem email đã tồn tại trong CSDL chưa
                var existingUser = db.users.FirstOrDefault(u => u.email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Địa chỉ email này đã được sử dụng.");
                    return View("Index", model);
                }

                // 2. Tìm vai trò 'CUSTOMER' trong CSDL
                var customerRole = db.roles.FirstOrDefault(r => r.code == "CUSTOMER");
                if (customerRole == null)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: Không tìm thấy vai trò mặc định. Vui lòng thử lại sau.");
                    return View("Index", model);
                }

                // 3. Tạo một đối tượng User mới
                var newUser = new user
                {
                    full_name = model.FullName,
                    phone_number = model.PhoneNumber,
                    email = model.Email,
                    password = model.Password,
                    is_active = true,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                // 4. Thêm người dùng mới vào DbContext
                db.users.Add(newUser);

                // 5. Tạo bản ghi UserRole để gán vai trò cho người dùng mới
                var newUserRole = new user_roles
                {
                    user = newUser,
                    role_id = customerRole.id,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                db.user_roles.Add(newUserRole);

                db.SaveChanges();
                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Index", "Login");
            }
            return View("Index", model);
        }
    }
}