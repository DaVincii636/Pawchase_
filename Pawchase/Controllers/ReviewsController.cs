using System.Linq;
using System.Web.Mvc;
using Pawchase.Models;

namespace Pawchase.Controllers
{
    public class ReviewsController : Controller
    {
        public ActionResult Index()
        {
            var reviews = MockData.Reviews.OrderByDescending(r => r.DatePosted).ToList();
            ViewBag.Products = MockData.Products.Select(p => new { p.Id, p.Name }).ToList();
            return View(reviews);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Submit(int productId, int stars, string comment, string customerName)
        {
            var review = new Review {
                Id = MockData.Reviews.Count + 1,
                ProductId = productId,
                CustomerName = string.IsNullOrEmpty(customerName)
                    ? (Session["UserName"] as string ?? "Anonymous")
                    : customerName,
                Stars = stars,
                Comment = comment,
                DatePosted = System.DateTime.Now
            };
            MockData.Reviews.Add(review);
            TempData["ReviewPosted"] = true;
            return RedirectToAction("Index");
        }

        public ActionResult Like(int id) { return Json(new { ok = true }, JsonRequestBehavior.AllowGet); }
    }
}
