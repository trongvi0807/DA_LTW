using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
            var categories = db.categories.OrderBy(c => c.name).ToList();
            ViewBag.Categories = new SelectList(categories, "id", "name", selectedCategory);

            var brands = db.brands.OrderBy(b => b.name).ToList();
            ViewBag.Brands = new SelectList(brands, "id", "name", selectedBrand);
        }


        public ActionResult Index(string q = "", int page = 1, int pageSize = 10)
        {
   
            var products = db.products.Where(p => p.is_active == true).AsQueryable();

            // tìm kiếm theo yêu cầu q
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

        public ActionResult Create()
        {
            PopulateDropdowns();
            return View(new product());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(product model,
            HttpPostedFileBase UploadThumb,         
            IEnumerable<HttpPostedFileBase> imageFiles, 
            IEnumerable<int> imageOrders)            
        {

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model.category_id, model.brand_id);
                return View(model);
            }

            // Sử dụng Transaction để đảm bảo an toàn
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 3. Xử lý ảnh đại diện 
                    if (UploadThumb != null && UploadThumb.ContentLength > 0)
                    {
                        var uploadParams = new ImageUploadParams
                        {
                            File = new FileDescription(UploadThumb.FileName, UploadThumb.InputStream),
                            Folder = "products" 
                        };
                        var result = cloudinary.Upload(uploadParams);
                        if (result.Error == null)
                        {
                            model.thumbnail_url = result.SecureUrl.ToString();
                        }
                    }
                    model.created_at = DateTime.Now;
                    model.updated_at = DateTime.Now; 
                    model.is_active = true;
                    db.products.Add(model);
                    db.SaveChanges(); 

                    // Xử lý upload NHIỀU ảnh chi tiết
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
                                    int displayOrder = (ordersList.Count > i) ? ordersList[i] : (i + 1);
                                    var productImage = new product_images
                                    {
                                        product_id = model.id,
                                        image_url = imgResult.SecureUrl.ToString(),
                                        display_order = displayOrder,
                                        created_at = DateTime.Now
                                    };
                                    db.product_images.Add(productImage);
                                }
                            }
                        }

                        db.SaveChanges();
                    }

                    transaction.Commit();

                    TempData["SuccessMessage"] = "Tạo sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
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

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model.category_id, model.brand_id);
                return View("Create", model);
            }
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var p = db.products.Find(model.id);
                    if (p == null) return HttpNotFound();

                    p.name = model.name;
                    p.sku = model.sku;
                    p.slug = model.slug; 
                    p.description = model.description;
                    p.content = model.content; 
                    p.ingredients = model.ingredients; 
                    p.contraindications = model.contraindications; 
                    p.prescription_required = model.prescription_required; 

                    p.original_price = model.original_price; 
                    p.sale_price = model.sale_price;
                    p.stock_quantity = model.stock_quantity;

                    p.category_id = model.category_id; 
                    p.brand_id = model.brand_id; 
                    p.updated_at = DateTime.Now;

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

                    if (imageFiles != null && imageFiles.Any(f => f != null))
                    {
 
                        var oldImages = db.product_images.Where(img => img.product_id == p.id);
                        db.product_images.RemoveRange(oldImages);

                        var filesList = imageFiles.ToList();
                        var ordersList = imageOrders != null ? imageOrders.ToList() : new List<int>();

                        for (int i = 0; i < filesList.Count; i++)
                        {
                            var file = filesList[i];
                            if (file != null && file.ContentLength > 0)
                            {
                                var imgUploadParams = new ImageUploadParams {  };
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

                    db.Entry(p).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("Index");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
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

            p.is_active = false;
            p.updated_at = DateTime.Now;

            db.Entry(p).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges(); 
            TempData["SuccessMessage"] = "Sản phẩm đã được ẩn (ngừng kinh doanh)!";
            return RedirectToAction("Index");
        }

    }
}