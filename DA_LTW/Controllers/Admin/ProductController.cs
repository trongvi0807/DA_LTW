using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DA_LTW.Models;
using DA_LTW.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace DA_LTW.Controllers.Customer
{
    public class ProductController : Controller
    {


        private db_cnpmEntities db = new db_cnpmEntities();
        private static Cloudinary cloudinary;

        public ProductController()
        {
            if (cloudinary == null)
            {
                var account = new Account(
                    ConfigurationManager.AppSettings["CloudinaryCloudName"],
                    ConfigurationManager.AppSettings["CloudinaryApiKey"],
                    ConfigurationManager.AppSettings["CloudinaryApiSecret"]
                );
                cloudinary = new Cloudinary(account);
            }
        }

        private void PopulateDropdowns(object selectedCategory = null, object selectedBrand = null)
        {
            // Dùng .OrderBy(c => c.name) để sắp xếp theo tên cho dễ nhìn
            var categories = db.categories.OrderBy(c => c.name).ToList();
            ViewBag.Categories = new SelectList(categories, "id", "name", selectedCategory);

            var brands = db.brands.OrderBy(b => b.name).ToList();
            ViewBag.Brands = new SelectList(brands, "id", "name", selectedBrand);
        }

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

        // GET: Admin/Product
        public ActionResult Index(string q = "", int page = 1, int pageSize = 10)
        {
            // BẮT ĐẦU BẰNG VIỆC LỌC CÁC SẢN PHẨM CÒN HOẠT ĐỘNG
            // Dựa trên model của bạn, 'is_active' là Nullable<bool>
            // nên ta dùng 'p.is_active == true' để lọc
            var products = db.products.Where(p => p.is_active == true).AsQueryable();

            // Tiếp tục lọc theo từ khóa tìm kiếm (nếu có)
            if (!string.IsNullOrEmpty(q))
            {
                products = products.Where(p => p.name.Contains(q) || p.sku.Contains(q));
            }

            // Sắp xếp
            products = products.OrderByDescending(p => p.created_at);

            // Phân trang
            var total = products.Count();
            var list = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Query = q;
            return View(list);
        }

        // GET: Admin/Product/Create
        public ActionResult Create()
        {
            PopulateDropdowns();
            return View(new product());
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(product model,
            HttpPostedFileBase UploadThumb,         // <-- Sửa tên từ 'imageFile'
            IEnumerable<HttpPostedFileBase> imageFiles, // <-- Thêm tham số này
            IEnumerable<int> imageOrders)             // <-- Thêm tham số này
        {
            // 1. Kiểm tra Validate Lỗi (giống code của bạn)
            if (!ModelState.IsValid)
            {
                // GỌI LẠI HÀM NÀY KHI VALIDATE LỖI
                PopulateDropdowns(model.category_id, model.brand_id);
                return View(model);
            }

            // 2. Sử dụng Transaction để đảm bảo an toàn
            // Nếu lưu sản phẩm thành công nhưng lưu ảnh lỗi, nó sẽ rollback (hủy) tất cả.
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 3. Xử lý ảnh đại diện (thumbnail)
                    if (UploadThumb != null && UploadThumb.ContentLength > 0)
                    {
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(UploadThumb.FileName, UploadThumb.InputStream),
                            Folder = "products" // Thư mục trên Cloudinary
                        };
                        var result = cloudinary.Upload(uploadParams);
                        if (result.Error == null)
                        {
                            model.thumbnail_url = result.SecureUrl.ToString();
                        }
                    }

                    // 4. Thiết lập thông tin và LƯU SẢN PHẨM (Lần 1)
                    // Phải lưu ở đây để lấy được 'model.id' cho bảng product_images
                    model.created_at = DateTime.Now;
                    model.updated_at = DateTime.Now; // Thêm luôn updated_at
                    model.is_active = true;
                    db.products.Add(model);
                    db.SaveChanges(); // <-- Lưu để lấy ID

                    // 5. Xử lý upload NHIỀU ảnh chi tiết (gallery)
                    if (imageFiles != null)
                    {
                        var filesList = imageFiles.ToList();
                        var ordersList = imageOrders != null ? imageOrders.ToList() : new List<int>();

                        for (int i = 0; i < filesList.Count; i++)
                        {
                            var file = filesList[i];
                            if (file != null && file.ContentLength > 0)
                            {
                                // Upload file lên Cloudinary
                                var imgUploadParams = new ImageUploadParams
                                {
                                    File = new FileDescription(file.FileName, file.InputStream),
                                    Folder = "products" 
                                };
                                var imgResult = cloudinary.Upload(imgUploadParams);

                                if (imgResult.Error == null)
                                {
                                    // Lấy thứ tự từ input (nếu không có thì dùng index + 1)
                                    int displayOrder = (ordersList.Count > i) ? ordersList[i] : (i + 1);

                                    // Tạo đối tượng product_images
                                    var productImage = new product_images
                                    {
                                        product_id = model.id, // <-- Lấy ID từ sản phẩm vừa lưu
                                        image_url = imgResult.SecureUrl.ToString(),
                                        display_order = displayOrder,
                                        created_at = DateTime.Now
                                    };
                                    db.product_images.Add(productImage);
                                }
                            }
                        }

                        // 6. LƯU THAY ĐỔI (Lần 2) - Lưu tất cả product_images
                        db.SaveChanges();
                    }

                    // 7. Nếu mọi thứ thành công, commit transaction
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Tạo sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // 8. Nếu có lỗi, rollback (hủy) tất cả thay đổi
                    transaction.Rollback();

                    // Báo lỗi cho người dùng và quay lại form
                    ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại. " + ex.Message);
                    PopulateDropdowns(model.category_id, model.brand_id);
                    return View(model);
                }
            }
        }

        // GET: Admin/Product/Edit/5
        public ActionResult Edit(int id)
        {
            var p = db.products.Find(id);
            if (p == null) return HttpNotFound();
            PopulateDropdowns(p.category_id, p.brand_id);
            return View("Create", p);
        }


        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(product model,
            HttpPostedFileBase thumbnailFile,
            IEnumerable<HttpPostedFileBase> imageFiles,
            IEnumerable<int> imageOrders)
        {
            // SỬA 3: Sửa khối 'if'
            if (!ModelState.IsValid)
            {
                // PHẢI GỌI LẠI HÀM NÀY
                PopulateDropdowns(model.category_id, model.brand_id);

                // PHẢI CHỈ ĐỊNH VIEW "Create"
                return View("Create", model);
            }

            // Bọc trong try-catch để bắt lỗi (ví dụ: SKU trùng lặp)
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var p = db.products.Find(model.id);
                    if (p == null) return HttpNotFound();

                    // SỬA 4: CẬP NHẬT TẤT CẢ CÁC TRƯỜNG TỪ FORM
                    p.name = model.name;
                    p.sku = model.sku;
                    p.slug = model.slug; // <-- Bị thiếu
                    p.description = model.description;
                    p.content = model.content; // <-- Bị thiếu
                    p.ingredients = model.ingredients; // <-- Bị thiếu
                    p.contraindications = model.contraindications; // <-- Bị thiếu
                    p.prescription_required = model.prescription_required; // <-- Bị thiếu

                    p.original_price = model.original_price; // <-- Bị thiếu
                    p.sale_price = model.sale_price;
                    p.stock_quantity = model.stock_quantity;

                    p.category_id = model.category_id; // <-- Bị thiếu
                    p.brand_id = model.brand_id; // <-- Bị thiếu

                    p.updated_at = DateTime.Now;

                    // SỬA 5: Dùng 'thumbnailFile'
                    if (thumbnailFile != null && thumbnailFile.ContentLength > 0)
                    {
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(thumbnailFile.FileName, thumbnailFile.InputStream),
                            Folder = "products"
                        };
                        var result = cloudinary.Upload(uploadParams);
                        if (result.Error == null) p.thumbnail_url = result.SecureUrl.ToString();
                    }

                    // SỬA 6: (Quan trọng) Xử lý cập nhật gallery ảnh
                    if (imageFiles != null && imageFiles.Any(f => f != null))
                    {
                        // Bước 1: Xóa hết ảnh gallery cũ (nếu bạn muốn thay thế)
                        var oldImages = db.product_images.Where(img => img.product_id == p.id);
                        db.product_images.RemoveRange(oldImages);

                        // Bước 2: Thêm ảnh mới (giống code Create)
                        var filesList = imageFiles.ToList();
                        var ordersList = imageOrders != null ? imageOrders.ToList() : new List<int>();

                        for (int i = 0; i < filesList.Count; i++)
                        {
                            var file = filesList[i];
                            if (file != null && file.ContentLength > 0)
                            {
                                var imgUploadParams = new ImageUploadParams { /* ... code upload ... */ };
                                var imgResult = cloudinary.Upload(imgUploadParams);

                                if (imgResult.Error == null)
                                {
                                    int displayOrder = (ordersList.Count > i) ? ordersList[i] : (i + 1);
                                    var productImage = new product_images
                                    {
                                        product_id = p.id,
                                        image_url = imgResult.SecureUrl.ToString(),
                                        display_order = displayOrder,
                                        created_at = DateTime.Now
                                    };
                                    db.product_images.Add(productImage);
                                }
                            }
                        }
                    }

                    // Lưu tất cả thay đổi
                    db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    // Bắt lỗi validation (giống lần trước)
                    transaction.Rollback();
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            ModelState.AddModelError("", $"Trường: '{validationError.PropertyName}' - Lỗi: '{validationError.ErrorMessage}'");
                        }
                    }
                    PopulateDropdowns(model.category_id, model.brand_id);
                    return View("Create", model);
                }
                catch (Exception ex)
                {
                    // Bắt lỗi chung
                    transaction.Rollback();
                    ModelState.AddModelError("", "Lỗi không mong muốn: " + ex.Message);
                    PopulateDropdowns(model.category_id, model.brand_id);
                    return View("Create", model);
                }
            }
        }



        // GET: Admin/Product/Delete/5
        public ActionResult Delete(int id)
        {
            var p = db.products.Find(id);
            if (p == null) return HttpNotFound();

            // Thay vì 'Remove', hãy set 'is_active' = false
            // db.products.Remove(p); // <-- KHÔNG DÙNG DÒNG NÀY

            p.is_active = false; // <-- THAY BẰNG DÒNG NÀY
            p.updated_at = DateTime.Now;

            db.Entry(p).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges(); // <-- Sẽ lưu thành công

            TempData["SuccessMessage"] = "Sản phẩm đã được ẩn (ngừng kinh doanh)!";
            return RedirectToAction("Index");
        }

    }
}