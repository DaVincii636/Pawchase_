using System;
using System.Collections.Generic;

namespace Pawchase.Models
{
    // Represents a single color/style variant of a product (image + label)
    public class ProductVariant
    {
        public string ImageUrl { get; set; }   // base64 data URL or relative path
        public string Label { get; set; }      // e.g. "Red", "Blue", "Large"
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string Category { get; set; }   // Treats, Toys, Accessories, Health, Others
        public string BreedSize { get; set; }  // Small, Medium, Large, All
        public string ImageUrl { get; set; }
        public int Stock { get; set; }
        public bool IsOnSale => OriginalPrice.HasValue && OriginalPrice.Value > Price;
        // Color/style variants — optional, up to 4
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
        // The specific variant the customer selected (null if product has no variants)
        public ProductVariant SelectedVariant { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public List<CartItem> Items { get; set; }
        public decimal Total { get; set; }
        public DateTime OrderDate { get; set; }
        // Statuses: To Ship | In Transit | Out for Delivery | Delivered | To Rate | Completed | Refund Requested
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
