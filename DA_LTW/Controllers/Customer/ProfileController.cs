using DA_LTW.Models;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration; 
using CloudinaryDotNet; 
using CloudinaryDotNet.Actions; 

namespace DA_LTW.Controllers.Customer
{
    public class ProfileController : Controller
    {
        private db_cnpmEntities db = new db_cnpmEntities();

        // Khai báo các biến Cloudinary
        private static Cloudinary cloudinary;

        // THÊM MỚI: Dùng Constructor để khởi tạo Cloudinary
        public ProfileController()
        {
            if (cloudinary == null)
            {
                var account = new Account(
                    ConfigurationManager.AppSettings["CloudinaryCloudName"],
                    ConfigurationManager.AppSettings["CloudinaryApiKey"],
                    ConfigurationManager.AppSettings["CloudinaryApiSecret"]
                );
                cloudinary = new Cloudinary(account);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult handleProfile(user updatedUser, HttpPostedFileBase avatarFile)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int userId = (int)Session["UserID"];

            // SỬA LẠI: Khi ModelState không hợp lệ, cần trả về View "Index" với model
            if (!ModelState.IsValid)
            {
                // Lấy lại thông tin user hiện tại để hiển thị lại form cho đúng
                var currentUser = db.users.Find(userId);
                // Gán lại các giá trị đã nhập để người dùng không phải nhập lại
                currentUser.full_name = updatedUser.full_name;
                currentUser.email = updatedUser.email;
                currentUser.phone_number = updatedUser.phone_number;
                return View("Index", currentUser);
            }

            var userInDb = db.users.Find(userId);
            if (userInDb == null)
            {
                return HttpNotFound();
            }

            // 1. Xử lý upload avatar lên Cloudinary nếu có file mới
            if (avatarFile != null && avatarFile.ContentLength > 0)
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(avatarFile.FileName, avatarFile.InputStream),
                    Folder = "products" // (Tùy chọn) Lưu vào thư mục cụ thể trên Cloudinary
                };

                var uploadResult = cloudinary.Upload(uploadParams);

                if (uploadResult.Error == null)
                {
                    userInDb.avatar = uploadResult.SecureUrl.ToString();
                }
                else
                {
                    ModelState.AddModelError("", "Lỗi khi tải ảnh lên. Vui lòng thử lại.");
                    return View("Index", userInDb);
                }
            }

            // 2. Cập nhật các thông tin khác
            userInDb.full_name = updatedUser.full_name;
            userInDb.phone_number = updatedUser.phone_number;
            userInDb.email = updatedUser.email;

            // 3. Cập nhật mật khẩu (NẾU người dùng nhập mật khẩu mới)
            if (!string.IsNullOrEmpty(updatedUser.password))
            {
                userInDb.password = updatedUser.password;
            }

            // 4. Lưu thay đổi vào database
            db.Entry(userInDb).State = EntityState.Modified;
            db.SaveChanges();

            // Cập nhật lại đối tượng user trong Session với thông tin mới nhất
            Session["User"] = userInDb;

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Index", "HomeCustomer");
        }
    }
}