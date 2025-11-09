using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DA_LTW.Models;
using System.Data.Entity; // Cần thiết cho Include (nếu bạn muốn dùng)

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

        // ✅ THÊM THAM SỐ ReturnUrl VÀO ĐÂY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password, string ReturnUrl)
        {
            if (ModelState.IsValid)
            {
                // 1. Tìm người dùng
                var user = db.users.FirstOrDefault(u => u.email.Equals(email));

                if (user != null)
                {
                    if (user.password.Equals(password)) // Vẫn khuyến nghị mã hóa mật khẩu
                    {
                        // Đăng nhập thành công!

                        // 2. ✅ TRUY VẤN VAI TRÒ (ROLE CODE)
                        var userRoleCode = db.user_roles
                                             .Where(ur => ur.user_id == user.id)
                                             .Select(ur => ur.role.code)
                                             .FirstOrDefault();

                        if (string.IsNullOrEmpty(userRoleCode))
                        {
                            ModelState.AddModelError("", "Tài khoản không được gán vai trò.");
                            return View("Index");
                        }

                        // 3. LƯU SESSION
                        Session["User"] = user;
                        Session["UserId"] = user.id;
                        Session["FullName"] = user.full_name;
                        Session["RoleCode"] = userRoleCode; // Lưu Role Code


                        // 4. ✅ PHÂN LUỒNG CHUYỂN HƯỚNG DỰA TRÊN VAI TRÒ
                        if (userRoleCode == "ADMIN")
                        {
                            // Kiểm tra ReturnUrl (được gửi từ AdminAuthorizeAttribute)
                            if (!string.IsNullOrEmpty(ReturnUrl))
                            {
                                // Chuyển về trang Admin bị chặn trước đó
                                return Redirect(ReturnUrl);
                            }
                            else
                            {
                                // Mặc định: Chuyển đến trang Dashboard Admin
                                return RedirectToAction("Index", "OrderManagement", new { area = "Admin" });
                            }
                        }
                        else
                        {
                            // Khách hàng: Chuyển hướng đến trang chủ
                            return RedirectToAction("Index", "HomeCustomer");
                        }

                    } // End if (user.password.Equals(password))
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

            // Nếu lỗi (invalid, sai mật khẩu, hoặc không tìm thấy user)
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