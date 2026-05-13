-- Tạo Database (Chạy xong 3 dòng này thì bôi đen chạy các phần dưới)
CREATE DATABASE DB_ShopQuanAo_DataWarehouse;
GO
USE DB_ShopQuanAo_DataWarehouse;
GO

-- ==========================================
-- PHẦN 1: TẠO CÁC BẢNG CHIỀU (DIMENSION)
-- ==========================================

-- 1. Bảng Chiều Thời Gian
CREATE TABLE Dim_Time (
    TimeKey INT PRIMARY KEY,       -- Định dạng YYYYMMDD (VD: 20240513)
    FullDate DATE NOT NULL,        -- Ngày gốc
    Day INT NOT NULL,              -- Ngày trong tháng
    Month INT NOT NULL,            -- Tháng
    Quarter INT NOT NULL,          -- Quý (1,2,3,4)
    Year INT NOT NULL              -- Năm
);

-- 2. Bảng Chiều Khách Hàng
CREATE TABLE Dim_Customer (
    CustomerKey INT IDENTITY(1,1) PRIMARY KEY, -- Khóa nhân tạo tự tăng
    SourceUserID INT NOT NULL,                 -- ID thật từ Web C#
    FullName NVARCHAR(255) NULL,
    Gender NVARCHAR(50) NULL,
    Age INT NULL,
    AgeGroup NVARCHAR(50) NULL,                -- Nhóm tuổi (VD: '18-24', '25-34')
    City NVARCHAR(255) NULL,
    Country NVARCHAR(255) NULL                 -- Quốc gia (Vừa thêm mới)
);

-- 3. Bảng Chiều Sản Phẩm
CREATE TABLE Dim_Product (
    ProductKey INT IDENTITY(1,1) PRIMARY KEY,
    SourceProductID INT NOT NULL,
    ProductName NVARCHAR(255) NULL,
    CategoryName NVARCHAR(255) NULL,
    Size NVARCHAR(50) NULL,
    OriginalPrice DECIMAL(18,2) NULL
);

-- 4. Bảng Chiều Nhà Cung Cấp
CREATE TABLE Dim_Supplier (
    SupplierKey INT IDENTITY(1,1) PRIMARY KEY,
    SourceSupplierID INT NOT NULL,
    SupplierName NVARCHAR(255) NULL,
    City NVARCHAR(255) NULL
);

-- ==========================================
-- PHẦN 2: TẠO CÁC BẢNG SỰ KIỆN (FACT)
-- ==========================================

-- 5. Bảng Sự Kiện Bán Hàng (Xuất hàng)
CREATE TABLE Fact_Sales (
    SalesKey INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Các khóa ngoại nối với bảng Chiều
    TimeKey INT NOT NULL FOREIGN KEY REFERENCES Dim_Time(TimeKey),
    CustomerKey INT NOT NULL FOREIGN KEY REFERENCES Dim_Customer(CustomerKey),
    ProductKey INT NOT NULL FOREIGN KEY REFERENCES Dim_Product(ProductKey),
    
    -- Các con số đo lường (Measures)
    Quantity INT NOT NULL,                 -- Số lượng mua
    UnitPrice DECIMAL(18,2) NOT NULL,      -- Giá bán 1 SP
    DiscountAmount DECIMAL(18,2) NOT NULL, -- Tiền được giảm
    TotalRevenue DECIMAL(18,2) NOT NULL    -- Tổng tiền = (Quantity * UnitPrice) - DiscountAmount
);

-- 6. Bảng Sự Kiện Nhập Hàng
CREATE TABLE Fact_Inventory (
    InventoryKey INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Các khóa ngoại
    TimeKey INT NOT NULL FOREIGN KEY REFERENCES Dim_Time(TimeKey),
    ProductKey INT NOT NULL FOREIGN KEY REFERENCES Dim_Product(ProductKey),
    SupplierKey INT NOT NULL FOREIGN KEY REFERENCES Dim_Supplier(SupplierKey),
    
    -- Các con số đo lường (Measures)
    QuantityReceived INT NOT NULL,         -- Số lượng nhập
    UnitCost DECIMAL(18,2) NOT NULL,       -- Giá nhập kho 1 SP
    TotalCost DECIMAL(18,2) NOT NULL       -- Tổng chi phí = QuantityReceived * UnitCost
);
