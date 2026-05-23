-- up
CREATE TABLE orders (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT NOT NULL,
    quantity INT NOT NULL,
    order_date DATETIME2 NOT NULL
);

CREATE TABLE products (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(MAX) NULL,
    price DECIMAL(18,4) NOT NULL,
    discount DECIMAL(18,4) NULL,
    in_stock BIT NOT NULL
);

-- down
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS products;
