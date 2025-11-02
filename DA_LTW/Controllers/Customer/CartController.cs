using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DA_LTW.Controllers.Customer
{
    public class CartController : Controller
    {
        private db_cnpmEntities db = new db_cnpmEntities();

        // --------------------------------------------------------------
        // 🧩 HÀM LẤY GIỎ HÀNG TỪ SESSION
        // --------------------------------------------------------------
        private List<cart_items> GetCart()
        {
            // Nếu session chưa có giỏ hàng => khởi tạo mới
            var cart = Session["Cart"] as List<cart_items>;
            if (cart == null)
            {
                cart = new List<cart_items>();
                Session["Cart"] = cart;
            }
            return cart;
        }

        // --------------------------------------------------------------
        // 🛒 THÊM SẢN PHẨM VÀO GIỎ HÀNG
        // --------------------------------------------------------------

        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            // Giả sử bạn đã có user đăng nhập, ví dụ:
            int userId = (int)Session["UserId"]; // hoặc truyền tạm userId = 1 để test

            // Tìm hoặc tạo giỏ hàng đang hoạt động
            var cart = db.carts.FirstOrDefault(c => c.user_id == userId && c.status == "ACTIVE");
            if (cart == null)
            {
                cart = new cart
                {
                    user_id = userId,
                    total_items = 0,
                    total_price = 0,
                    status = "ACTIVE",
                    created_at = DateTime.Now
                };
                db.carts.Add(cart);
                db.SaveChanges();
            }

            // Lấy thông tin sản phẩm
            var product = db.products.Find(productId);
            if (product == null)
            {
                return HttpNotFound("Không tìm thấy sản phẩm");
            }

            // Tìm item trong giỏ
            var existingItem = db.cart_items.FirstOrDefault(i => i.cart_id == cart.id && i.product_id == productId);
            if (existingItem != null)
            {
                existingItem.quantity += quantity;
                existingItem.updated_at = DateTime.Now;
            }
            else
            {
                db.cart_items.Add(new cart_items
                {
                    cart_id = cart.id,
                    product_id = product.id,
                    product_name = product.name,
                    image = product.thumbnail_url,
                    quantity = quantity,
                    original_price = product.original_price,
                    sale_price = product.sale_price,
                    created_at = DateTime.Now
                });
            }

            // Cập nhật tổng số lượng và tổng giá
            cart.total_items = db.cart_items
    .Where(i => i.cart_id == cart.id)
    .Select(i => (int?)i.quantity) // ép sang nullable int
    .Sum() ?? 0;

            cart.total_price = db.cart_items
                .Where(i => i.cart_id == cart.id)
                .Select(i => (decimal?)(i.sale_price * i.quantity)) // ép sang nullable decimal
                .Sum() ?? 0;

            db.SaveChanges();

            // Quay lại trang giỏ hàng
            return RedirectToAction("Index");
        }


        // --------------------------------------------------------------
        // 🧾 TRANG XEM GIỎ HÀNG
        // --------------------------------------------------------------
        public ActionResult Index()
        {
            int userId = (int)Session["UserId"];
            var cart = db.carts.FirstOrDefault(c => c.user_id == userId && c.status == "ACTIVE");
            if (cart == null)
                return View(new List<cart_items>());

            var items = db.cart_items.Where(i => i.cart_id == cart.id).ToList();
            return View(items);
        }

        // --------------------------------------------------------------
        // ❌ XÓA 1 SẢN PHẨM KHỎI GIỎ HÀNG
        // --------------------------------------------------------------
        public ActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.product_id == id);

            if (item != null)
            {
                cart.Remove(item);
                Session["Cart"] = cart;
            }

            return RedirectToAction("Index");
        }

        // --------------------------------------------------------------
        // 🧮 HIỂN THỊ TỔNG SỐ SẢN PHẨM (PARTIAL)
        // --------------------------------------------------------------
        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            int count = 0;

            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                var cart = db.carts.FirstOrDefault(c => c.user_id == userId && c.status == "ACTIVE");

                if (cart != null)
                    count = cart.cart_items.Count();
            }

            return PartialView("_CartSummary", count);
        }


        // --------------------------------------------------------------
        // 🧹 GIẢI PHÓNG NGUỒN LỰC
        // --------------------------------------------------------------
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


    }
}
