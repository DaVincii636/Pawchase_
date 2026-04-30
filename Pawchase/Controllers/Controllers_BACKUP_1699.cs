using Pawchase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // ── Updated SaveProfile — now saves Phone and GCashNumber too ──
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveProfile(string fullName, string email, string phone,
                                        string gcashNumber,
                                        string currentPassword, string newPassword, string confirmPassword)
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

                // Password change (optional)
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
                user.Phone = phone?.Trim();
                user.GCashNumber = gcashNumber?.Trim();
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            FormsAuthentication.SignOut();

            return RedirectToAction("Login", "Account");
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
<<<<<<< Updated upstream

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
                if (p != null)
                {
                    if (buyNowItem.SelectedVariant != null && p.Variants != null)
                    {
                        var pv = p.Variants.FirstOrDefault(v => v.Label == buyNowItem.SelectedVariant.Label);
                        if (pv != null) pv.Stock = Math.Max(0, pv.Stock - buyNowItem.Quantity);
                        // Recalculate total product stock from variants
                        p.Stock = p.Variants.Sum(v => v.Stock);
                    }
                    else
                    {
                        p.Stock = Math.Max(0, p.Stock - buyNowItem.Quantity);
                    }
                }

                Session["BuyNowItem"] = null;
                return RedirectToAction("Confirmation", "Order",
                    new { referenceNumber = order.ReferenceNumber });
=======
>>>>>>> Stashed changes
            }

            public ActionResult Add(int id, string returnUrl, string variantLabel, int qty = 1)
            {
                try
                {
                    if (!IsLoggedIn)
                        return RedirectToAction("Login", "Account",
                            new { returnUrl = returnUrl ?? Url.Action("Index", "Product") });

<<<<<<< Updated upstream
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
                    if (p != null)
                    {
                        if (item.SelectedVariant != null && p.Variants != null)
                        {
                            var pv = p.Variants.FirstOrDefault(v => v.Label == item.SelectedVariant.Label);
                            if (pv != null) pv.Stock = Math.Max(0, pv.Stock - item.Quantity);
                            // Recalculate total product stock from variants
                            p.Stock = p.Variants.Sum(v => v.Stock);
                        }
                        else
                        {
                            p.Stock = Math.Max(0, p.Stock - item.Quantity);
                        }
                    }
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
=======
>>>>>>> Stashed changes
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
                        if (quantity <= 0) cart.Remove(item);
                        else item.Quantity = Math.Min(quantity, item.Product.Stock);
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
                            item.SelectedVariant = product.Variants.FirstOrDefault(v => v.Label == newVariantLabel);
                        else
                            item.SelectedVariant = null;

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
                                selected.Add(new CartItem
                                {
                                    Product = item.Product,
                                    SelectedVariant = item.SelectedVariant,
                                    Quantity = Math.Min(qty, item.Product.Stock)
                                });
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

<<<<<<< Updated upstream
        // ── Cancel order (only allowed within 15 min of placing, while To Ship) ───
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CancelOrder(string referenceNumber, string cancelReason)
=======
        // ════════════════════════════ ORDER ═══════════════════════════════
        public class OrderController : Controller
>>>>>>> Stashed changes
        {
            public ActionResult Track(string tab = "All")
            {
                try
                {
                    if (Session["UserId"] == null)
                        return RedirectToAction("Login", "Account", new { returnUrl = "/Order/Track" });
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

            // ── Mark order as Completed (skipping To Rate) ───────────────
            public ActionResult MarkReceived(string referenceNumber)
            {
                try
                {
                    var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                    if (order != null)
                    {
                        order.Status = "Completed";
                        order.IsReceivedByCustomer = true;
                    }
                    TempData["Success"] = "Order marked as received!";
                    return RedirectToAction("Track", "Order", new { tab = "Completed" });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("MarkReceived Error: " + ex.Message);
                    TempData["Error"] = "Could not update order. Please try again.";
                    return RedirectToAction("Track", "Order", new { tab = "Out for Delivery" });
                }
            }

            // ── Cancel order (only allowed on To Ship) ───────────────────
            [HttpPost, ValidateAntiForgeryToken]
            public ActionResult CancelOrder(string referenceNumber, string cancelReason)
            {
                try
                {
                    var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                    if (order == null)
                    {
                        TempData["Error"] = "Order not found.";
                        return RedirectToAction("Track", "Order", new { tab = "To Ship" });
                    }
                    if (order.Status != "To Ship")
                    {
                        TempData["Error"] = "Order can no longer be cancelled.";
                        return RedirectToAction("Track", "Order", new { tab = order.Status });
                    }

                    order.Status = "Cancelled";
                    order.CancelReason = cancelReason;

                    // Restore stock
                    if (order.Snapshots != null)
                    {
                        foreach (var snap in order.Snapshots)
                        {
                            var product = MockData.Products.FirstOrDefault(p => p.Id == snap.ProductId);
                            if (product != null) product.Stock += snap.Quantity;
                        }
                    }

                    TempData["Success"] = "Your order has been cancelled.";
                    return RedirectToAction("Track", "Order", new { tab = "Cancelled" });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("CancelOrder Error: " + ex.Message);
                    TempData["Error"] = "Could not cancel order. Please try again.";
                    return RedirectToAction("Track", "Order", new { tab = "To Ship" });
                }
<<<<<<< Updated upstream
                if (order.Status != "To Ship")
                {
                    TempData["Error"] = "Order can no longer be cancelled.";
                    return RedirectToAction("Track", "Order", new { tab = order.Status });
                }
                if ((DateTime.Now - order.OrderDate).TotalMinutes > 15)
                {
                    TempData["Error"] = "The 15-minute cancellation window has passed. Please use Return/Refund instead.";
                    return RedirectToAction("Track", "Order", new { tab = "To Ship" });
                }
=======
            }
>>>>>>> Stashed changes

            // ── Request Return/Refund (GCash-based) ──────────────────────
            [HttpPost, ValidateAntiForgeryToken]
            public ActionResult RequestRefund(string referenceNumber, string refundReason, string gcashNumber, string refundEvidenceUrl)
            {
                try
                {
                    var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                    var fallbackTab = order?.Status ?? "Out for Delivery";

                    if (string.IsNullOrWhiteSpace(gcashNumber) || gcashNumber.Trim().Length != 11 || !gcashNumber.Trim().All(char.IsDigit))
                    {
<<<<<<< Updated upstream
                        var product = MockData.Products.FirstOrDefault(p => p.Id == snap.ProductId);
                        if (product != null)
                        {
                            if (!string.IsNullOrEmpty(snap.VariantLabel) && product.Variants != null)
                            {
                                var pv = product.Variants.FirstOrDefault(v => v.Label == snap.VariantLabel);
                                if (pv != null) pv.Stock += snap.Quantity;
                                product.Stock = product.Variants.Sum(v => v.Stock);
                            }
                            else
                            {
                                product.Stock += snap.Quantity;
                            }
                        }
=======
                        TempData["Error"] = "Please enter a valid 11-digit GCash number.";
                        return RedirectToAction("Track", "Order", new { tab = fallbackTab });
>>>>>>> Stashed changes
                    }

                    if (order != null)
                    {
                        order.Status = "Return/Refund";
                        order.HasRefundRequest = true;
                        order.RefundReason = refundReason;
                        order.GCashNumber = gcashNumber.Trim();
                        order.RefundEvidenceUrl = refundEvidenceUrl;
                    }

                    TempData["Success"] = "Your return/refund request has been submitted.";
                    return RedirectToAction("Track", "Order", new { tab = "Return/Refund" });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("RequestRefund Error: " + ex.Message);
                    TempData["Error"] = "Could not submit refund request. Please try again.";
                    return RedirectToAction("Track", "Order", new { tab = "Out for Delivery" });
                }
            }

            // ── Submit Review from Completed tab ─────────────────────────
            [HttpPost, ValidateAntiForgeryToken]
            public ActionResult SubmitReview(int productId, string referenceNumber, int stars, string comment)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(comment))
                    {
                        TempData["Error"] = "Please write a review comment.";
                        return RedirectToAction("Track", "Order", new { tab = "Completed" });
                    }
                    if (stars < 1 || stars > 5)
                    {
                        TempData["Error"] = "Please select a star rating.";
                        return RedirectToAction("Track", "Order", new { tab = "Completed" });
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

                    // Mark the order as reviewed so the Rate button is hidden afterward
                    var reviewedOrder = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                    if (reviewedOrder != null) reviewedOrder.IsReviewed = true;

                    // Order stays Completed after rating
                    TempData["Success"] = "Thank you for your review!";
                    return RedirectToAction("Track", "Order", new { tab = "Completed" });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SubmitReview Error: " + ex.Message);
                    TempData["Error"] = "Could not submit review. Please try again.";
                    return RedirectToAction("Track", "Order", new { tab = "Completed" });
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
<<<<<<< Updated upstream
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
        public ActionResult AddProduct(Product product, string[] VariantImagePaths, string[] VariantLabels, int[] VariantStocks)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");

                if (string.IsNullOrWhiteSpace(product.Name))
                { TempData["Error"] = "Product name is required."; return RedirectToAction("AddProduct"); }
                if (product.Price <= 0)
                { TempData["Error"] = "Price must be greater than zero."; return RedirectToAction("AddProduct"); }
                if (product.OriginalPrice.HasValue && product.OriginalPrice.Value > 0
                    && product.OriginalPrice.Value <= product.Price)
                { TempData["Error"] = "Original Price must be greater than Sale Price."; return RedirectToAction("AddProduct"); }
                if (product.Stock < 0)
                { TempData["Error"] = "Stock cannot be negative."; return RedirectToAction("AddProduct"); }

                product.Id = MockData.Products.Any() ? MockData.Products.Max(p => p.Id) + 1 : 1;

                if (string.IsNullOrWhiteSpace(product.ImageUrl))
                    product.ImageUrl = "/Content/images/products/placeholder.png";

                product.Variants = new List<ProductVariant>();
                int maxV = Math.Max(
                    VariantImagePaths != null ? VariantImagePaths.Length : 0,
                    Math.Max(
                        VariantLabels != null ? VariantLabels.Length : 0,
                        VariantStocks != null ? VariantStocks.Length : 0));

                bool hasVariantStock = false;
                int totalVariantStock = 0;
                for (int i = 0; i < maxV; i++)
                {
                    var img = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                    var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                    var stock = (VariantStocks != null && i < VariantStocks.Length) ? VariantStocks[i] : 0;
                    if (!string.IsNullOrWhiteSpace(img) || !string.IsNullOrWhiteSpace(label))
                    {
                        product.Variants.Add(new ProductVariant
                        {
                            ImageUrl = string.IsNullOrWhiteSpace(img) ? null : img,
                            Label = label,
                            Stock = Math.Max(0, stock)
                        });
                        hasVariantStock = true;
                        totalVariantStock += Math.Max(0, stock);
                    }
                }

                // If variants have stock set, use the sum; otherwise use the manually entered stock
                if (hasVariantStock)
                    product.Stock = totalVariantStock;

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
                if (p == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Products"); }
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
        public ActionResult EditProduct(Product updated, string[] VariantImagePaths, string[] VariantLabels, int[] VariantStocks)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");

                if (string.IsNullOrWhiteSpace(updated.Name))
                { TempData["Error"] = "Product name is required."; return RedirectToAction("EditProduct", new { id = updated.Id }); }
                if (updated.Price <= 0)
                { TempData["Error"] = "Price must be greater than zero."; return RedirectToAction("EditProduct", new { id = updated.Id }); }
                if (updated.OriginalPrice.HasValue && updated.OriginalPrice.Value > 0
                    && updated.OriginalPrice.Value <= updated.Price)
                { TempData["Error"] = "Original Price must be greater than Sale Price."; return RedirectToAction("EditProduct", new { id = updated.Id }); }

                var p = MockData.Products.FirstOrDefault(x => x.Id == updated.Id);
                if (p == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Products"); }

                p.Name = updated.Name;
                p.Description = updated.Description;
                p.Price = updated.Price;
                p.OriginalPrice = updated.OriginalPrice;
                p.Category = updated.Category;
                p.BreedSize = updated.BreedSize;
                if (!string.IsNullOrWhiteSpace(updated.ImageUrl))
                    p.ImageUrl = updated.ImageUrl;

                p.Variants = new List<ProductVariant>();
                int maxV = Math.Max(
                    VariantImagePaths != null ? VariantImagePaths.Length : 0,
                    Math.Max(
                        VariantLabels != null ? VariantLabels.Length : 0,
                        VariantStocks != null ? VariantStocks.Length : 0));

                bool hasVariantStock = false;
                int totalVariantStock = 0;
                for (int i = 0; i < maxV; i++)
                {
                    var img = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                    var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                    var stock = (VariantStocks != null && i < VariantStocks.Length) ? VariantStocks[i] : 0;
                    if (!string.IsNullOrWhiteSpace(img) || !string.IsNullOrWhiteSpace(label))
                    {
                        p.Variants.Add(new ProductVariant
                        {
                            ImageUrl = string.IsNullOrWhiteSpace(img) ? null : img,
                            Label = label,
                            Stock = Math.Max(0, stock)
                        });
                        hasVariantStock = true;
                        totalVariantStock += Math.Max(0, stock);
                    }
=======
                try
                {
                    if (!IsAdmin) return RedirectToAction("Login");

                    if (string.IsNullOrWhiteSpace(product.Name))
                    { TempData["Error"] = "Product name is required."; return RedirectToAction("AddProduct"); }
                    if (product.Price <= 0)
                    { TempData["Error"] = "Price must be greater than zero."; return RedirectToAction("AddProduct"); }
                    if (product.OriginalPrice.HasValue && product.OriginalPrice.Value > 0
                        && product.OriginalPrice.Value <= product.Price)
                    { TempData["Error"] = "Original Price must be greater than Sale Price."; return RedirectToAction("AddProduct"); }
                    if (product.Stock < 0)
                    { TempData["Error"] = "Stock cannot be negative."; return RedirectToAction("AddProduct"); }

                    product.Id = MockData.Products.Any() ? MockData.Products.Max(p => p.Id) + 1 : 1;

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
>>>>>>> Stashed changes
                }
            }

<<<<<<< Updated upstream
                // If variants have stock set, use the sum; otherwise use the manually entered stock
                p.Stock = hasVariantStock ? totalVariantStock : Math.Max(0, updated.Stock);

                TempData["Success"] = "Product \"" + updated.Name + "\" updated!";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
=======
            public ActionResult EditProduct(int id)
>>>>>>> Stashed changes
            {
                try
                {
                    if (!IsAdmin) return RedirectToAction("Login");
                    var p = MockData.Products.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
                    if (p == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Products"); }
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
                    { TempData["Error"] = "Product name is required."; return RedirectToAction("EditProduct", new { id = updated.Id }); }
                    if (updated.Price <= 0)
                    { TempData["Error"] = "Price must be greater than zero."; return RedirectToAction("EditProduct", new { id = updated.Id }); }
                    if (updated.OriginalPrice.HasValue && updated.OriginalPrice.Value > 0
                        && updated.OriginalPrice.Value <= updated.Price)
                    { TempData["Error"] = "Original Price must be greater than Sale Price."; return RedirectToAction("EditProduct", new { id = updated.Id }); }

                    var p = MockData.Products.FirstOrDefault(x => x.Id == updated.Id);
                    if (p == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Products"); }

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
                    if (p != null) { p.IsDeleted = true; TempData["Success"] = "Product \"" + p.Name + "\" deleted."; }
                    else TempData["Error"] = "Product not found.";
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
                                            "Cancelled", "Return/Refund",
                                            "Refund Requested", "Refund Approved", "Completed" };
                    if (!validStatuses.Contains(status))
                    { TempData["Error"] = "Invalid order status."; return RedirectToAction("Orders"); }

                    var o = MockData.Orders.FirstOrDefault(x => x.Id == id);
                    if (o != null)
                    {
                        if (o.IsReceivedByCustomer)
                        {
                            TempData["Error"] = "This order was confirmed received by the customer and cannot be changed.";
                            return RedirectToAction("Orders");
                        }
                        o.Status = status;
                        if (status == "Refund Approved" || status == "Completed")
                            o.HasRefundRequest = false;
                        TempData["Success"] = "Order status updated to \"" + status + "\".";
                    }
                    else TempData["Error"] = "Order not found.";

                    return RedirectToAction("Orders");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("UpdateOrderStatus Error: " + ex.Message);
                    TempData["Error"] = "Could not update order status.";
                    return RedirectToAction("Orders");
                }
            }

            // ── NEW: Flagged / Reported Reviews ──────────────────────────
            public ActionResult FlaggedReviews()
            {
                try
                {
                    if (!IsAdmin) return RedirectToAction("Login");
                    return View(MockData.Reviews.ToList());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FlaggedReviews Error: " + ex.Message);
                    TempData["Error"] = "Could not load flagged reviews.";
                    return RedirectToAction("Dashboard");
                }
            }

            // ── NEW: Dismiss all reports on a review (keep the review) ───
            public ActionResult DismissReport(int id)
            {
                try
                {
                    if (!IsAdmin) return RedirectToAction("Login");
                    var review = MockData.Reviews.FirstOrDefault(r => r.Id == id);
                    if (review != null)
                    {
                        review.ReportCount = 0;
                        TempData["Success"] = "Reports dismissed for review #" + id + ".";
                    }
                    else TempData["Error"] = "Review not found.";

                    return RedirectToAction("FlaggedReviews");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("DismissReport Error: " + ex.Message);
                    TempData["Error"] = "Could not dismiss report. Please try again.";
                    return RedirectToAction("FlaggedReviews");
                }
            }

            // ── NEW: Permanently delete a review from the admin panel ────
            public ActionResult DeleteReview(int id)
            {
                try
                {
                    if (!IsAdmin) return RedirectToAction("Login");
                    var review = MockData.Reviews.FirstOrDefault(r => r.Id == id);
                    if (review != null)
                    {
                        MockData.Reviews.Remove(review);
                        TempData["Success"] = "Review #" + id + " has been deleted.";
                    }
                    else TempData["Error"] = "Review not found.";

                    return RedirectToAction("FlaggedReviews");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("DeleteReview Error: " + ex.Message);
                    TempData["Error"] = "Could not delete review. Please try again.";
                    return RedirectToAction("FlaggedReviews");
                }
            }
            // ── Paste this action inside AdminController, after SalesReport() ──

            public ActionResult SalesReport()
            {
                try
                {
                    if (!IsAdmin) return RedirectToAction("Login");
                    return View();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SalesReport Error: " + ex.Message);
                    TempData["Error"] = "Could not load sales report.";
                    return RedirectToAction("Dashboard");
                }
            }

            // ── Replace your ExportSalesReport() action in AdminController with this ──
            // Requires: EPPlus NuGet package (EPPlusSoftware, v8+)

            public ActionResult ExportSalesReport(string from, string to)
            {
                try
                {
                    OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("PawChase");

                    if (!IsAdmin) return RedirectToAction("Login");

                    DateTime fromDate, toDate;
                    if (!DateTime.TryParse(from, out fromDate)) fromDate = DateTime.Now.AddDays(-29).Date;
                    if (!DateTime.TryParse(to, out toDate)) toDate = DateTime.Now.Date;
                    toDate = toDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

                    var orders = MockData.Orders
                        .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                        .OrderByDescending(o => o.OrderDate)
                        .ToList();

                    // ── Computed stats ──
                    var periodStr = fromDate.ToString("MMMM dd, yyyy") + "  \u2013  " + toDate.ToString("MMMM dd, yyyy");
                    var generated = DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt");
                    var totalRevenue = orders.Where(o => o.Status != "Refund Approved").Sum(o => o.Total);
                    var refundAmount = orders.Where(o => o.Status == "Refund Approved").Sum(o => o.Total);
                    var netIncome = totalRevenue - refundAmount;
                    var totalOrders = orders.Count;
                    var refundCount = orders.Count(o => o.HasRefundRequest || o.Status == "Refund Approved");
                    var aov = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

                    var productsSold = orders
                        .Where(o => o.Snapshots != null)
                        .SelectMany(o => o.Snapshots)
                        .GroupBy(s => new { s.ProductName, s.Category })
                        .Select(g => new
                        {
                            Name = g.Key.ProductName,
                            Category = g.Key.Category,
                            Qty = g.Sum(s => s.Quantity),
                            Price = g.First().UnitPrice,
                            Revenue = g.Sum(s => s.UnitPrice * s.Quantity)
                        })
                        .OrderByDescending(x => x.Qty).ToList();

                    var inventory = MockData.Products
                        .Where(p => !p.IsDeleted)
                        .OrderBy(p => p.Category).ThenBy(p => p.Name).ToList();

                    var refundOrders = orders
                        .Where(o => o.HasRefundRequest || o.Status == "Refund Approved").ToList();

                    var statusList = new[] {
            "To Ship","In Transit","Out for Delivery",
            "Delivered","Refund Approved","Completed"
        };

                    // ── Colours ──
                    var DARK_NAVY = System.Drawing.Color.FromArgb(0x1E, 0x2A, 0x33);
                    var MID_BLUE = System.Drawing.Color.FromArgb(0x81, 0xA6, 0xC6);
                    var PALE_BLUE = System.Drawing.Color.FromArgb(0xD6, 0xE8, 0xF4);
                    var WHITE = System.Drawing.Color.White;
                    var LGRAY = System.Drawing.Color.FromArgb(0xF7, 0xF9, 0xFB);
                    var TEXT_MID = System.Drawing.Color.FromArgb(0x44, 0x44, 0x44);
                    var TEXT_LIGHT = System.Drawing.Color.FromArgb(0x88, 0x88, 0x88);
                    var BLUE_FG = System.Drawing.Color.FromArgb(0x1E, 0x4D, 0x78);
                    var GREEN_BG = System.Drawing.Color.FromArgb(0xE8, 0xF5, 0xE9);
                    var GREEN_FG = System.Drawing.Color.FromArgb(0x1B, 0x5E, 0x20);
                    var RED_BG = System.Drawing.Color.FromArgb(0xFF, 0xEB, 0xEE);
                    var RED_FG = System.Drawing.Color.FromArgb(0xB7, 0x1C, 0x1C);
                    var AMBER_BG = System.Drawing.Color.FromArgb(0xFF, 0xF8, 0xE1);
                    var AMBER_FG = System.Drawing.Color.FromArgb(0xE6, 0x51, 0x00);

                    const string FONT = "Poppins";

                    using (var pkg = new OfficeOpenXml.ExcelPackage())
                    {
                        // ── Helpers ────────────────────────────────────────────
                        Action<OfficeOpenXml.ExcelWorksheet, int, int, string, bool, int, System.Drawing.Color, System.Drawing.Color?, string, string>
                        C = (ws, row, col, val, bold, size, fg, bg, align, fmt) =>
                        {
                            var c = ws.Cells[row, col];
                            c.Value = val;
                            c.Style.Font.Name = FONT;
                            c.Style.Font.Bold = bold;
                            c.Style.Font.Size = size;
                            c.Style.Font.Color.SetColor(fg);
                            if (bg.HasValue) c.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            if (bg.HasValue) c.Style.Fill.BackgroundColor.SetColor(bg.Value);
                            c.Style.HorizontalAlignment = align == "center"
                                ? OfficeOpenXml.Style.ExcelHorizontalAlignment.Center
                                : align == "right"
                                ? OfficeOpenXml.Style.ExcelHorizontalAlignment.Right
                                : OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                            c.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                            if (!string.IsNullOrEmpty(fmt)) c.Style.Numberformat.Format = fmt;
                        };

                        Action<OfficeOpenXml.ExcelWorksheet, int, int, decimal, bool, System.Drawing.Color, System.Drawing.Color?, string, string>
                        CN = (ws, row, col, val, bold, fg, bg, align, fmt) =>
                        {
                            var c = ws.Cells[row, col];
                            c.Value = val;
                            c.Style.Font.Name = FONT;
                            c.Style.Font.Bold = bold;
                            c.Style.Font.Size = 10;
                            c.Style.Font.Color.SetColor(fg);
                            if (bg.HasValue) c.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            if (bg.HasValue) c.Style.Fill.BackgroundColor.SetColor(bg.Value);
                            c.Style.HorizontalAlignment = align == "right"
                                ? OfficeOpenXml.Style.ExcelHorizontalAlignment.Right
                                : align == "center"
                                ? OfficeOpenXml.Style.ExcelHorizontalAlignment.Center
                                : OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                            c.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                            if (!string.IsNullOrEmpty(fmt)) c.Style.Numberformat.Format = fmt;
                        };

                        Action<OfficeOpenXml.ExcelWorksheet, int, int, int, string>
                        CN2 = (ws, row, col, val, fmt) =>
                        {
                            var c = ws.Cells[row, col];
                            c.Value = val;
                            c.Style.Font.Name = FONT; c.Style.Font.Size = 10;
                            c.Style.Font.Color.SetColor(DARK_NAVY);
                            c.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            c.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                            if (!string.IsNullOrEmpty(fmt)) c.Style.Numberformat.Format = fmt;
                        };

                        Action<OfficeOpenXml.ExcelWorksheet, string, int> LogoRow = (ws, title, ncols) =>
                        {
                            ws.Row(1).Height = 36;
                            ws.Cells[1, 1, 1, ncols].Merge = true;
                            C(ws, 1, 1, "PAWCHASE", true, 18, WHITE, DARK_NAVY, "left", "");
                            ws.Row(2).Height = 22;
                            ws.Cells[2, 1, 2, ncols].Merge = true;
                            C(ws, 2, 1, title, true, 12, BLUE_FG, PALE_BLUE, "left", "");
                        };

                        Action<OfficeOpenXml.ExcelWorksheet, int, string, int> SectionHdr = (ws, row, label, ncols) =>
                        {
                            ws.Row(row).Height = 22;
                            ws.Cells[row, 1, row, ncols].Merge = true;
                            C(ws, row, 1, "  " + label, true, 10, DARK_NAVY, MID_BLUE, "left", "");
                        };

                        Action<OfficeOpenXml.ExcelWorksheet, int, int, string> ColHdr = (ws, row, col, val) =>
                        {
                            ws.Row(row).Height = 20;
                            C(ws, row, col, val, true, 9, DARK_NAVY, PALE_BLUE, "center", "");
                            ws.Cells[row, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin,
                                System.Drawing.Color.FromArgb(0xC5, 0xD9, 0xE8));
                        };

                        Action<OfficeOpenXml.ExcelWorksheet, int, string, int> Spacer = (ws, row, h, ncols) =>
                        {
                            ws.Row(row).Height = string.IsNullOrEmpty(h) ? 10 : int.Parse(h);
                        };

                        // ════════════════════════════════════════════════
                        //  TAB 1 — SUMMARY
                        // ════════════════════════════════════════════════
                        var ws1 = pkg.Workbook.Worksheets.Add("Summary");
                        ws1.Column(1).Width = 32; ws1.Column(2).Width = 24;
                        ws1.Column(3).Width = 16; ws1.Column(4).Width = 16;
                        LogoRow(ws1, "Sales Summary Report", 4);
                        ws1.Row(3).Height = 19; ws1.Row(4).Height = 19;
                        C(ws1, 3, 1, "Period:", true, 9, TEXT_LIGHT, null, "left", "");
                        C(ws1, 3, 2, periodStr, false, 9, TEXT_MID, null, "left", "");
                        C(ws1, 4, 1, "Generated:", true, 9, TEXT_LIGHT, null, "left", "");
                        C(ws1, 4, 2, generated, false, 9, TEXT_MID, null, "left", "");
                        Spacer(ws1, 5, "10", 4);

                        SectionHdr(ws1, 6, "REVENUE", 4);
                        // Label col A, value col B
                        int[] kvRows = { 7, 8, 9 };
                        string[] kvLabels = { "Total Revenue", "Total Refunded Amount", "Net Income" };
                        decimal[] kvVals = { totalRevenue, refundAmount, netIncome };
                        for (int i = 0; i < 3; i++)
                        {
                            ws1.Row(kvRows[i]).Height = 20;
                            C(ws1, kvRows[i], 1, kvLabels[i], false, 10, TEXT_MID, null, "left", "");
                            ws1.Cells[kvRows[i], 1].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            ws1.Cells[kvRows[i], 1].Style.Border.Bottom.Color.SetColor(System.Drawing.Color.FromArgb(0xCC, 0xCC, 0xCC));
                            CN(ws1, kvRows[i], 2, kvVals[i], true, DARK_NAVY, null, "left", "\u20B1#,##0.00");
                            ws1.Cells[kvRows[i], 2].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            ws1.Cells[kvRows[i], 2].Style.Border.Bottom.Color.SetColor(System.Drawing.Color.FromArgb(0xCC, 0xCC, 0xCC));
                        }
                        Spacer(ws1, 10, "10", 4);

                        SectionHdr(ws1, 11, "ORDERS", 4);
                        string[] oLabels = { "Total Orders", "Average Order Value", "Refund Requests" };
                        for (int i = 0; i < 3; i++)
                        {
                            int r = 12 + i;
                            ws1.Row(r).Height = 20;
                            C(ws1, r, 1, oLabels[i], false, 10, TEXT_MID, null, "left", "");
                            ws1.Cells[r, 1].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        }
                        CN(ws1, 12, 2, totalRevenue > 0 ? totalOrders : 0, false, DARK_NAVY, null, "left", "#,##0");
                        ws1.Cells[12, 2].Value = totalOrders;
                        CN(ws1, 13, 2, aov, true, DARK_NAVY, null, "left", "\u20B1#,##0.00");
                        ws1.Cells[14, 2].Value = refundCount;
                        ws1.Cells[14, 2].Style.Font.Name = FONT; ws1.Cells[14, 2].Style.Font.Bold = true;
                        ws1.Cells[14, 2].Style.Font.Color.SetColor(DARK_NAVY);
                        Spacer(ws1, 15, "10", 4);

                        SectionHdr(ws1, 16, "ORDER STATUS BREAKDOWN", 4);
                        ColHdr(ws1, 17, 1, "Status"); ColHdr(ws1, 17, 2, "Count"); ColHdr(ws1, 17, 3, "% of Total");
                        int totalS = orders.Count;
                        for (int i = 0; i < statusList.Length; i++)
                        {
                            int r = 18 + i;
                            var bg = i % 2 == 0 ? (System.Drawing.Color?)LGRAY : WHITE;
                            ws1.Row(r).Height = 19;
                            int cnt = orders.Count(o => o.Status == statusList[i]);
                            C(ws1, r, 1, statusList[i], false, 10, TEXT_MID, bg, "left", "");
                            CN2(ws1, r, 2, cnt, "#,##0");
                            if (bg.HasValue) { ws1.Cells[r, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid; ws1.Cells[r, 2].Style.Fill.BackgroundColor.SetColor(bg.Value); }
                            double pct = totalS > 0 ? (double)cnt / totalS : 0;
                            ws1.Cells[r, 3].Value = pct;
                            ws1.Cells[r, 3].Style.Numberformat.Format = "0.0%";
                            ws1.Cells[r, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            ws1.Cells[r, 3].Style.Font.Name = FONT;
                            if (bg.HasValue) { ws1.Cells[r, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid; ws1.Cells[r, 3].Style.Fill.BackgroundColor.SetColor(bg.Value); }
                            for (int cc = 1; cc <= 3; cc++)
                                ws1.Cells[r, cc].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin,
                                    System.Drawing.Color.FromArgb(0xDD, 0xDD, 0xDD));
                        }

                        // ════════════════════════════════════════════════
                        //  TAB 2 — ORDERS BREAKDOWN
                        // ════════════════════════════════════════════════
                        var ws2 = pkg.Workbook.Worksheets.Add("Orders Breakdown");
                        ws2.Column(1).Width = 16; ws2.Column(2).Width = 20; ws2.Column(3).Width = 24;
                        ws2.Column(4).Width = 14; ws2.Column(5).Width = 8; ws2.Column(6).Width = 14;
                        ws2.Column(7).Width = 20; ws2.Column(8).Width = 10;
                        LogoRow(ws2, "Orders Breakdown   |   " + periodStr, 8);
                        C(ws2, 3, 1, "Period:", true, 9, TEXT_LIGHT, null, "left", "");
                        C(ws2, 3, 2, periodStr, false, 9, TEXT_MID, null, "left", "");
                        ws2.Row(4).Height = 8;
                        string[] h2 = { "Reference No.", "Customer", "Email", "Date", "Items", "Total (₱)", "Status", "Refund?" };
                        for (int i = 0; i < h2.Length; i++) ColHdr(ws2, 5, i + 1, h2[i]);

                        for (int idx = 0; idx < orders.Count; idx++)
                        {
                            var o = orders[idx];
                            int r = 6 + idx;
                            var bg = idx % 2 == 0 ? (System.Drawing.Color?)LGRAY : WHITE;
                            var items = o.Snapshots != null && o.Snapshots.Any()
                                ? o.Snapshots.Sum(s => s.Quantity)
                                : (o.Items != null ? o.Items.Sum(i2 => i2.Quantity) : 0);
                            ws2.Row(r).Height = 20;
                            C(ws2, r, 1, o.ReferenceNumber, true, 10, DARK_NAVY, bg, "left", "");
                            C(ws2, r, 2, o.CustomerName, false, 10, TEXT_MID, bg, "left", "");
                            C(ws2, r, 3, o.Email, false, 9, TEXT_LIGHT, bg, "left", "");
                            C(ws2, r, 4, o.OrderDate.ToString("MMM dd, yyyy"), false, 10, TEXT_MID, bg, "center", "");
                            CN2(ws2, r, 5, items, "#,##0");
                            if (bg.HasValue) { ws2.Cells[r, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid; ws2.Cells[r, 5].Style.Fill.BackgroundColor.SetColor(bg.Value); }
                            CN(ws2, r, 6, (decimal)o.Total, false, DARK_NAVY, bg, "right", "\u20B1#,##0.00");

                            // Status coloured cell
                            System.Drawing.Color sbg, sfg;
                            if (o.Status == "Delivered" || o.Status == "Completed") { sbg = GREEN_BG; sfg = GREEN_FG; }
                            else if (o.Status.Contains("Refund")) { sbg = RED_BG; sfg = RED_FG; }
                            else if (o.Status == "To Ship") { sbg = PALE_BLUE; sfg = BLUE_FG; }
                            else { sbg = AMBER_BG; sfg = AMBER_FG; }
                            var sc = ws2.Cells[r, 7];
                            sc.Value = o.Status;
                            sc.Style.Font.Name = FONT; sc.Style.Font.Bold = true; sc.Style.Font.Size = 9;
                            sc.Style.Font.Color.SetColor(sfg);
                            sc.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            sc.Style.Fill.BackgroundColor.SetColor(sbg);
                            sc.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            sc.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                            bool refund = o.HasRefundRequest;
                            C(ws2, r, 8, refund ? "Yes" : "No", refund, 10, refund ? RED_FG : TEXT_LIGHT, bg, "center", "");
                            for (int cc = 1; cc <= 8; cc++)
                                ws2.Cells[r, cc].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin,
                                    System.Drawing.Color.FromArgb(0xDD, 0xDD, 0xDD));
                        }

                        // Totals row
                        int tr2 = 6 + orders.Count;
                        ws2.Row(tr2).Height = 22;
                        ws2.Cells[tr2, 1, tr2, 5].Merge = true;
                        C(ws2, tr2, 1, "TOTAL", true, 10, DARK_NAVY, PALE_BLUE, "right", "");
                        CN(ws2, tr2, 6, (decimal)orders.Sum(o => o.Total), true, DARK_NAVY, PALE_BLUE, "right", "\u20B1#,##0.00");

                        // ════════════════════════════════════════════════
                        //  TAB 3 — PRODUCTS SOLD
                        // ════════════════════════════════════════════════
                        var ws3 = pkg.Workbook.Worksheets.Add("Products Sold");
                        ws3.Column(1).Width = 32; ws3.Column(2).Width = 18;
                        ws3.Column(3).Width = 14; ws3.Column(4).Width = 16; ws3.Column(5).Width = 16;
                        LogoRow(ws3, "Products Sold   |   " + periodStr, 5);
                        C(ws3, 3, 1, "Period:", true, 9, TEXT_LIGHT, null, "left", "");
                        C(ws3, 3, 2, periodStr, false, 9, TEXT_MID, null, "left", "");
                        ws3.Row(4).Height = 8;
                        string[] h3 = { "Product Name", "Category", "Units Sold", "Unit Price (₱)", "Revenue (₱)" };
                        for (int i = 0; i < h3.Length; i++) ColHdr(ws3, 5, i + 1, h3[i]);

                        int start3 = 6;
                        for (int idx = 0; idx < productsSold.Count; idx++)
                        {
                            var p = productsSold[idx]; int r = start3 + idx;
                            var bg = idx % 2 == 0 ? (System.Drawing.Color?)LGRAY : WHITE;
                            ws3.Row(r).Height = 20;
                            C(ws3, r, 1, p.Name, true, 10, DARK_NAVY, bg, "left", "");
                            C(ws3, r, 2, p.Category, false, 10, TEXT_MID, bg, "left", "");
                            CN2(ws3, r, 3, p.Qty, "#,##0");
                            if (bg.HasValue) { ws3.Cells[r, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid; ws3.Cells[r, 3].Style.Fill.BackgroundColor.SetColor(bg.Value); }
                            CN(ws3, r, 4, p.Price, false, DARK_NAVY, bg, "right", "\u20B1#,##0.00");
                            CN(ws3, r, 5, p.Revenue, false, DARK_NAVY, bg, "right", "\u20B1#,##0.00");
                            for (int cc = 1; cc <= 5; cc++) ws3.Cells[r, cc].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.FromArgb(0xDD, 0xDD, 0xDD));
                        }
                        int tr3 = start3 + productsSold.Count; ws3.Row(tr3).Height = 22;
                        ws3.Cells[tr3, 1, tr3, 2].Merge = true;
                        C(ws3, tr3, 1, "TOTAL", true, 10, DARK_NAVY, PALE_BLUE, "right", "");
                        CN2(ws3, tr3, 3, productsSold.Sum(p => p.Qty), "#,##0");
                        ws3.Cells[tr3, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        ws3.Cells[tr3, 3].Style.Fill.BackgroundColor.SetColor(PALE_BLUE);
                        ws3.Cells[tr3, 3].Style.Font.Bold = true; ws3.Cells[tr3, 3].Style.Font.Color.SetColor(DARK_NAVY);
                        CN(ws3, tr3, 5, (decimal)productsSold.Sum(p => p.Revenue), true, DARK_NAVY, PALE_BLUE, "right", "\u20B1#,##0.00");

                        // ════════════════════════════════════════════════
                        //  TAB 4 — INVENTORY SNAPSHOT
                        // ════════════════════════════════════════════════
                        var ws4 = pkg.Workbook.Worksheets.Add("Inventory Snapshot");
                        ws4.Column(1).Width = 32; ws4.Column(2).Width = 18;
                        ws4.Column(3).Width = 14; ws4.Column(4).Width = 16; ws4.Column(5).Width = 16;
                        LogoRow(ws4, "Inventory Snapshot", 5);
                        C(ws4, 3, 1, "As of:", true, 9, TEXT_LIGHT, null, "left", "");
                        C(ws4, 3, 2, generated, false, 9, TEXT_MID, null, "left", "");
                        ws4.Row(4).Height = 8;
                        string[] h4 = { "Product Name", "Category", "Breed Size", "Current Stock", "Status" };
                        for (int i = 0; i < h4.Length; i++) ColHdr(ws4, 5, i + 1, h4[i]);

                        int start4 = 6;
                        for (int idx = 0; idx < inventory.Count; idx++)
                        {
                            var p = inventory[idx]; int r = start4 + idx;
                            var bg = idx % 2 == 0 ? (System.Drawing.Color?)LGRAY : WHITE;
                            ws4.Row(r).Height = 20;
                            C(ws4, r, 1, p.Name, true, 10, DARK_NAVY, bg, "left", "");
                            C(ws4, r, 2, p.Category, false, 10, TEXT_MID, bg, "left", "");
                            C(ws4, r, 3, p.BreedSize, false, 10, TEXT_MID, bg, "center", "");
                            CN2(ws4, r, 4, p.Stock, "#,##0");
                            if (bg.HasValue) { ws4.Cells[r, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid; ws4.Cells[r, 4].Style.Fill.BackgroundColor.SetColor(bg.Value); }

                            System.Drawing.Color sbg, sfg; string slabel;
                            if (p.Stock == 0) { sbg = RED_BG; sfg = RED_FG; slabel = "Out of Stock"; }
                            else if (p.Stock <= 5) { sbg = AMBER_BG; sfg = AMBER_FG; slabel = "Low Stock"; }
                            else { sbg = GREEN_BG; sfg = GREEN_FG; slabel = "OK"; }
                            var sc = ws4.Cells[r, 5]; sc.Value = slabel;
                            sc.Style.Font.Name = FONT; sc.Style.Font.Bold = true; sc.Style.Font.Size = 9;
                            sc.Style.Font.Color.SetColor(sfg);
                            sc.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            sc.Style.Fill.BackgroundColor.SetColor(sbg);
                            sc.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            sc.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                            for (int cc = 1; cc <= 5; cc++) ws4.Cells[r, cc].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.FromArgb(0xDD, 0xDD, 0xDD));
                        }

                        // ════════════════════════════════════════════════
                        //  TAB 5 — REFUNDS
                        // ════════════════════════════════════════════════
                        var ws5 = pkg.Workbook.Worksheets.Add("Refunds");
                        ws5.Column(1).Width = 16; ws5.Column(2).Width = 22;
                        ws5.Column(3).Width = 14; ws5.Column(4).Width = 16; ws5.Column(5).Width = 20;
                        LogoRow(ws5, "Refund Report   |   " + periodStr, 5);
                        C(ws5, 3, 1, "Period:", true, 9, TEXT_LIGHT, null, "left", "");
                        C(ws5, 3, 2, periodStr, false, 9, TEXT_MID, null, "left", "");
                        ws5.Row(4).Height = 8;
                        string[] h5 = { "Reference No.", "Customer", "Date", "Amount (₱)", "Status" };
                        for (int i = 0; i < h5.Length; i++) ColHdr(ws5, 5, i + 1, h5[i]);

                        if (refundOrders.Any())
                        {
                            for (int idx = 0; idx < refundOrders.Count; idx++)
                            {
                                var o = refundOrders[idx]; int r = 6 + idx;
                                var bg = idx % 2 == 0 ? (System.Drawing.Color?)LGRAY : WHITE;
                                ws5.Row(r).Height = 20;
                                C(ws5, r, 1, o.ReferenceNumber, true, 10, DARK_NAVY, bg, "left", "");
                                C(ws5, r, 2, o.CustomerName, false, 10, TEXT_MID, bg, "left", "");
                                C(ws5, r, 3, o.OrderDate.ToString("MMM dd, yyyy"), false, 10, TEXT_MID, bg, "center", "");
                                CN(ws5, r, 4, (decimal)o.Total, false, DARK_NAVY, bg, "right", "\u20B1#,##0.00");
                                C(ws5, r, 5, o.Status, false, 9, RED_FG, RED_BG, "center", "");
                                for (int cc = 1; cc <= 5; cc++) ws5.Cells[r, cc].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, System.Drawing.Color.FromArgb(0xDD, 0xDD, 0xDD));
                            }
                        }
                        else
                        {
                            ws5.Row(6).Height = 20;
                            C(ws5, 6, 1, "—", false, 10, TEXT_LIGHT, null, "center", "");
                            ws5.Cells[6, 2, 6, 5].Merge = true;
                            C(ws5, 6, 2, "No refund requests in this period.", false, 10, TEXT_LIGHT, null, "left", "");
                            ws5.Cells[6, 2].Style.Font.Italic = true;
                        }

                        // ── Return as .xlsx ──
                        var bytes = pkg.GetAsByteArray();
                        var filename = "PawChase_SalesReport_"
                                     + fromDate.ToString("yyyyMMdd") + "_"
                                     + toDate.ToString("yyyyMMdd") + ".xlsx";
                        return File(bytes,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            filename);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("ExportSalesReport Error: " + ex.Message);
                    TempData["Error"] = "Could not generate report. Please try again.";
                    return RedirectToAction("SalesReport");
                }
            }
        }
    }
}

