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
        public ActionResult PlaceOrder(
    // ✅ SỬA LỖI 1: Thêm 'shippingMethod'
    string fullName, string phone, string address,
    string shippingMethod, string paymentMethod, string discountCode)
        {
            int userId = Session["UserId"] != null ? (int)Session["UserId"] : 1;
            var cart = db.carts.Include(c => c.cart_items) // Tải kèm cart_items
                               .FirstOrDefault(c => c.user_id == userId && c.status == "ACTIVE");

            if (cart == null || !cart.cart_items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // ✅ SỬA LỖI 2: Tính toán lại mọi thứ ở Server
            decimal subtotal = cart.cart_items.Sum(i => i.sale_price * i.quantity);

            // Tính phí vận chuyển ở Server (KHÔNG tin tưởng JS)
            decimal shippingFee = 0;
            switch (shippingMethod)
            {
                case "standard":
                    shippingFee = 20000;
                    break;
                case "express":
                    shippingFee = 40000;
                    break;
                case "pickup":
                default:
                    shippingFee = 0;
                    break;
            }

            // Tính giảm giá
            decimal discount = 0;
            if (!string.IsNullOrEmpty(discountCode) && discountCode.ToUpper() == "SALE10")
            {
                discount = subtotal * 0.1m; // giảm 10% trên TẠM TÍNH
            }

            // Sử dụng Transaction để đảm bảo tất cả hoặc không gì cả
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // ✅ SỬA LỖI 3: Khớp với cấu trúc SQL (không set total_money)
                    var order = new order
                    {
                        user_id = userId,
                        order_date = DateTime.Now,

                        // Thông tin khách hàng
                        full_name = fullName,
                        phone_number = phone,
                        shipping_address = address,

                        // Thông tin tài chính (KHỚP VỚI CỘT COMPUTED)
                        subtotal_money = subtotal,
                        shipping_fee = shippingFee,
                        discount_amount = discount,
                        // 'total_money' sẽ được SQL tự động tính toán

                        // Thông tin thanh toán & vận chuyển
                        payment_method = paymentMethod, // "COD", "BANK_TRANSFER", "VNPAY"
                        shipping_method = shippingMethod, // "standard", "express"

                        // ✅ SỬS LỖI 4: Khớp với CHECK constraint
                        payment_status = (paymentMethod == "COD") ? "UNPAID" : "PENDING", // Nếu là COD thì chưa trả, nếu là bank thì chờ
                        status = "PENDING_CONFIRMATION", // Trạng thái xử lý

                        tracking_number = "", // Gán giá trị rỗng mặc định
                        // Timestamps
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };

                    db.orders.Add(order);

                    // ✅ SỬA LỖI 5 (QUAN TRỌNG): Chuyển cart_items sang order_items
                    foreach (var item in cart.cart_items)
                    {
                        var orderItem = new order_items
                        {
                            order_id = order.id, // Lấy ID của đơn hàng vừa tạo
                            product_id = item.product_id,
                            quantity = item.quantity,
                            price = item.sale_price, // "Snapshot" giá tại thời điểm mua
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        };
                        db.order_items.Add(orderItem);
                    }

                    // ✅ SỬA LỖI 6: Cập nhật trạng thái giỏ hàng
                    cart.status = "CONVERTED"; // Đã chuyển đổi
                    cart.updated_at = DateTime.Now;

                    db.SaveChanges(); // Lưu tất cả thay đổi (items và cart)

                    // Nếu mọi thứ thành công
                    transaction.Commit();

                    // ✅ Chuyển hướng đến trang cảm ơn
                    return RedirectToAction("Success", new { orderId = order.id }); // Gửi kèm ID đơn hàng
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi, rollback tất cả
                    transaction.Rollback();

                    // Log lỗi (quan trọng)
                    // Logger.Log(ex.Message); 

                    // Quay lại giỏ hàng với thông báo lỗi
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi đặt hàng. Vui lòng thử lại.";
                    return RedirectToAction("Index", "Cart");
                }
            }
        }

        // ------------------------------
        // GET: /Checkout/Success
        // ------------------------------
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