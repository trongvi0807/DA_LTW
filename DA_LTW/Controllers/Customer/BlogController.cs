using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DA_LTW.Models;

namespace DA_LTW.Controllers.Customer
{
    public class BlogController : Controller
    {
        private db_cnpmEntities db = new db_cnpmEntities();

        // GET: Blog
        public ActionResult Index()
        {
            var blogs = db.posts.Where(p => p.status.Equals("PUBLISHED"))
                .OrderByDescending(p => p.created_at).Take(12).ToList();
            return View(blogs);
        }

        public ActionResult Detail (string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return RedirectToAction("Index");
            }

            var post = db.posts.FirstOrDefault(p => p.slug.Equals(slug) && p.status.Equals("PUBLISHED"));
            if (post == null)
            {
                return HttpNotFound();
            }
            // Tăng lượt xem
            post.view_count = (post.view_count ?? 0) + 1;
            db.SaveChanges();

            // Gợi ý bài viết liên quan cùng chuyên mục
            var relatedPosts = db.posts
                .Where(p => p.category_id == post.category_id && p.id != post.id && p.status == "PUBLISHED")
                .OrderByDescending(p => p.published_at)
                .Take(4)
                .ToList();

            ViewBag.RelatedPosts = relatedPosts;
            return View(post);
        }

    }
}