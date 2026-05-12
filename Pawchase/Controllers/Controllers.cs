using Pawchase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;

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
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "Email and password are required."; ViewBag.ReturnUrl = returnUrl; return View();
                }
                var normalizedEmail = email.ToLower().Trim();
                var user = MockData.Users.FirstOrDefault(u =>
                    u.Email.ToLower() == normalizedEmail &&
                    (u.Password == password || DbHelper.VerifyPassword(password, u.Password)));
                if (user != null)
                {
                    Session["UserId"] = user.Id; Session["UserName"] = user.FullName; Session["UserEmail"] = user.Email;
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }
                ViewBag.Error = "Invalid email or password."; ViewBag.ReturnUrl = returnUrl; return View();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Login Error: " + ex.Message); ViewBag.Error = "Login failed."; return View(); }
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
            try
            {
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "All fields are required."; return View();
                }
                if (password != confirmPassword) { ViewBag.Error = "Passwords do not match."; return View(); }
                if (password.Length < 6) { ViewBag.Error = "Password must be at least 6 characters."; return View(); }
                if (MockData.Users.Any(u => u.Email.ToLower() == email.ToLower().Trim()))
                {
                    ViewBag.Error = "Email already registered."; return View();
                }
                var user = new User
                {
                    FullName = fullName.Trim(),
                    Email = email.Trim().ToLower(),
                    Password = password,
                    IsAdmin = false
                };
                user.Id = DbHelper.AddUser(user);
                MockData.RefreshUsers();
                Session["UserId"] = user.Id; Session["UserName"] = user.FullName; Session["UserEmail"] = user.Email;
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Register Error: " + ex.Message); ViewBag.Error = "Registration failed."; return View(); }
        }

        public ActionResult Orders(string tab = "All")
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login");
                var email = Session["UserEmail"].ToString();
                var orders = MockData.Orders.Where(o => o.Email == email).ToList();
                var userId = 0; int.TryParse(Session["UserId"]?.ToString(), out userId);
                var user = MockData.Users.FirstOrDefault(u => u.Id == userId);
                ViewBag.Tab = tab;
                ViewBag.ProfileUser = user;
                ViewBag.CurrentUserId = userId;
                ViewBag.SavedAddresses = DbHelper.GetAddressesByUser(userId);
                return View(orders);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Account Orders Error: " + ex.Message); return RedirectToAction("Index", "Home"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveProfile(string fullName, string email, string phone, string gcashNumber, string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login");
                var userId = 0; int.TryParse(Session["UserId"]?.ToString(), out userId);
                var user = MockData.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null) return RedirectToAction("Login");
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
                {
                    TempData["ProfileError"] = "Name and email are required."; return RedirectToAction("Orders", new { tab = "Profile" });
                }
                if (MockData.Users.Any(u => u.Id != userId && u.Email.ToLower() == email.ToLower().Trim()))
                {
                    TempData["ProfileError"] = "Email already in use."; return RedirectToAction("Orders", new { tab = "Profile" });
                }
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    if (user.Password != currentPassword && !DbHelper.VerifyPassword(currentPassword, user.Password)) { TempData["ProfileError"] = "Current password incorrect."; return RedirectToAction("Orders", new { tab = "Profile" }); }
                    if (newPassword.Length < 6) { TempData["ProfileError"] = "New password must be at least 6 characters."; return RedirectToAction("Orders", new { tab = "Profile" }); }
                    if (newPassword != confirmPassword) { TempData["ProfileError"] = "New passwords do not match."; return RedirectToAction("Orders", new { tab = "Profile" }); }
                    DbHelper.UpdateUserPassword(user.Id, newPassword);
                    user.Password = DbHelper.HashPassword(newPassword);
                }
                user.FullName = fullName.Trim(); user.Email = email.Trim().ToLower(); user.Phone = phone?.Trim(); user.GCashNumber = gcashNumber?.Trim();
                DbHelper.UpdateUserProfile(user);
                MockData.RefreshUsers();
                Session["UserName"] = user.FullName; Session["UserEmail"] = user.Email;
                TempData["ProfileSuccess"] = "Profile updated."; return RedirectToAction("Orders", new { tab = "Profile" });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("SaveProfile Error: " + ex.Message); TempData["ProfileError"] = "Could not save profile."; return RedirectToAction("Orders", new { tab = "Profile" }); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear(); Session.Abandon(); FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }
    }

    // ════════════════════════════ CART ════════════════════════════
    public class CartController : Controller
    {
        private bool IsLoggedIn => Session["UserId"] != null;
        private List<CartItem> GetCart()
        {
            if (Session["Cart"] == null) Session["Cart"] = new List<CartItem>();
            return (List<CartItem>)Session["Cart"];
        }

        public ActionResult Index()
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
                var cart = GetCart();
                cart.RemoveAll(c => c.Product == null || c.Product.IsDeleted);
                Session["Cart"] = cart;
                return View(cart);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Cart Index Error: " + ex.Message); return RedirectToAction("Index", "Home"); }
        }

        public ActionResult Checkout()
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
                var cart = GetCart();
                cart.RemoveAll(c => c.Product == null || c.Product.IsDeleted);
                if (!cart.Any()) { TempData["Error"] = "Your cart is empty."; return RedirectToAction("Index"); }
                return View(cart);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Checkout GET Error: " + ex.Message); return RedirectToAction("Index"); }
        }

        public ActionResult BuyNow(int id, string variantLabel, int qty = 1)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "Product", new { id = id }) });
                var product = MockData.Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
                if (product == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Index", "Product"); }
                if (product.Stock <= 0) { TempData["Error"] = "Product out of stock."; return RedirectToAction("Details", "Product", new { id = id }); }
                qty = Math.Min(Math.Max(1, qty), product.Stock);
                ProductVariant chosen = null;
                if (!string.IsNullOrEmpty(variantLabel) && product.Variants != null)
                    chosen = product.Variants.FirstOrDefault(v => v.Label == variantLabel);
                var item = new CartItem { Product = product, Quantity = qty, SelectedVariant = chosen };
                Session["BuyNowItem"] = item;
                return View("BuyNowCheckout", new List<CartItem> { item });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("BuyNow Error: " + ex.Message); return RedirectToAction("Details", "Product", new { id = id }); }
        }

        public ActionResult Add(int id, string returnUrl, string variantLabel, int qty = 1)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = returnUrl ?? Url.Action("Index", "Product") });
                var product = MockData.Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
                if (product == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Index", "Product"); }
                if (product.Stock <= 0) { TempData["Error"] = "Out of stock."; return RedirectToAction("Details", "Product", new { id = id }); }
                if (qty < 1) qty = 1;
                ProductVariant chosen = null;
                if (!string.IsNullOrEmpty(variantLabel) && product.Variants != null)
                    chosen = product.Variants.FirstOrDefault(v => v.Label == variantLabel);
                var cart = GetCart();
                var existing = cart.FirstOrDefault(c => c.Product.Id == id &&
                    ((c.SelectedVariant == null && chosen == null) ||
                     (c.SelectedVariant != null && chosen != null && c.SelectedVariant.Label == chosen.Label)));
                if (existing != null) existing.Quantity = Math.Min(existing.Quantity + qty, product.Stock);
                else cart.Add(new CartItem { Product = product, Quantity = Math.Min(qty, product.Stock), SelectedVariant = chosen });
                Session["Cart"] = cart;
                TempData["Success"] = product.Name + " added to cart!";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Index");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Cart Add Error: " + ex.Message); TempData["Error"] = "Could not add item."; return RedirectToAction("Index", "Product"); }
        }

        public ActionResult Remove(int id, string variantLabel)
        {
            try
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(c => c.Product.Id == id &&
                    ((string.IsNullOrEmpty(variantLabel) && c.SelectedVariant == null) ||
                     (c.SelectedVariant != null && c.SelectedVariant.Label == variantLabel)));
                if (item != null) cart.Remove(item);
                Session["Cart"] = cart;
                return RedirectToAction("Index");
            }
            catch { return RedirectToAction("Index"); }
        }

        public ActionResult Reorder(string referenceNumber)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Track", "Order") });
                var email = Session["UserEmail"]?.ToString();
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber && o.Email == email);
                if (order == null) { TempData["Error"] = "Order not found."; return RedirectToAction("Track", "Order"); }

                var cart = GetCart();
                foreach (var snap in order.Snapshots ?? new List<OrderItemSnapshot>())
                {
                    var product = MockData.Products.FirstOrDefault(p => p.Id == snap.ProductId && !p.IsDeleted);
                    if (product == null || product.Stock <= 0) continue;
                    var variant = product.Variants?.FirstOrDefault(v => v.Label == snap.VariantLabel);
                    var qty = Math.Min(Math.Max(1, snap.Quantity), product.Stock);
                    cart.Add(new CartItem { Product = product, SelectedVariant = variant, Quantity = qty });
                }

                Session["Cart"] = cart;
                TempData["Success"] = "Items from " + referenceNumber + " were added to your cart.";
                return RedirectToAction("Index");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Reorder Error: " + ex.Message); TempData["Error"] = "Could not reorder items."; return RedirectToAction("Track", "Order"); }
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int id, int quantity, string variantLabel)
        {
            try
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(c => c.Product.Id == id &&
                    ((string.IsNullOrEmpty(variantLabel) && c.SelectedVariant == null) ||
                     (c.SelectedVariant != null && c.SelectedVariant.Label == variantLabel)));
                if (item != null)
                {
                    if (quantity <= 0) cart.Remove(item);
                    else item.Quantity = Math.Min(quantity, item.Product.Stock);
                }
                Session["Cart"] = cart;
                return RedirectToAction("Index");
            }
            catch { return RedirectToAction("Index"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ChangeVariant(int id, string oldVariantLabel, string newVariantLabel)
        {
            try
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(c => c.Product.Id == id &&
                    ((string.IsNullOrEmpty(oldVariantLabel) && c.SelectedVariant == null) ||
                     (c.SelectedVariant != null && c.SelectedVariant.Label == oldVariantLabel)));
                if (item != null)
                {
                    var product = MockData.Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
                    if (product != null && product.Variants != null && !string.IsNullOrEmpty(newVariantLabel))
                        item.SelectedVariant = product.Variants.FirstOrDefault(v => v.Label == newVariantLabel);
                    else item.SelectedVariant = null;
                    Session["Cart"] = cart;
                }
                return RedirectToAction("Index");
            }
            catch { return RedirectToAction("Index"); }
        }

        [HttpPost]
        public ActionResult CheckoutSelected(int[] selectedId, string[] selectedVariantLabel, int[] selectedQty)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
                var cart = GetCart();
                var selected = new List<CartItem>();
                if (selectedId != null)
                {
                    for (int i = 0; i < selectedId.Length; i++)
                    {
                        var pid = selectedId[i];
                        var variant = (selectedVariantLabel != null && i < selectedVariantLabel.Length) ? selectedVariantLabel[i] : null;
                        var qty = (selectedQty != null && i < selectedQty.Length) ? Math.Max(1, selectedQty[i]) : 1;
                        var item = cart.FirstOrDefault(c => c.Product.Id == pid &&
                            ((string.IsNullOrEmpty(variant) && c.SelectedVariant == null) ||
                             (c.SelectedVariant != null && c.SelectedVariant.Label == variant)));
                        if (item != null) selected.Add(new CartItem { Product = item.Product, SelectedVariant = item.SelectedVariant, Quantity = Math.Min(qty, item.Product.Stock) });
                    }
                }
                if (!selected.Any()) { TempData["Error"] = "No items selected."; return RedirectToAction("Index"); }
                return View("Checkout", selected);
            }
            catch { return RedirectToAction("Index"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string address, string phone)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account");
                if (string.IsNullOrWhiteSpace(address)) { TempData["Error"] = "Delivery address is required."; return RedirectToAction("Checkout"); }
                var cart = GetCart();
                if (!cart.Any()) { TempData["Error"] = "Your cart is empty."; return RedirectToAction("Index"); }
                var shipping = cart.Sum(c => c.Subtotal) >= 500 ? 0 : 80;
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
                    ReferenceNumber = DbHelper.NextOrderReference(),
                    CustomerName = Session["UserName"].ToString(),
                    Email = Session["UserEmail"].ToString(),
                    Address = address.Trim(),
                    Phone = phone?.Trim(),
                    Items = new List<CartItem>(cart),
                    Snapshots = snapshots,
                    Total = cart.Sum(c => c.Subtotal) + shipping,
                    OrderDate = DateTime.Now,
                    Status = "To Ship"
                };
                order.Id = DbHelper.AddOrder(order);
                foreach (var item in cart)
                {
                    var p = MockData.Products.FirstOrDefault(x => x.Id == item.Product.Id);
                    if (p != null)
                    {
                        p.Stock = Math.Max(0, p.Stock - item.Quantity);
                        DbHelper.UpdateProductStock(p.Id, p.Stock);
                        if (item.SelectedVariant != null) DbHelper.UpdateVariantStock(p.Id, item.SelectedVariant.Label, Math.Max(0, item.SelectedVariant.Stock - item.Quantity));
                    }
                }
                MockData.RefreshOrders();
                MockData.RefreshProducts();
                Session["Cart"] = new List<CartItem>();
                return RedirectToAction("Confirmation", "Order", new { referenceNumber = order.ReferenceNumber });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("PlaceOrder Error: " + ex.Message); TempData["Error"] = "Order could not be placed."; return RedirectToAction("Checkout"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PlaceBuyNowOrder(string address, string phone)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account");
                if (string.IsNullOrWhiteSpace(address)) { TempData["Error"] = "Delivery address is required."; return RedirectToAction("Index", "Home"); }
                var buyNowItem = Session["BuyNowItem"] as CartItem;
                if (buyNowItem == null || buyNowItem.Product == null || buyNowItem.Product.IsDeleted) { TempData["Error"] = "Item no longer available."; return RedirectToAction("Index", "Product"); }
                var items = new List<CartItem> { buyNowItem };
                var shipping = items.Sum(c => c.Subtotal) >= 500 ? 0 : 80;
                var snapshots = items.Select(c => new OrderItemSnapshot
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
                    ReferenceNumber = DbHelper.NextOrderReference(),
                    CustomerName = Session["UserName"].ToString(),
                    Email = Session["UserEmail"].ToString(),
                    Address = address.Trim(),
                    Phone = phone?.Trim(),
                    Items = items,
                    Snapshots = snapshots,
                    Total = items.Sum(c => c.Subtotal) + shipping,
                    OrderDate = DateTime.Now,
                    Status = "To Ship"
                };
                order.Id = DbHelper.AddOrder(order);
                var p = MockData.Products.FirstOrDefault(x => x.Id == buyNowItem.Product.Id);
                if (p != null)
                {
                    p.Stock = Math.Max(0, p.Stock - buyNowItem.Quantity);
                    DbHelper.UpdateProductStock(p.Id, p.Stock);
                    if (buyNowItem.SelectedVariant != null) DbHelper.UpdateVariantStock(p.Id, buyNowItem.SelectedVariant.Label, Math.Max(0, buyNowItem.SelectedVariant.Stock - buyNowItem.Quantity));
                }
                MockData.RefreshOrders();
                MockData.RefreshProducts();
                Session["BuyNowItem"] = null;
                return RedirectToAction("Confirmation", "Order", new { referenceNumber = order.ReferenceNumber });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("PlaceBuyNowOrder Error: " + ex.Message); TempData["Error"] = "Order could not be placed."; return RedirectToAction("Index", "Home"); }
        }
    }

    // ════════════════════════════ ORDER ════════════════════════════
    public class OrderController : Controller
    {
        public ActionResult Track(string tab = "All")
        {
            try
            {
                if (Session["UserId"] == null) return RedirectToAction("Login", "Account", new { returnUrl = "/Order/Track" });
                var email = Session["UserEmail"]?.ToString();
                var orders = MockData.Orders.Where(o => o.Email == email).ToList();
                var currentUserId = 0; int.TryParse(Session["UserId"]?.ToString(), out currentUserId);
                ViewBag.Tab = tab; ViewBag.CurrentUserId = currentUserId;
                return View(orders);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Track Error: " + ex.Message); return RedirectToAction("Index", "Home"); }
        }

        public ActionResult Confirmation(string referenceNumber)
        {
            try
            {
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                if (order == null) return RedirectToAction("Index", "Home");
                return View(order);
            }
            catch { return RedirectToAction("Index", "Home"); }
        }

        public ActionResult MarkReceived(string referenceNumber)
        {
            try
            {
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                if (order != null)
                {
                    DbHelper.MarkOrderReceived(order.Id);
                    MockData.RefreshOrders();
                }
                return RedirectToAction("Orders", "Account");
            }
            catch { return RedirectToAction("Orders", "Account"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CancelOrder(string referenceNumber, string cancelReason)
        {
            try
            {
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                if (order != null && order.Status == "To Ship")
                {
                    DbHelper.CancelOrder(order.Id, cancelReason);
                    MockData.RefreshOrders();
                }
                return RedirectToAction("Orders", "Account");
            }
            catch { return RedirectToAction("Orders", "Account"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RequestRefund(string referenceNumber, string refundReason, string gcashNumber, string refundEvidenceUrl)
        {
            try
            {
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                if (order != null)
                {
                    DbHelper.RequestRefund(order.Id, refundReason, gcashNumber, refundEvidenceUrl);
                    MockData.RefreshOrders();
                }
                return RedirectToAction("Orders", "Account");
            }
            catch { return RedirectToAction("Orders", "Account"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SubmitReview(int productId, string referenceNumber, int stars, string comment, string photoUrl)
        {
            try
            {
                if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
                var userId = 0; int.TryParse(Session["UserId"]?.ToString(), out userId);
                var user = MockData.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null) return RedirectToAction("Login", "Account");
                var review = new Review
                {
                    ProductId = productId,
                    UserId = userId,
                    CustomerName = user.FullName,
                    Stars = Math.Max(1, Math.Min(5, stars)),
                    Comment = comment?.Trim(),
                    PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl,
                    DatePosted = DateTime.Now,
                    IsVerifiedPurchase = true
                };
                review.Id = DbHelper.AddReview(review);
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                if (order != null) DbHelper.MarkOrderReviewed(order.Id);
                MockData.RefreshReviews();
                MockData.RefreshOrders();
                return RedirectToAction("Orders", "Account");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("SubmitReview Error: " + ex.Message); return RedirectToAction("Orders", "Account"); }
        }
    }

    // ════════════════════════════ ADMIN ════════════════════════════
    public class AdminController : Controller
    {
        private bool IsAdmin => Session["UserEmail"]?.ToString() == MockData.AdminEmail;

        public ActionResult Login()
        {
            if (IsAdmin) return RedirectToAction("Dashboard");
            return View();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        public ActionResult KeepAlive()
        {
            return Json(new { ok = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            try
            {
                if (email == MockData.AdminEmail && password == MockData.AdminPassword)
                {
                    Session["UserEmail"] = email; return RedirectToAction("Dashboard");
                }
                ViewBag.Error = "Invalid admin credentials."; return View();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("AdminLogin Error: " + ex.Message); return View(); }
        }

        public ActionResult Dashboard()
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var orders = MockData.Orders;
                var products = MockData.Products.Where(p => !p.IsDeleted).ToList();
                ViewBag.TotalOrders = orders.Count;
                ViewBag.PendingOrders = orders.Count(o => o.Status == "To Ship");
                ViewBag.TotalRevenue = (decimal)orders.Sum(o => o.Total);
                ViewBag.TotalProducts = products.Count;
                ViewBag.TotalUsers = MockData.Users.Count;
                ViewBag.LowStockCount = products.Count(p => p.Stock > 0 && p.Stock <= 5);
                ViewBag.OutOfStockCount = products.Count(p => p.Stock == 0);
                ViewBag.RefundCount = orders.Count(o => o.Status == "Refund Requested");
                ViewBag.RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
                return View();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Dashboard Error: " + ex.Message); return View(); }
        }

        public ActionResult Orders()
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                return View(MockData.Orders.OrderByDescending(o => o.OrderDate).ToList());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Admin Orders Error: " + ex.Message); TempData["Error"] = "Could not load orders."; return RedirectToAction("Dashboard"); }
        }

        public ActionResult Products()
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                return View(MockData.Products.Where(p => !p.IsDeleted).ToList());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Admin Products Error: " + ex.Message); TempData["Error"] = "Could not load products."; return RedirectToAction("Dashboard"); }
        }

        public ActionResult AddProduct()
        {
            if (!IsAdmin) return RedirectToAction("Login");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddProduct(Product product, string[] VariantImagePaths, string[] VariantLabels, int[] VariantStocks)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                if (string.IsNullOrWhiteSpace(product.Name)) { TempData["Error"] = "Product name is required."; return RedirectToAction("AddProduct"); }
                if (product.Price <= 0) { TempData["Error"] = "Price must be greater than zero."; return RedirectToAction("AddProduct"); }
                if (product.OriginalPrice.HasValue && product.OriginalPrice.Value > 0 && product.OriginalPrice.Value <= product.Price) { TempData["Error"] = "Original Price must be greater than Sale Price."; return RedirectToAction("AddProduct"); }
                if (product.Stock < 0) { TempData["Error"] = "Stock cannot be negative."; return RedirectToAction("AddProduct"); }
                if (string.IsNullOrWhiteSpace(product.ImageUrl)) product.ImageUrl = "/Content/images/products/placeholder.png";
                product.Variants = new List<ProductVariant>();
                int maxV = Math.Max(VariantImagePaths != null ? VariantImagePaths.Length : 0, Math.Max(VariantLabels != null ? VariantLabels.Length : 0, VariantStocks != null ? VariantStocks.Length : 0));
                bool hasVariantStock = false; int totalVariantStock = 0;
                for (int i = 0; i < maxV; i++)
                {
                    var img = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                    var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                    var stock = (VariantStocks != null && i < VariantStocks.Length) ? VariantStocks[i] : 0;
                    if (!string.IsNullOrWhiteSpace(img) || !string.IsNullOrWhiteSpace(label))
                    {
                        product.Variants.Add(new ProductVariant { ImageUrl = string.IsNullOrWhiteSpace(img) ? null : img, Label = label, Stock = Math.Max(0, stock) });
                        hasVariantStock = true; totalVariantStock += Math.Max(0, stock);
                    }
                }
                if (hasVariantStock) product.Stock = totalVariantStock;
                product.Id = DbHelper.AddProduct(product);
                MockData.RefreshProducts();
                TempData["Success"] = "Product \"" + product.Name + "\" added!";
                return RedirectToAction("Products");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("AddProduct Error: " + ex.Message); TempData["Error"] = "Could not add product."; return RedirectToAction("AddProduct"); }
        }

        public ActionResult EditProduct(int id)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var p = MockData.Products.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
                if (p == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Products"); }
                return View(p);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("EditProduct GET Error: " + ex.Message); TempData["Error"] = "Could not load product."; return RedirectToAction("Products"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditProduct(Product updated, string[] VariantImagePaths, string[] VariantLabels, int[] VariantStocks)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                if (string.IsNullOrWhiteSpace(updated.Name)) { TempData["Error"] = "Product name is required."; return RedirectToAction("EditProduct", new { id = updated.Id }); }
                if (updated.Price <= 0) { TempData["Error"] = "Price must be greater than zero."; return RedirectToAction("EditProduct", new { id = updated.Id }); }
                if (updated.OriginalPrice.HasValue && updated.OriginalPrice.Value > 0 && updated.OriginalPrice.Value <= updated.Price) { TempData["Error"] = "Original Price must be greater than Sale Price."; return RedirectToAction("EditProduct", new { id = updated.Id }); }
                var p = MockData.Products.FirstOrDefault(x => x.Id == updated.Id);
                if (p == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Products"); }
                p.Name = updated.Name; p.Description = updated.Description; p.Price = updated.Price;
                p.OriginalPrice = updated.OriginalPrice; p.Category = updated.Category; p.BreedSize = updated.BreedSize;
                if (!string.IsNullOrWhiteSpace(updated.ImageUrl)) p.ImageUrl = updated.ImageUrl;
                p.Variants = new List<ProductVariant>();
                int maxV = Math.Max(VariantImagePaths != null ? VariantImagePaths.Length : 0, Math.Max(VariantLabels != null ? VariantLabels.Length : 0, VariantStocks != null ? VariantStocks.Length : 0));
                bool hasVariantStock = false; int totalVariantStock = 0;
                for (int i = 0; i < maxV; i++)
                {
                    var img = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                    var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                    var stock = (VariantStocks != null && i < VariantStocks.Length) ? VariantStocks[i] : 0;
                    if (!string.IsNullOrWhiteSpace(img) || !string.IsNullOrWhiteSpace(label))
                    {
                        p.Variants.Add(new ProductVariant { ImageUrl = string.IsNullOrWhiteSpace(img) ? null : img, Label = label, Stock = Math.Max(0, stock) });
                        hasVariantStock = true; totalVariantStock += Math.Max(0, stock);
                    }
                }
                p.Stock = hasVariantStock ? totalVariantStock : Math.Max(0, updated.Stock);
                DbHelper.UpdateProduct(p);
                MockData.RefreshProducts();
                TempData["Success"] = "Product \"" + updated.Name + "\" updated!";
                return RedirectToAction("Products");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("EditProduct POST Error: " + ex.Message); TempData["Error"] = "Could not update product."; return RedirectToAction("EditProduct", new { id = updated.Id }); }
        }

        public ActionResult DeleteProduct(int id)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var p = MockData.Products.FirstOrDefault(x => x.Id == id);
                if (p != null) { DbHelper.SoftDeleteProduct(id); MockData.RefreshProducts(); TempData["Success"] = "Product \"" + p.Name + "\" deleted."; }
                else TempData["Error"] = "Product not found.";
                return RedirectToAction("Products");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("DeleteProduct Error: " + ex.Message); TempData["Error"] = "Could not delete product."; return RedirectToAction("Products"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateStock(int id, int stock)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var p = MockData.Products.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
                if (p != null) { p.Stock = Math.Max(0, stock); DbHelper.UpdateProductStock(id, p.Stock); MockData.RefreshProducts(); TempData["Success"] = "Stock updated."; }
                return RedirectToAction("Products");
            }
            catch { return RedirectToAction("Products"); }
        }

        public ActionResult FlaggedReviews()
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                return View(MockData.Reviews.ToList());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("FlaggedReviews Error: " + ex.Message); return RedirectToAction("Dashboard"); }
        }

        public ActionResult DismissReport(int id)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var review = MockData.Reviews.FirstOrDefault(r => r.Id == id);
                if (review != null) { DbHelper.UpdateReviewReports(id, 0); MockData.RefreshReviews(); TempData["Success"] = "Reports dismissed."; }
                return RedirectToAction("FlaggedReviews");
            }
            catch { return RedirectToAction("FlaggedReviews"); }
        }

        public ActionResult DeleteReview(int id)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var review = MockData.Reviews.FirstOrDefault(r => r.Id == id);
                if (review != null) { DbHelper.DeleteReview(id); MockData.RefreshReviews(); TempData["Success"] = "Review deleted."; }
                return RedirectToAction("FlaggedReviews");
            }
            catch { return RedirectToAction("FlaggedReviews"); }
        }

        public ActionResult SalesReport()
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                return View();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("SalesReport Error: " + ex.Message); return RedirectToAction("Dashboard"); }
        }

        public ActionResult UpdateOrderStatus(int id = 0, int orderId = 0, string status = null)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                orderId = orderId != 0 ? orderId : id;
                var validStatuses = new[] { "To Ship", "In Transit", "Out for Delivery", "Cancelled", "Return/Refund", "Refund Requested", "Refund Approved", "Refund Denied", "Completed" };
                if (!validStatuses.Contains(status)) { TempData["Error"] = "Invalid status."; return RedirectToAction("Orders"); }
                var o = MockData.Orders.FirstOrDefault(x => x.Id == orderId);
                if (o != null)
                {
                    var isRefundDecision = status == "Refund Approved" || status == "Refund Denied";
                    if (o.IsReceivedByCustomer && !isRefundDecision) { TempData["Error"] = "Order already received by customer."; return RedirectToAction("Orders"); }
                    DbHelper.SetOrderStatus(orderId, status);
                    MockData.RefreshOrders();
                    TempData["Success"] = "Status updated to \"" + status + "\".";
                }
                else TempData["Error"] = "Order not found.";
                return RedirectToAction("Orders");
            }
            catch { return RedirectToAction("Orders"); }
        }

        public ActionResult DenyRefund(int id = 0, int orderId = 0, string reason = null)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                orderId = orderId != 0 ? orderId : id;
                var o = MockData.Orders.FirstOrDefault(x => x.Id == orderId);
                if (o != null) { DbHelper.DenyRefund(orderId, reason); MockData.RefreshOrders(); TempData["Success"] = "Refund denied."; }
                return RedirectToAction("Orders");
            }
            catch { return RedirectToAction("Orders"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PostSellerReply(int reviewId, string replyText)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var review = MockData.Reviews.FirstOrDefault(r => r.Id == reviewId);
                if (review != null) { DbHelper.UpdateSellerReply(reviewId, replyText); MockData.RefreshReviews(); }
                return RedirectToAction("FlaggedReviews");
            }
            catch { return RedirectToAction("FlaggedReviews"); }
        }

        public FileResult ExportSalesReport(string from, string to)
        {
            var orders = MockData.Orders.AsEnumerable();
            if (DateTime.TryParse(from, out var fromDate)) orders = orders.Where(o => o.OrderDate.Date >= fromDate.Date);
            if (DateTime.TryParse(to, out var toDate)) orders = orders.Where(o => o.OrderDate.Date <= toDate.Date);

            var csv = new StringBuilder();
            csv.AppendLine("Reference,Customer,Email,Date,Status,Total");
            foreach (var order in orders.OrderByDescending(o => o.OrderDate))
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    EscapeCsv(order.ReferenceNumber),
                    EscapeCsv(order.CustomerName),
                    EscapeCsv(order.Email),
                    EscapeCsv(order.OrderDate.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv(order.Status),
                    order.Total.ToString("0.00")
                }));
            }
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "pawchase-sales-report.csv");
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}

