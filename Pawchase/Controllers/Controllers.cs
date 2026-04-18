using System;
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
            try
            {
                if (IsLoggedIn) return RedirectToAction("Index", "Home");
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Login GET Error: " + ex.Message);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password, string returnUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "Email and password are required.";
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }

                var user = MockData.Users.FirstOrDefault(u =>
                    u.Email.ToLower() == email.ToLower().Trim() &&
                    u.Password == password);

                if (user != null)
                {
                    Session["UserId"] = user.Id;
                    Session["UserName"] = user.FullName;
                    Session["UserEmail"] = user.Email;
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Error = "Invalid email or password. Please try again.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Login POST Error: " + ex.Message);
                ViewBag.Error = "Login failed. Please try again.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
        }

        public ActionResult Register(string returnUrl)
        {
            try
            {
                if (IsLoggedIn) return RedirectToAction("Index", "Home");
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Register GET Error: " + ex.Message);
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(string fullName, string email, string password, string confirmPassword, string returnUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "All fields are required.";
                    return View();
                }
                if (password != confirmPassword)
                {
                    ViewBag.Error = "Passwords do not match.";
                    return View();
                }
                if (password.Length < 6)
                {
                    ViewBag.Error = "Password must be at least 6 characters.";
                    return View();
                }
                if (MockData.Users.Any(u => u.Email.ToLower() == email.ToLower().Trim()))
                {
                    ViewBag.Error = "This email is already registered.";
                    return View();
                }

                var user = new User
                {
                    Id = MockData.Users.Count + 1,
                    FullName = fullName.Trim(),
                    Email = email.Trim().ToLower(),
                    Password = password
                };
                MockData.Users.Add(user);
                Session["UserId"] = user.Id;
                Session["UserName"] = user.FullName;
                Session["UserEmail"] = user.Email;

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Register POST Error: " + ex.Message);
                ViewBag.Error = "Registration failed. Please try again.";
                return View();
            }
        }

        public ActionResult GoogleLogin(string returnUrl)
        {
            try
            {
                var email = "googleuser@gmail.com";
                var user = MockData.Users.FirstOrDefault(u => u.Email == email)
                         ?? new User { Id = MockData.Users.Count + 1, FullName = "Google User", Email = email };
                if (!MockData.Users.Contains(user)) MockData.Users.Add(user);
                Session["UserId"] = user.Id;
                Session["UserName"] = user.FullName;
                Session["UserEmail"] = user.Email;
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GoogleLogin Error: " + ex.Message);
                return RedirectToAction("Login");
            }
        }

        public ActionResult Orders(string tab = "All")
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login");
                var email = Session["UserEmail"].ToString();
                var orders = MockData.Orders.Where(o => o.Email == email).ToList();
                var userId = 0;
                int.TryParse(Session["UserId"]?.ToString(), out userId);
                var user = MockData.Users.FirstOrDefault(u => u.Id == userId);
                ViewBag.Tab = tab;
                ViewBag.ProfileUser = user;
                ViewBag.CurrentUserId = userId;
                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Orders Error: " + ex.Message);
                TempData["Error"] = "Could not load your orders. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveProfile(string fullName, string email, string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login");
                var userId = 0;
                int.TryParse(Session["UserId"]?.ToString(), out userId);
                var user = MockData.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null) return RedirectToAction("Login");

                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
                {
                    TempData["ProfileError"] = "Name and email are required.";
                    return RedirectToAction("Orders", new { tab = "Profile" });
                }
                if (MockData.Users.Any(u => u.Id != userId && u.Email.ToLower() == email.ToLower().Trim()))
                {
                    TempData["ProfileError"] = "That email is already in use by another account.";
                    return RedirectToAction("Orders", new { tab = "Profile" });
                }

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    if (user.Password != currentPassword)
                    {
                        TempData["ProfileError"] = "Current password is incorrect.";
                        return RedirectToAction("Orders", new { tab = "Profile" });
                    }
                    if (newPassword.Length < 6)
                    {
                        TempData["ProfileError"] = "New password must be at least 6 characters.";
                        return RedirectToAction("Orders", new { tab = "Profile" });
                    }
                    if (newPassword != confirmPassword)
                    {
                        TempData["ProfileError"] = "New passwords do not match.";
                        return RedirectToAction("Orders", new { tab = "Profile" });
                    }
                    user.Password = newPassword;
                }

                user.FullName = fullName.Trim();
                user.Email = email.Trim().ToLower();
                Session["UserName"] = user.FullName;
                Session["UserEmail"] = user.Email;

                TempData["ProfileSuccess"] = "Profile updated successfully.";
                return RedirectToAction("Orders", new { tab = "Profile" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SaveProfile Error: " + ex.Message);
                TempData["ProfileError"] = "Could not save profile. Please try again.";
                return RedirectToAction("Orders", new { tab = "Profile" });
            }
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
                if (!IsLoggedIn)
                    return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
                var cart = GetCart();
                cart.RemoveAll(c => c.Product == null || c.Product.IsDeleted);
                Session["Cart"] = cart;
                return View(cart);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Cart Index Error: " + ex.Message);
                TempData["Error"] = "Could not load your cart. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult Add(int id, string returnUrl, string variantLabel, int qty = 1)
        {
            try
            {
                if (!IsLoggedIn)
                    return RedirectToAction("Login", "Account",
                        new { returnUrl = returnUrl ?? Url.Action("Index", "Product") });

                var product = MockData.Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (product.Stock <= 0)
                {
                    TempData["Error"] = "Sorry, this product is out of stock.";
                    return RedirectToAction("Details", "Product", new { id = id });
                }

                if (qty < 1) qty = 1;

                ProductVariant chosen = null;
                if (!string.IsNullOrEmpty(variantLabel) && product.Variants != null)
                    chosen = product.Variants.FirstOrDefault(v => v.Label == variantLabel);

                var cart = GetCart();
                var existing = cart.FirstOrDefault(c =>
                    c.Product.Id == id &&
                    ((c.SelectedVariant == null && chosen == null) ||
                     (c.SelectedVariant != null && chosen != null &&
                      c.SelectedVariant.Label == chosen.Label)));

                if (existing != null)
                    existing.Quantity = Math.Min(existing.Quantity + qty, product.Stock);
                else
                    cart.Add(new CartItem
                    {
                        Product = product,
                        Quantity = Math.Min(qty, product.Stock),
                        SelectedVariant = chosen
                    });

                Session["Cart"] = cart;
                TempData["Success"] = product.Name + " added to cart!";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Cart Add Error: " + ex.Message);
                TempData["Error"] = "Could not add item to cart. Please try again.";
                return RedirectToAction("Index", "Product");
            }
        }

        public ActionResult Remove(int id, string variantLabel)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Cart Remove Error: " + ex.Message);
                TempData["Error"] = "Could not remove item. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int id, int quantity, string variantLabel)
        {
            try
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
                        item.Quantity = Math.Min(quantity, item.Product.Stock);
                }
                Session["Cart"] = cart;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Cart UpdateQty Error: " + ex.Message);
                TempData["Error"] = "Could not update quantity. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult Checkout()
        {
            try
            {
                if (!IsLoggedIn)
                    return RedirectToAction("Login", "Account",
                        new { returnUrl = Url.Action("Checkout", "Cart") });
                var cart = GetCart();
                cart.RemoveAll(c => c.Product == null || c.Product.IsDeleted);
                if (!cart.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }
                return View(cart);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Checkout GET Error: " + ex.Message);
                TempData["Error"] = "Could not load checkout. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // ── BUY NOW: single-item express checkout, never touches the cart ──
        public ActionResult BuyNow(int id, string variantLabel, int qty = 1)
        {
            try
            {
                if (!IsLoggedIn)
                    return RedirectToAction("Login", "Account",
                        new { returnUrl = Url.Action("Details", "Product", new { id = id }) });

                var product = MockData.Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }
                if (product.Stock <= 0)
                {
                    TempData["Error"] = "Sorry, this product is out of stock.";
                    return RedirectToAction("Details", "Product", new { id = id });
                }

                if (qty < 1) qty = 1;
                qty = Math.Min(qty, product.Stock);

                ProductVariant chosen = null;
                if (!string.IsNullOrEmpty(variantLabel) && product.Variants != null)
                    chosen = product.Variants.FirstOrDefault(v => v.Label == variantLabel);

                var buyNowItem = new CartItem
                {
                    Product = product,
                    Quantity = qty,
                    SelectedVariant = chosen
                };

                Session["BuyNowItem"] = buyNowItem;
                return View("BuyNowCheckout", new List<CartItem> { buyNowItem });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BuyNow Error: " + ex.Message);
                TempData["Error"] = "Could not start checkout. Please try again.";
                return RedirectToAction("Details", "Product", new { id = id });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PlaceBuyNowOrder(string address, string phone)
        {
            try
            {
                if (!IsLoggedIn)
                    return RedirectToAction("Login", "Account");

                if (string.IsNullOrWhiteSpace(address))
                {
                    TempData["Error"] = "Delivery address is required.";
                    return RedirectToAction("Index", "Home");
                }

                var buyNowItem = Session["BuyNowItem"] as CartItem;
                if (buyNowItem == null || buyNowItem.Product == null || buyNowItem.Product.IsDeleted)
                {
                    TempData["Error"] = "Your buy now item is no longer available.";
                    return RedirectToAction("Index", "Product");
                }

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
                    Id = MockData.Orders.Count + 1,
                    ReferenceNumber = "PWC-" + (MockData.Orders.Count + 1).ToString("D6"),
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

                MockData.Orders.Add(order);

                var p = MockData.Products.FirstOrDefault(x => x.Id == buyNowItem.Product.Id);
                if (p != null) p.Stock = Math.Max(0, p.Stock - buyNowItem.Quantity);

                Session["BuyNowItem"] = null;
                return RedirectToAction("Confirmation", "Order",
                    new { referenceNumber = order.ReferenceNumber });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PlaceBuyNowOrder Error: " + ex.Message);
                TempData["Error"] = "Order could not be placed. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string address, string phone)
        {
            try
            {
                if (!IsLoggedIn)
                    return RedirectToAction("Login", "Account");

                if (string.IsNullOrWhiteSpace(address))
                {
                    TempData["Error"] = "Delivery address is required.";
                    return RedirectToAction("Checkout");
                }

                var cart = GetCart();
                if (!cart.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

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
                    Id = MockData.Orders.Count + 1,
                    ReferenceNumber = "PWC-" + (MockData.Orders.Count + 1).ToString("D6"),
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

                MockData.Orders.Add(order);

                foreach (var item in cart)
                {
                    var p = MockData.Products.FirstOrDefault(x => x.Id == item.Product.Id);
                    if (p != null) p.Stock = Math.Max(0, p.Stock - item.Quantity);
                }

                Session["Cart"] = new List<CartItem>();
                return RedirectToAction("Confirmation", "Order",
                    new { referenceNumber = order.ReferenceNumber });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PlaceOrder Error: " + ex.Message);
                TempData["Error"] = "Order could not be placed. Please try again.";
                return RedirectToAction("Checkout");
            }
        }

        // NEW: Update the selected variant for an existing cart item
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ChangeVariant(int id, string oldVariantLabel, string newVariantLabel)
        {
            try
            {
                var cart = GetCart();
                var item = cart.FirstOrDefault(c =>
                    c.Product.Id == id &&
                    ((string.IsNullOrEmpty(oldVariantLabel) && c.SelectedVariant == null) ||
                     (c.SelectedVariant != null && c.SelectedVariant.Label == oldVariantLabel)));

                if (item != null)
                {
                    var product = MockData.Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
                    if (product != null && product.Variants != null && !string.IsNullOrEmpty(newVariantLabel))
                    {
                        var chosen = product.Variants.FirstOrDefault(v => v.Label == newVariantLabel);
                        item.SelectedVariant = chosen;
                    }
                    else
                    {
                        item.SelectedVariant = null;
                    }

                    Session["Cart"] = cart;
                    TempData["Success"] = "Variant updated.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ChangeVariant Error: " + ex.Message);
                TempData["Error"] = "Could not update variant. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public ActionResult CheckoutSelected(int[] selectedId, string[] selectedVariantLabel, int[] selectedQty)
        {
            try
            {
                if (!IsLoggedIn)
                    return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });

                var cart = GetCart();
                var selected = new List<CartItem>();

                if (selectedId != null && selectedId.Length > 0)
                {
                    for (int i = 0; i < selectedId.Length; i++)
                    {
                        var id = selectedId[i];
                        var variant = (selectedVariantLabel != null && i < selectedVariantLabel.Length) ? selectedVariantLabel[i] : null;
                        var qty = (selectedQty != null && i < selectedQty.Length) ? Math.Max(1, selectedQty[i]) : 1;

                        var item = cart.FirstOrDefault(c => c.Product.Id == id &&
                            ((string.IsNullOrEmpty(variant) && c.SelectedVariant == null) ||
                             (c.SelectedVariant != null && c.SelectedVariant.Label == variant)));

                        if (item != null)
                        {
                            selected.Add(new CartItem
                            {
                                Product = item.Product,
                                SelectedVariant = item.SelectedVariant,
                                Quantity = Math.Min(qty, item.Product.Stock)
                            });
                        }
                    }
                }

                if (!selected.Any())
                {
                    TempData["Error"] = "No items selected for checkout.";
                    return RedirectToAction("Index");
                }

                return View("Checkout", selected);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CheckoutSelected Error: " + ex.Message);
                TempData["Error"] = "Could not start checkout. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }

    // ════════════════════════════ ORDER ═══════════════════════════════
    public class OrderController : Controller
    {
        public ActionResult Track(string tab = "All")
        {
            try
            {
                if (Session["UserId"] == null) return RedirectToAction("Login", "Account", new { returnUrl = "/Order/Track" });
                var email = Session["UserEmail"]?.ToString();
                var orders = MockData.Orders.Where(o => o.Email == email).ToList();
                var currentUserId = 0;
                int.TryParse(Session["UserId"]?.ToString(), out currentUserId);
                ViewBag.Tab = tab;
                ViewBag.CurrentUserId = currentUserId;
                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Track Error: " + ex.Message);
                TempData["Error"] = "Could not load your orders. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }


        public ActionResult Confirmation(string referenceNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(referenceNumber))
                    return RedirectToAction("Index", "Home");
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                if (order == null) return HttpNotFound();
                return View(order);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Confirmation Error: " + ex.Message);
                return RedirectToAction("Error", "Home");
            }
        }

        public ActionResult MarkReceived(string referenceNumber)
        {
            try
            {
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                if (order != null) order.Status = "To Rate";
                return RedirectToAction("Orders", "Account");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MarkReceived Error: " + ex.Message);
                TempData["Error"] = "Could not update order. Please try again.";
                return RedirectToAction("Orders", "Account");
            }
        }

        public ActionResult RequestRefund(string referenceNumber)
        {
            try
            {
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                if (order != null) { order.Status = "Refund Requested"; order.HasRefundRequest = true; }
                return RedirectToAction("Orders", "Account");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RequestRefund Error: " + ex.Message);
                TempData["Error"] = "Could not submit refund request. Please try again.";
                return RedirectToAction("Orders", "Account");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SubmitReview(int productId, string referenceNumber, int stars, string comment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(comment))
                {
                    TempData["Error"] = "Please write a review comment.";
                    return RedirectToAction("Orders", "Account", new { tab = "To Rate" });
                }
                if (stars < 1 || stars > 5)
                {
                    TempData["Error"] = "Please select a star rating.";
                    return RedirectToAction("Orders", "Account", new { tab = "To Rate" });
                }

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

                TempData["Success"] = "Thank you for your review!";
                return RedirectToAction("Orders", "Account", new { tab = "Completed" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SubmitReview Error: " + ex.Message);
                TempData["Error"] = "Could not submit review. Please try again.";
                return RedirectToAction("Orders", "Account", new { tab = "To Rate" });
            }
        }
    }

    // ════════════════════════════ ADMIN ═══════════════════════════════
    public class AdminController : Controller
    {
        private bool IsAdmin => Session["IsAdmin"] != null && (bool)Session["IsAdmin"];

        public ActionResult Login()
        {
            if (IsAdmin) return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "Email and password are required.";
                    return View();
                }
                if (email == MockData.AdminEmail && password == MockData.AdminPassword)
                {
                    Session["IsAdmin"] = true;
                    Session["AdminName"] = "Admin";
                    return RedirectToAction("Dashboard");
                }
                ViewBag.Error = "Invalid admin credentials.";
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Admin Login Error: " + ex.Message);
                ViewBag.Error = "Login failed. Please try again.";
                return View();
            }
        }

        public ActionResult Logout()
        {
            Session["IsAdmin"] = null;
            Session["AdminName"] = null;
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Dashboard()
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                ViewBag.TotalProducts = MockData.Products.Count(p => !p.IsDeleted);
                ViewBag.TotalOrders = MockData.Orders.Count;
                ViewBag.PendingOrders = MockData.Orders.Count(o => o.Status == "To Ship");
                ViewBag.TotalRevenue = MockData.Orders.Sum(o => o.Total);
                ViewBag.TotalUsers = MockData.Users.Count;
                ViewBag.RecentOrders = MockData.Orders.OrderByDescending(o => o.OrderDate).Take(5).ToList();
                ViewBag.RefundCount = MockData.Orders.Count(o => o.HasRefundRequest);
                ViewBag.LowStockCount = MockData.Products.Count(p => !p.IsDeleted && p.Stock > 0 && p.Stock <= 5);
                ViewBag.OutOfStockCount = MockData.Products.Count(p => !p.IsDeleted && p.Stock == 0);
                ViewBag.TotalReviews = MockData.Reviews.Count;
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard Error: " + ex.Message);
                TempData["Error"] = "Could not load dashboard data.";
                return View();
            }
        }

        public ActionResult Products()
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                return View(MockData.Products.Where(p => !p.IsDeleted).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Admin Products Error: " + ex.Message);
                TempData["Error"] = "Could not load products.";
                return RedirectToAction("Dashboard");
            }
        }

        public ActionResult AddProduct()
        {
            if (!IsAdmin) return RedirectToAction("Login");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddProduct(Product product, string[] VariantImagePaths, string[] VariantLabels)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");

                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    TempData["Error"] = "Product name is required.";
                    return RedirectToAction("AddProduct");
                }
                if (product.Price <= 0)
                {
                    TempData["Error"] = "Price must be greater than zero.";
                    return RedirectToAction("AddProduct");
                }
                if (product.OriginalPrice.HasValue && product.OriginalPrice.Value > 0
                    && product.OriginalPrice.Value <= product.Price)
                {
                    TempData["Error"] = "Original Price must be greater than Sale Price.";
                    return RedirectToAction("AddProduct");
                }
                if (product.Stock < 0)
                {
                    TempData["Error"] = "Stock cannot be negative.";
                    return RedirectToAction("AddProduct");
                }

                product.Id = MockData.Products.Any()
                    ? MockData.Products.Max(p => p.Id) + 1
                    : 1;

                if (string.IsNullOrWhiteSpace(product.ImageUrl))
                    product.ImageUrl = "/Content/images/products/placeholder.png";

                product.Variants = new List<ProductVariant>();
                int maxV = Math.Max(
                    VariantImagePaths != null ? VariantImagePaths.Length : 0,
                    VariantLabels != null ? VariantLabels.Length : 0);

                for (int i = 0; i < maxV; i++)
                {
                    var img = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                    var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                    if (!string.IsNullOrWhiteSpace(img) || !string.IsNullOrWhiteSpace(label))
                        product.Variants.Add(new ProductVariant
                        {
                            ImageUrl = string.IsNullOrWhiteSpace(img) ? null : img,
                            Label = label
                        });
                }

                MockData.Products.Add(product);
                TempData["Success"] = "Product \"" + product.Name + "\" added!";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AddProduct Error: " + ex.Message);
                TempData["Error"] = "Could not add product. Please try again.";
                return RedirectToAction("AddProduct");
            }
        }

        public ActionResult EditProduct(int id)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var p = MockData.Products.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
                if (p == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Products");
                }
                return View(p);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EditProduct GET Error: " + ex.Message);
                TempData["Error"] = "Could not load product.";
                return RedirectToAction("Products");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditProduct(Product updated, string[] VariantImagePaths, string[] VariantLabels)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");

                if (string.IsNullOrWhiteSpace(updated.Name))
                {
                    TempData["Error"] = "Product name is required.";
                    return RedirectToAction("EditProduct", new { id = updated.Id });
                }
                if (updated.Price <= 0)
                {
                    TempData["Error"] = "Price must be greater than zero.";
                    return RedirectToAction("EditProduct", new { id = updated.Id });
                }
                if (updated.OriginalPrice.HasValue && updated.OriginalPrice.Value > 0
                    && updated.OriginalPrice.Value <= updated.Price)
                {
                    TempData["Error"] = "Original Price must be greater than Sale Price.";
                    return RedirectToAction("EditProduct", new { id = updated.Id });
                }

                var p = MockData.Products.FirstOrDefault(x => x.Id == updated.Id);
                if (p == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Products");
                }

                p.Name = updated.Name;
                p.Description = updated.Description;
                p.Price = updated.Price;
                p.OriginalPrice = updated.OriginalPrice;
                p.Category = updated.Category;
                p.BreedSize = updated.BreedSize;
                p.Stock = Math.Max(0, updated.Stock);
                if (!string.IsNullOrWhiteSpace(updated.ImageUrl))
                    p.ImageUrl = updated.ImageUrl;

                p.Variants = new List<ProductVariant>();
                int maxV = Math.Max(
                    VariantImagePaths != null ? VariantImagePaths.Length : 0,
                    VariantLabels != null ? VariantLabels.Length : 0);

                for (int i = 0; i < maxV; i++)
                {
                    var img = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                    var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                    if (!string.IsNullOrWhiteSpace(img) || !string.IsNullOrWhiteSpace(label))
                        p.Variants.Add(new ProductVariant
                        {
                            ImageUrl = string.IsNullOrWhiteSpace(img) ? null : img,
                            Label = label
                        });
                }

                TempData["Success"] = "Product \"" + updated.Name + "\" updated!";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EditProduct POST Error: " + ex.Message);
                TempData["Error"] = "Could not update product. Please try again.";
                return RedirectToAction("EditProduct", new { id = updated.Id });
            }
        }

        public ActionResult DeleteProduct(int id)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var p = MockData.Products.FirstOrDefault(x => x.Id == id);
                if (p != null)
                {
                    p.IsDeleted = true;
                    TempData["Success"] = "Product \"" + p.Name + "\" deleted.";
                }
                else
                {
                    TempData["Error"] = "Product not found.";
                }
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DeleteProduct Error: " + ex.Message);
                TempData["Error"] = "Could not delete product. Please try again.";
                return RedirectToAction("Products");
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateStock(int id, int stock)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var p = MockData.Products.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
                if (p != null)
                {
                    p.Stock = Math.Max(0, stock);
                    TempData["Success"] = "Stock for \"" + p.Name + "\" updated to " + p.Stock + ".";
                }
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateStock Error: " + ex.Message);
                TempData["Error"] = "Could not update stock.";
                return RedirectToAction("Products");
            }
        }

        public ActionResult Orders()
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                return View(MockData.Orders.OrderByDescending(o => o.OrderDate).ToList());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Admin Orders Error: " + ex.Message);
                TempData["Error"] = "Could not load orders.";
                return RedirectToAction("Dashboard");
            }
        }

        public ActionResult UpdateOrderStatus(int id, string status)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                var validStatuses = new[] { "To Ship", "In Transit", "Out for Delivery",
                                            "Delivered", "To Rate", "Completed", "Refund Requested" };
                if (!validStatuses.Contains(status))
                {
                    TempData["Error"] = "Invalid order status.";
                    return RedirectToAction("Orders");
                }
                var o = MockData.Orders.FirstOrDefault(x => x.Id == id);
                if (o != null)
                {
                    o.Status = status;
                    if (status != "Refund Requested") o.HasRefundRequest = false;
                    TempData["Success"] = "Order status updated to \"" + status + "\".";
                }
                else
                {
                    TempData["Error"] = "Order not found.";
                }
                return RedirectToAction("Orders");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateOrderStatus Error: " + ex.Message);
                TempData["Error"] = "Could not update order status.";
                return RedirectToAction("Orders");
            }
        }
    }
}
