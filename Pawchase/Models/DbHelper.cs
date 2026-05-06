using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using MySql.Data.MySqlClient;
using Pawchase.Models;

namespace Pawchase.Models
{
    public static class DbHelper
    {
        private static string ConnStr =>
            ConfigurationManager.ConnectionStrings["PawChaseDB"].ConnectionString;

        private static MySqlConnection Open()
        {
            var conn = new MySqlConnection(ConnStr);
            conn.Open();
            return conn;
        }

        // ═══════════════════════════════════════════
        // PRODUCTS
        // ═══════════════════════════════════════════

        public static List<Product> GetAllProducts()
        {
            var list = new List<Product>();
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                "SELECT id, name, description, price, original_price, category, breed_size, image_url, stock, is_deleted FROM products", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Product
                    {
                        Id = r.GetInt32("id"),
                        Name = r.GetString("name"),
                        Description = r.IsDBNull(r.GetOrdinal("description")) ? "" : r.GetString("description"),
                        Price = r.GetDecimal("price"),
                        OriginalPrice = r.IsDBNull(r.GetOrdinal("original_price")) ? (decimal?)null : r.GetDecimal("original_price"),
                        Category = r.IsDBNull(r.GetOrdinal("category")) ? "" : r.GetString("category"),
                        BreedSize = r.IsDBNull(r.GetOrdinal("breed_size")) ? "" : r.GetString("breed_size"),
                        ImageUrl = r.IsDBNull(r.GetOrdinal("image_url")) ? "/Content/images/products/placeholder.png" : r.GetString("image_url"),
                        Stock = r.GetInt32("stock"),
                        IsDeleted = r.GetBoolean("is_deleted"),
                        Variants = new List<ProductVariant>()
                    });
                }
            }

            // Load variants
            using (var conn2 = Open())
            using (var cmd2 = new MySqlCommand("SELECT id, product_id, label, image_url, stock FROM product_variants", conn2))
            using (var r2 = cmd2.ExecuteReader())
            {
                while (r2.Read())
                {
                    var pid = r2.GetInt32("product_id");
                    var p = list.FirstOrDefault(x => x.Id == pid);
                    if (p == null) continue;
                    p.Variants.Add(new ProductVariant
                    {
                        Label = r2.IsDBNull(r2.GetOrdinal("label")) ? null : r2.GetString("label"),
                        ImageUrl = r2.IsDBNull(r2.GetOrdinal("image_url")) ? null : r2.GetString("image_url"),
                        Stock = r2.GetInt32("stock")
                    });
                }
            }

            return list;
        }

        public static int AddProduct(Product p)
        {
            int newId;
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                @"INSERT INTO products (name, description, price, original_price, category, breed_size, image_url, stock, is_deleted)
                  VALUES (@n,@d,@p,@op,@cat,@bs,@img,@stock,0);
                  SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@n", p.Name);
                cmd.Parameters.AddWithValue("@d", (object)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", p.Price);
                cmd.Parameters.AddWithValue("@op", (object)p.OriginalPrice ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cat", p.Category);
                cmd.Parameters.AddWithValue("@bs", (object)p.BreedSize ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@img", (object)p.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@stock", p.Stock);
                newId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (p.Variants != null)
            {
                foreach (var v in p.Variants)
                {
                    using (var conn = Open())
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO product_variants (product_id, label, image_url, stock) VALUES (@pid,@l,@img,@s)", conn))
                    {
                        cmd.Parameters.AddWithValue("@pid", newId);
                        cmd.Parameters.AddWithValue("@l", (object)v.Label ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@img", (object)v.ImageUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@s", v.Stock);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return newId;
        }

        public static void UpdateProduct(Product p)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                @"UPDATE products SET name=@n, description=@d, price=@p, original_price=@op,
                  category=@cat, breed_size=@bs, image_url=@img, stock=@stock WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@n", p.Name);
                cmd.Parameters.AddWithValue("@d", (object)p.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@p", p.Price);
                cmd.Parameters.AddWithValue("@op", (object)p.OriginalPrice ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cat", p.Category);
                cmd.Parameters.AddWithValue("@bs", (object)p.BreedSize ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@img", (object)p.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@stock", p.Stock);
                cmd.Parameters.AddWithValue("@id", p.Id);
                cmd.ExecuteNonQuery();
            }

            using (var conn = Open())
            using (var cmd = new MySqlCommand("DELETE FROM product_variants WHERE product_id=@id", conn))
            { cmd.Parameters.AddWithValue("@id", p.Id); cmd.ExecuteNonQuery(); }

            if (p.Variants != null)
            {
                foreach (var v in p.Variants)
                {
                    using (var conn = Open())
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO product_variants (product_id, label, image_url, stock) VALUES (@pid,@l,@img,@s)", conn))
                    {
                        cmd.Parameters.AddWithValue("@pid", p.Id);
                        cmd.Parameters.AddWithValue("@l", (object)v.Label ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@img", (object)v.ImageUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@s", v.Stock);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
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

        public static void SoftDeleteProduct(int id)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE products SET is_deleted=1 WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // ═══════════════════════════════════════════
        // USERS
        // ═══════════════════════════════════════════

        public static List<User> GetAllUsers()
        {
            var list = new List<User>();
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                "SELECT id, full_name, email, password, phone, gcash_number, is_admin FROM users", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new User
                    {
                        Id = r.GetInt32("id"),
                        FullName = r.GetString("full_name"),
                        Email = r.GetString("email"),
                        Password = r.IsDBNull(r.GetOrdinal("password")) ? "" : r.GetString("password"),
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
            using (var cmd = new MySqlCommand(
                @"INSERT INTO users (full_name, email, password, phone, gcash_number, is_admin)
                  VALUES (@fn,@em,@pw,@ph,@gc,0);
                  SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@fn", u.FullName);
                cmd.Parameters.AddWithValue("@em", u.Email);
                cmd.Parameters.AddWithValue("@pw", u.Password);
                cmd.Parameters.AddWithValue("@ph", (object)u.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gc", (object)u.GCashNumber ?? DBNull.Value);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void UpdateUser(User u)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                @"UPDATE users SET full_name=@fn, email=@em, password=@pw,
                  phone=@ph, gcash_number=@gc WHERE id=@id", conn))
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

        // ═══════════════════════════════════════════
        // ORDERS
        // ═══════════════════════════════════════════

        public static List<Order> GetAllOrders()
        {
            var orders = new List<Order>();
            using (var conn = Open())
            {
                using (var cmd = new MySqlCommand(
                    @"SELECT id, reference_number, customer_name, email, address, phone,
                      total, order_date, status, has_refund_request, is_received_by_customer,
                      is_reviewed, cancel_reason, refund_reason, gcash_number, refund_evidence_url
                      FROM orders", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        orders.Add(new Order
                        {
                            Id = r.GetInt32("id"),
                            ReferenceNumber = r.GetString("reference_number"),
                            CustomerName = r.GetString("customer_name"),
                            Email = r.GetString("email"),
                            Address = r.IsDBNull(r.GetOrdinal("address")) ? "" : r.GetString("address"),
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
                            Snapshots = new List<OrderItemSnapshot>()
                        });
                    }
                }

                // order_items columns: product_image, variant_image (not _url)
                using (var cmd2 = new MySqlCommand(
                    @"SELECT order_id, product_id, product_name, product_image, category,
                      breed_size, unit_price, quantity, variant_label, variant_image
                      FROM order_items", conn))
                using (var r2 = cmd2.ExecuteReader())
                {
                    while (r2.Read())
                    {
                        var orderId = r2.GetInt32("order_id");
                        var order = orders.FirstOrDefault(o => o.Id == orderId);
                        if (order == null) continue;
                        order.Snapshots.Add(new OrderItemSnapshot
                        {
                            ProductId = r2.GetInt32("product_id"),
                            ProductName = r2.GetString("product_name"),
                            ProductImageUrl = r2.IsDBNull(r2.GetOrdinal("product_image")) ? "" : r2.GetString("product_image"),
                            Category = r2.IsDBNull(r2.GetOrdinal("category")) ? "" : r2.GetString("category"),
                            BreedSize = r2.IsDBNull(r2.GetOrdinal("breed_size")) ? "" : r2.GetString("breed_size"),
                            UnitPrice = r2.GetDecimal("unit_price"),
                            Quantity = r2.GetInt32("quantity"),
                            VariantLabel = r2.IsDBNull(r2.GetOrdinal("variant_label")) ? null : r2.GetString("variant_label"),
                            VariantImageUrl = r2.IsDBNull(r2.GetOrdinal("variant_image")) ? null : r2.GetString("variant_image")
                        });
                    }
                }
            }
            return orders;
        }

        public static int AddOrder(Order o)
        {
            int newId;
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                @"INSERT INTO orders (reference_number, customer_name, email, address, phone,
                  total, order_date, status, has_refund_request, is_received_by_customer, is_reviewed)
                  VALUES (@ref,@cn,@em,@addr,@ph,@tot,@od,@st,0,0,0);
                  SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@ref", o.ReferenceNumber);
                cmd.Parameters.AddWithValue("@cn", o.CustomerName);
                cmd.Parameters.AddWithValue("@em", o.Email);
                cmd.Parameters.AddWithValue("@addr", o.Address);
                cmd.Parameters.AddWithValue("@ph", (object)o.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tot", o.Total);
                cmd.Parameters.AddWithValue("@od", o.OrderDate);
                cmd.Parameters.AddWithValue("@st", o.Status);
                newId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            foreach (var snap in o.Snapshots ?? new List<OrderItemSnapshot>())
            {
                using (var conn = Open())
                using (var cmd = new MySqlCommand(
                    @"INSERT INTO order_items (order_id, product_id, product_name, product_image,
                      category, breed_size, unit_price, quantity, variant_label, variant_image)
                      VALUES (@oid,@pid,@pn,@pimg,@cat,@bs,@up,@qty,@vl,@vimg)", conn))
                {
                    cmd.Parameters.AddWithValue("@oid", newId);
                    cmd.Parameters.AddWithValue("@pid", snap.ProductId);
                    cmd.Parameters.AddWithValue("@pn", snap.ProductName);
                    cmd.Parameters.AddWithValue("@pimg", (object)snap.ProductImageUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cat", (object)snap.Category ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@bs", (object)snap.BreedSize ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@up", snap.UnitPrice);
                    cmd.Parameters.AddWithValue("@qty", snap.Quantity);
                    cmd.Parameters.AddWithValue("@vl", (object)snap.VariantLabel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@vimg", (object)snap.VariantImageUrl ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            return newId;
        }

        public static void UpdateOrderStatus(int id, string status)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE orders SET status=@s WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@s", status);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateOrderRefund(int id, string reason, string gcash)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                @"UPDATE orders SET has_refund_request=1, refund_reason=@rr,
                  gcash_number=@gc, status='Return/Refund' WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@rr", (object)reason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gc", (object)gcash ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateOrderCancel(int id, string reason)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                "UPDATE orders SET status='Cancelled', cancel_reason=@cr WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@cr", (object)reason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void MarkOrderReceived(int id)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                "UPDATE orders SET status='Completed', is_received_by_customer=1 WHERE id=@id", conn))
            {
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

        public static void ApproveRefund(int id)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                "UPDATE orders SET status='Refund Approved', has_refund_request=0 WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // ═══════════════════════════════════════════
        // REVIEWS
        // ═══════════════════════════════════════════

        public static List<Review> GetAllReviews()
        {
            var reviews = new List<Review>();
            using (var conn = Open())
            {
                using (var cmd = new MySqlCommand(
                    @"SELECT id, product_id, user_id, customer_name, stars, comment, photo_url,
                      date_posted, last_edited_at, likes, report_count, category
                      FROM reviews", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        reviews.Add(new Review
                        {
                            Id = r.GetInt32("id"),
                            ProductId = r.GetInt32("product_id"),
                            UserId = r.GetInt32("user_id"),
                            CustomerName = r.GetString("customer_name"),
                            Stars = r.GetInt32("stars"),
                            Comment = r.IsDBNull(r.GetOrdinal("comment")) ? "" : r.GetString("comment"),
                            PhotoUrl = r.IsDBNull(r.GetOrdinal("photo_url")) ? null : r.GetString("photo_url"),
                            DatePosted = r.GetDateTime("date_posted"),
                            LastEditedAt = r.IsDBNull(r.GetOrdinal("last_edited_at")) ? (DateTime?)null : r.GetDateTime("last_edited_at"),
                            Likes = r.GetInt32("likes"),
                            ReportCount = r.GetInt32("report_count"),
                            Category = r.IsDBNull(r.GetOrdinal("category")) ? "" : r.GetString("category"),
                            Comments = new List<ReviewComment>()
                        });
                    }
                }

                using (var cmd2 = new MySqlCommand(
                    "SELECT id, review_id, user_id, user_name, text, date_posted FROM review_comments", conn))
                using (var r2 = cmd2.ExecuteReader())
                {
                    while (r2.Read())
                    {
                        var reviewId = r2.GetInt32("review_id");
                        var review = reviews.FirstOrDefault(rv => rv.Id == reviewId);
                        if (review == null) continue;
                        review.Comments.Add(new ReviewComment
                        {
                            Id = r2.GetInt32("id"),
                            ReviewId = reviewId,
                            UserId = r2.GetInt32("user_id"),
                            UserName = r2.GetString("user_name"),
                            Text = r2.GetString("text"),
                            DatePosted = r2.GetDateTime("date_posted")
                        });
                    }
                }
            }
            return reviews;
        }

        public static int AddReview(Review rv)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                @"INSERT INTO reviews (product_id, user_id, customer_name, stars, comment,
                  photo_url, date_posted, likes, report_count, category)
                  VALUES (@pid,@uid,@cn,@s,@c,@ph,@dp,0,0,@cat);
                  SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@pid", rv.ProductId);
                cmd.Parameters.AddWithValue("@uid", rv.UserId);
                cmd.Parameters.AddWithValue("@cn", rv.CustomerName);
                cmd.Parameters.AddWithValue("@s", rv.Stars);
                cmd.Parameters.AddWithValue("@c", rv.Comment);
                cmd.Parameters.AddWithValue("@ph", (object)rv.PhotoUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dp", rv.DatePosted);
                cmd.Parameters.AddWithValue("@cat", (object)rv.Category ?? DBNull.Value);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void UpdateReview(int id, int stars, string comment)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                "UPDATE reviews SET stars=@s, comment=@c, last_edited_at=@now WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@s", stars);
                cmd.Parameters.AddWithValue("@c", comment);
                cmd.Parameters.AddWithValue("@now", DateTime.Now);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteReview(int id)
        {
            using (var conn = Open())
            {
                using (var cmd = new MySqlCommand("DELETE FROM review_comments WHERE review_id=@id", conn))
                { cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery(); }
                using (var cmd2 = new MySqlCommand("DELETE FROM reviews WHERE id=@id", conn))
                { cmd2.Parameters.AddWithValue("@id", id); cmd2.ExecuteNonQuery(); }
            }
        }

        public static void UpdateReviewLikes(int id, int likes)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE reviews SET likes=@l WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@l", likes);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateReviewReports(int id, int count)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("UPDATE reviews SET report_count=@rc WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@rc", count);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static int AddReviewComment(ReviewComment c)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                @"INSERT INTO review_comments (review_id, user_id, user_name, text, date_posted)
                  VALUES (@rid,@uid,@un,@t,@dp);
                  SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@rid", c.ReviewId);
                cmd.Parameters.AddWithValue("@uid", c.UserId);
                cmd.Parameters.AddWithValue("@un", c.UserName);
                cmd.Parameters.AddWithValue("@t", c.Text);
                cmd.Parameters.AddWithValue("@dp", c.DatePosted);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // ═══════════════════════════════════════════
        // SAVED ADDRESSES
        // ═══════════════════════════════════════════

        public static List<SavedAddress> GetAddressesByUser(int userId)
        {
            var list = new List<SavedAddress>();
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                "SELECT id, user_id, label, address, phone, is_default FROM saved_addresses WHERE user_id=@uid", conn))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new SavedAddress
                        {
                            Id = r.GetInt32("id"),
                            UserId = r.GetInt32("user_id"),
                            Label = r.IsDBNull(r.GetOrdinal("label")) ? null : r.GetString("label"),
                            Address = r.GetString("address"),
                            Phone = r.IsDBNull(r.GetOrdinal("phone")) ? null : r.GetString("phone"),
                            IsDefault = r.GetBoolean("is_default")
                        });
                    }
                }
            }
            return list;
        }

        public static int AddAddress(SavedAddress a)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand(
                @"INSERT INTO saved_addresses (user_id, label, address, phone, is_default)
                  VALUES (@uid,@l,@addr,@ph,@def);
                  SELECT LAST_INSERT_ID();", conn))
            {
                cmd.Parameters.AddWithValue("@uid", a.UserId);
                cmd.Parameters.AddWithValue("@l", (object)a.Label ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@addr", a.Address);
                cmd.Parameters.AddWithValue("@ph", (object)a.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@def", a.IsDefault);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void DeleteAddress(int id)
        {
            using (var conn = Open())
            using (var cmd = new MySqlCommand("DELETE FROM saved_addresses WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}