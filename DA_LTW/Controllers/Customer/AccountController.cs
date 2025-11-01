using CongNghePhanMen.Models;
using DA_LTW.Helper;
using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
namespace CongNghePhanMen.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Display(Name = "Họ tên")]
        public string full_name { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string phone_number { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email")]
        public string email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 50 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string password { get; set; }


    }


    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string password { get; set; }
    }


    //public class ForgotPasswordViewModel
    //{
    //    [Required(ErrorMessage = "Email không được để trống.")]
    //    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    //    [Display(Name = "Email")]
    //    public string email { get; set; }
    //}



    public class ResetPasswordViewModel
    {
        //[Required(ErrorMessage = "Token xác thực không hợp lệ.")]
        public string token { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 50 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string newPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        public string confirmPassword { get; set; }
    }
}

namespace DA_LTW.Controllers.Customer
{
    public class AccountController : Controller
    {
        private readonly db_cnpmEntities data = new db_cnpmEntities();
        // GET: Account

        public ActionResult Index()
        {
            return View();
           
        }
        public ActionResult Register()
        {
            return View();
        }
        public ActionResult Login()
        {

            return View();
        }
        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Form không hợp lệ → quay lại view và hiển thị lỗi
                return View(model);
            }
            string hashPass = HashHelper.HashPassword(model.password);


            var user = data.users.FirstOrDefault(u => u.email == model.email && u.password == hashPass && u.is_active == true);

            if (user != null)
            {
                FormsAuthentication.SetAuthCookie(user.email, false);
                Session["UserId"] = user.id;
                Session["UserName"] = user.full_name;

                // Lấy vai trò
                var role = (from ur in data.user_roles
                            join r in data.roles on ur.role_id equals r.id
                            where ur.user_id == user.id
                            select r.code).FirstOrDefault();

                Session["UserRole"] = role ?? "CUSTOMER";

                // Chuyển hướng
                if (role == "ADMIN")
                    return RedirectToAction("Index", "_LayoutAdmin");
                else
                    return RedirectToAction("Index", "Account");
            }

            ViewBag.Error = "Email hoặc mật khẩu không đúng!";


            return View();
        }

        public ActionResult Logout()
        {
            return View();
        }
        public ActionResult ForgotPassword()
        {
            return View();
        }
        public ActionResult ResetPassword()
        {
            return View();
        }
    }
}