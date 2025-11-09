// Trong DA_LTW.Filters/AdminAuthorizeAttribute.cs (ví dụ)

using System.Web;
using System.Web.Mvc;

namespace DA_LTW.Filters // Đặt trong namespace của dự án của bạn
{
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // 1. Kiểm tra Session["RoleCode"]
            string roleCode = httpContext.Session["RoleCode"] as string;

            // 2. Chỉ cho phép nếu vai trò là ADMIN
            if (!string.IsNullOrEmpty(roleCode) && roleCode == "ADMIN")
            {
                return true;
            }

            return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (filterContext.HttpContext.Session["UserId"] == null)
            {
                // Chuyển hướng về trang đăng nhập
                filterContext.Result = new RedirectResult("/Login/Index");
            }
            else
            {
                // Đã đăng nhập nhưng không phải Admin, chuyển về trang chủ Khách hàng
                // Đã sửa đường dẫn để gọi Controller/Action cụ thể, an toàn hơn
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(new { controller = "HomeCustomer", action = "Index", area = "" })
                );
            }
        }
    }
}