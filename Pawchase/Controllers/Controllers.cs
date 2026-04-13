using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Pawchase.Models;
using System;

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
                Session["UserId"] = user.Id;
                Session["UserName"] = user.FullName;
                Session["UserEmail"] = user.Email;
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Invalid email or password.";
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
            Session["UserId"] = user.Id;
            Session["UserName"] = user.FullName;
            Session["UserEmail"] = user.Email;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult GoogleLogin(string returnUrl)
        {
            var email = "googleuser@gmail.com";
            var user = MockData.Users.FirstOrDefault(u => u.Email == email)
                     ?? new User { Id = MockData.Users.Count + 1, FullName = "Google User", Email = email };
            if (!MockData.Users.Contains(user)) MockData.Users.Add(user);
            Session["UserId"] = user.Id;
            Session["UserName"] = user.FullName;
            Session["UserEmail"] = user.Email;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Orders(string tab = "All")
        {
            if (!IsLoggedIn) return RedirectToAction("Login");
            var email = Session["UserEmail"].ToString();
            var orders = MockData.Orders.Where(o => o.Email == email).ToList();
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
        private List<CartItem> GetCart()
        {
            if (Session["Cart"] == null) Session["Cart"] = new List<CartItem>();
            return (List<CartItem>)Session["Cart"];
        }
        private bool IsLoggedIn => Session["UserId"] != null;

        public ActionResult Index()
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
            // Purge cart items whose product was soft-deleted
            var cart = GetCart();
            cart.RemoveAll(c => c.Product == null || c.Product.IsDeleted);
            Session["Cart"] = cart;
            return View(cart);
        }

        public ActionResult Add(int id, string returnUrl, string variantLabel, int qty = 1)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = returnUrl ?? Url.Action("Index", "Product") });

            var product = MockData.Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
            if (product == null) return HttpNotFound();

            // FIX: stock guard — cannot add out-of-stock products even via direct URL
            if (product.Stock <= 0)
                return RedirectToAction("Details", "Product", new { id = id });

            if (product.Variants != null && product.Variants.Any() && string.IsNullOrEmpty(variantLabel))
            {
                var detailUrl = Url.Action("Details", "Product", new { id = id });
                return Redirect(detailUrl + "?variantRequired=1");
            }

            ProductVariant chosen = null;
            if (!string.IsNullOrEmpty(variantLabel) && product.Variants != null)
                chosen = product.Variants.FirstOrDefault(v => v.Label == variantLabel);

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c =>
                c.Product.Id == id &&
                (c.SelectedVariant == null && chosen == null ||
                 c.SelectedVariant != null && chosen != null && c.SelectedVariant.Label == chosen.Label));

            if (existing != null)
            {
                // FIX: don't exceed available stock
                var newQty = existing.Quantity + qty;
                existing.Quantity = Math.Min(newQty, product.Stock);
            }
            else
            {
                cart.Add(new CartItem { Product = product, Quantity = Math.Min(qty, product.Stock), SelectedVariant = chosen });
            }

            Session["Cart"] = cart;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        public ActionResult Remove(int id, string variantLabel)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c =>
                c.Product.Id == id &&
                ((string.IsNullOrEmpty(variantLabel) && c.SelectedVariant == null) ||
                 (c.SelectedVariant != null && c.SelectedVariant.Label == variantLabel)));
            if (item != null) cart.Remove(item);
            Session["Cart"] = cart;
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int id, int quantity, string variantLabel)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c =>
                c.Product.Id == id &&
                ((string.IsNullOrEmpty(variantLabel) && c.SelectedVariant == null) ||
                 (c.SelectedVariant != null && c.SelectedVariant.Label == variantLabel)));
            if (item != null)
            {
                if (quantity <= 0)
                    cart.Remove(item);
                else
                    // FIX: cap at available stock
                    item.Quantity = Math.Min(quantity, item.Product.Stock);
            }
            Session["Cart"] = cart;
            return RedirectToAction("Index");
        }

        public ActionResult Checkout()
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
            var cart = GetCart();
            cart.RemoveAll(c => c.Product == null || c.Product.IsDeleted);
            if (!cart.Any()) return RedirectToAction("Index");
            return View(cart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string address, string phone)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            var shipping = cart.Sum(c => c.Subtotal) >= 500 ? 0 : 80;

            // FIX: snapshot each item so order history is immune to product edits/deletes
            var snapshots = cart.Select(c => new OrderItemSnapshot
            {
                ProductId = c.Product.Id,
                ProductName = c.Product.Name,
                ProductImageUrl = c.Product.ImageUrl,
                Category = c.Product.Category,
                BreedSize = c.Product.BreedSize,
                UnitPrice = c.Product.Price,
                Quantity = c.Quantity,
                VariantLabel = c.SelectedVariant?.Label,
                VariantImageUrl = c.SelectedVariant?.ImageUrl
            }).ToList();

            var order = new Order
            {
                Id = MockData.Orders.Count + 1,
                ReferenceNumber = "PWC-" + (MockData.Orders.Count + 1).ToString("D6"),
                CustomerName = Session["UserName"].ToString(),
                Email = Session["UserEmail"].ToString(),
                Address = address,
                Phone = phone,
                Items = new List<CartItem>(cart), // keep for backward compat
                Snapshots = snapshots,
                Total = cart.Sum(c => c.Subtotal) + shipping,
                OrderDate = DateTime.Now,
                Status = "To Ship"
            };

            MockData.Orders.Add(order);

            // FIX: decrement stock for each item ordered
            foreach (var item in cart)
            {
                var product = MockData.Products.FirstOrDefault(p => p.Id == item.Product.Id);
                if (product != null)
                    product.Stock = Math.Max(0, product.Stock - item.Quantity);
            }

            Session["Cart"] = new List<CartItem>();
            return RedirectToAction("Confirmation", "Order", new { referenceNumber = order.ReferenceNumber });
        }
    }

    // ════════════════════════════ ORDER ═══════════════════════════════
    public class OrderController : Controller
    {
        public ActionResult Track() { return View(); }

        [HttpPost]
        public ActionResult Track(string referenceNumber)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber)) { ViewBag.Error = "Please enter a reference number."; return View(); }
            var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber.ToUpper() == referenceNumber.Trim().ToUpper());
            if (order == null) { ViewBag.Error = $"No order found for \"{referenceNumber.Trim()}\"."; return View(); }
            return View("TrackResult", order);
        }

        public ActionResult Confirmation(string referenceNumber)
        {
            var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
            if (order == null) return HttpNotFound();
            return View(order);
        }

        public ActionResult MarkReceived(string referenceNumber)
        {
            var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
            if (order != null) order.Status = "To Rate";
            return RedirectToAction("Orders", "Account");
        }

        public ActionResult RequestRefund(string referenceNumber)
        {
            var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
            if (order != null) { order.Status = "Refund Requested"; order.HasRefundRequest = true; }
            return RedirectToAction("Orders", "Account");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SubmitReview(int productId, string referenceNumber, int stars, string comment)
        {
            var userId = 0;
            int.TryParse(Session["UserId"]?.ToString(), out userId);

            var review = new Review
            {
                Id = MockData.Reviews.Count + 1,
                ProductId = productId,
                UserId = userId,
                CustomerName = Session["UserName"]?.ToString() ?? "Customer",
                Stars = stars,
                Comment = comment,
                DatePosted = DateTime.Now,
                Category = MockData.Products.FirstOrDefault(p => p.Id == productId)?.Category ?? "Others"
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
        public ActionResult Login(string email, string password)
        {
            if (email == MockData.AdminEmail && password == MockData.AdminPassword)
            {
                Session["IsAdmin"] = true; Session["AdminName"] = "Admin";
                return RedirectToAction("Dashboard");
            }
            ViewBag.Error = "Invalid admin credentials.";
            return View();
        }

        public ActionResult Logout() { Session["IsAdmin"] = null; Session["AdminName"] = null; return RedirectToAction("Index", "Home"); }

        public ActionResult Dashboard()
        {
            if (!IsAdmin) return RedirectToAction("Login");
            ViewBag.TotalProducts = MockData.Products.Count(p => !p.IsDeleted);
            ViewBag.TotalOrders = MockData.Orders.Count;
            ViewBag.PendingOrders = MockData.Orders.Count(o => o.Status == "To Ship");
            ViewBag.TotalRevenue = MockData.Orders.Sum(o => o.Total);
            ViewBag.TotalUsers = MockData.Users.Count;
            ViewBag.RecentOrders = MockData.Orders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
            ViewBag.RefundCount = MockData.Orders.Count(o => o.HasRefundRequest);
            // FIX: low-stock alert for dashboard
            ViewBag.LowStockCount = MockData.Products.Count(p => !p.IsDeleted && p.Stock > 0 && p.Stock <= 5);
            ViewBag.OutOfStockCount = MockData.Products.Count(p => !p.IsDeleted && p.Stock == 0);
            return View();
        }

        public ActionResult Products()
        {
            if (!IsAdmin) return RedirectToAction("Login");
            // Only show non-deleted products in admin list
            return View(MockData.Products.Where(p => !p.IsDeleted).ToList());
        }

        public ActionResult AddProduct() { if (!IsAdmin) return RedirectToAction("Login"); return View(); }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddProduct(Product product, string[] VariantImagePaths, string[] VariantLabels)
        {
            if (!IsAdmin) return RedirectToAction("Login");

            // FIX: validate that original price > sale price
            if (product.OriginalPrice.HasValue && product.OriginalPrice.Value > 0 && product.OriginalPrice.Value <= product.Price)
            {
                TempData["Error"] = "Original Price must be greater than Sale Price for a sale badge to appear.";
                return RedirectToAction("AddProduct");
            }

            product.Id = MockData.Products.Max(p => p.Id) + 1;

            if (string.IsNullOrWhiteSpace(product.ImageUrl))
                product.ImageUrl = "/Content/images/products/placeholder.png";
            else if (!product.ImageUrl.StartsWith("data:image"))
            {
                var fileName = System.IO.Path.GetFileName(product.ImageUrl);
                var folder = (product.Category ?? "Others").ToLower();
                product.ImageUrl = "/Content/images/products/" + folder + "/" + fileName;
            }

            product.Variants = new List<ProductVariant>();
            int maxV = Math.Max(VariantImagePaths != null ? VariantImagePaths.Length : 0,
                                VariantLabels != null ? VariantLabels.Length : 0);
            for (int i = 0; i < maxV; i++)
            {
                var imgPath = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                if (!string.IsNullOrWhiteSpace(imgPath) || !string.IsNullOrWhiteSpace(label))
                    product.Variants.Add(new ProductVariant { ImageUrl = string.IsNullOrWhiteSpace(imgPath) ? null : imgPath, Label = label });
            }

            MockData.Products.Add(product);
            TempData["Success"] = $"Product \"{product.Name}\" added!";
            return RedirectToAction("Products");
        }

        public ActionResult EditProduct(int id)
        {
            if (!IsAdmin) return RedirectToAction("Login");
            var p = MockData.Products.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
            if (p == null) return HttpNotFound();
            return View(p);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditProduct(Product updated, string[] VariantImagePaths, string[] VariantLabels)
        {
            if (!IsAdmin) return RedirectToAction("Login");

            // FIX: validate original price > sale price
            if (updated.OriginalPrice.HasValue && updated.OriginalPrice.Value > 0 && updated.OriginalPrice.Value <= updated.Price)
            {
                TempData["Error"] = "Original Price must be greater than Sale Price.";
                return RedirectToAction("EditProduct", new { id = updated.Id });
            }

            var p = MockData.Products.FirstOrDefault(x => x.Id == updated.Id);
            if (p != null)
            {
                p.Name = updated.Name;
                p.Description = updated.Description;
                p.Price = updated.Price;
                p.OriginalPrice = updated.OriginalPrice;
                p.Category = updated.Category;
                p.BreedSize = updated.BreedSize;
                p.Stock = updated.Stock;

                if (string.IsNullOrWhiteSpace(updated.ImageUrl))
                    p.ImageUrl = "/Content/images/products/placeholder.png";
                else
                    p.ImageUrl = updated.ImageUrl;

                p.Variants = new List<ProductVariant>();
                int maxV = Math.Max(VariantImagePaths != null ? VariantImagePaths.Length : 0,
                                    VariantLabels != null ? VariantLabels.Length : 0);
                for (int i = 0; i < maxV; i++)
                {
                    var imgPath = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                    var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                    if (!string.IsNullOrWhiteSpace(imgPath) || !string.IsNullOrWhiteSpace(label))
                        p.Variants.Add(new ProductVariant { ImageUrl = string.IsNullOrWhiteSpace(imgPath) ? null : imgPath, Label = label });
                }
            }
            TempData["Success"] = $"Product \"{updated.Name}\" updated!";
            return RedirectToAction("Products");
        }

        // FIX: soft-delete — product is hidden but order/cart references stay intact
        public ActionResult DeleteProduct(int id)
        {
            if (!IsAdmin) return RedirectToAction("Login");
            var p = MockData.Products.FirstOrDefault(x => x.Id == id);
            if (p != null)
            {
                p.IsDeleted = true;
                TempData["Success"] = $"Product \"{p.Name}\" deleted.";
            }
            return RedirectToAction("Products");
        }

        // FIX: quick stock update from the products table (AJAX-friendly)
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateStock(int id, int stock)
        {
            if (!IsAdmin) return RedirectToAction("Login");
            var p = MockData.Products.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
            if (p != null)
            {
                p.Stock = Math.Max(0, stock);
                TempData["Success"] = $"Stock for \"{p.Name}\" updated to {p.Stock}.";
            }
            return RedirectToAction("Products");
        }

        public ActionResult Orders()
        {
            if (!IsAdmin) return RedirectToAction("Login");
            return View(MockData.Orders.OrderByDescending(o => o.OrderDate).ToList());
        }

        public ActionResult UpdateOrderStatus(int id, string status)
        {
            if (!IsAdmin) return RedirectToAction("Login");
            var o = MockData.Orders.FirstOrDefault(x => x.Id == id);
            if (o != null) { o.Status = status; if (status != "Refund Requested") o.HasRefundRequest = false; }
            TempData["Success"] = $"Order status updated to \"{status}\".";
            return RedirectToAction("Orders");
        }
    }
}
