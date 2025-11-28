using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity; // Cần thiết cho Transaction

namespace DA_LTW.Controllers.Customer
{
    public class CheckOutController : Controller
    {
        private db_cnpmEntities db = new db_cnpmEntities();


        // ---------------------------------------------------------
        // 1. HÀM RIÊNG: TÍNH GIẢM GIÁ TỪ DB (Logic cốt lõi)
        // ---------------------------------------------------------
        private decimal GetDiscountValue(string code, decimal orderSubtotal, out string message)
        {
            message = "";
            if (string.IsNullOrEmpty(code)) return 0;

            // 1. Tìm coupon có tồn tại và đang active không
            var coupon = db.coupons.FirstOrDefault(c => c.code == code && c.is_active == true);
            if (coupon == null)
            {
                message = "Mã giảm giá không tồn tại hoặc đã hết hạn.";
                return 0;
            }

            // 2. Lấy điều kiện áp dụng (Giả sử 1 coupon có 1 điều kiện chính)
            var condition = db.coupon_conditions.FirstOrDefault(c => c.coupon_id == coupon.id);
            if (condition == null)
            {
                // Nếu không có điều kiện, cho phép áp dụng luôn (hoặc chặn tùy bạn)
                return 0;
            }

            // 3. Kiểm tra điều kiện (Attribute + Operator + Value)
            // Ví dụ: attribute="MIN_ORDER_VALUE", operator=">=", value="100000"
            bool isConditionMet = false;
            decimal conditionValue = decimal.Parse(condition.value); // Giá trị cột mốc (ví dụ 100k)

            // Logic so sánh đơn giản
            if (condition.attribute == "MIN_ORDER_VALUE")
            {
                if (condition.@operator == ">=" && orderSubtotal >= conditionValue) isConditionMet = true;
                else if (condition.@operator == ">" && orderSubtotal > conditionValue) isConditionMet = true;
            }
            // Bạn có thể mở rộng thêm các attribute khác ở đây

            if (!isConditionMet)
            {
                message = $"Đơn hàng chưa đủ điều kiện áp dụng mã này (Tối thiểu {conditionValue:N0}đ).";
                return 0;
            }

            // 4. Tính số tiền được giảm
            decimal discountAmount = 0;
            if (condition.discount_type == "PERCENTAGE")
            {
                // Giảm theo % (condition.discount_amount lưu số %, ví dụ 10)
                discountAmount = orderSubtotal * (condition.discount_amount / 100);

                // (Tùy chọn) Bạn có thể thêm logic "Giảm tối đa bao nhiêu tiền" nếu muốn
            }
            else if (condition.discount_type == "FIXED_AMOUNT")
            {
                // Giảm số tiền cố định (ví dụ 50000)
                discountAmount = condition.discount_amount;
            }

            // Đảm bảo không giảm quá giá trị đơn hàng
            return discountAmount > orderSubtotal ? orderSubtotal : discountAmount;
        }

        [HttpPost]
        public ActionResult ApplyCoupon(string code)
        {
            // Lấy lại subtotal từ session hoặc tính lại từ DB để bảo mật
            int userId = Session["UserId"] != null ? (int)Session["UserId"] : 1;
            var cart = db.carts.Include(c => c.cart_items).FirstOrDefault(c => c.user_id == userId && c.status == "ACTIVE");

            if (cart == null || !cart.cart_items.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống." });
            }

            decimal subtotal = cart.cart_items.Sum(i => i.sale_price * i.quantity);
            string msg = "";
            decimal discount = GetDiscountValue(code, subtotal, out msg);

            if (discount > 0)
            {
                return Json(new { success = true, discount = discount, message = "Áp dụng mã thành công!" });
            }
            else
            {
                return Json(new { success = false, message = msg });
            }
        }



        // ------------------------------
        // GET: /Checkout
        // ------------------------------
        public ActionResult Index()
        {
            // Lấy userId (tạm test với user = 1 nếu chưa có login)
            int userId = Session["UserId"] != null ? (int)Session["UserId"] : 1;

            // Lấy giỏ hàng hiện tại
            var cart = db.carts.FirstOrDefault(c => c.user_id == userId && c.status == "ACTIVE");
            if (cart == null)
            {
                ViewBag.Error = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy danh sách sản phẩm trong giỏ
            var items = db.cart_items.Where(i => i.cart_id == cart.id).ToList();

            // Truyền dữ liệu sang View
            ViewBag.Cart = cart;
            ViewBag.Items = items;

            return View();
        }

        // ------------------------------
        // POST: /Checkout/PlaceOrder
        // ------------------------------

        [HttpPost]
        public ActionResult PlaceOrder(string fullName, string phone, string address, string shippingMethod, string paymentMethod, string discountCode)
        {
            int userId = Session["UserId"] != null ? (int)Session["UserId"] : 1;
            var cart = db.carts.Include(c => c.cart_items).FirstOrDefault(c => c.user_id == userId && c.status == "ACTIVE");

            if (cart == null || !cart.cart_items.Any()) return RedirectToAction("Index", "Cart");

            // Tính Subtotal
            decimal subtotal = cart.cart_items.Sum(i => i.sale_price * i.quantity);

            // Tính Ship
            decimal shippingFee = (shippingMethod == "express") ? 40000 : (shippingMethod == "standard" ? 20000 : 0);

            // ✅ LOGIC MỚI: Tính giảm giá (Gọi hàm private, không tin tưởng Client)
            string msg = "";
            decimal discount = GetDiscountValue(discountCode, subtotal, out msg);

            // Nếu mã lỗi hoặc không hợp lệ, discount sẽ tự bằng 0 -> Vẫn cho đặt hàng nhưng không giảm giá
            // Hoặc bạn có thể return View và báo lỗi nếu muốn bắt buộc mã phải đúng.

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = new order
                    {
                        user_id = userId,
                        order_date = DateTime.Now,
                        full_name = fullName,
                        phone_number = phone,
                        shipping_address = address,
                        subtotal_money = subtotal,
                        shipping_fee = shippingFee,
                        discount_amount = discount, // ✅ Đã lưu đúng giá trị tính từ DB
                        payment_method = paymentMethod,
                        shipping_method = shippingMethod,
                        payment_status = (paymentMethod == "COD") ? "UNPAID" : "PENDING",
                        status = "PENDING_CONFIRMATION",
                        tracking_number = "",
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };

                    db.orders.Add(order);

                    // Chuyển items sang order_items
                    foreach (var item in cart.cart_items)
                    {
                        var orderItem = new order_items
                        {
                            order_id = order.id,
                            product_id = item.product_id,
                            quantity = item.quantity,
                            price = item.sale_price,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        };
                        db.order_items.Add(orderItem);
                    }

                    // Cập nhật giỏ hàng
                    cart.status = "CONVERTED";
                    cart.updated_at = DateTime.Now;

                    db.SaveChanges();
                    transaction.Commit();

                    return RedirectToAction("Success", new { orderId = order.id });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                    return RedirectToAction("Index", "Cart");
                }
            }
        }



        public ActionResult Success()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ChooseShipping(string shippingMethod)
        {
            // Giả sử có lưu session tạm thời
            Session["ShippingMethod"] = shippingMethod;

            // Sau khi chọn xong thì chuyển sang trang Checkout
            return RedirectToAction("Index", "Checkout");
        }

    }
}