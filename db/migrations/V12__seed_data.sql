-- V12__seed_data.sql
-- Starter product_categories/product_types so the expiry fallback chain
-- has something to resolve against on day one. Deliberately sparse —
-- per PROJECT_NOTES.md, these are pre-fills the user confirms/edits, not
-- an authoritative list, so a short starter set is fine; add more as
-- real usage surfaces gaps. Looked up by name rather than hardcoded
-- UUIDs, since both tables generate their own ids.

INSERT INTO product_categories (name) VALUES
    ('Meat & Fish'),
    ('Dairy & Eggs'),
    ('Produce'),
    ('Frozen'),
    ('Bakery'),
    ('Pantry'),
    ('Leftovers')
ON CONFLICT (name) DO NOTHING;

INSERT INTO product_types (category_id, name, default_expiry_days)
SELECT c.id, t.name, t.default_expiry_days
FROM (VALUES
    ('Meat & Fish',  'Raw ground meat',       2),
    ('Meat & Fish',  'Raw poultry',           2),
    ('Meat & Fish',  'Fresh fish',            2),
    ('Dairy & Eggs', 'Milk',                  7),
    ('Dairy & Eggs', 'Eggs',                  21),
    ('Dairy & Eggs', 'Hard cheese',           21),
    ('Produce',      'Leafy greens',          5),
    ('Produce',      'Root vegetables',       14),
    ('Frozen',       'Frozen vegetables',     270),
    ('Frozen',       'Frozen fries',          270),
    ('Frozen',       'Frozen meat',           180),
    ('Bakery',       'Bread',                 5),
    ('Pantry',       'Canned goods',          365),
    ('Leftovers',    'Cooked leftovers',      4)
) AS t(category_name, name, default_expiry_days)
JOIN product_categories c ON c.name = t.category_name
ON CONFLICT DO NOTHING;
