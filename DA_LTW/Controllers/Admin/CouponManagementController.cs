using DA_LTW.Filters; // Dùng lại bộ lọc AdminAuthorize bạn đã có
using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace DA_LTW.Controllers.Admin
{
    [AdminAuthorize] // Chặn người lạ, chỉ Admin mới vào được
    public class CouponManagementController : Controller
    {
        private db_cnpmEntities db = new db_cnpmEntities();

        // 1. Danh sách mã giảm giá
        public ActionResult Index()
        {
            // Lấy coupon kèm theo điều kiện của nó
            // Lưu ý: Giả định quan hệ 1-1 hoặc 1-n, ở đây ta lấy điều kiện đầu tiên để hiển thị
            var list = db.coupons.Include(c => c.coupon_conditions).OrderByDescending(c => c.created_at).ToList();
            return View(list);
        }

        // 2. Trang tạo mới (Giao diện)
        public ActionResult Create()
        {
            return View();
        }

        // 3. Xử lý tạo mới (Logic lưu 2 bảng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string code, string attribute, string operator_sign, decimal conditionValue, decimal discountAmount, string discountType)
        {
            // Kiểm tra mã đã tồn tại chưa
            if (db.coupons.Any(c => c.code == code))
            {
                ModelState.AddModelError("", "Mã giảm giá này đã tồn tại!");
                return View();
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // A. Lưu bảng Coupon
                    var coupon = new coupon();
                    coupon.code = code.ToUpper().Trim(); // Viết hoa mã
                    coupon.is_active = true;
                    coupon.created_at = DateTime.Now;
                    coupon.updated_at = DateTime.Now;

                    db.coupons.Add(coupon);
                    db.SaveChanges(); // Lưu để lấy ID

                    // B. Lưu bảng Condition
                    var condition = new coupon_conditions();
                    condition.coupon_id = coupon.id;
                    condition.attribute = attribute; // Ví dụ: MIN_ORDER_VALUE
                    condition.@operator = operator_sign; // Ví dụ: >=
                    condition.value = conditionValue.ToString(); // Ví dụ: 100000
                    condition.discount_amount = discountAmount;
                    condition.discount_type = discountType; // PERCENTAGE hoặc FIXED_AMOUNT
                    condition.created_at = DateTime.Now;
                    condition.updated_at = DateTime.Now;

                    db.coupon_conditions.Add(condition);
                    db.SaveChanges();

                    transaction.Commit();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                    return View();
                }
            }
        }

        // 4. Xóa Coupon
        public ActionResult Delete(int id)
        {
            var coupon = db.coupons.Find(id);
            if (coupon != null)
            {
                // Phải xóa điều kiện trước (Do khóa ngoại)
                var conditions = db.coupon_conditions.Where(c => c.coupon_id == id).ToList();
                db.coupon_conditions.RemoveRange(conditions);

                // Sau đó xóa coupon
                db.coupons.Remove(coupon);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // 5. Kích hoạt / Ngưng kích hoạt nhanh
        public ActionResult ToggleStatus(int id)
        {
            var coupon = db.coupons.Find(id);
            if (coupon != null)
            {
                coupon.is_active = !coupon.is_active; // Đảo ngược trạng thái
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}