// Trong DA_LTW.Controllers.Customer/OrderController.cs

// ... (các using và khai báo db)

using DA_LTW.Models;
using System.Linq;
using System.Web.Mvc;

public class OrderController : Controller
{
    private db_cnpmEntities db = new db_cnpmEntities();

    // GET: /Order/OrderHistory
    public ActionResult OrderHistory()
    {
        // 1. Lấy User ID hiện tại
        int currentUserId = Session["UserId"] != null ? (int)Session["UserId"] : 0;

        if (currentUserId == 0)
        {
            // Bảo mật: Nếu chưa đăng nhập, chuyển hướng đến trang Đăng nhập
            return RedirectToAction("Login", "Account");
        }

        // 2. Lấy tất cả đơn hàng của User này
        // Sắp xếp theo ngày đặt hàng mới nhất lên đầu
        var orders = db.orders
                       .Where(o => o.user_id == currentUserId)
                       .OrderByDescending(o => o.order_date)
                       .ToList();

        // 3. Truyền danh sách đơn hàng sang View
        return View(orders);
    }

    // GET: /Order/Tracking/{orderId}
    public ActionResult Tracking(int? orderId)
    {
        // 1. Nếu không có ID, chuyển hướng về trang LỊCH SỬ ĐƠN HÀNG mới tạo
        if (orderId == null)
        {
            // ✅ Đã sửa: Quay về trang Lịch sử đơn hàng
            return RedirectToAction("OrderHistory");
        }

        // 2. Lấy User ID hiện tại (code cũ đã đúng)
        int currentUserId = Session["UserId"] != null ? (int)Session["UserId"] : 0;
        if (currentUserId == 0) return RedirectToAction("Login", "Account");

        // 3. Lấy dữ liệu đơn hàng (code cũ đã đúng)
        var order = db.orders
                      .FirstOrDefault(o => o.id == orderId && o.user_id == currentUserId);

        if (order == null)
        {
            return HttpNotFound();
        }

        return View(order);
    }
}