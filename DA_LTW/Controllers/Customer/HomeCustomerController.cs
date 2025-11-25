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
        public ActionResult Profile()
        {
            var result = db.users.FirstOrDefault();
            return View(result);
        }
        public ActionResult HT_DSDMSP()
        {
            var dsdm = db.categories.ToList();
            return PartialView(dsdm);
        }
        public ActionResult HT_DSSP_OFDM(int id)
        {
            var products = db.products
                             .Where(p => p.category_id == id && p.is_active == true)
                             .OrderByDescending(p => p.created_at)
                             .ToList();

            return View(products);
        }
        public ActionResult SP_DropDown (string category)
        {
            var products = db.products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.category.slug == category);
            }

            return View(products.ToList());
        }

    }
}