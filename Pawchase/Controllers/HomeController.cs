using System;
using System.Linq;
using System.Web.Mvc;
using Pawchase.Models;

namespace Pawchase.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            try
            {
                var reviews = MockData.Reviews.Take(3).ToList();
                ViewBag.Reviews = reviews;
                ViewBag.FeaturedProducts = MockData.Products
                    .Where(p => !p.IsDeleted && p.Stock > 0)
                    .Take(4).ToList();
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Home Index Error: " + ex.Message);
                return View();
            }
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        // ── Global error pages ────────────────────────────────────────
        public ActionResult Error()
        {
            Response.StatusCode = 500;
            return View();
        }

        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}
