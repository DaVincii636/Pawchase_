USE pawchase_db;

DELETE rc
FROM review_comments rc
JOIN reviews r ON r.id = rc.review_id
WHERE r.customer_name = 'Google User'
   OR r.user_id IN (SELECT id FROM users WHERE email = 'googleuser@gmail.com');

DELETE FROM reviews
WHERE customer_name = 'Google User'
   OR user_id IN (SELECT id FROM users WHERE email = 'googleuser@gmail.com');

DELETE oi
FROM order_items oi
JOIN orders o ON o.id = oi.order_id
WHERE o.email = 'googleuser@gmail.com';

DELETE FROM orders
WHERE email = 'googleuser@gmail.com';

DELETE FROM saved_addresses
WHERE user_id IN (SELECT id FROM users WHERE email = 'googleuser@gmail.com');

DELETE FROM users
WHERE email = 'googleuser@gmail.com';
