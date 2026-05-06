using System;
using System.Collections.Generic;
using System.Linq;

namespace Pawchase.Models
{
    public static class MockData
    {
        // ── Products ────────────────────────────────────────────────────────────
        private static List<Product> _products;
        public static List<Product> Products
        {
            get { return _products ?? (_products = DbHelper.GetAllProducts()); }
        }

        // ── Users ───────────────────────────────────────────────────────────────
        private static List<User> _users;
        public static List<User> Users
        {
            get { return _users ?? (_users = DbHelper.GetAllUsers()); }
        }

        // ── Orders ──────────────────────────────────────────────────────────────
        private static List<Order> _orders;
        public static List<Order> Orders
        {
            get { return _orders ?? (_orders = DbHelper.GetAllOrders()); }
        }

        // ── Reviews ─────────────────────────────────────────────────────────────
        private static List<Review> _reviews;
        public static List<Review> Reviews
        {
            get { return _reviews ?? (_reviews = DbHelper.GetAllReviews()); }
        }

        // ── In-session saved addresses (not persisted between app restarts) ───────
        public static Dictionary<int, List<SavedAddress>> UserAddresses =
            new Dictionary<int, List<SavedAddress>>();

        // ── Admin credentials ────────────────────────────────────────────────────
        public static string AdminEmail = "admin@pawchase.com";
        public static string AdminPassword = "PawChase@Admin2024";

        // ── Cache refresh helpers ────────────────────────────────────────────────
        // Call after any write so the next read pulls fresh data from the DB.
        public static void RefreshProducts() { _products = null; }
        public static void RefreshUsers() { _users = null; }
        public static void RefreshOrders() { _orders = null; }
        public static void RefreshReviews() { _reviews = null; }
    }
}