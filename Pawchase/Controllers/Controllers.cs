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
                var userId = 0; int.TryParse(Session["UserId"]?.ToString(), out userId);
                var user = MockData.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null) return RedirectToAction("Login");
                Session["UserName"] = user.FullName;
                Session["UserEmail"] = user.Email;
                var orders = MockData.Orders.Where(o => string.Equals(o.Email, user.Email, StringComparison.OrdinalIgnoreCase)).ToList();
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

        [HttpGet]
        public ActionResult SaveAddress()
        {
            TempData["ProfileError"] = "Please add or edit addresses from the Saved Addresses tab.";
            return RedirectToAction("Orders", new { tab = "Addresses" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveAddress(int id, string label, string address, string phone, bool isDefault = false)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login");
                var userId = 0; int.TryParse(Session["UserId"]?.ToString(), out userId);
                if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone))
                {
                    TempData["ProfileError"] = "Address label, address, and phone are required.";
                    return RedirectToAction("Orders", new { tab = "Addresses" });
                }

                phone = new string(phone.Where(char.IsDigit).ToArray());
                if (phone.Length == 0 || phone.Length > 11)
                {
                    TempData["ProfileError"] = "Phone number must contain up to 11 digits.";
                    return RedirectToAction("Orders", new { tab = "Addresses" });
                }

                var existing = DbHelper.GetAddressesByUser(userId);
                if (isDefault || !existing.Any()) DbHelper.ClearDefaultAddress(userId);

                var savedAddress = new SavedAddress
                {
                    Id = id,
                    UserId = userId,
                    Label = label.Trim(),
                    Address = address.Trim(),
                    Phone = phone,
                    IsDefault = isDefault || !existing.Any()
                };

                if (id > 0) DbHelper.UpdateAddress(savedAddress);
                else DbHelper.AddAddress(savedAddress);

                TempData["ProfileSuccess"] = id > 0 ? "Address updated." : "Address added.";
                return RedirectToAction("Orders", new { tab = "Addresses" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SaveAddress Error: " + ex.Message);
                TempData["ProfileError"] = "Could not save address.";
                return RedirectToAction("Orders", new { tab = "Addresses" });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteAddress(int id)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login");
                var userId = 0; int.TryParse(Session["UserId"]?.ToString(), out userId);
                DbHelper.DeleteAddress(id, userId);
                TempData["ProfileSuccess"] = "Address deleted.";
                return RedirectToAction("Orders", new { tab = "Addresses" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DeleteAddress Error: " + ex.Message);
                TempData["ProfileError"] = "Could not delete address.";
                return RedirectToAction("Orders", new { tab = "Addresses" });
            }
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

        private User CurrentCustomer()
        {
            var userId = 0;
            int.TryParse(Session["UserId"]?.ToString(), out userId);
            var user = MockData.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                Session["UserName"] = user.FullName;
                Session["UserEmail"] = user.Email;
            }
            return user;
        }

        private List<CartItem> GetCart()
        {
            if (Session["Cart"] == null) Session["Cart"] = new List<CartItem>();
            return (List<CartItem>)Session["Cart"];
        }

        private int AvailableStock(Product product, ProductVariant variant)
        {
            return variant != null ? variant.Stock : product.Stock;
        }

        private ProductVariant FindVariant(Product product, string variantLabel)
        {
            if (product == null || product.Variants == null || string.IsNullOrWhiteSpace(variantLabel)) return null;
            return product.Variants.FirstOrDefault(v => v.Label == variantLabel);
        }

        private List<CartItem> ResolveSelectedItems(List<CartItem> cart, int[] selectedId, string[] selectedVariantLabel, int[] selectedQty)
        {
            if (selectedId == null || selectedId.Length == 0)
            {
                return cart.Select(c => new CartItem { Product = c.Product, SelectedVariant = c.SelectedVariant, Quantity = c.Quantity }).ToList();
            }

            var selected = new List<CartItem>();
            for (int i = 0; i < selectedId.Length; i++)
            {
                var productId = selectedId[i];
                var variant = (selectedVariantLabel != null && i < selectedVariantLabel.Length) ? selectedVariantLabel[i] : null;
                var qty = (selectedQty != null && i < selectedQty.Length) ? Math.Max(1, selectedQty[i]) : 1;
                var item = cart.FirstOrDefault(c => c.Product.Id == productId &&
                    ((string.IsNullOrEmpty(variant) && c.SelectedVariant == null) ||
                     (c.SelectedVariant != null && c.SelectedVariant.Label == variant)));
                if (item != null)
                {
                    selected.Add(new CartItem { Product = item.Product, SelectedVariant = item.SelectedVariant, Quantity = Math.Min(qty, item.Quantity) });
                }
            }

            return selected;
        }

        private bool RefreshAndValidateItems(List<CartItem> items, out string error)
        {
            foreach (var item in items)
            {
                var product = MockData.Products.FirstOrDefault(p => p.Id == item.Product.Id && !p.IsDeleted);
                if (product == null)
                {
                    error = item.Product.Name + " is no longer available.";
                    return false;
                }

                var variant = FindVariant(product, item.SelectedVariant?.Label);
                if (item.SelectedVariant != null && variant == null)
                {
                    error = item.Product.Name + " variant is no longer available.";
                    return false;
                }

                var available = AvailableStock(product, variant);
                if (available <= 0 || item.Quantity > available)
                {
                    error = item.Product.Name + " does not have enough stock.";
                    return false;
                }

                item.Product = product;
                item.SelectedVariant = variant;
            }

            error = null;
            return true;
        }

        private void RefreshCartForDisplay(List<CartItem> cart)
        {
            for (int i = cart.Count - 1; i >= 0; i--)
            {
                var item = cart[i];
                var product = MockData.Products.FirstOrDefault(p => p.Id == item.Product.Id && !p.IsDeleted);
                if (product == null)
                {
                    cart.RemoveAt(i);
                    continue;
                }

                var variant = FindVariant(product, item.SelectedVariant?.Label);
                if (item.SelectedVariant != null && variant == null)
                {
                    cart.RemoveAt(i);
                    continue;
                }

                item.Product = product;
                item.SelectedVariant = variant;
                var available = AvailableStock(product, variant);
                if (available <= 0)
                {
                    cart.RemoveAt(i);
                    continue;
                }
                item.Quantity = Math.Min(item.Quantity, available);
            }
        }

        private void RemoveOrderedItemsFromCart(List<CartItem> cart, List<CartItem> orderedItems)
        {
            foreach (var ordered in orderedItems)
            {
                var item = cart.FirstOrDefault(c => c.Product.Id == ordered.Product.Id &&
                    ((c.SelectedVariant == null && ordered.SelectedVariant == null) ||
                     (c.SelectedVariant != null && ordered.SelectedVariant != null && c.SelectedVariant.Label == ordered.SelectedVariant.Label)));
                if (item == null) continue;
                if (item.Quantity <= ordered.Quantity) cart.Remove(item);
                else item.Quantity -= ordered.Quantity;
            }
        }

        public ActionResult Index()
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
                var cart = GetCart();
                cart.RemoveAll(c => c.Product == null || c.Product.IsDeleted);
                RefreshCartForDisplay(cart);
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
                var user = CurrentCustomer();
                if (user == null) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
                var cart = GetCart();
                cart.RemoveAll(c => c.Product == null || c.Product.IsDeleted);
                RefreshCartForDisplay(cart);
                Session["Cart"] = cart;
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
                var user = CurrentCustomer();
                if (user == null) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "Product", new { id = id }) });
                var product = MockData.Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted);
                if (product == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Index", "Product"); }
                ProductVariant chosen = null;
                if (!string.IsNullOrEmpty(variantLabel) && product.Variants != null)
                    chosen = product.Variants.FirstOrDefault(v => v.Label == variantLabel);
                var available = AvailableStock(product, chosen);
                if (available <= 0) { TempData["Error"] = "Product out of stock."; return RedirectToAction("Details", "Product", new { id = id }); }
                qty = Math.Min(Math.Max(1, qty), available);
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
                if (qty < 1) qty = 1;
                ProductVariant chosen = null;
                if (!string.IsNullOrEmpty(variantLabel) && product.Variants != null)
                    chosen = product.Variants.FirstOrDefault(v => v.Label == variantLabel);
                var available = AvailableStock(product, chosen);
                if (available <= 0) { TempData["Error"] = "Out of stock."; return RedirectToAction("Details", "Product", new { id = id }); }
                var cart = GetCart();
                var existing = cart.FirstOrDefault(c => c.Product.Id == id &&
                    ((c.SelectedVariant == null && chosen == null) ||
                     (c.SelectedVariant != null && chosen != null && c.SelectedVariant.Label == chosen.Label)));
                if (existing != null) existing.Quantity = Math.Min(existing.Quantity + qty, available);
                else cart.Add(new CartItem { Product = product, Quantity = Math.Min(qty, available), SelectedVariant = chosen });
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
                var user = CurrentCustomer();
                if (user == null) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Track", "Order") });
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber && string.Equals(o.Email, user.Email, StringComparison.OrdinalIgnoreCase));
                if (order == null) { TempData["Error"] = "Order not found."; return RedirectToAction("Track", "Order"); }

                var cart = GetCart();
                var addedCount = 0;
                foreach (var snap in order.Snapshots ?? new List<OrderItemSnapshot>())
                {
                    var product = MockData.Products.FirstOrDefault(p => p.Id == snap.ProductId && !p.IsDeleted);
                    if (product == null || product.Stock <= 0) continue;
                    var variant = product.Variants?.FirstOrDefault(v => v.Label == snap.VariantLabel);
                    var qty = Math.Min(Math.Max(1, snap.Quantity), AvailableStock(product, variant));
                    if (qty <= 0) continue;
                    cart.Add(new CartItem { Product = product, SelectedVariant = variant, Quantity = qty });
                    addedCount++;
                }

                Session["Cart"] = cart;
                if (addedCount == 0) { TempData["Error"] = "No available items could be reordered."; return RedirectToAction("Track", "Order"); }
                TempData["Success"] = "Items from " + referenceNumber + " were added to your cart.";
                return RedirectToAction("Checkout");
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
                    else
                    {
                        var available = AvailableStock(item.Product, item.SelectedVariant);
                        if (available <= 0) cart.Remove(item);
                        else item.Quantity = Math.Min(quantity, available);
                    }
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
                    if (product != null)
                    {
                        var available = AvailableStock(product, item.SelectedVariant);
                        if (available <= 0) { cart.Remove(item); TempData["Error"] = "Selected variant is out of stock."; }
                        else item.Quantity = Math.Min(item.Quantity, available);
                    }
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
                var user = CurrentCustomer();
                if (user == null) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Checkout", "Cart") });
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
                        if (item != null) selected.Add(new CartItem { Product = item.Product, SelectedVariant = item.SelectedVariant, Quantity = Math.Min(qty, AvailableStock(item.Product, item.SelectedVariant)) });
                    }
                }
                if (!selected.Any()) { TempData["Error"] = "No items selected."; return RedirectToAction("Index"); }
                if (!RefreshAndValidateItems(selected, out var stockError)) { TempData["Error"] = stockError; return RedirectToAction("Index"); }
                return View("Checkout", selected);
            }
            catch { return RedirectToAction("Index"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string address, string phone, string orderNotes, int[] selectedId, string[] selectedVariantLabel, int[] selectedQty)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account");
                var user = CurrentCustomer();
                if (user == null) return RedirectToAction("Login", "Account");
                if (string.IsNullOrWhiteSpace(address)) { TempData["Error"] = "Delivery address is required."; return RedirectToAction("Checkout"); }
                var cart = GetCart();
                if (!cart.Any()) { TempData["Error"] = "Your cart is empty."; return RedirectToAction("Index"); }
                var orderItems = ResolveSelectedItems(cart, selectedId, selectedVariantLabel, selectedQty);
                if (!orderItems.Any()) { TempData["Error"] = "No selected items were found in your cart."; return RedirectToAction("Index"); }
                if (!RefreshAndValidateItems(orderItems, out var stockError)) { TempData["Error"] = stockError; return RedirectToAction("Index"); }
                var shipping = orderItems.Sum(c => c.Subtotal) >= 500 ? 0 : 80;
                var snapshots = orderItems.Select(c => new OrderItemSnapshot
                {
                    ProductId = c.Product.Id,
                    ProductName = c.Product.Name,
                    ProductImageUrl = c.Product.ImageUrl,
                    Category = c.Product.Category,
                    BreedSize = c.Product.BreedSize,
                    UnitPrice = c.UnitPrice,
                    Quantity = c.Quantity,
                    VariantLabel = c.SelectedVariant?.Label,
                    VariantImageUrl = c.SelectedVariant?.ImageUrl
                }).ToList();
                var order = new Order
                {
                    ReferenceNumber = DbHelper.NextOrderReference(),
                    CustomerName = user.FullName,
                    Email = user.Email,
                    Address = address.Trim(),
                    Phone = phone?.Trim(),
                    Items = new List<CartItem>(orderItems),
                    Snapshots = snapshots,
                    Total = orderItems.Sum(c => c.Subtotal) + shipping,
                    OrderDate = DateTime.Now,
                    Status = "To Ship",
                    OrderNotes = string.IsNullOrWhiteSpace(orderNotes) ? null : orderNotes.Trim()
                };
                order.Id = DbHelper.AddOrder(order);
                foreach (var item in orderItems)
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
                RemoveOrderedItemsFromCart(cart, orderItems);
                Session["Cart"] = cart;
                return RedirectToAction("Confirmation", "Order", new { referenceNumber = order.ReferenceNumber });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("PlaceOrder Error: " + ex.Message); TempData["Error"] = "Order could not be placed."; return RedirectToAction("Checkout"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PlaceBuyNowOrder(string address, string phone, string orderNotes)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account");
                var user = CurrentCustomer();
                if (user == null) return RedirectToAction("Login", "Account");
                if (string.IsNullOrWhiteSpace(address)) { TempData["Error"] = "Delivery address is required."; return RedirectToAction("Index", "Home"); }
                var buyNowItem = Session["BuyNowItem"] as CartItem;
                if (buyNowItem == null || buyNowItem.Product == null || buyNowItem.Product.IsDeleted) { TempData["Error"] = "Item no longer available."; return RedirectToAction("Index", "Product"); }
                var items = new List<CartItem> { buyNowItem };
                if (!RefreshAndValidateItems(items, out var stockError)) { TempData["Error"] = stockError; return RedirectToAction("Index", "Product"); }
                var shipping = items.Sum(c => c.Subtotal) >= 500 ? 0 : 80;
                var snapshots = items.Select(c => new OrderItemSnapshot
                {
                    ProductId = c.Product.Id,
                    ProductName = c.Product.Name,
                    ProductImageUrl = c.Product.ImageUrl,
                    Category = c.Product.Category,
                    BreedSize = c.Product.BreedSize,
                    UnitPrice = c.UnitPrice,
                    Quantity = c.Quantity,
                    VariantLabel = c.SelectedVariant?.Label,
                    VariantImageUrl = c.SelectedVariant?.ImageUrl
                }).ToList();
                var order = new Order
                {
                    ReferenceNumber = DbHelper.NextOrderReference(),
                    CustomerName = user.FullName,
                    Email = user.Email,
                    Address = address.Trim(),
                    Phone = phone?.Trim(),
                    Items = items,
                    Snapshots = snapshots,
                    Total = items.Sum(c => c.Subtotal) + shipping,
                    OrderDate = DateTime.Now,
                    Status = "To Ship",
                    OrderNotes = string.IsNullOrWhiteSpace(orderNotes) ? null : orderNotes.Trim()
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
        private bool IsLoggedIn => Session["UserId"] != null;

        private User CurrentCustomer()
        {
            var userId = 0;
            int.TryParse(Session["UserId"]?.ToString(), out userId);
            var user = MockData.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                Session["UserName"] = user.FullName;
                Session["UserEmail"] = user.Email;
            }
            return user;
        }

        private Order CurrentUserOrder(string referenceNumber)
        {
            var email = CurrentCustomer()?.Email;
            return MockData.Orders.FirstOrDefault(o =>
                o.ReferenceNumber == referenceNumber &&
                string.Equals(o.Email, email, StringComparison.OrdinalIgnoreCase));
        }

        private ActionResult RedirectToTrack(string tab = "All")
        {
            return RedirectToAction("Track", "Order", new { tab = tab });
        }

        private string TabForStatus(string status)
        {
            return status == "Return/Refund" || status == "Refund Requested" || status == "Refund Approved" || status == "Refund Denied"
                ? "Return/Refund"
                : (string.IsNullOrWhiteSpace(status) ? "All" : status);
        }

        public ActionResult Track(string tab = "All")
        {
            try
            {
                if (Session["UserId"] == null) return RedirectToAction("Login", "Account", new { returnUrl = "/Order/Track" });
                var user = CurrentCustomer();
                if (user == null) return RedirectToAction("Login", "Account", new { returnUrl = "/Order/Track" });
                var orders = MockData.Orders.Where(o => string.Equals(o.Email, user.Email, StringComparison.OrdinalIgnoreCase)).ToList();
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
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Confirmation", "Order", new { referenceNumber = referenceNumber }) });
                var order = MockData.Orders.FirstOrDefault(o => o.ReferenceNumber == referenceNumber);
                if (order == null) return RedirectToAction("Index", "Home");
                var email = CurrentCustomer()?.Email;
                var isAdmin = string.Equals(Session["AdminEmail"]?.ToString(), MockData.AdminEmail, StringComparison.OrdinalIgnoreCase);
                if (!string.Equals(order.Email, email, StringComparison.OrdinalIgnoreCase) && !isAdmin)
                {
                    return RedirectToTrack();
                }
                return View(order);
            }
            catch { return RedirectToAction("Index", "Home"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult MarkReceived(string referenceNumber)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Track", "Order") });
                var order = CurrentUserOrder(referenceNumber);
                if (order == null) { TempData["Error"] = "Order not found."; return RedirectToTrack(); }
                if (order.Status != "Out for Delivery")
                {
                    TempData["Error"] = "Only out-for-delivery orders can be marked as received.";
                    return RedirectToTrack(TabForStatus(order.Status));
                }
                DbHelper.MarkOrderReceived(order.Id);
                MockData.RefreshOrders();
                TempData["Success"] = "Order marked as received.";
                return RedirectToTrack("Completed");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("MarkReceived Error: " + ex.Message); TempData["Error"] = "Could not mark order as received."; return RedirectToTrack(); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CancelOrder(string referenceNumber, string cancelReason)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Track", "Order") });
                var order = CurrentUserOrder(referenceNumber);
                if (order == null) { TempData["Error"] = "Order not found."; return RedirectToTrack(); }
                if (order.Status != "To Ship")
                {
                    TempData["Error"] = "Only orders that are still to ship can be cancelled.";
                    return RedirectToTrack(TabForStatus(order.Status));
                }
                if ((DateTime.Now - order.OrderDate).TotalMinutes >= 15)
                {
                    TempData["Error"] = "The 15-minute cancellation window has passed. Please use Return/Refund instead.";
                    return RedirectToTrack("To Ship");
                }

                DbHelper.CancelOrder(order.Id, cancelReason);
                MockData.RefreshOrders();
                TempData["Success"] = "Order cancelled.";
                return RedirectToTrack("Cancelled");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("CancelOrder Error: " + ex.Message); TempData["Error"] = "Could not cancel order."; return RedirectToTrack(); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult RequestRefund(string referenceNumber, string refundReason, string gcashNumber, string refundEvidenceUrl)
        {
            try
            {
                if (!IsLoggedIn) return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Track", "Order") });
                var order = CurrentUserOrder(referenceNumber);
                if (order == null) { TempData["Error"] = "Order not found."; return RedirectToTrack(); }
                if (order.Status != "Out for Delivery" && order.Status != "Completed")
                {
                    TempData["Error"] = "Only delivered or completed orders can request a refund.";
                    return RedirectToTrack(TabForStatus(order.Status));
                }
                if (order.HasRefundRequest) { TempData["Error"] = "A refund request is already pending for this order."; return RedirectToTrack("Return/Refund"); }
                if (string.IsNullOrWhiteSpace(refundReason) || string.IsNullOrWhiteSpace(gcashNumber) || string.IsNullOrWhiteSpace(refundEvidenceUrl))
                {
                    TempData["Error"] = "Refund reason, GCash number, and evidence photo are required.";
                    return RedirectToTrack(TabForStatus(order.Status));
                }

                DbHelper.RequestRefund(order.Id, refundReason.Trim(), gcashNumber.Trim(), refundEvidenceUrl);
                MockData.RefreshOrders();
                TempData["Success"] = "Refund request submitted.";
                return RedirectToTrack("Return/Refund");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("RequestRefund Error: " + ex.Message); TempData["Error"] = "Could not submit refund request."; return RedirectToTrack(); }
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
                var order = CurrentUserOrder(referenceNumber);
                if (order == null) { TempData["Error"] = "Order not found."; return RedirectToTrack(); }
                if (order.Status != "Completed") { TempData["Error"] = "Only completed orders can be reviewed."; return RedirectToTrack(TabForStatus(order.Status)); }
                if (order.IsReviewed) { TempData["Error"] = "This order has already been reviewed."; return RedirectToTrack("Completed"); }
                var purchasedItem = order.Snapshots?.FirstOrDefault(s => s.ProductId == productId);
                if (purchasedItem == null) { TempData["Error"] = "You can only review products from this order."; return RedirectToTrack("Completed"); }
                if (string.IsNullOrWhiteSpace(comment)) { TempData["Error"] = "Please write your review."; return RedirectToTrack("Completed"); }

                var review = new Review
                {
                    ProductId = productId,
                    UserId = userId,
                    CustomerName = user.FullName,
                    Stars = Math.Max(1, Math.Min(5, stars)),
                    Comment = comment?.Trim(),
                    PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl,
                    DatePosted = DateTime.Now,
                    Category = purchasedItem.Category,
                    IsVerifiedPurchase = true
                };
                review.Id = DbHelper.AddReview(review);
                DbHelper.MarkOrderReviewed(order.Id);
                MockData.RefreshReviews();
                MockData.RefreshOrders();
                TempData["Success"] = "Review submitted.";
                return RedirectToTrack("Completed");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine("SubmitReview Error: " + ex.Message); TempData["Error"] = "Could not submit review."; return RedirectToTrack("Completed"); }
        }
    }

    // ════════════════════════════ ADMIN ════════════════════════════
    public class AdminController : Controller
    {
        private bool IsAdmin => string.Equals(Session["AdminEmail"]?.ToString(), MockData.AdminEmail, StringComparison.OrdinalIgnoreCase);
        private static readonly string[] ValidAdminStatuses = { "To Ship", "In Transit", "Out for Delivery", "Cancelled", "Return/Refund", "Refund Approved", "Refund Denied", "Completed" };

        private bool CanAdminSetStatus(Order order, string status, out string error)
        {
            if (order == null)
            {
                error = "Order not found.";
                return false;
            }
            if (!ValidAdminStatuses.Contains(status))
            {
                error = "Invalid status.";
                return false;
            }

            var refundDecision = status == "Refund Approved" || status == "Refund Denied";
            if (refundDecision)
            {
                if (!order.HasRefundRequest)
                {
                    error = "Only orders with pending refund requests can be approved or denied.";
                    return false;
                }
                error = null;
                return true;
            }

            if (order.IsReceivedByCustomer)
            {
                error = "Order already received by customer.";
                return false;
            }

            if (status == "In Transit" && order.Status == "To Ship") { error = null; return true; }
            if (status == "Out for Delivery" && order.Status == "In Transit") { error = null; return true; }
            if (status == "Cancelled" && order.Status == "To Ship") { error = null; return true; }
            if (status == "Return/Refund" && (order.Status == "Out for Delivery" || order.Status == "Completed")) { error = null; return true; }

            error = "This status change does not match the order flow.";
            return false;
        }

        public ActionResult Login()
        {
            if (IsAdmin) return RedirectToAction("Dashboard");
            return View();
        }

        public ActionResult Logout()
        {
            Session.Remove("AdminEmail");
            Session.Remove("AdminName");
            Session.Remove("IsAdmin");
            return RedirectToAction("Login");
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
                    Session["AdminEmail"] = email;
                    Session["AdminName"] = "Admin";
                    Session["IsAdmin"] = true;
                    return RedirectToAction("Dashboard");
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
                ViewBag.TotalRevenue = (decimal)orders.Where(o => o.Status != "Refund Approved").Sum(o => o.Total);
                ViewBag.TotalProducts = products.Count;
                ViewBag.TotalUsers = MockData.Users.Count;
                ViewBag.LowStockCount = products.Count(p => p.Stock > 0 && p.Stock <= 5);
                ViewBag.OutOfStockCount = products.Count(p => p.Stock == 0);
                ViewBag.RefundCount = orders.Count(o => o.HasRefundRequest);
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
        public ActionResult AddProduct(Product product, string[] VariantImagePaths, string[] VariantLabels, int[] VariantStocks, decimal?[] VariantPrices)
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
                int maxV = Math.Max(Math.Max(VariantImagePaths != null ? VariantImagePaths.Length : 0, VariantLabels != null ? VariantLabels.Length : 0), Math.Max(VariantStocks != null ? VariantStocks.Length : 0, VariantPrices != null ? VariantPrices.Length : 0));
                bool hasVariantStock = false; int totalVariantStock = 0;
                for (int i = 0; i < maxV; i++)
                {
                    var img = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                    var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                    var stock = (VariantStocks != null && i < VariantStocks.Length) ? VariantStocks[i] : 0;
                    var price = (VariantPrices != null && i < VariantPrices.Length && VariantPrices[i].HasValue && VariantPrices[i].Value > 0) ? VariantPrices[i] : null;
                    if (!string.IsNullOrWhiteSpace(img) || !string.IsNullOrWhiteSpace(label))
                    {
                        product.Variants.Add(new ProductVariant { ImageUrl = string.IsNullOrWhiteSpace(img) ? null : img, Label = label, Stock = Math.Max(0, stock), Price = price });
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
        public ActionResult EditProduct(Product updated, string[] VariantImagePaths, string[] VariantLabels, int[] VariantStocks, decimal?[] VariantPrices)
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
                int maxV = Math.Max(Math.Max(VariantImagePaths != null ? VariantImagePaths.Length : 0, VariantLabels != null ? VariantLabels.Length : 0), Math.Max(VariantStocks != null ? VariantStocks.Length : 0, VariantPrices != null ? VariantPrices.Length : 0));
                bool hasVariantStock = false; int totalVariantStock = 0;
                for (int i = 0; i < maxV; i++)
                {
                    var img = (VariantImagePaths != null && i < VariantImagePaths.Length) ? VariantImagePaths[i] : null;
                    var label = (VariantLabels != null && i < VariantLabels.Length) ? VariantLabels[i] : null;
                    var stock = (VariantStocks != null && i < VariantStocks.Length) ? VariantStocks[i] : 0;
                    var price = (VariantPrices != null && i < VariantPrices.Length && VariantPrices[i].HasValue && VariantPrices[i].Value > 0) ? VariantPrices[i] : null;
                    if (!string.IsNullOrWhiteSpace(img) || !string.IsNullOrWhiteSpace(label))
                    {
                        p.Variants.Add(new ProductVariant { ImageUrl = string.IsNullOrWhiteSpace(img) ? null : img, Label = label, Stock = Math.Max(0, stock), Price = price });
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
                var o = MockData.Orders.FirstOrDefault(x => x.Id == orderId);
                if (CanAdminSetStatus(o, status, out var error))
                {
                    DbHelper.SetOrderStatus(orderId, status);
                    MockData.RefreshOrders();
                    TempData["Success"] = status == "Refund Denied" ? "Refund request denied." : "Status updated to \"" + status + "\".";
                }
                else TempData["Error"] = error;
                return RedirectToAction("Orders");
            }
            catch { return RedirectToAction("Orders"); }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult BulkUpdateOrderStatus(int[] orderIds, string status)
        {
            try
            {
                if (!IsAdmin) return RedirectToAction("Login");
                if (orderIds == null || orderIds.Length == 0) { TempData["Error"] = "No orders selected."; return RedirectToAction("Orders"); }

                var updated = 0;
                var skipped = 0;
                foreach (var orderId in orderIds.Distinct())
                {
                    var order = MockData.Orders.FirstOrDefault(o => o.Id == orderId);
                    if (!CanAdminSetStatus(order, status, out var error))
                    {
                        skipped++;
                        continue;
                    }

                    DbHelper.SetOrderStatus(orderId, status);
                    updated++;
                }

                MockData.RefreshOrders();
                if (updated > 0) TempData["Success"] = updated + " order(s) updated to \"" + status + "\"." + (skipped > 0 ? " " + skipped + " skipped because they do not match the order flow." : "");
                else TempData["Error"] = "No orders were updated because the selected status did not match their current flow.";
                return RedirectToAction("Orders");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BulkUpdateOrderStatus Error: " + ex.Message);
                TempData["Error"] = "Could not update selected orders.";
                return RedirectToAction("Orders");
            }
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
            return SaveSellerReply(reviewId, replyText);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ReplyToReview(int reviewId, string replyText)
        {
            return SaveSellerReply(reviewId, replyText);
        }

        private ActionResult SaveSellerReply(int reviewId, string replyText)
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
