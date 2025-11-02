using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DA_LTW.Controllers.Customer
{
    public class HomeCustomerController : Controller
    {

        private db_cnpmEntities db = new db_cnpmEntities();

        // Action này sẽ hiển thị trang chủ với danh sách sản phẩm
        public ActionResult Index()
        {
            // Lấy danh sách sản phẩm đang được bán, sắp xếp mới nhất lên đầu
            var products = db.products
                             .Where(p => p.is_active == true)
                             .OrderByDescending(p => p.created_at)
                             .ToList();

            // Truyền danh sách sản phẩm sang View
            return View(products);
        }
    }
}