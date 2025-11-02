using DA_LTW.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DA_LTW.ViewModels
{
    public class ProductDetailViewModel
    {
        // Thông tin sản phẩm chính
        public product Product { get; set; }

        // Danh sách các ảnh phụ của sản phẩm
        public List<product_images> Images { get; set; }

        // Danh sách thành phần đã được giải mã từ JSON
        public List<IngredientViewModel> IngredientsList { get; set; }
    }
}