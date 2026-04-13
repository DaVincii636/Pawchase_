using System;
using System.Collections.Generic;
using System.Linq;

namespace Pawchase.Models
{
    public static class MockData
    {
        static MockData()
        {
            EnsureRichComments();
        }

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
            new User { Id=1, FullName="Alex", Email="alex@email.com",  Password="user123", IsAdmin=false },
            new User { Id=2, FullName="Maria Santos",   Email="maria@email.com", Password="user123", IsAdmin=false },
        };

        public static List<Order> Orders = new List<Order>
        {
            new Order {
                Id=1, ReferenceNumber="PWC-000001", CustomerName="Alex",
                Email="alex@email.com", Address="123 Rizal St, Brgy. Poblacion, Quezon City, 1100",
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
            new Review { Id=1, ProductId=1, UserId=1, CustomerName="alex", Stars=5, Comment="My dog goes crazy for these treats! Great for training.", PhotoUrl="/Content/images/review4.jpg", DatePosted=DateTime.Now.AddDays(-5), Likes=12, Category="Treats",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=1, ReviewId=1, UserId=2, UserName="Maria Santos", Text="Same! My pup loves this too.", DatePosted=DateTime.Now.AddDays(-4) },
                    new ReviewComment { Id=2, ReviewId=1, UserId=2, UserName="Carlo Reyes", Text="Thanks for sharing this review.", DatePosted=DateTime.Now.AddDays(-3) }
                }
            },
            new Review { Id=2, ProductId=5, UserId=2, CustomerName="Maria Santos", Stars=4, Comment="Durable and my bulldog loves it. Fast shipping too!", DatePosted=DateTime.Now.AddDays(-8), Likes=8, Category="Toys",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=3, ReviewId=2, UserId=1, UserName="alex", Text="How long did it last for you?", DatePosted=DateTime.Now.AddDays(-7) },
                    new ReviewComment { Id=22, ReviewId=2, UserId=2, UserName="Maria Santos", Text="Around 2 months with daily play.", DatePosted=DateTime.Now.AddDays(-6) }
                }
            },
            new Review { Id=3, ProductId=8, UserId=1, CustomerName="alex", Stars=5, Comment="Perfect fit and very sturdy. No more pulling!", PhotoUrl="/Content/images/review5.jpg", DatePosted=DateTime.Now.AddDays(-12), Likes=15, Category="Accessories",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=4, ReviewId=3, UserId=2, UserName="Nina Cruz", Text="Looks high quality!", DatePosted=DateTime.Now.AddDays(-11) },
                    new ReviewComment { Id=23, ReviewId=3, UserId=5, UserName="Ivy Ramos", Text="The buckle also feels very secure.", DatePosted=DateTime.Now.AddDays(-10) }
                }
            },
            new Review { Id=4, ProductId=2, UserId=2, CustomerName="Maria Santos", Stars=5, Comment="High quality beef jerky. My labrador retriever cannot get enough!", PhotoUrl="/Content/images/review1.jpg", DatePosted=DateTime.Now.AddDays(-3), Likes=18, ReportCount=2, Category="Treats",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=5, ReviewId=4, UserId=1, UserName="alex", Text="I will reorder this soon.", DatePosted=DateTime.Now.AddDays(-2) },
                    new ReviewComment { Id=6, ReviewId=4, UserId=3, UserName="Jude Lim", Text="Good value for money.", DatePosted=DateTime.Now.AddDays(-2) }
                }
            },
            new Review { Id=5, ProductId=11, UserId=1, CustomerName="alex", Stars=4, Comment="Great vitamins. My dog's coat is shinier now.", PhotoUrl="/Content/images/review6.jpg", DatePosted=DateTime.Now.AddDays(-10), Likes=9, Category="Health",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=7, ReviewId=5, UserId=2, UserName="Paula", Text="How many weeks before results?", DatePosted=DateTime.Now.AddDays(-9) },
                    new ReviewComment { Id=24, ReviewId=5, UserId=1, UserName="alex", Text="Around week two we noticed improvement.", DatePosted=DateTime.Now.AddDays(-8) }
                }
            },
            new Review { Id=6, ProductId=4, UserId=2, CustomerName="Maria Santos", Stars=5, Comment="My chihuahua loves these squeaky balls! So cute and durable.", PhotoUrl="/Content/images/review2.jpg", DatePosted=DateTime.Now.AddDays(-7), Likes=22, ReportCount=1, Category="Toys",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=8, ReviewId=6, UserId=1, UserName="alex", Text="This is the top toy in our house.", DatePosted=DateTime.Now.AddDays(-6) },
                    new ReviewComment { Id=9, ReviewId=6, UserId=4, UserName="Rica", Text="Can confirm, very durable.", DatePosted=DateTime.Now.AddDays(-6) }
                }
            },
            new Review { Id=7, ProductId=7, UserId=1, CustomerName="alex", Stars=4, Comment="Fits perfectly. Good quality material.", DatePosted=DateTime.Now.AddDays(-15), Likes=6, Category="Accessories",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=25, ReviewId=7, UserId=3, UserName="Karen Uy", Text="Looks neat and comfy.", DatePosted=DateTime.Now.AddDays(-14) },
                    new ReviewComment { Id=26, ReviewId=7, UserId=6, UserName="Mia Dela Pena", Text="Mine also fit well on a small breed.", DatePosted=DateTime.Now.AddDays(-13) }
                }
            },
            new Review { Id=8, ProductId=13, UserId=2, CustomerName="Maria Santos", Stars=5, Comment="Effective and safe. No side effects on my dog.", DatePosted=DateTime.Now.AddDays(-20), Likes=11, Category="Health",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=10, ReviewId=8, UserId=1, UserName="alex", Text="Worked well for my dog too.", DatePosted=DateTime.Now.AddDays(-19) },
                    new ReviewComment { Id=27, ReviewId=8, UserId=7, UserName="Leo Torres", Text="No irritation for my dog either.", DatePosted=DateTime.Now.AddDays(-18) }
                }
            },
            new Review { Id=9, ProductId=15, UserId=3, CustomerName="Karen Uy", Stars=5, Comment="Bowls are sturdy and easy to clean.", DatePosted=DateTime.Now.AddDays(-16), Likes=20, Category="Others",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=11, ReviewId=9, UserId=1, UserName="alex", Text="Loved this one.", DatePosted=DateTime.Now.AddDays(-15) },
                    new ReviewComment { Id=28, ReviewId=9, UserId=4, UserName="Ben Cruz", Text="Very stable even for fast eaters.", DatePosted=DateTime.Now.AddDays(-14) }
                }
            },
            new Review { Id=10, ProductId=16, UserId=4, CustomerName="Ben Cruz", Stars=5, Comment="Memory foam bed is super comfy. Worth it.", PhotoUrl="/Content/images/review3.jpg", DatePosted=DateTime.Now.AddDays(-2), Likes=24, ReportCount=3, Category="Others",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=12, ReviewId=10, UserId=1, UserName="alex", Text="This is now on my wishlist.", DatePosted=DateTime.Now.AddDays(-1) },
                    new ReviewComment { Id=13, ReviewId=10, UserId=2, UserName="Maria Santos", Text="My dog sleeps better now.", DatePosted=DateTime.Now.AddDays(-1) }
                }
            },
            new Review { Id=11, ProductId=3, UserId=1, CustomerName="alex", Stars=5, Comment="Dental chews reduced tartar quickly and breath smells better.", PhotoUrl="/Content/images/review7.jpg", DatePosted=DateTime.Now.AddDays(-13), Likes=14, Category="Treats",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=14, ReviewId=11, UserId=2, UserName="Liza", Text="Thanks for this, trying it soon.", DatePosted=DateTime.Now.AddDays(-12) },
                    new ReviewComment { Id=29, ReviewId=11, UserId=9, UserName="Noel Javier", Text="Same result here after one week.", DatePosted=DateTime.Now.AddDays(-11) }
                }
            },
            new Review { Id=12, ProductId=9, UserId=1, CustomerName="alex", Stars=5, Comment="Leash quality is excellent for large breed walks.", PhotoUrl="/Content/images/review8.jpg", DatePosted=DateTime.Now.AddDays(-9), Likes=17, Category="Accessories",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=15, ReviewId=12, UserId=3, UserName="Kyle", Text="How is the grip when wet?", DatePosted=DateTime.Now.AddDays(-8) },
                    new ReviewComment { Id=30, ReviewId=12, UserId=1, UserName="alex", Text="Grip is still solid even after rain.", DatePosted=DateTime.Now.AddDays(-7) }
                }
            },
            new Review { Id=13, ProductId=6, UserId=5, CustomerName="Ivy Ramos", Stars=2, Comment="The rope frayed faster than expected. Not ideal for heavy chewers.", DatePosted=DateTime.Now.AddDays(-6), Likes=2, Category="Toys",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=16, ReviewId=13, UserId=1, UserName="alex", Text="Thanks for sharing, I might skip this for my dog.", DatePosted=DateTime.Now.AddDays(-5) },
                    new ReviewComment { Id=31, ReviewId=13, UserId=8, UserName="Trisha Ong", Text="Mine had the same issue after a week.", DatePosted=DateTime.Now.AddDays(-4) }
                }
            },
            new Review { Id=14, ProductId=14, UserId=6, CustomerName="Mia Dela Pena", Stars=3, Comment="Shampoo is okay. Cleans well but scent fades quickly after one day.", PhotoUrl="/Content/images/review9.jpg", DatePosted=DateTime.Now.AddDays(-4), Likes=5, Category="Health",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=17, ReviewId=14, UserId=2, UserName="Maria Santos", Text="Good note on scent longevity.", DatePosted=DateTime.Now.AddDays(-3) },
                    new ReviewComment { Id=32, ReviewId=14, UserId=7, UserName="Leo Torres", Text="Neutral for me, cleaning was fine but smell was weak.", DatePosted=DateTime.Now.AddDays(-2) }
                }
            },
            new Review { Id=15, ProductId=10, UserId=7, CustomerName="Leo Torres", Stars=1, Comment="Carrier stitching loosened after first trip. Very disappointed with durability.", DatePosted=DateTime.Now.AddDays(-2), Likes=1, ReportCount=4, Category="Accessories",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=18, ReviewId=15, UserId=1, UserName="alex", Text="Sorry to hear that. Hope support can replace it.", DatePosted=DateTime.Now.AddDays(-2) },
                    new ReviewComment { Id=33, ReviewId=15, UserId=6, UserName="Mia Dela Pena", Text="Customer support helped me before, try contacting them.", DatePosted=DateTime.Now.AddDays(-1) }
                }
            },
            new Review { Id=16, ProductId=3, UserId=8, CustomerName="Trisha Ong", Stars=4, Comment="Dental chews helped a lot with tartar buildup. Texture is firm and dogs enjoy it.", PhotoUrl="/Content/images/review10.jpg", DatePosted=DateTime.Now.AddDays(-1), Likes=9, Category="Treats",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=19, ReviewId=16, UserId=3, UserName="Kyle", Text="Will try this for my husky.", DatePosted=DateTime.Now.AddDays(-1) },
                    new ReviewComment { Id=34, ReviewId=16, UserId=2, UserName="Maria Santos", Text="Our retriever enjoyed this too.", DatePosted=DateTime.Now.AddHours(-20) }
                }
            },
            new Review { Id=17, ProductId=11, UserId=9, CustomerName="Noel Javier", Stars=2, Comment="Didn't notice major improvement after 3 weeks. Might need a stronger formula.", DatePosted=DateTime.Now.AddDays(-11), Likes=3, Category="Health",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=20, ReviewId=17, UserId=2, UserName="Maria Santos", Text="Could depend on age and breed too.", DatePosted=DateTime.Now.AddDays(-10) },
                    new ReviewComment { Id=35, ReviewId=17, UserId=5, UserName="Ivy Ramos", Text="Same experience here, results were mild.", DatePosted=DateTime.Now.AddDays(-9) }
                }
            },
            new Review { Id=18, ProductId=15, UserId=10, CustomerName="Paolo Garcia", Stars=5, Comment="Best bowl set I've bought so far. Stable, heavy enough, and easy to wash daily.", PhotoUrl="/Content/images/review11.jpg", DatePosted=DateTime.Now.AddDays(-17), Likes=12, Category="Others",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=21, ReviewId=18, UserId=1, UserName="alex", Text="Looks very practical, thanks!", DatePosted=DateTime.Now.AddDays(-16) },
                    new ReviewComment { Id=36, ReviewId=18, UserId=4, UserName="Ben Cruz", Text="Great quality and no rust so far.", DatePosted=DateTime.Now.AddDays(-15) }
                }
            },
            new Review { Id=19, ProductId=12, UserId=11, CustomerName="Diane Flores", Stars=5, Comment="Excellent joint supplement. We noticed smoother movement after around ten days, and even our vet said our senior beagle looked more comfortable during walks. The chews are easy to give and never upset his stomach.", PhotoUrl="/Content/images/review6.jpg", DatePosted=DateTime.Now.AddDays(-6), Likes=16, Category="Health",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=37, ReviewId=19, UserId=2, UserName="Maria Santos", Text="Great to hear the improvement was that quick.", DatePosted=DateTime.Now.AddDays(-5) },
                    new ReviewComment { Id=38, ReviewId=19, UserId=3, UserName="Kyle", Text="Thanks for the detailed update.", DatePosted=DateTime.Now.AddDays(-4) }
                }
            },
            new Review { Id=20, ProductId=4, UserId=12, CustomerName="Rina Bautista", Stars=3, Comment="Cute set. My puppy likes the squeak but one ball lost air after a week.", PhotoUrl="/Content/images/review2.jpg", DatePosted=DateTime.Now.AddDays(-4), Likes=4, Category="Toys",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=39, ReviewId=20, UserId=1, UserName="alex", Text="Helpful and balanced feedback.", DatePosted=DateTime.Now.AddDays(-3) },
                    new ReviewComment { Id=40, ReviewId=20, UserId=6, UserName="Mia Dela Pena", Text="Had a similar issue with one piece too.", DatePosted=DateTime.Now.AddDays(-2) }
                }
            },
            new Review { Id=21, ProductId=10, UserId=13, CustomerName="Gerald Lim", Stars=2, Comment="The carrier looked nice in photos, but the side seam started loosening after two city trips. It still works for very short rides, though I would not trust it for long transport without reinforcement.", PhotoUrl="/Content/images/review9.jpg", DatePosted=DateTime.Now.AddDays(-3), Likes=2, Category="Accessories",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=41, ReviewId=21, UserId=7, UserName="Leo Torres", Text="This matches what happened to mine.", DatePosted=DateTime.Now.AddDays(-2) },
                    new ReviewComment { Id=42, ReviewId=21, UserId=2, UserName="Maria Santos", Text="Thanks, this helps before ordering.", DatePosted=DateTime.Now.AddDays(-1) }
                }
            },
            new Review { Id=22, ProductId=1, UserId=14, CustomerName="Faith Gomez", Stars=4, Comment="Solid training treat. Great value.", PhotoUrl="/Content/images/review4.jpg", DatePosted=DateTime.Now.AddDays(-2), Likes=7, Category="Treats",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=43, ReviewId=22, UserId=5, UserName="Ivy Ramos", Text="Nice quick review, thanks.", DatePosted=DateTime.Now.AddDays(-1) },
                    new ReviewComment { Id=44, ReviewId=22, UserId=1, UserName="alex", Text="Agree, works well for rewards.", DatePosted=DateTime.Now.AddHours(-20) }
                }
            },
            new Review { Id=23, ProductId=14, UserId=15, CustomerName="Tina Valdez", Stars=3, Comment="This shampoo cleans decently and leaves the coat soft for a day. The scent is mild and doesn't last long, but it did not irritate my dog's skin, so I might keep it as a backup option.", PhotoUrl="/Content/images/review10.jpg", DatePosted=DateTime.Now.AddDays(-8), Likes=5, Category="Health",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=45, ReviewId=23, UserId=9, UserName="Noel Javier", Text="Backup option is a fair way to put it.", DatePosted=DateTime.Now.AddDays(-7) },
                    new ReviewComment { Id=46, ReviewId=23, UserId=2, UserName="Maria Santos", Text="Good note about skin sensitivity.", DatePosted=DateTime.Now.AddDays(-6) }
                }
            },
            new Review { Id=24, ProductId=16, UserId=16, CustomerName="Ralph Aquino", Stars=5, Comment="Our husky immediately settled on this bed and now sleeps through the night. Cushion density is excellent and the cover is easy to remove for cleaning.", PhotoUrl="/Content/images/review3.jpg", DatePosted=DateTime.Now.AddDays(-1), Likes=10, Category="Others",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=47, ReviewId=24, UserId=4, UserName="Ben Cruz", Text="Great, comfort is what we were looking for.", DatePosted=DateTime.Now.AddHours(-18) },
                    new ReviewComment { Id=48, ReviewId=24, UserId=1, UserName="alex", Text="Nice to know the cover is easy to wash.", DatePosted=DateTime.Now.AddHours(-12) }
                }
            },
            new Review { Id=25, ProductId=2, UserId=1, CustomerName="Alex", Stars=5, Comment="Tried this after our evening walk and my dog finished the whole serving quickly. Texture looks fresh and easy to tear into smaller pieces for training.", PhotoUrl="/Content/images/review1.jpg", DatePosted=DateTime.Now.AddDays(-5), Likes=11, Category="Treats",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=49, ReviewId=25, UserId=2, UserName="Maria Santos", Text="Great tip on tearing it for rewards.", DatePosted=DateTime.Now.AddDays(-4) },
                    new ReviewComment { Id=50, ReviewId=25, UserId=3, UserName="Kyle", Text="Looks like a solid pick.", DatePosted=DateTime.Now.AddDays(-3) }
                }
            },
            new Review { Id=26, ProductId=7, UserId=1, CustomerName="Alex", Stars=4, Comment="Comfortable fit and the buckle feels secure.", DatePosted=DateTime.Now.AddDays(-3), Likes=6, Category="Accessories",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=51, ReviewId=26, UserId=6, UserName="Mia Dela Pena", Text="Good to know it stays secure.", DatePosted=DateTime.Now.AddDays(-2) },
                    new ReviewComment { Id=52, ReviewId=26, UserId=2, UserName="Maria Santos", Text="Thanks, I was checking this size too.", DatePosted=DateTime.Now.AddDays(-1) }
                }
            },
            new Review { Id=27, ProductId=11, UserId=1, CustomerName="Alex", Stars=3, Comment="I noticed a small improvement in coat shine after two weeks, but energy levels stayed mostly the same. Might continue for another month before deciding if I will reorder.", PhotoUrl="/Content/images/review6.jpg", DatePosted=DateTime.Now.AddDays(-2), Likes=4, Category="Health",
                Comments = new List<ReviewComment> {
                    new ReviewComment { Id=53, ReviewId=27, UserId=9, UserName="Noel Javier", Text="Fair and helpful review.", DatePosted=DateTime.Now.AddDays(-1) },
                    new ReviewComment { Id=54, ReviewId=27, UserId=5, UserName="Ivy Ramos", Text="Please update after a month.", DatePosted=DateTime.Now.AddHours(-14) }
                }
            }
        };

        public static string AdminEmail = "admin@pawchase.com";
        public static string AdminPassword = "PawChase@Admin2024";

        private static void EnsureRichComments()
        {
            var nextId = Reviews
                .SelectMany(r => r.Comments ?? new List<ReviewComment>())
                .Select(c => c.Id)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var positive = new[]
            {
                "Super helpful review, thanks for sharing this.",
                "Totally agree, this worked really well for us too.",
                "This is one of the better products we've tried so far."
            };

            var neutral = new[]
            {
                "Good to know. How long have you been using it?",
                "Seems okay based on your experience.",
                "Noted. I'll compare this with other options first."
            };

            var negative = new[]
            {
                "I had the opposite experience, quality felt inconsistent.",
                "Honestly expected better for the price.",
                "Did not work that well for us, but maybe depends on the dog."
            };

            var users = new[] { "Mara", "Josh", "Irene", "Ken", "Lia", "Paolo" };

            foreach (var review in Reviews)
            {
                if (review.Comments == null)
                {
                    review.Comments = new List<ReviewComment>();
                }

                // Ensure several comments per review with mixed tones.
                while (review.Comments.Count < 4)
                {
                    var toneIndex = review.Comments.Count % 3;
                    var text = toneIndex == 0
                        ? positive[(review.Id + review.Comments.Count) % positive.Length]
                        : toneIndex == 1
                            ? neutral[(review.Id + review.Comments.Count) % neutral.Length]
                            : negative[(review.Id + review.Comments.Count) % negative.Length];

                    review.Comments.Add(new ReviewComment
                    {
                        Id = nextId++,
                        ReviewId = review.Id,
                        UserId = 100 + review.Comments.Count,
                        UserName = users[(review.Id + review.Comments.Count) % users.Length],
                        Text = text,
                        DatePosted = review.DatePosted.AddHours(review.Comments.Count + 1)
                    });
                }
            }
        }
    }
}
