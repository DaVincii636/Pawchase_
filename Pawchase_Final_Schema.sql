-- PawChase final MySQL schema
-- Put this file beside Pawchase.sln and run it against your MySQL connection.
-- It is safe for an existing pawchase_db database: it creates missing tables,
-- adds the missing product image/variant columns, and widens image columns.

CREATE DATABASE IF NOT EXISTS `pawchase_db`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

USE `pawchase_db`;

CREATE TABLE IF NOT EXISTS `users` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `full_name` VARCHAR(200) NOT NULL,
  `email` VARCHAR(200) NOT NULL,
  `password` VARCHAR(255) NOT NULL,
  `phone` VARCHAR(20) DEFAULT NULL,
  `gcash_number` VARCHAR(20) DEFAULT NULL,
  `is_admin` TINYINT(1) NOT NULL DEFAULT 0,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `products` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(200) NOT NULL,
  `description` TEXT,
  `price` DECIMAL(10,2) NOT NULL,
  `original_price` DECIMAL(10,2) DEFAULT NULL,
  `category` VARCHAR(100) NOT NULL,
  `breed_size` VARCHAR(50) DEFAULT NULL,
  `image_url` LONGTEXT,
  `additional_images` LONGTEXT,
  `stock` INT NOT NULL DEFAULT 0,
  `is_deleted` TINYINT(1) NOT NULL DEFAULT 0,
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_products_category` (`category`),
  KEY `idx_products_breed_size` (`breed_size`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `product_variants` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `product_id` INT NOT NULL,
  `label` VARCHAR(100) DEFAULT NULL,
  `image_url` LONGTEXT,
  `stock` INT NOT NULL DEFAULT 0,
  `price` DECIMAL(10,2) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `product_id` (`product_id`),
  CONSTRAINT `product_variants_ibfk_1`
    FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `orders` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `reference_number` VARCHAR(50) NOT NULL,
  `customer_name` VARCHAR(200) NOT NULL,
  `email` VARCHAR(200) NOT NULL,
  `address` TEXT NOT NULL,
  `phone` VARCHAR(20) DEFAULT NULL,
  `total` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
  `order_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `status` VARCHAR(50) NOT NULL DEFAULT 'To Ship',
  `has_refund_request` TINYINT(1) NOT NULL DEFAULT 0,
  `is_received_by_customer` TINYINT(1) NOT NULL DEFAULT 0,
  `is_reviewed` TINYINT(1) NOT NULL DEFAULT 0,
  `cancel_reason` VARCHAR(500) DEFAULT NULL,
  `refund_reason` VARCHAR(500) DEFAULT NULL,
  `gcash_number` VARCHAR(20) DEFAULT NULL,
  `refund_evidence_url` LONGTEXT,
  `order_notes` VARCHAR(500) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `reference_number` (`reference_number`),
  KEY `idx_orders_email` (`email`),
  KEY `idx_orders_status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `order_items` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `order_id` INT NOT NULL,
  `product_id` INT NOT NULL,
  `product_name` VARCHAR(200) NOT NULL,
  `product_image_url` LONGTEXT,
  `category` VARCHAR(100) DEFAULT NULL,
  `breed_size` VARCHAR(50) DEFAULT NULL,
  `unit_price` DECIMAL(10,2) NOT NULL,
  `quantity` INT NOT NULL DEFAULT 1,
  `variant_label` VARCHAR(100) DEFAULT NULL,
  `variant_image_url` LONGTEXT,
  PRIMARY KEY (`id`),
  KEY `order_id` (`order_id`),
  CONSTRAINT `order_items_ibfk_1`
    FOREIGN KEY (`order_id`) REFERENCES `orders` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `reviews` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `product_id` INT NOT NULL,
  `user_id` INT NOT NULL,
  `customer_name` VARCHAR(200) NOT NULL,
  `stars` INT NOT NULL DEFAULT 5,
  `comment` TEXT NOT NULL,
  `photo_url` LONGTEXT,
  `date_posted` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `last_edited_at` DATETIME DEFAULT NULL,
  `likes` INT NOT NULL DEFAULT 0,
  `report_count` INT NOT NULL DEFAULT 0,
  `category` VARCHAR(100) DEFAULT NULL,
  `seller_reply` TEXT,
  `seller_reply_date` DATETIME DEFAULT NULL,
  `is_verified_purchase` TINYINT(1) DEFAULT 0,
  PRIMARY KEY (`id`),
  KEY `user_id` (`user_id`),
  KEY `idx_reviews_product_id` (`product_id`),
  CONSTRAINT `reviews_ibfk_1`
    FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE CASCADE,
  CONSTRAINT `reviews_ibfk_2`
    FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `review_comments` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `review_id` INT NOT NULL,
  `user_id` INT NOT NULL,
  `user_name` VARCHAR(200) NOT NULL,
  `text` TEXT NOT NULL,
  `date_posted` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `review_id` (`review_id`),
  CONSTRAINT `review_comments_ibfk_1`
    FOREIGN KEY (`review_id`) REFERENCES `reviews` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `saved_addresses` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `user_id` INT NOT NULL,
  `label` VARCHAR(100) DEFAULT NULL,
  `address` TEXT NOT NULL,
  `phone` VARCHAR(20) DEFAULT NULL,
  `is_default` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  KEY `idx_saved_addresses_user_id` (`user_id`),
  CONSTRAINT `saved_addresses_ibfk_1`
    FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

DELIMITER $$

DROP PROCEDURE IF EXISTS `pawchase_ensure_column`$$
CREATE PROCEDURE `pawchase_ensure_column`(
  IN p_table_name VARCHAR(64),
  IN p_column_name VARCHAR(64),
  IN p_definition TEXT,
  IN p_after_column VARCHAR(64)
)
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = p_table_name
      AND COLUMN_NAME = p_column_name
  ) THEN
    SET @sql = CONCAT(
      'ALTER TABLE `', p_table_name, '` ADD COLUMN `', p_column_name, '` ', p_definition,
      IF(p_after_column IS NULL OR p_after_column = '', '', CONCAT(' AFTER `', p_after_column, '`'))
    );
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
  END IF;
END$$

DROP PROCEDURE IF EXISTS `pawchase_modify_column_type`$$
CREATE PROCEDURE `pawchase_modify_column_type`(
  IN p_table_name VARCHAR(64),
  IN p_column_name VARCHAR(64),
  IN p_required_type VARCHAR(64),
  IN p_definition TEXT
)
BEGIN
  DECLARE v_data_type VARCHAR(64);

  SELECT DATA_TYPE INTO v_data_type
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = p_table_name
    AND COLUMN_NAME = p_column_name
  LIMIT 1;

  IF v_data_type IS NOT NULL AND LOWER(v_data_type) <> LOWER(p_required_type) THEN
    SET @sql = CONCAT(
      'ALTER TABLE `', p_table_name, '` MODIFY COLUMN `', p_column_name, '` ', p_definition
    );
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
  END IF;
END$$

DELIMITER ;

CALL `pawchase_ensure_column`('products', 'additional_images', 'LONGTEXT NULL', 'image_url');
CALL `pawchase_ensure_column`('product_variants', 'price', 'DECIMAL(10,2) NULL', 'stock');

CALL `pawchase_modify_column_type`('products', 'image_url', 'longtext', 'LONGTEXT NULL');
CALL `pawchase_modify_column_type`('products', 'additional_images', 'longtext', 'LONGTEXT NULL');
CALL `pawchase_modify_column_type`('product_variants', 'image_url', 'longtext', 'LONGTEXT NULL');
CALL `pawchase_modify_column_type`('orders', 'refund_evidence_url', 'longtext', 'LONGTEXT NULL');
CALL `pawchase_modify_column_type`('order_items', 'product_image_url', 'longtext', 'LONGTEXT NULL');
CALL `pawchase_modify_column_type`('order_items', 'variant_image_url', 'longtext', 'LONGTEXT NULL');
CALL `pawchase_modify_column_type`('reviews', 'photo_url', 'longtext', 'LONGTEXT NULL');

DROP PROCEDURE IF EXISTS `pawchase_modify_column_type`;
DROP PROCEDURE IF EXISTS `pawchase_ensure_column`;
