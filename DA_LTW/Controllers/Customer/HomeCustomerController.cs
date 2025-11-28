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
    public class HomeCustomerController : Controller
    {

        private db_cnpmEntities db = new db_cnpmEntities();
        private static Cloudinary _cloudinary;

        // Hàm khởi tạo Cloudinary (Singleton pattern đơn giản)
        private Cloudinary GetCloudinary()
        {
            if (_cloudinary == null)
            {
                var account = new Account(
                    ConfigurationManager.AppSettings["CloudinaryCloudName"],
                    ConfigurationManager.AppSettings["CloudinaryApiKey"],
                    ConfigurationManager.AppSettings["CloudinaryApiSecret"]
                );
                _cloudinary = new Cloudinary(account);
            }
            return _cloudinary;
        }

        public ActionResult Index(string keyword)
        {
            // 1. Khởi tạo query lấy sản phẩm active
            var products = db.products.Where(p => p.is_active == true);

            // 2. Nếu có từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(keyword))
            {
                // Chuyển về chữ thường để tìm kiếm không phân biệt hoa thường (tùy cấu hình DB)
                // Tìm theo Tên sản phẩm HOẶC Mô tả
                products = products.Where(p => p.name.Contains(keyword) ||
                                               p.description.Contains(keyword) ||
                                               p.sku.Contains(keyword));

                // Lưu từ khóa lại để hiển thị bên View
                ViewBag.Keyword = keyword;
            }

            // 3. Sắp xếp và lấy danh sách
            var result = products.OrderByDescending(p => p.created_at).ToList();

            // Truyền danh sách sản phẩm sang View
            return View(result);
        }


        // detail product
        // GET: Chi tiết sản phẩm
        public ActionResult Detail(string slug)
        {
            // 1. Kiểm tra tham số đầu vào
            if (string.IsNullOrEmpty(slug))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 2. Tìm sản phẩm (Chỉ lấy sản phẩm đang active)
            var product = db.products.FirstOrDefault(p => p.slug == slug && p.is_active == true);

            // QUAN TRỌNG: Nếu không tìm thấy sản phẩm thì trả về 404 ngay
            if (product == null)
            {
                return HttpNotFound();
            }

            // 3. Lấy danh sách ảnh liên quan (Gallery)
            var images = db.product_images
                           .Where(pi => pi.product_id == product.id)
                           .OrderBy(pi => pi.display_order)
                           .ToList();

            // 4. Lấy danh sách Comment (Chỉ lấy đã duyệt, mới nhất lên đầu)
            var comments = db.comments
                             .Where(c => c.product_id == product.id && c.status == "approved")
                             .OrderByDescending(c => c.created_at)
                             .ToList();

            // 5. Tính toán điểm đánh giá trung bình
            double avgRating = 0;
            int totalReviews = comments.Count;

            if (totalReviews > 0)
            {
                // Tính trung bình rating, xử lý trường hợp null
                avgRating = comments.Average(c => (double)(c.rating ?? 0));
            }

            // 6. Xử lý chuỗi JSON thành phần (Ingredients)
            var ingredientsList = new List<IngredientViewModel>();
            if (!string.IsNullOrEmpty(product.ingredients) && product.ingredients != "[]")
            {
                try
                {
                    ingredientsList = JsonConvert.DeserializeObject<List<IngredientViewModel>>(product.ingredients);
                }
                catch
                {

                }
            }

            // 7. Khởi tạo ViewModel
            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                Images = images,
                IngredientsList = ingredientsList,

                // Dữ liệu hiển thị comment
                Comments = comments,
                TotalReviews = totalReviews,
                AverageRating = Math.Round(avgRating, 1), // Làm tròn 1 chữ số thập phân (VD: 4.5)

                // Form nhập liệu comment (Giữ lại dữ liệu cũ nếu trước đó submit bị lỗi)
                NewComment = new ProductCommentViewModel
                {
                    ProductId = product.id,

                    // Lấy lại nội dung cũ từ TempData (nếu có), nếu không thì để chuỗi rỗng
                    Content = TempData["OldContent"] as string ?? "",

                    // Lấy lại số sao cũ từ TempData (nếu có), nếu không thì để 0
                    Rating = TempData["OldRating"] != null ? (int)TempData["OldRating"] : 0
                }
            };

            // 8. Trả về View
            return View(viewModel);
        }

        // POST: Xử lý khi người dùng gửi comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddComment([Bind(Prefix = "NewComment")] ProductCommentViewModel model)
        {
            // 1. Lấy thông tin User
            var user = Session["User"] as user;

            // 2. Lấy Slug sản phẩm (QUAN TRỌNG: Cần cái này để quay lại đúng trang)
            var productSlug = db.products
                                .Where(p => p.id == model.ProductId)
                                .Select(p => p.slug)
                                .FirstOrDefault();

            // Trường hợp hiếm: Không tìm thấy sản phẩm -> Về trang chủ
            if (string.IsNullOrEmpty(productSlug))
            {
                return RedirectToAction("Index", "HomeCustomer");
            }

            // Nếu chưa đăng nhập -> Chuyển sang Login, sau đó quay lại đúng trang chi tiết này
            if (user == null)
            {
                return RedirectToAction("Login", "Login", new { returnUrl = Url.Action("Detail", "HomeCustomer", new { slug = productSlug }) });
            }

            // 3. Kiểm tra tính hợp lệ của dữ liệu (Validation)
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction()) // Dùng transaction để an toàn dữ liệu
                {
                    try
                    {
                        // --- BƯỚC A: LƯU COMMENT ---
                        bool hasBought = CheckIfUserBoughtProduct(user.id, model.ProductId);

                        var newComment = new comment
                        {
                            product_id = model.ProductId,
                            user_id = user.id,
                            content = model.Content,
                            rating = (byte)model.Rating,
                            status = "approved", // cho phép hiển thị luôn
                            is_verified_purchase = hasBought,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        };
                        
                        db.comments.Add(newComment);
                        db.SaveChanges(); 

                        // UPLOAD MEDIA LÊN CLOUDINARY ---
                        if (model.UploadImages != null && model.UploadImages.Count > 0)
                        {
                            var cloudinary = GetCloudinary();

                            foreach (var file in model.UploadImages)
                            {
                                if (file != null && file.ContentLength > 0)
                                {
                                    string mediaUrl = "";
                                    string mediaType = "image";

                                    // Kiểm tra MIME type
                                    if (file.ContentType.StartsWith("image/"))
                                    {
                                        var uploadParams = new ImageUploadParams()
                                        {
                                            File = new FileDescription(file.FileName, file.InputStream),
                                            Folder = "products"
                                        };
                                        var uploadResult = cloudinary.Upload(uploadParams);
                                        mediaUrl = uploadResult.SecureUrl.ToString();
                                        mediaType = "image";
                                    }
                                    else if (file.ContentType.StartsWith("video/"))
                                    {
                                        var uploadParams = new VideoUploadParams()
                                        {
                                            File = new FileDescription(file.FileName, file.InputStream),
                                            Folder = "products"
                                        };
                                        // Upload video cần tham số riêng
                                        var uploadResult = cloudinary.Upload(uploadParams, "video");
                                        mediaUrl = uploadResult.SecureUrl.ToString();
                                        mediaType = "video";
                                    }

                                    if (!string.IsNullOrEmpty(mediaUrl))
                                    {
                                        var media = new comment_medias
                                        {
                                            comment_id = newComment.id,
                                            media_type = mediaType,
                                            media_url = mediaUrl,
                                            created_at = DateTime.Now,
                                            updated_at = DateTime.Now
                                        };
                                        db.comment_medias.Add(media);
                                    }
                                }
                            }
                            db.SaveChanges(); 
                        }

                        transaction.Commit(); 
                        TempData["SuccessMessage"] = "Cảm ơn bạn! Đánh giá đã được gửi và đang chờ duyệt.";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); 
                        TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý: " + ex.Message;

                        TempData["OldContent"] = model.Content;
                        TempData["OldRating"] = model.Rating;
                    }
                }
            }
            else
            {
                // Lấy danh sách lỗi ra chuỗi để hiển thị
                var validationErrors = string.Join("; ", ModelState.Values
                                                    .SelectMany(x => x.Errors)
                                                    .Select(x => x.ErrorMessage));

                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại: " + validationErrors;

                // Lưu lại dữ liệu cũ để form hiển thị lại (cần xử lý bên View/Controller Detail)
                TempData["OldContent"] = model.Content;
                TempData["OldRating"] = model.Rating;
            }

            // 4. CHUYỂN HƯỚNG VỀ LẠI TRANG CHI TIẾT SẢN PHẨM
            // Dù thành công hay thất bại đều chạy dòng này
            return RedirectToAction("Detail", new { slug = productSlug });
        }

        // Hàm kiểm tra xem user đã mua sản phẩm và đơn hàng thành công chưa
        private bool CheckIfUserBoughtProduct(int userId, int productId)
        {

            // Điều kiện:
            // 1. Đúng User ID
            // 2. Đúng Product ID
            // 3. Trạng thái đơn hàng phải là 'DELIVERED' (Đã giao hàng) thì mới được tính là đã mua thật

            var hasBought = (from o in db.orders
                             join oi in db.order_items on o.id equals oi.order_id
                             where o.user_id == userId
                                   && oi.product_id == productId
                                   && o.status == "DELIVERED"
                             select o.id).Any();

            return hasBought;
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