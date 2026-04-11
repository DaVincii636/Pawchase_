// ════════════════════════════════════════════════
// HomeController.cs
// ════════════════════════════════════════════════
using System.Linq;
using System.Web.Mvc;
using Pawchase.Models;

namespace Pawchase.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var reviews = MockData.Reviews.Take(3).ToList();
            ViewBag.Reviews = reviews;
            return View();
        }
        public ActionResult About()   { return View(); }
        public ActionResult Contact() { return View(); }
    }
}
