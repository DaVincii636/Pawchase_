using System.Linq;
using System.Web.Mvc;
using Pawchase.Models;

namespace Pawchase.Controllers
{
    public class ProductController : Controller
    {
        public ActionResult Index(string category = null, string size = null, string search = null)
        {
            var products = MockData.Products.AsEnumerable();

            if (!string.IsNullOrEmpty(category))
                products = products.Where(p => p.Category == category);

            if (!string.IsNullOrEmpty(size))
                products = products.Where(p => p.BreedSize == size || p.BreedSize == "All");

            if (!string.IsNullOrEmpty(search))
                products = products.Where(p =>
                    p.Name.ToLower().Contains(search.ToLower()) ||
                    p.Description.ToLower().Contains(search.ToLower()) ||
                    p.Category.ToLower().Contains(search.ToLower()));

            ViewBag.Category = category;
            ViewBag.Size     = size;
            ViewBag.Search   = search;

            return View(products.ToList());
        }

        public ActionResult Details(int id)
        {
            var product = MockData.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return HttpNotFound();

            var related = MockData.Products
                .Where(p => p.Category == product.Category && p.Id != id)
                .Take(4).ToList();

            var reviews = MockData.Reviews
                .Where(r => r.ProductId == id).ToList();

            ViewBag.RelatedProducts = related;
            ViewBag.Reviews         = reviews;
            return View(product);
        }
    }
}
