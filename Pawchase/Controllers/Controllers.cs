using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Pawchase.Models;

namespace Pawchase.Controllers
{
    // ════════════════════════════ ACCOUNT ════════════════════════════
    public class AccountController : Controller
    {
        private bool IsLoggedIn => Session["UserId"] != null;

        public ActionResult Login(string returnUrl)
        {
            if (IsLoggedIn) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password, string returnUrl)
        {
            var user = MockData.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
            if (user != null)
            {
                Session["UserId"]    = user.Id;
                Session["UserName"]  = user.FullName;
                Session["UserEmail"] = user.Email;
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error     = "Invalid email or password.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public ActionResult Register(string returnUrl)
        {
            if (IsLoggedIn) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(string fullName, string email, string password, string confirmPassword, string returnUrl)
        {
            if (password != confirmPassword) { ViewBag.Error = "Passwords do not match."; return View(); }
            if (MockData.Users.Any(u => u.Email.ToLower() == email.ToLower())) { ViewBag.Error = "Email already registered."; return View(); }

            var user = new User { Id = MockData.Users.Count + 1, FullName = fullName, Email = email, Password = password };
            MockData.Users.Add(user);
            Session["UserId"]    = user.Id;
            Session["UserName"]  = user.FullName;
            Session["UserEmail"] = user.Email;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult GoogleLogin(string returnUrl)
        {
            var email = "googleuser@gmail.com";
            var user  = MockData.Users.FirstOrDefault(u => u.Email == email)
                     ?? new User { Id = MockData.Users.Count + 1, FullName = "Google User", Email = email };
            if (!MockData.Users.Contains(user)) MockData.Users.Add(user);
            Session["UserId"]    = user.Id;
            Session["UserName"]  = user.FullName;
            Session["UserEmail"] = user.Email;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Orders(string tab = "All")
        {
            if (!IsLoggedIn) return RedirectToAction("Login");
            var email   = Session["UserEmail"].ToString();
            var orders  = MockData.Orders.Where(o => o.Email == email).ToList();
            ViewBag.Tab = tab;
            return View(orders);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }

    // ════════════════════════════ CART ════════════════════════════════
    public class CartController : Controller
    {
        private List<CartItem> GetCart() {
            if (Session["Cart"] == null) Session["Cart"] = new List<CartItem>();
            return (List<CartItem>)Session["Cart"];
        }
        private bool IsLoggedIn => Session["UserId"] != null;

        public ActionResult Index() {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
            return View(GetCart());
        }

        public ActionResult Add(int id, string returnUrl) {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = returnUrl ?? Url.Action("Index", "Product") });
            var product = MockData.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return HttpNotFound();
            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.Product.Id == id);
            if (existing != null) existing.Quantity++;
            else cart.Add(new CartItem { Product = product, Quantity = 1 });
            Session["Cart"] = cart;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        public ActionResult Remove(int id) {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Product.Id == id);
            if (item != null) cart.Remove(item);
            Session["Cart"] = cart;
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int id, int quantity) {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Product.Id == id);
            if (item != null) { if (quantity <= 0) cart.Remove(item); else item.Quantity = quantity; }
            Session["Cart"] = cart;
            return RedirectToAction("Index");
        }

        public ActionResult Checkout() {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");
            return View(cart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string address, string phone) {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");
            var shipping = cart.Sum(c => c.Subtotal) >= 500 ? 0 : 80;
            var order = new Order {
                Id              = MockData.Orders.Count + 1,
                ReferenceNumber = "PWC-" + (MockData.Orders.Count + 1).ToString("D6"),
                CustomerName    = Session["UserName"].ToString(),
                Email           = Session["UserEmail"].ToString(),
                Address         = address, Phone = phone,
                Items           = new List<CartItem>(cart),
                Total           = cart.Sum(c => c.Subtotal) + shipping,
                OrderDate       = System.DateTime.Now,
                Status          = "To Ship"
            };
            MockData.Orders.Add(order);
            Session["Cart"] = new List<CartItem>();
            return RedirectToAction("Confirmation", "Order", new { referenceNumber = order.ReferenceNumber });
        }
    }

    // ════════════════════════════ ORDER ═══════════════════════════════
    public class OrderController : Controller
    {
        public ActionResult Track() { return View(); }

        [HttpPost]
        public ActionResult Track(string referenceNumber) {
            if (string.IsNullOrWhiteSpace(referenceNumber)) { ViewBag.Error = "Please enter a reference number."; return View(); }
            var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber.ToUpper() == referenceNumber.Trim().ToUpper());
            if (order == null) { ViewBag.Error = $"No order found for \"{referenceNumber.Trim()}\"."; return View(); }
            return View("TrackResult", order);
        }

        public ActionResult Confirmation(string referenceNumber) {
            var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
            if (order == null) return HttpNotFound();
            return View(order);
        }

        public ActionResult MarkReceived(string referenceNumber) {
            var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
            if (order != null) order.Status = "To Rate";
            return RedirectToAction("Orders", "Account");
        }

        public ActionResult RequestRefund(string referenceNumber) {
            var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
            if (order != null) { order.Status = "Refund Requested"; order.HasRefundRequest = true; }
            return RedirectToAction("Orders", "Account");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SubmitReview(int productId, string referenceNumber, int stars, string comment) {
            var review = new Review {
                Id           = MockData.Reviews.Count + 1,
                ProductId    = productId,
                CustomerName = Session["UserName"]?.ToString() ?? "Customer",
                Stars        = stars, Comment = comment,
                DatePosted   = System.DateTime.Now
            };
            MockData.Reviews.Add(review);
            var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
            if (order != null) order.Status = "Completed";
            return RedirectToAction("Orders", "Account", new { tab = "Completed" });
        }
    }

    // ════════════════════════════ ADMIN ═══════════════════════════════
    public class AdminController : Controller
    {
        private bool IsAdmin => Session["IsAdmin"] != null && (bool)Session["IsAdmin"];

        public ActionResult Login() { if (IsAdmin) return RedirectToAction("Dashboard"); return View(); }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password) {
            if (email == MockData.AdminEmail && password == MockData.AdminPassword) {
                Session["IsAdmin"] = true; Session["AdminName"] = "Admin";
                return RedirectToAction("Dashboard");
            }
            ViewBag.Error = "Invalid admin credentials.";
            return View();
        }

        public ActionResult Logout() { Session["IsAdmin"] = null; Session["AdminName"] = null; return RedirectToAction("Index", "Home"); }

        public ActionResult Dashboard() {
            if (!IsAdmin) return RedirectToAction("Login");
            ViewBag.TotalProducts = MockData.Products.Count;
            ViewBag.TotalOrders   = MockData.Orders.Count;
            ViewBag.PendingOrders = MockData.Orders.Count(o => o.Status == "To Ship");
            ViewBag.TotalRevenue  = MockData.Orders.Sum(o => o.Total);
            ViewBag.TotalUsers    = MockData.Users.Count;
            ViewBag.RecentOrders  = MockData.Orders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
            ViewBag.RefundCount   = MockData.Orders.Count(o => o.HasRefundRequest);
            return View();
        }

        public ActionResult Products() { if (!IsAdmin) return RedirectToAction("Login"); return View(MockData.Products); }

        public ActionResult AddProduct() { if (!IsAdmin) return RedirectToAction("Login"); return View(); }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddProduct(Product product) {
            if (!IsAdmin) return RedirectToAction("Login");
            product.Id       = MockData.Products.Max(p => p.Id) + 1;
            product.ImageUrl = "/Content/images/products/placeholder.png";
            MockData.Products.Add(product);
            TempData["Success"] = $"Product \"{product.Name}\" added!";
            return RedirectToAction("Products");
        }

        public ActionResult EditProduct(int id) {
            if (!IsAdmin) return RedirectToAction("Login");
            var p = MockData.Products.FirstOrDefault(x => x.Id == id);
            if (p == null) return HttpNotFound();
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditProduct(Product updated) {
            if (!IsAdmin) return RedirectToAction("Login");
            var p = MockData.Products.FirstOrDefault(x => x.Id == updated.Id);
            if (p != null) { p.Name = updated.Name; p.Description = updated.Description; p.Price = updated.Price; p.OriginalPrice = updated.OriginalPrice; p.Category = updated.Category; p.BreedSize = updated.BreedSize; p.Stock = updated.Stock; }
            TempData["Success"] = $"Product \"{updated.Name}\" updated!";
            return RedirectToAction("Products");
        }

        public ActionResult DeleteProduct(int id) {
            if (!IsAdmin) return RedirectToAction("Login");
            var p = MockData.Products.FirstOrDefault(x => x.Id == id);
            if (p != null) MockData.Products.Remove(p);
            TempData["Success"] = "Product deleted.";
            return RedirectToAction("Products");
        }

        public ActionResult Orders() {
            if (!IsAdmin) return RedirectToAction("Login");
            return View(MockData.Orders.OrderByDescending(o => o.OrderDate).ToList());
        }

        public ActionResult UpdateOrderStatus(int id, string status) {
            if (!IsAdmin) return RedirectToAction("Login");
            var o = MockData.Orders.FirstOrDefault(x => x.Id == id);
            if (o != null) { o.Status = status; if (status != "Refund Requested") o.HasRefundRequest = false; }
            TempData["Success"] = $"Order status updated to \"{status}\".";
            return RedirectToAction("Orders");
        }
    }
}
