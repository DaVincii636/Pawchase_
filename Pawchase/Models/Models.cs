using System;
using System.Collections.Generic;

namespace Pawchase.Models
{
    public class ProductVariant
    {
        public string ImageUrl { get; set; }
        public string Label { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string Category { get; set; }
        public string BreedSize { get; set; }
        public string ImageUrl { get; set; }
        public int Stock { get; set; }
        // Soft-delete: hidden from store but orders/cart refs stay intact
        public bool IsDeleted { get; set; } = false;
        public bool IsOnSale => OriginalPrice.HasValue && OriginalPrice.Value > Price;
        public List<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }

    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class CartItem
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Product.Price * Quantity;
        public ProductVariant SelectedVariant { get; set; }
    }

    // Snapshot of a product at the time an order was placed —
    // so deleting/editing a product never corrupts order history
    public class OrderItemSnapshot
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public string Category { get; set; }
        public string BreedSize { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string VariantLabel { get; set; }
        public string VariantImageUrl { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }

    public class Order
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        // Snapshots — permanent record of what was ordered
        public List<OrderItemSnapshot> Snapshots { get; set; } = new List<OrderItemSnapshot>();
        // Keep Items for backward compat with existing mock orders (may be null for new orders)
        public List<CartItem> Items { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public bool HasRefundRequest { get; set; }
    }

    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string CustomerName { get; set; }
        public int Stars { get; set; }
        public string Comment { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime DatePosted { get; set; }
        public DateTime? LastEditedAt { get; set; }
        public int Likes { get; set; }
        public int ReportCount { get; set; }
        public string Category { get; set; }
        public bool IsTextOnly => string.IsNullOrEmpty(PhotoUrl);
        public bool IsEdited => LastEditedAt.HasValue && LastEditedAt.Value > DatePosted;
        public List<ReviewComment> Comments { get; set; } = new List<ReviewComment>();
    }

    public class ReviewComment
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
        public DateTime DatePosted { get; set; }
    }
}
