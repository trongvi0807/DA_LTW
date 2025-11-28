using System.Web;
using System.Web.Mvc;
using System.Web.Routing; // Cần thư viện này để dùng RouteValueDictionary

namespace DA_LTW.Filters
{
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        // 1. Kiểm tra quyền truy cập (Trả về True/False)
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Lấy session
            var userSession = httpContext.Session["User"];
            var roleCode = httpContext.Session["RoleCode"] as string;

            // Điều kiện để được vào: Phải có User và RoleCode là "ADMIN"
            if (userSession != null && !string.IsNullOrEmpty(roleCode) && roleCode == "ADMIN")
            {
                return true; // Cho phép truy cập
            }

            return false; // Từ chối truy cập -> Sẽ nhảy xuống hàm HandleUnauthorizedRequest
        }

        // 2. Xử lý khi bị từ chối truy cập
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Trường hợp 1: Chưa đăng nhập (Session Null)
            if (filterContext.HttpContext.Session["User"] == null)
            {
                // Chuyển hướng về trang Login + Kèm theo ReturnUrl
                // ReturnUrl giúp user sau khi login sẽ tự động quay lại trang Admin đang vào dở
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", "Login" },
                        { "action", "Index" },
                        { "ReturnUrl", filterContext.HttpContext.Request.RawUrl } // Lấy URL hiện tại
                    });
            }
            // Trường hợp 2: Đã đăng nhập nhưng KHÔNG PHẢI ADMIN
            else
            {
                // Chuyển hướng sang trang báo lỗi 403 (Forbidden)
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", "Error" }, // Tên Controller lỗi
                        { "action", "Page403" }    // Tên Action lỗi
                    });
            }
        }
    }
}