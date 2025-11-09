using DA_LTW.Filters;
using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using DA_LTW.Filters;

namespace DA_LTW.Controllers.Admin
{
    [AdminAuthorize]
    public class OrderManagementController : Controller
    {
        // GET: OrderManagement
        
        private db_cnpmEntities db = new db_cnpmEntities();
        // Cải tiến Action Index (thêm tham số lọc)
        public ActionResult Index(string statusFilter = "PENDING_CONFIRMATION")
        {
            var orders = db.orders.Include("user").AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Tất cả")
            {
                // Chỉ lấy các đơn hàng có trạng thái được chọn
                orders = orders.Where(o => o.status == statusFilter);
            }

            // Sắp xếp đơn hàng mới nhất lên đầu
            orders = orders.OrderByDescending(o => o.created_at);

            return View(orders.ToList());
        }
        [HttpPost]
        // Trong OrderManagementController.cs

        public ActionResult UpdateStatus(int id, string status, string trackingNumber = null) // Thêm trackingNumber
        {
            var order = db.orders.FirstOrDefault(o => o.id == id);
            if (order == null) return HttpNotFound();

            string oldStatus = order.status; // Lưu trạng thái cũ

            order.status = status;
            order.updated_at = DateTime.Now;

            // Logic đặc biệt khi chuyển trạng thái sang Đang giao
            if (status == "SHIPPING" && !string.IsNullOrEmpty(trackingNumber))
            {
                order.tracking_number = trackingNumber;
            }

            db.SaveChanges();

            // 💡 GỬI EMAIL THÔNG BÁO CHO KHÁCH HÀNG
            if (oldStatus != status)
            {
                // Giả định bạn có một hàm SendEmailService
                // var customerEmail = order.user.email;
                // EmailService.SendOrderStatusUpdate(customerEmail, order.id, status);
                // Để gửi email, bạn cần tích hợp một thư viện/service như SmtpClient, SendGrid, v.v.
            }

            // Quay về trang chi tiết đơn hàng vừa cập nhật
            return RedirectToAction("Details", new { id = order.id });
        }
        // Trong OrderManagementController.cs
        public ActionResult Details(int? id)
        {
            if (id == null) return RedirectToAction("Index");

            // Cần tải thêm chi tiết đơn hàng (order_items) và thông tin sản phẩm (product)
            var order = db.orders
                .Include("user")
                .Include("order_items.product") // Lấy danh sách sản phẩm
                .FirstOrDefault(o => o.id == id);

            if (order == null) return HttpNotFound();

            // Tên của các trạng thái để đổ vào Dropdown trong View Details
            ViewBag.OrderStatuses = new List<string> {
        "PENDING_CONFIRMATION",
        "PROCESSING",
        "SHIPPING",
        "DELIVERED",
        "CANCELLED"
    };

            return View(order);
        }
    }
}