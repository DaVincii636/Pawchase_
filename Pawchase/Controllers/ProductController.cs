using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Pawchase.Models;

namespace Pawchase.Controllers
{
    public class ProductController : Controller
    {
        public ActionResult Index(string category = null, string size = null,
                                  string search = null, string sort = null,
                                  decimal? minPrice = null, decimal? maxPrice = null,
                                  bool onSale = false)
        {
            var products = MockData.Products.Where(p => !p.IsDeleted).AsEnumerable();

            if (!string.IsNullOrEmpty(category))
                products = products.Where(p => p.Category == category);

            if (!string.IsNullOrEmpty(size))
                products = products.Where(p => p.BreedSize == size || p.BreedSize == "All");

            if (!string.IsNullOrEmpty(search))
                products = products.Where(p =>
                    p.Name.ToLower().Contains(search.ToLower()) ||
                    p.Description.ToLower().Contains(search.ToLower()) ||
                    p.Category.ToLower().Contains(search.ToLower()));

            if (onSale)
                products = products.Where(p => p.IsOnSale);

            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value);

            // Sorting
            switch (sort)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                case "sale":
                    products = products.OrderByDescending(p => p.IsOnSale).ThenBy(p => p.Price);
                    break;
                case "name":
                    products = products.OrderBy(p => p.Name);
                    break;
                default:
                    products = products.OrderBy(p => p.Id);
                    break;
            }

            // Compute average ratings per product for the grid
            var ratings = MockData.Reviews
                .GroupBy(r => r.ProductId)
                .ToDictionary(g => g.Key, g => new { Avg = g.Average(r => r.Stars), Count = g.Count() });

            ViewBag.Ratings = ratings;
            ViewBag.Category = category;
            ViewBag.Size = size;
            ViewBag.Search = search;
            ViewBag.Sort = sort ?? "default";
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.OnSale = onSale;

            // Overall price range for the slider bounds
            var allProducts = MockData.Products.Where(p => !p.IsDeleted).ToList();
            ViewBag.GlobalMin = allProducts.Any() ? (int)allProducts.Min(p => p.Price) : 0;
            ViewBag.GlobalMax = allProducts.Any() ? (int)allProducts.Max(p => p.Price) + 1 : 2000;

            return View(products.ToList());
        }

        public ActionResult Details(int id)
        {
            var product = MockData.Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
            if (product == null) return HttpNotFound();

            var related = MockData.Products
                .Where(p => p.Category == product.Category && p.Id != id && !p.IsDeleted)
                .Take(4).ToList();

            var reviews = MockData.Reviews
                .Where(r => r.ProductId == id).ToList();

            // Average rating for this product
            var avgRating = reviews.Any() ? reviews.Average(r => r.Stars) : 0;
            ViewBag.AvgRating = avgRating;
            ViewBag.ReviewCount = reviews.Count;

            // Recently viewed — store in session, max 6, exclude current
            var recentIds = Session["RecentlyViewed"] as List<int> ?? new List<int>();
            recentIds.Remove(id);
            recentIds.Insert(0, id);
            if (recentIds.Count > 7) recentIds = recentIds.Take(7).ToList();
            Session["RecentlyViewed"] = recentIds;

            // Build recently viewed product list (exclude current, max 4)
            var recentProducts = recentIds
                .Where(rid => rid != id)
                .Take(4)
                .Select(rid => MockData.Products.FirstOrDefault(p => p.Id == rid && !p.IsDeleted))
                .Where(p => p != null)
                .ToList();

            ViewBag.RelatedProducts = related;
            ViewBag.Reviews = reviews;
            ViewBag.RecentlyViewed = recentProducts;
            return View(product);
        }
    }
}
