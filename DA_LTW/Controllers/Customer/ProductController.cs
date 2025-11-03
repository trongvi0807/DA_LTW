using DA_LTW.Models;
using DA_LTW.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace DA_LTW.Controllers.Customer
{
    public class ProductController : Controller
    {
        private db_cnpmEntities db = new db_cnpmEntities();
        // GET: Product
        public ActionResult Detail(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 1. Tìm sản phẩm dựa trên slug (dùng FirstOrDefault)
            var product = db.products.FirstOrDefault(p => p.slug == slug && p.is_active == true);

            if (product == null)
            {
                return HttpNotFound(); // Không tìm thấy sản phẩm, trả về lỗi 404
            }

            // 2. Lấy danh sách các ảnh liên quan, sắp xếp theo thứ tự
            var images = db.product_images
                           .Where(pi => pi.product_id == product.id)
                           .OrderBy(pi => pi.display_order)
                           .ToList();

            // 3. Giải mã (Deserialize) chuỗi JSON thành phần
            var ingredientsList = new List<IngredientViewModel>();
            if (!string.IsNullOrEmpty(product.ingredients))
            {
                try
                {
                    // Chuyển chuỗi JSON trong DB thành một danh sách các đối tượng
                    ingredientsList = JsonConvert.DeserializeObject<List<IngredientViewModel>>(product.ingredients);
                }
                catch
                {
                    // Nếu JSON bị lỗi, danh sách sẽ vẫn rỗng, không làm crash trang
                }
            }

            // 4. Tạo ViewModel và gán tất cả dữ liệu đã lấy được
            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                Images = images,
                IngredientsList = ingredientsList
            };

            // 5. Trả về View, truyền ViewModel chứa đầy đủ dữ liệu sang
            return View(viewModel);
        }
    }
}