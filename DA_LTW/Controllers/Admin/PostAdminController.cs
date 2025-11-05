using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DA_LTW.Controllers.Admin
{
    public class PostAdminController : Controller
    {
        private db_cnpmEntities db = new db_cnpmEntities();

        private static Cloudinary cloudinary;

        public PostAdminController()
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

        public ActionResult Index()
        {
            var posts = db.posts
                            .OrderByDescending(p => p.created_at)
                            .ToList();
            return View(posts);
        }

        // Thêm bài viết
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.CategoryList = new SelectList(db.post_categories.ToList(), "id", "name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(post model, HttpPostedFileBase thumbnail)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Upload ảnh lên Cloudinary nếu có chọn
                    if (thumbnail != null && thumbnail.ContentLength > 0)
                    {
                        var uploadParams = new ImageUploadParams()
                        {
                            File = new FileDescription(thumbnail.FileName, thumbnail.InputStream),
                            Folder = "blog_thumbnails"
                        };

                        var uploadResult = cloudinary.Upload(uploadParams);
                        model.thumbnail_url = uploadResult.SecureUrl.ToString();
                    }

                    model.slug = model.title.Replace(" ", "-").ToLower();
                    model.status = "DRAFT";
                    model.created_at = DateTime.Now;
                    model.updated_at = DateTime.Now;
                    model.author_id = 1; // Tạm cứng (bạn có thể đổi theo user login)

                    db.posts.Add(model);
                    db.SaveChanges();

                    TempData["Success"] = "✅ Bài viết đã được tạo thành công!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Lỗi khi tạo bài viết: " + ex.Message;
            }

            ViewBag.CategoryList = new SelectList(db.post_categories.ToList(), "id", "name");
            return View(model);
        }

        // Sửa bài viết
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var post = db.posts.Find(id);
            if (post == null)
            {
                return HttpNotFound();
            }

            ViewBag.CategoryList = new SelectList(db.post_categories.ToList(), "id", "name", post.category_id);
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(post model, HttpPostedFileBase thumbnail)
        {
            try
            {
                var existing = db.posts.Find(model.id);
                if (existing == null)
                {
                    return HttpNotFound();
                }

                if (thumbnail != null && thumbnail.ContentLength > 0)
                {
                    // Upload Cloudinary
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(thumbnail.FileName, thumbnail.InputStream),
                        Folder = "blog_thumbnails"
                    };

                    var uploadResult = cloudinary.Upload(uploadParams);
                    existing.thumbnail_url = uploadResult.SecureUrl.ToString();
                }

                existing.title = model.title;
                existing.excerpt = model.excerpt;
                existing.content = model.content;
                existing.category_id = model.category_id;
                existing.updated_at = DateTime.Now;
                existing.status = model.status ?? existing.status;

                db.SaveChanges();
                TempData["Success"] = "✅ Cập nhật bài viết thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Có lỗi xảy ra: " + ex.Message;
            }

            ViewBag.CategoryList = new SelectList(db.post_categories.ToList(), "id", "name", model.category_id);
            return View(model);
        }

        // Xóa bài viết
        public ActionResult Delete(int id)
        {
            var post = db.posts.Find(id);
            if (post == null) return HttpNotFound();

            db.posts.Remove(post);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}