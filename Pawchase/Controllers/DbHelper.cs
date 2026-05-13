using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using Pawchase.Models;

namespace Pawchase.Models
{
    public static class DbHelper
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["PawChaseDB"].ConnectionString;

        private static MySqlConnection Open()
        {
            var conn = new MySqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        private static bool ColumnExists(MySqlConnection conn, string tableName, string columnName)
        {
            using (var cmd = new MySqlCommand(@"SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table AND COLUMN_NAME = @column", conn))
            {
                cmd.Parameters.AddWithValue("@table", tableName);
                cmd.Parameters.AddWithValue("@column", columnName);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void EnsureProductVariantPriceColumn(MySqlConnection conn)
        {
            if (ColumnExists(conn, "product_variants", "price")) return;
            using (var cmd = new MySqlCommand("ALTER TABLE product_variants ADD COLUMN price DECIMAL(10,2) NULL AFTER stock", conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureAdditionalImagesColumn(MySqlConnection conn)
        {
            if (!ColumnExists(conn, "products", "additional_images"))
            {
                // Column doesn't exist yet — add it as MEDIUMTEXT from the start
                using (var cmd = new MySqlCommand("ALTER TABLE products ADD COLUMN additional_images MEDIUMTEXT NULL", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                // Column already exists — upgrade it to MEDIUMTEXT if it is still plain TEXT
                // (TEXT is only 65 535 bytes which gets silently truncated by base64-encoded images)
                using (var cmd = new MySqlCommand(
                    "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
                    "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'products' AND COLUMN_NAME = 'additional_images'", conn))
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read() && string.Equals(r.GetString(0), "text", StringComparison.OrdinalIgnoreCase))
                    {
                        r.Close();
                        using (var alter = new MySqlCommand(
                            "ALTER TABLE products MODIFY COLUMN additional_images MEDIUMTEXT NULL", conn))
                        {
                            alter.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ensures products.image_url and product_variants.image_url are MEDIUMTEXT so they
        /// can hold base64-encoded images (~750 KB each after compression).  VARCHAR(255) or
        /// plain TEXT would silently truncate the data and cause images to vanish on save.
        /// </summary>
        private static void EnsureImageUrlColumns(MySqlConnection conn)
        {
            // products.image_url
            UpgradeColumnToMediumText(conn, "products", "image_url");
            // product_variants.image_url
            UpgradeColumnToMediumText(conn, "product_variants", "image_url");
        }

        private static void UpgradeColumnToMediumText(MySqlConnection conn, string table, string column)
        {
            // Only touch the column when it is narrower than MEDIUMTEXT (i.e. varchar or text).
            using (var cmd = new MySqlCommand(
                "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tbl AND COLUMN_NAME = @col", conn))
            {
                cmd.Parameters.AddWithValue("@tbl", table);
                cmd.Parameters.AddWithValue("@col", column);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return; // column doesn't exist — nothing to do
                    var dataType = r.GetString(0).ToLowerInvariant();
                    r.Close();
                    if (dataType == "mediumtext" || dataType == "longtext") return; // already wide enough
                    using (var alter = new MySqlCommand(
                        $"ALTER TABLE `{table}` MODIFY COLUMN `{column}` MEDIUMTEXT NULL", conn))
                    {
                        alter.ExecuteNonQuery();
                    }
                }
            }
        }

        // ════════════════════════════ SECURITY & AUTH (NO EXTERNAL DLLs) ════════════════════════════
        
        // Using built-in SHA256 with a simple salt to avoid BCrypt.dll dependency issues
        private const string SALT = "PawChase_Secure_Salt_2024";

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var combined = password + SALT;
                var bytes = Encoding.UTF8.GetBytes(combined);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        public static bool VerifyPassword(string password, string storedHash)
        {
            try { return HashPassword(password) == storedHash; }
            catch { return false; }
        }

        // ════════════════════════════ PRODUCTS ════════════════════════════
        
        public static List<Product> GetAllProducts()
        {
            var list = new List<Product>();
            using (var conn = Open())
            {
                EnsureAdditionalImagesColumn(conn);
                EnsureImageUrlColumns(conn);
                using (var cmd = new MySqlCommand("SELECT * FROM products", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var raw = r.IsDBNull(r.GetOrdinal("additional_images")) ? null : r.GetString("additional_images");
                        list.Add(new Product {
                            Id = r.GetInt32("id"),
                            Name = r.GetString("name"),
                            Description = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString("description"),
                            Price = r.GetDecimal("price"),
                            OriginalPrice = r.IsDBNull(r.GetOrdinal("original_price")) ? (decimal?)null : r.GetDecimal("original_price"),
                            Category = r.GetString("category"),
                            BreedSize = r.GetString("breed_size"),
                            ImageUrl = r.IsDBNull(r.GetOrdinal("image_url")) ? null : r.GetString("image_url"),
                            AdditionalImages = string.IsNullOrWhiteSpace(raw) ? new List<string>() : new List<string>(raw.Split('|').Where(s => !string.IsNullOrWhiteSpace(s))),
                            Stock = r.GetInt32("stock"),
                            IsDeleted = r.GetBoolean("is_deleted"),
                            Variants = new List<ProductVariant>()
                        });
                    }
                }
                EnsureProductVariantPriceColumn(conn);
                using (var cmd2 = new MySqlCommand("SELECT * FROM product_variants", conn))
                using (var r2 = cmd2.ExecuteReader())
                {
                    while (r2.Read()) {
                        var p = list.FirstOrDefault(x => x.Id == r2.GetInt32("product_id"));
                        if (p != null) p.Variants.Add(new ProductVariant { Label = r2.IsDBNull(r2.GetOrdinal("label")) ? null : r2.GetString("label"), ImageUrl = r2.IsDBNull(r2.GetOrdinal("image_url")) ? null : r2.GetString("image_url"), Stock = r2.GetInt32("stock"), Price = r2.IsDBNull(r2.GetOrdinal("price")) ? (decimal?)null : r2.GetDecimal("price") });
                    }
                }
            }
            return list;
        }

        public static void UpdateProductStock(int id, int stock)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE products SET stock=@s WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@s", stock);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateVariantStock(int productId, string label, int stock)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE product_variants SET stock=@s WHERE product_id=@pid AND label=@l", conn))
            {
                cmd.Parameters.AddWithValue("@s", stock);
                cmd.Parameters.AddWithValue("@pid", productId);
                cmd.Parameters.AddWithValue("@l", label);
                cmd.ExecuteNonQuery();
            }
        }

        // ════════════════════════════ USERS ════════════════════════════
        
        public static int AddProduct(Product p)
        {
            int newId;
            using (var conn = Open())
            {
                EnsureAdditionalImagesColumn(conn);
                EnsureImageUrlColumns(conn);
                using (var cmd = new MySqlCommand(@"INSERT INTO products (name, description, price, original_price, category, breed_size, image_url, additional_images, stock, is_deleted)
VALUES (@n,@d,@p,@op,@c,@bs,@img,@addimgs,@s,@del); SELECT LAST_INSERT_ID();", conn))
                {
                    cmd.Parameters.AddWithValue("@n", p.Name);
                    cmd.Parameters.AddWithValue("@d", (object)p.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p", p.Price);
                    cmd.Parameters.AddWithValue("@op", (object)p.OriginalPrice ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c", p.Category);
                    cmd.Parameters.AddWithValue("@bs", p.BreedSize);
                    cmd.Parameters.AddWithValue("@img", (object)p.ImageUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@addimgs", p.AdditionalImages != null && p.AdditionalImages.Any() ? (object)string.Join("|", p.AdditionalImages) : DBNull.Value);
                    cmd.Parameters.AddWithValue("@s", p.Stock);
                    cmd.Parameters.AddWithValue("@del", p.IsDeleted);
                    newId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            ReplaceProductVariants(newId, p.Variants);
            return newId;
        }

        public static void UpdateProduct(Product p)
        {
            using (var conn = Open())
            {
                EnsureAdditionalImagesColumn(conn);
                EnsureImageUrlColumns(conn);
                using (var cmd = new MySqlCommand(@"UPDATE products SET name=@n, description=@d, price=@p, original_price=@op,
category=@c, breed_size=@bs, image_url=@img, additional_images=@addimgs, stock=@s, is_deleted=@del WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@n", p.Name);
                    cmd.Parameters.AddWithValue("@d", (object)p.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p", p.Price);
                    cmd.Parameters.AddWithValue("@op", (object)p.OriginalPrice ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@c", p.Category);
                    cmd.Parameters.AddWithValue("@bs", p.BreedSize);
                    cmd.Parameters.AddWithValue("@img", (object)p.ImageUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@addimgs", p.AdditionalImages != null && p.AdditionalImages.Any() ? (object)string.Join("|", p.AdditionalImages) : DBNull.Value);
                    cmd.Parameters.AddWithValue("@s", p.Stock);
                    cmd.Parameters.AddWithValue("@del", p.IsDeleted);
                    cmd.Parameters.AddWithValue("@id", p.Id);
                    cmd.ExecuteNonQuery();
                }
            }
            ReplaceProductVariants(p.Id, p.Variants);
        }

        public static void SoftDeleteProduct(int id)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE products SET is_deleted=1 WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static void ReplaceProductVariants(int productId, IEnumerable<ProductVariant> variants)
        {
            using (var conn = Open())
            {
                EnsureProductVariantPriceColumn(conn);
                using (var delete = new MySqlCommand("DELETE FROM product_variants WHERE product_id=@pid", conn))
                {
                    delete.Parameters.AddWithValue("@pid", productId);
                    delete.ExecuteNonQuery();
                }

                foreach (var v in variants ?? Enumerable.Empty<ProductVariant>())
                {
                    using (var insert = new MySqlCommand(@"INSERT INTO product_variants (product_id, label, image_url, stock, price)
VALUES (@pid,@l,@img,@s,@price)", conn))
                    {
                        insert.Parameters.AddWithValue("@pid", productId);
                        insert.Parameters.AddWithValue("@l", (object)v.Label ?? DBNull.Value);
                        insert.Parameters.AddWithValue("@img", (object)v.ImageUrl ?? DBNull.Value);
                        insert.Parameters.AddWithValue("@s", v.Stock);
                        insert.Parameters.AddWithValue("@price", v.Price.HasValue && v.Price.Value > 0 ? (object)v.Price.Value : DBNull.Value);
                        insert.ExecuteNonQuery();
                    }
                }
            }
        }

        public static List<User> GetAllUsers()
        {
            var list = new List<User>();
            using (var conn = Open())
            using (var cmd = new MySqlCommand("SELECT * FROM users", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new User {
                        Id = r.GetInt32("id"),
                        FullName = r.GetString("full_name"),
                        Email = r.GetString("email"),
                        Password = r.GetString("password"),
                        Phone = r.IsDBNull(r.GetOrdinal("phone")) ? null : r.GetString("phone"),
                        GCashNumber = r.IsDBNull(r.GetOrdinal("gcash_number")) ? null : r.GetString("gcash_number"),
                        IsAdmin = r.GetBoolean("is_admin")
                    });
                }
            }
            return list;
        }

        public static int AddUser(User u)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(@"INSERT INTO users (full_name, email, password, phone, gcash_number, is_admin) VALUES (@fn,@em,@pw,@ph,@gc,@adm); SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@fn", u.FullName);
                cmd.Parameters.AddWithValue("@em", u.Email);
                cmd.Parameters.AddWithValue("@pw", HashPassword(u.Password));
                cmd.Parameters.AddWithValue("@ph", (object)u.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gc", (object)u.GCashNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@adm", u.IsAdmin);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void UpdateUser(User u)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE users SET full_name=@fn, email=@em, password=@pw, phone=@ph, gcash_number=@gc WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@fn", u.FullName);
                cmd.Parameters.AddWithValue("@em", u.Email);
                cmd.Parameters.AddWithValue("@pw", u.Password);
                cmd.Parameters.AddWithValue("@ph", (object)u.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gc", (object)u.GCashNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", u.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateUserProfile(User u)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE users SET full_name=@fn, email=@em, phone=@ph, gcash_number=@gc WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@fn", u.FullName);
                cmd.Parameters.AddWithValue("@em", u.Email);
                cmd.Parameters.AddWithValue("@ph", (object)u.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gc", (object)u.GCashNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", u.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateUserPassword(int id, string plainPassword)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE users SET password=@pw WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@pw", HashPassword(plainPassword));
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // ════════════════════════════ ORDERS ════════════════════════════
        
        public static List<Order> GetAllOrders()
        {
            var orders = new List<Order>();
            using (var conn = Open())
            {
                using (var cmd = new MySqlCommand("SELECT * FROM orders", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        orders.Add(new Order {
                            Id = r.GetInt32("id"),
                            ReferenceNumber = r.GetString("reference_number"),
                            CustomerName = r.GetString("customer_name"),
                            Email = r.GetString("email"),
                            Address = r.GetString("address"),
                            Phone = r.IsDBNull(r.GetOrdinal("phone")) ? null : r.GetString("phone"),
                            Total = r.GetDecimal("total"),
                            OrderDate = r.GetDateTime("order_date"),
                            Status = r.GetString("status"),
                            HasRefundRequest = r.GetBoolean("has_refund_request"),
                            IsReceivedByCustomer = r.GetBoolean("is_received_by_customer"),
                            IsReviewed = r.GetBoolean("is_reviewed"),
                            CancelReason = r.IsDBNull(r.GetOrdinal("cancel_reason")) ? null : r.GetString("cancel_reason"),
                            RefundReason = r.IsDBNull(r.GetOrdinal("refund_reason")) ? null : r.GetString("refund_reason"),
                            GCashNumber = r.IsDBNull(r.GetOrdinal("gcash_number")) ? null : r.GetString("gcash_number"),
                            RefundEvidenceUrl = r.IsDBNull(r.GetOrdinal("refund_evidence_url")) ? null : r.GetString("refund_evidence_url"),
                            OrderNotes = r.IsDBNull(r.GetOrdinal("order_notes")) ? null : r.GetString("order_notes"),
                            Snapshots = new List<OrderItemSnapshot>()
                        });
                    }
                }
                using (var cmd2 = new MySqlCommand("SELECT * FROM order_items", conn))
                using (var r2 = cmd2.ExecuteReader())
                {
                    while (r2.Read()) {
                        var order = orders.FirstOrDefault(o => o.Id == r2.GetInt32("order_id"));
                        if (order != null) order.Snapshots.Add(new OrderItemSnapshot { ProductId = r2.GetInt32("product_id"), ProductName = r2.GetString("product_name"), ProductImageUrl = r2.IsDBNull(r2.GetOrdinal("product_image_url")) ? "" : r2.GetString("product_image_url"), Category = r2.IsDBNull(r2.GetOrdinal("category")) ? "" : r2.GetString("category"), BreedSize = r2.IsDBNull(r2.GetOrdinal("breed_size")) ? "" : r2.GetString("breed_size"), UnitPrice = r2.GetDecimal("unit_price"), Quantity = r2.GetInt32("quantity"), VariantLabel = r2.IsDBNull(r2.GetOrdinal("variant_label")) ? null : r2.GetString("variant_label"), VariantImageUrl = r2.IsDBNull(r2.GetOrdinal("variant_image_url")) ? null : r2.GetString("variant_image_url") });
                    }
                }
            }
            return orders;
        }

        public static int AddOrder(Order o)
        {
            int newId;
            using (var conn = Open())
            using (var cmd = new MySqlCommand(@"INSERT INTO orders (reference_number, customer_name, email, address, phone, total, order_date, status, has_refund_request, is_received_by_customer, is_reviewed, order_notes) VALUES (@ref,@cn,@em,@addr,@ph,@tot,@od,@st,0,0,0,@notes); SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@ref", o.ReferenceNumber);
                cmd.Parameters.AddWithValue("@cn", o.CustomerName);
                cmd.Parameters.AddWithValue("@em", o.Email);
                cmd.Parameters.AddWithValue("@addr", o.Address);
                cmd.Parameters.AddWithValue("@ph", (object)o.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tot", o.Total);
                cmd.Parameters.AddWithValue("@od", o.OrderDate);
                cmd.Parameters.AddWithValue("@st", o.Status);
                cmd.Parameters.AddWithValue("@notes", (object)o.OrderNotes ?? DBNull.Value);
                newId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            foreach (var snap in o.Snapshots) {
                using (var conn = Open())
                using (var cmd = new MySqlCommand(@"INSERT INTO order_items (order_id, product_id, product_name, product_image_url, category, breed_size, unit_price, quantity, variant_label, variant_image_url) VALUES (@oid,@pid,@pn,@pimg,@cat,@bs,@up,@qty,@vl,@vimg)", conn)) {
                    cmd.Parameters.AddWithValue("@oid", newId); cmd.Parameters.AddWithValue("@pid", snap.ProductId); cmd.Parameters.AddWithValue("@pn", snap.ProductName); cmd.Parameters.AddWithValue("@pimg", (object)snap.ProductImageUrl ?? DBNull.Value); cmd.Parameters.AddWithValue("@cat", (object)snap.Category ?? DBNull.Value); cmd.Parameters.AddWithValue("@bs", (object)snap.BreedSize ?? DBNull.Value); cmd.Parameters.AddWithValue("@up", snap.UnitPrice); cmd.Parameters.AddWithValue("@qty", snap.Quantity); cmd.Parameters.AddWithValue("@vl", (object)snap.VariantLabel ?? DBNull.Value); cmd.Parameters.AddWithValue("@vimg", (object)snap.VariantImageUrl ?? DBNull.Value); cmd.ExecuteNonQuery();
                }
            }
            return newId;
        }

        public static string NextOrderReference()
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("SELECT COALESCE(MAX(id), 0) + 1 FROM orders", conn))
            {
                return "PWC-" + Convert.ToInt32(cmd.ExecuteScalar()).ToString("D6");
            }
        }

        private static readonly Dictionary<string, HashSet<string>> ValidTransitions = new Dictionary<string, HashSet<string>> {
            ["To Ship"] = new HashSet<string> { "Out for Delivery", "Cancelled" },
            ["Out for Delivery"] = new HashSet<string> { "Completed", "Return/Refund" },
            ["Completed"] = new HashSet<string> { "Return/Refund" },
            ["Return/Refund"] = new HashSet<string> { "Refund Approved", "Refund Denied" },
            ["Cancelled"] = new HashSet<string>(), ["Refund Approved"] = new HashSet<string>(), ["Refund Denied"] = new HashSet<string>(),
        };

        public static void UpdateOrderStatus(int id, string newStatus)
        {
            string current;
            using (var conn = Open())
            using (var cmd = new MySqlCommand("SELECT status FROM orders WHERE id=@id", conn)) {
                cmd.Parameters.AddWithValue("@id", id);
                current = cmd.ExecuteScalar()?.ToString();
            }
            if (current == null || !ValidTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(newStatus)) return;
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE orders SET status=@s WHERE id=@id", conn)) {
                cmd.Parameters.AddWithValue("@s", newStatus); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
            }
        }

        public static void MarkOrderReceived(int id)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE orders SET status='Completed', is_received_by_customer=1 WHERE id=@id AND status IN ('Out for Delivery','Completed')", conn)) {
                cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
            }
        }

        public static void SetOrderStatus(int id, string status)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(@"UPDATE orders SET status=@s,
has_refund_request=CASE WHEN @s IN ('Refund Approved','Refund Denied','Completed','Cancelled') THEN 0 ELSE has_refund_request END
WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void CancelOrder(int id, string reason)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE orders SET status='Cancelled', cancel_reason=@r WHERE id=@id AND status='To Ship'", conn))
            {
                cmd.Parameters.AddWithValue("@r", (object)reason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void MarkOrderReviewed(int id)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE orders SET is_reviewed=1 WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void RequestRefund(int id, string reason, string gcash, string evidence)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE orders SET status='Return/Refund', has_refund_request=1, refund_reason=@r, gcash_number=@g, refund_evidence_url=@e WHERE id=@id", conn)) {
                cmd.Parameters.AddWithValue("@r", reason); cmd.Parameters.AddWithValue("@g", gcash); cmd.Parameters.AddWithValue("@e", (object)evidence ?? DBNull.Value); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
            }
        }

        public static void DenyRefund(int id, string reason)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE orders SET status='Refund Denied', has_refund_request=0, cancel_reason=@r WHERE id=@id", conn)) {
                cmd.Parameters.AddWithValue("@r", reason); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
            }
        }

        // ════════════════════════════ REVIEWS ════════════════════════════
        
        public static List<Review> GetAllReviews()
        {
            var reviews = new List<Review>();
            using (var conn = Open())
            {
                using (var cmd = new MySqlCommand("SELECT * FROM reviews", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        reviews.Add(new Review {
                            Id = r.GetInt32("id"), ProductId = r.GetInt32("product_id"), UserId = r.GetInt32("user_id"), CustomerName = r.GetString("customer_name"), Stars = r.GetInt32("stars"), Comment = r.IsDBNull(r.GetOrdinal("comment")) ? "" : r.GetString("comment"), PhotoUrl = r.IsDBNull(r.GetOrdinal("photo_url")) ? null : r.GetString("photo_url"), DatePosted = r.GetDateTime("date_posted"), LastEditedAt = r.IsDBNull(r.GetOrdinal("last_edited_at")) ? (DateTime?)null : r.GetDateTime("last_edited_at"), Likes = r.GetInt32("likes"), ReportCount = r.GetInt32("report_count"), Category = r.IsDBNull(r.GetOrdinal("category")) ? "" : r.GetString("category"), SellerReply = r.IsDBNull(r.GetOrdinal("seller_reply")) ? null : r.GetString("seller_reply"), SellerReplyDate = r.IsDBNull(r.GetOrdinal("seller_reply_date")) ? (DateTime?)null : r.GetDateTime("seller_reply_date"), IsVerifiedPurchase = r.GetBoolean("is_verified_purchase"), Comments = new List<ReviewComment>()
                        });
                    }
                }
                using (var cmd2 = new MySqlCommand("SELECT * FROM review_comments", conn))
                using (var r2 = cmd2.ExecuteReader())
                {
                    while (r2.Read()) {
                        var review = reviews.FirstOrDefault(rv => rv.Id == r2.GetInt32("review_id"));
                        if (review != null) review.Comments.Add(new ReviewComment { Id = r2.GetInt32("id"), ReviewId = r2.GetInt32("review_id"), UserId = r2.GetInt32("user_id"), UserName = r2.GetString("user_name"), Text = r2.GetString("text"), DatePosted = r2.GetDateTime("date_posted") });
                    }
                }
            }
            return reviews;
        }

        public static int AddReview(Review rv)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(@"INSERT INTO reviews (product_id, user_id, customer_name, stars, comment, photo_url, date_posted, likes, report_count, category, is_verified_purchase) VALUES (@pid,@uid,@cn,@s,@c,@ph,@dp,0,0,@cat,@ivp); SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@pid", rv.ProductId); cmd.Parameters.AddWithValue("@uid", rv.UserId); cmd.Parameters.AddWithValue("@cn", rv.CustomerName); cmd.Parameters.AddWithValue("@s", rv.Stars); cmd.Parameters.AddWithValue("@c", rv.Comment); cmd.Parameters.AddWithValue("@ph", (object)rv.PhotoUrl ?? DBNull.Value); cmd.Parameters.AddWithValue("@dp", rv.DatePosted); cmd.Parameters.AddWithValue("@cat", (object)rv.Category ?? DBNull.Value); cmd.Parameters.AddWithValue("@ivp", rv.IsVerifiedPurchase); return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void UpdateReview(int id, int stars, string comment)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE reviews SET stars=@s, comment=@c, last_edited_at=@now WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@s", stars); cmd.Parameters.AddWithValue("@c", comment); cmd.Parameters.AddWithValue("@now", DateTime.Now); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteReview(int id)
        {
            using (var conn = Open())
            {
                using (var cmd = new MySqlCommand("DELETE FROM review_comments WHERE review_id=@id", conn)) { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
                using (var cmd2 = new MySqlCommand("DELETE FROM reviews WHERE id=@id", conn)) { cmd2.Parameters.AddWithValue("@id", id); cmd2.ExecuteNonQuery(); }
            }
        }

        public static void UpdateReviewLikes(int id, int likes)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE reviews SET likes=@l WHERE id=@id", conn)) {
                cmd.Parameters.AddWithValue("@l", likes); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateReviewReports(int id, int count)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE reviews SET report_count=@rc WHERE id=@id", conn)) {
                cmd.Parameters.AddWithValue("@rc", count); cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
            }
        }

        public static int AddReviewComment(ReviewComment c)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(@"INSERT INTO review_comments (review_id, user_id, user_name, text, date_posted) VALUES (@rid,@uid,@un,@t,@dp); SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@rid", c.ReviewId); cmd.Parameters.AddWithValue("@uid", c.UserId); cmd.Parameters.AddWithValue("@un", c.UserName); cmd.Parameters.AddWithValue("@t", c.Text); cmd.Parameters.AddWithValue("@dp", c.DatePosted); return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void UpdateSellerReply(int reviewId, string replyText)
        {
            replyText = string.IsNullOrWhiteSpace(replyText) ? null : replyText.Trim();
            using (var conn = Open())
            using (var cmd = new MySqlCommand(@"UPDATE reviews
SET seller_reply=@r, seller_reply_date=@replyDate
WHERE id=@id", conn)) {
                cmd.Parameters.AddWithValue("@r", (object)replyText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@replyDate", replyText == null ? (object)DBNull.Value : DateTime.Now);
                cmd.Parameters.AddWithValue("@id", reviewId);
                cmd.ExecuteNonQuery();
            }
        }

        // ════════════════════════════ ADDRESSES ════════════════════════════
        
        public static List<SavedAddress> GetAddressesByUser(int userId)
        {
            var list = new List<SavedAddress>();
            using (var conn = Open())
            using (var cmd = new MySqlCommand("SELECT * FROM saved_addresses WHERE user_id=@uid ORDER BY is_default DESC, id DESC", conn)) {
                cmd.Parameters.AddWithValue("@uid", userId);
                using (var r = cmd.ExecuteReader()) {
                    while (r.Read()) list.Add(new SavedAddress { Id = r.GetInt32("id"), UserId = r.GetInt32("user_id"), Label = r.IsDBNull(r.GetOrdinal("label")) ? null : r.GetString("label"), Address = r.GetString("address"), Phone = r.IsDBNull(r.GetOrdinal("phone")) ? null : r.GetString("phone"), IsDefault = r.GetBoolean("is_default") });
                }
            }
            return list;
        }

        public static void AddAddress(SavedAddress a)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(@"INSERT INTO saved_addresses (user_id, label, address, phone, is_default) VALUES (@uid,@l,@addr,@ph,@def)", conn)) {
                cmd.Parameters.AddWithValue("@uid", a.UserId); cmd.Parameters.AddWithValue("@l", (object)a.Label ?? DBNull.Value); cmd.Parameters.AddWithValue("@addr", a.Address); cmd.Parameters.AddWithValue("@ph", (object)a.Phone ?? DBNull.Value); cmd.Parameters.AddWithValue("@def", a.IsDefault); cmd.ExecuteNonQuery();
            }
        }

        public static void ClearDefaultAddress(int userId)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE saved_addresses SET is_default=0 WHERE user_id=@uid", conn)) {
                cmd.Parameters.AddWithValue("@uid", userId); cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateAddress(SavedAddress a)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(@"UPDATE saved_addresses
SET label=@l, address=@addr, phone=@ph, is_default=@def
WHERE id=@id AND user_id=@uid", conn)) {
                cmd.Parameters.AddWithValue("@l", (object)a.Label ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@addr", a.Address);
                cmd.Parameters.AddWithValue("@ph", (object)a.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@def", a.IsDefault);
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.Parameters.AddWithValue("@uid", a.UserId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteAddress(int id, int userId)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("DELETE FROM saved_addresses WHERE id=@id AND user_id=@uid", conn)) {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
