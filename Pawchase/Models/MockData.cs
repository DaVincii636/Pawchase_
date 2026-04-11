using System;
using System.Collections.Generic;

namespace Pawchase.Models
{
    public static class MockData
    {
        public static List<Product> Products = new List<Product>
        {
            // TREATS
            new Product { Id=1,  Name="Premium Chicken Treats",     Description="High-protein chicken treats for small dogs. Great for training.", Price=299m, OriginalPrice=399m, Category="Treats",      BreedSize="Small",  ImageUrl="/Content/images/products/placeholder.png", Stock=50 },
            new Product { Id=2,  Name="Beef Jerky Dog Strips",       Description="Natural beef jerky strips. No artificial preservatives.",         Price=349m,                     Category="Treats",      BreedSize="Medium", ImageUrl="/Content/images/products/placeholder.png", Stock=40 },
            new Product { Id=3,  Name="Large Breed Dental Chews",    Description="Reduces tartar and freshens breath. Made for big dogs.",          Price=499m, OriginalPrice=599m, Category="Treats",      BreedSize="Large",  ImageUrl="/Content/images/products/placeholder.png", Stock=30 },
            // TOYS
            new Product { Id=4,  Name="Mini Squeaky Ball Set",       Description="Set of 3 squeaky balls sized for small breeds.",                  Price=199m, OriginalPrice=260m, Category="Toys",        BreedSize="Small",  ImageUrl="/Content/images/products/placeholder.png", Stock=80 },
            new Product { Id=5,  Name="Rubber Chew Toy",             Description="Durable rubber toy for medium to large chewers.",                 Price=249m,                     Category="Toys",        BreedSize="Medium", ImageUrl="/Content/images/products/placeholder.png", Stock=100 },
            new Product { Id=6,  Name="Tug Rope Toy XL",             Description="Heavy-duty braided rope toy for large breeds.",                   Price=299m,                     Category="Toys",        BreedSize="Large",  ImageUrl="/Content/images/products/placeholder.png", Stock=60 },
            // ACCESSORIES
            new Product { Id=7,  Name="Adjustable Puppy Collar",     Description="Soft nylon collar with quick-release buckle. Small size.",        Price=150m,                     Category="Accessories", BreedSize="Small",  ImageUrl="/Content/images/products/placeholder.png", Stock=75 },
            new Product { Id=8,  Name="Padded Harness & Leash Set",  Description="No-pull harness set for medium breed dogs.",                      Price=450m, OriginalPrice=550m, Category="Accessories", BreedSize="Medium", ImageUrl="/Content/images/products/placeholder.png", Stock=35 },
            new Product { Id=9,  Name="Heavy Duty Leash 2m",         Description="Strong nylon leash for large breed dogs. 2 meters.",              Price=220m,                     Category="Accessories", BreedSize="Large",  ImageUrl="/Content/images/products/placeholder.png", Stock=50 },
            new Product { Id=10, Name="Soft Pet Carrier",            Description="Airline-approved soft carrier for small dogs.",                   Price=899m, OriginalPrice=1099m,Category="Accessories", BreedSize="Small",  ImageUrl="/Content/images/products/placeholder.png", Stock=20 },
            // HEALTH
            new Product { Id=11, Name="Small Dog Multivitamins",     Description="Daily chewable vitamins for small breed dogs. Chicken flavor.",   Price=399m,                     Category="Health",      BreedSize="Small",  ImageUrl="/Content/images/products/placeholder.png", Stock=55 },
            new Product { Id=12, Name="Joint Support Supplement",    Description="Glucosamine & chondroitin for medium breed joint health.",        Price=549m, OriginalPrice=649m, Category="Health",      BreedSize="Medium", ImageUrl="/Content/images/products/placeholder.png", Stock=40 },
            new Product { Id=13, Name="Tick & Flea Drops",           Description="Fast-acting topical treatment. Works for all sizes.",             Price=399m,                     Category="Health",      BreedSize="All",    ImageUrl="/Content/images/products/placeholder.png", Stock=60 },
            new Product { Id=14, Name="Dog Shampoo -- All Coats",     Description="Gentle oatmeal shampoo suitable for any coat type.",              Price=249m,                     Category="Health",      BreedSize="All",    ImageUrl="/Content/images/products/placeholder.png", Stock=70 },
            // OTHERS
            new Product { Id=15, Name="Stainless Steel Bowl Set",    Description="Non-slip double bowl set. Dishwasher safe.",                      Price=299m,                     Category="Others",      BreedSize="All",    ImageUrl="/Content/images/products/placeholder.png", Stock=45 },
            new Product { Id=16, Name="Orthopedic Dog Bed L",        Description="Memory foam bed for large breed dogs. Washable cover.",           Price=1299m,OriginalPrice=1599m,Category="Others",      BreedSize="Large",  ImageUrl="/Content/images/products/placeholder.png", Stock=15 },
        };

        public static List<User> Users = new List<User>
        {
            new User { Id=1, FullName="Juan dela Cruz", Email="juan@email.com",  Password="user123", IsAdmin=false },
            new User { Id=2, FullName="Maria Santos",   Email="maria@email.com", Password="user123", IsAdmin=false },
        };

        public static List<Order> Orders = new List<Order>
        {
            new Order {
                Id=1, ReferenceNumber="PWC-000001", CustomerName="Juan dela Cruz",
                Email="juan@email.com", Address="123 Rizal St, Brgy. Poblacion, Quezon City, 1100",
                Phone="09171234567",
                Items=new List<CartItem> {
                    new CartItem { Product=Products[0], Quantity=2 },
                    new CartItem { Product=Products[4], Quantity=1 }
                },
                Total=1047m, OrderDate=DateTime.Now.AddDays(-3), Status="Out for Delivery"
            },
            new Order {
                Id=2, ReferenceNumber="PWC-000002", CustomerName="Maria Santos",
                Email="maria@email.com", Address="456 Mabini Ave, Marikina City, 1800",
                Phone="09281234567",
                Items=new List<CartItem> {
                    new CartItem { Product=Products[1], Quantity=1 }
                },
                Total=349m, OrderDate=DateTime.Now.AddDays(-1), Status="To Ship"
            }
        };

        public static List<Review> Reviews = new List<Review>
        {
            new Review { Id=1, ProductId=1, CustomerName="Juan dela Cruz", Stars=5, Comment="My dog goes crazy for these treats! Great for training.", DatePosted=DateTime.Now.AddDays(-5) },
            new Review { Id=2, ProductId=5, CustomerName="Maria Santos",   Stars=4, Comment="Durable and my bulldog loves it. Fast shipping too!", DatePosted=DateTime.Now.AddDays(-8) },
            new Review { Id=3, ProductId=8, CustomerName="Pedro Cruz",     Stars=5, Comment="Perfect fit and very sturdy. No more pulling!", DatePosted=DateTime.Now.AddDays(-12) },
        };

        public static string AdminEmail    = "admin@pawchase.com";
        public static string AdminPassword = "PawChase@Admin2024";
    }
}
