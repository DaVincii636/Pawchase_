USE pawchase_db;

-- Run this after testing the site, then close and rerun the Visual Studio app.
-- If these rows are still here after rerun, the system is saving to MySQL.

SELECT id, full_name, email, role, created_at
FROM users
ORDER BY id DESC
LIMIT 10;

SELECT id, reference_number, customer_name, status, total, has_refund_request,
       refund_reason, gcash_number,
       CASE
           WHEN refund_evidence_url IS NULL OR refund_evidence_url = '' THEN 'NO'
           ELSE 'YES'
       END AS has_refund_photo
FROM orders
ORDER BY id DESC
LIMIT 10;

SELECT id, product_id, user_id, customer_name, stars, comment,
       CASE
           WHEN photo_url IS NULL OR photo_url = '' THEN 'NO'
           ELSE 'YES'
       END AS has_review_photo,
       date_posted
FROM reviews
ORDER BY id DESC
LIMIT 10;
