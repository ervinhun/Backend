-- V15__simplify_product_categories.sql
-- One set of plain-language product categories, replacing both the V12
-- starter categories and the finer USDA FoodKeeper ones DbInitializer used
-- to create (e.g. the four "Meat - ..." categories are now just "Meat").
-- DataAccess/Seed/foodkeeper.json uses the same 16 names.
--
-- The V12 starter types are retired: FoodKeeper has a more specific type for
-- each ("Milk (plain or flavored)" for "Milk"). Only soft deletes, so items
-- that point at a retired type or category keep working.

-- 1. Retire the V12 starter types.
UPDATE product_types t
SET deleted_at = now()
FROM product_categories c
WHERE t.category_id = c.id
  AND t.deleted_at IS NULL
  AND (c.name, t.name) IN (VALUES
    ('Meat & Fish',  'Raw ground meat'),
    ('Meat & Fish',  'Raw poultry'),
    ('Meat & Fish',  'Fresh fish'),
    ('Dairy & Eggs', 'Milk'),
    ('Dairy & Eggs', 'Eggs'),
    ('Dairy & Eggs', 'Hard cheese'),
    ('Produce',      'Leafy greens'),
    ('Produce',      'Root vegetables'),
    ('Frozen',       'Frozen vegetables'),
    ('Frozen',       'Frozen fries'),
    ('Frozen',       'Frozen meat'),
    ('Bakery',       'Bread'),
    ('Pantry',       'Canned goods'),
    ('Leftovers',    'Cooked leftovers')
  );

-- 2. The categories. Bakery, Dairy & Eggs, Frozen and Pantry carry over
-- from V12 as they are; Leftovers becomes Deli & Leftovers.
UPDATE product_categories
SET name = 'Deli & Leftovers'
WHERE name = 'Leftovers'
  AND NOT EXISTS (SELECT 1 FROM product_categories WHERE name = 'Deli & Leftovers');

INSERT INTO product_categories (name) VALUES
    ('Baby Food'),
    ('Bakery'),
    ('Baking & Cooking'),
    ('Dairy & Eggs'),
    ('Deli & Leftovers'),
    ('Drinks'),
    ('Fish & Seafood'),
    ('Frozen'),
    ('Fruit'),
    ('Grains, Beans & Pasta'),
    ('Meat'),
    ('Pantry'),
    ('Plant Proteins'),
    ('Poultry'),
    ('Sauces & Condiments'),
    ('Vegetables')
ON CONFLICT (name) DO NOTHING;

-- 3. Move every type out of a replaced category, then retire those. On a new
-- database only the V12 ones exist here; DbInitializer then fills the new
-- categories. One that already ran the old seed keeps its rows, just moved.
CREATE TEMPORARY TABLE category_moves (old_name TEXT, new_name TEXT) ON COMMIT DROP;
INSERT INTO category_moves VALUES
    ('Baked Goods - Bakery',              'Bakery'),
    ('Baked Goods - Refrigerated Dough',  'Bakery'),
    ('Baked Goods - Baking and Cooking',  'Baking & Cooking'),
    ('Beverages',                         'Drinks'),
    ('Condiments, Sauces & Canned Goods', 'Sauces & Condiments'),
    ('Dairy Products & Eggs',             'Dairy & Eggs'),
    ('Food Purchased Frozen',             'Frozen'),
    ('Meat - Fresh',                      'Meat'),
    ('Meat - Shelf Stable Foods',         'Meat'),
    ('Meat - Smoked or Processed',        'Meat'),
    ('Meat - Stuffed or Assembled',       'Meat'),
    ('Poultry - Cooked or Processed',     'Poultry'),
    ('Poultry - Fresh',                   'Poultry'),
    ('Poultry - Shelf Stable Foods',      'Poultry'),
    ('Poultry - Stuffed or Assembled',    'Poultry'),
    ('Seafood - Fresh',                   'Fish & Seafood'),
    ('Seafood - Shellfish',               'Fish & Seafood'),
    ('Seafood - Smoked',                  'Fish & Seafood'),
    ('Produce - Fresh Fruits',            'Fruit'),
    ('Produce - Fresh Vegetables',        'Vegetables'),
    ('Shelf Stable Foods',                'Pantry'),
    ('Vegetarian Proteins',               'Plant Proteins'),
    ('Deli & Prepared Foods',             'Deli & Leftovers'),
    -- V12 categories that don't carry over, for any types users added to them.
    ('Meat & Fish',                       'Meat'),
    ('Produce',                           'Vegetables');

-- A type that would clash with one already in its new category stays put.
UPDATE product_types t
SET category_id = new_category.id
FROM category_moves m
JOIN product_categories old_category ON old_category.name = m.old_name
JOIN product_categories new_category ON new_category.name = m.new_name
WHERE t.category_id = old_category.id
  AND NOT EXISTS (
      SELECT 1 FROM product_types existing
      WHERE existing.category_id = new_category.id
        AND existing.name = t.name
        AND existing.deleted_at IS NULL
  );

UPDATE product_categories c
SET deleted_at = now()
FROM category_moves m
WHERE c.name = m.old_name
  AND c.deleted_at IS NULL;
