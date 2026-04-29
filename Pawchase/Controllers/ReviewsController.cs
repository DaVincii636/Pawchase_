using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using Pawchase.Models;

namespace Pawchase.Controllers
{
    public class ReviewsController : Controller
    {
        public ActionResult Index(string sortBy = "recent", string category = "All", string photoFilter = "all", int? openReviewId = null)
        {
            SyncReviewAuthorNames();
            var reviews = MockData.Reviews.AsEnumerable();
            var reportedIds = GetReportedReviewIds();

            if (!string.Equals(category, "All", System.StringComparison.OrdinalIgnoreCase))
            {
                var productCategoryById = MockData.Products
                    .GroupBy(p => p.Id)
                    .ToDictionary(g => g.Key, g => g.First().Category);

                reviews = reviews.Where(r =>
                    productCategoryById.TryGetValue(r.ProductId, out var productCategory) &&
                    string.Equals(productCategory, category, System.StringComparison.OrdinalIgnoreCase));
            }

            if (photoFilter == "without")
            {
                reviews = reviews.Where(r => r.IsTextOnly);
            }
            else if (photoFilter == "with")
            {
                reviews = reviews.Where(r => !r.IsTextOnly);
            }

            switch (sortBy)
            {
                case "rating":
                    reviews = reviews
                        .OrderByDescending(r => r.Stars)
                        .ThenBy(r => reportedIds.Contains(r.Id));
                    break;
                case "likes":
                    reviews = reviews.OrderByDescending(r => r.Likes);
                    break;
                case "recent":
                default:
                    reviews = reviews.OrderByDescending(r => r.DatePosted);
                    break;
            }

            ViewBag.SortBy = sortBy;
            ViewBag.Category = category;
            ViewBag.PhotoFilter = photoFilter;
            ViewBag.OpenReviewId = openReviewId;
            ViewBag.LikedReviewIds = GetLikedReviewIds();
            ViewBag.ReportedReviewIds = reportedIds;
            ViewBag.Products = MockData.Products.Select(p => new { p.Id, p.Name }).ToList();

            return View(reviews.ToList());
        }

        public ActionResult MyReviews()
        {
            SyncReviewAuthorNames();
            int userId = GetCurrentUserId();
            var reviews = MockData.Reviews
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.IsTextOnly)
                .ThenByDescending(r => r.DatePosted)
                .ToList();

            var likedIds = GetLikedReviewIds();
            ViewBag.LikedReviews = MockData.Reviews
                .Where(r => likedIds.Contains(r.Id))
                .OrderByDescending(r => r.Likes)
                .ToList();
            ViewBag.LikedReviewIds = likedIds;
            ViewBag.ReportedReviewIds = GetReportedReviewIds();
            ViewBag.DisplayUserName = GetCurrentUserName();

            return View(reviews);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Submit(int productId, int stars, string comment, string customerName, HttpPostedFileBase photo)
        {
            int userId = GetCurrentUserId();
            var resolvedName = GetCurrentUserName();

            var review = new Review
            {
                Id = MockData.Reviews.Count > 0 ? MockData.Reviews.Max(r => r.Id) + 1 : 1,
                ProductId = productId,
                UserId = userId,
                CustomerName = resolvedName,
                Stars = stars,
                Comment = comment,
                PhotoUrl = photo != null && photo.ContentLength > 0
                    ? "/Content/images/reviews/upload-placeholder.png"
                    : null,
                DatePosted = System.DateTime.Now,
                Likes = 0,
                Category = GetProductCategory(productId)
            };
            MockData.Reviews.Add(review);
            TempData["ReviewPosted"] = true;

            if (!string.IsNullOrEmpty(Request.Form["returnTo"]) &&
                Request.Form["returnTo"] == "myreviews")
            {
                return RedirectToAction("MyReviews");
            }

            return RedirectToAction("Index", new { sortBy = "recent", category = "All", photoFilter = "all" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(int id, int stars, string comment)
        {
            var review = MockData.Reviews.FirstOrDefault(r => r.Id == id);
            if (review != null && review.UserId == GetCurrentUserId())
            {
                review.Stars = stars;
                review.Comment = comment;
                review.LastEditedAt = System.DateTime.Now;
                TempData["ReviewUpdated"] = true;
                return RedirectToAction("MyReviews");
            }
            return HttpNotFound();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var review = MockData.Reviews.FirstOrDefault(r => r.Id == id);
            if (review != null && review.UserId == GetCurrentUserId())
            {
                MockData.Reviews.Remove(review);
                TempData["ReviewDeleted"] = true;
                return RedirectToAction("MyReviews");
            }
            return HttpNotFound();
        }

        [HttpPost]
        public ActionResult Like(int id)
        {
            var review = MockData.Reviews.FirstOrDefault(r => r.Id == id);
            if (review != null)
            {
                // eunice modified: cannot like a review you have already reported
                var reportedIds = GetReportedReviewIds();
                if (reportedIds.Contains(id))
                {
                    return Json(new { ok = false, error = "You cannot like a review you have reported." });
                }

                var likedIds = GetLikedReviewIds();
                var alreadyLiked = likedIds.Contains(id);

                if (alreadyLiked)
                {
                    likedIds.Remove(id);
                    if (review.Likes > 0)
                    {
                        review.Likes--;
                    }
                }
                else
                {
                    likedIds.Add(id);
                    review.Likes++;
                }

                SaveLikedReviewIds(likedIds);
                return Json(new { ok = true, likes = review.Likes, liked = !alreadyLiked });
            }
            return Json(new { ok = false });
        }

        [HttpPost]
        public ActionResult Report(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Json(new { ok = false, error = "Please choose a reason before submitting your report." });
            }

            var review = MockData.Reviews.FirstOrDefault(r => r.Id == id);
            if (review == null)
            {
                return Json(new { ok = false, error = "Review not found." });
            }

            var reportedIds = GetReportedReviewIds();
            if (!reportedIds.Contains(id))
            {
                reportedIds.Add(id);
                review.ReportCount++;
                SaveReportedReviewIds(reportedIds);

                // eunice modified: auto-unlike the review if the user had previously liked it
                var likedIds = GetLikedReviewIds();
                if (likedIds.Contains(id))
                {
                    likedIds.Remove(id);
                    if (review.Likes > 0) review.Likes--;
                    SaveLikedReviewIds(likedIds);
                }
            }

            return Json(new { ok = true, reportCount = review.ReportCount, reported = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddComment(int reviewId, string commentText, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Index") : returnUrl);
            }

            var review = MockData.Reviews.FirstOrDefault(r => r.Id == reviewId);
            if (review == null)
            {
                return HttpNotFound();
            }

            if (review.Comments == null)
            {
                review.Comments = new List<ReviewComment>();
            }

            var nextCommentId = MockData.Reviews
                .SelectMany(r => r.Comments ?? new List<ReviewComment>())
                .Select(c => c.Id)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var newComment = new ReviewComment
            {
                Id = nextCommentId,
                ReviewId = reviewId,
                UserId = GetCurrentUserId(),
                UserName = Session["UserName"] as string ?? "alex",
                Text = commentText.Trim(),
                DatePosted = System.DateTime.Now
            };

            review.Comments.Add(newComment);

            if (Request.IsAjaxRequest())
            {
                return Json(new
                {
                    ok = true,
                    reviewId = reviewId,
                    commentCount = review.Comments.Count,
                    comment = new
                    {
                        userName = newComment.UserName,
                        text = newComment.Text,
                        date = newComment.DatePosted.ToString("MMM dd, yyyy hh:mm tt")
                    }
                });
            }

            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Index") : returnUrl);
        }

        private int GetCurrentUserId()
{
    var userId = 0;
    int.TryParse(Session["UserId"]?.ToString(), out userId);
    return userId;
}

        private string GetCurrentUserName()
        {
            var sessionName = Session["UserName"] as string;
            if (!string.IsNullOrWhiteSpace(sessionName))
            {
                return sessionName;
            }

            var currentUser = MockData.Users.FirstOrDefault(u => u.Id == GetCurrentUserId());
            return !string.IsNullOrWhiteSpace(currentUser?.FullName)
                ? currentUser.FullName
                : "Anonymous";
        }

        private void SyncReviewAuthorNames()
        {
            var userLookup = MockData.Users
                .GroupBy(u => u.Id)
                .ToDictionary(g => g.Key, g => g.First().FullName);

            foreach (var review in MockData.Reviews)
            {
                if (userLookup.TryGetValue(review.UserId, out var userName) && !string.IsNullOrWhiteSpace(userName))
                {
                    review.CustomerName = userName;
                }
            }
        }

        private string GetProductCategory(int productId)
        {
            var product = MockData.Products.FirstOrDefault(p => p.Id == productId);
            return product?.Category ?? "Others";
        }

        private List<int> GetLikedReviewIds()
        {
            var liked = Session["LikedReviewIds"] as List<int>;
            if (liked == null)
            {
                liked = new List<int>();
                Session["LikedReviewIds"] = liked;
            }

            return liked;
        }

        private void SaveLikedReviewIds(List<int> likedIds)
        {
            Session["LikedReviewIds"] = likedIds;
        }

        private List<int> GetReportedReviewIds()
        {
            var reported = Session["ReportedReviewIds"] as List<int>;
            if (reported == null)
            {
                reported = new List<int>();
                Session["ReportedReviewIds"] = reported;
            }

            return reported;
        }

        private void SaveReportedReviewIds(List<int> reportedIds)
        {
            Session["ReportedReviewIds"] = reportedIds;
        }
    }
}
