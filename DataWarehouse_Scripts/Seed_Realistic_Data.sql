Use ClothingShopWebsiteDB
go

-- Đảm bảo cột Status tồn tại trước khi seed (0 = chưa hiển thị, 1 = đã hiển thị giao diện khách)
IF COL_LENGTH('dbo.Products', 'Status') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [Status] INT NOT NULL CONSTRAINT [DF_Products_Status] DEFAULT (0);
    PRINT N'Đã thêm cột Products.Status.';
END
GO

-- 1. XÓA DỮ LIỆU CŨ THEO ĐÚNG THỨ TỰ RÀNG BUỘC KHÓA NGOẠI (Từ bảng con đến bảng cha)
DELETE FROM [InventoryReceiptDetails];
DELETE FROM [OrderDetails];
DELETE FROM [CartItems];
DELETE FROM [ProductSizes];
DELETE FROM [Advertisements];
DELETE FROM [UserAddresses];
DELETE FROM [Orders];
DELETE FROM [InventoryReceipts];
DELETE FROM [Products];
DELETE FROM [StaffShifts];
DELETE FROM [StaffDetails];
DELETE FROM [CustomerDetails];
DELETE FROM [Users];
DELETE FROM [Suppliers];
DELETE FROM [Discounts];
DELETE FROM [Categories];
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 1: [Categories] (Danh mục sản phẩm)
-- ==================================================================================
SET IDENTITY_INSERT [Categories] ON;
INSERT INTO [Categories] ([CategoryID], [CategoryName]) VALUES
(1, N'Áo Nam'),
(2, N'Quần Nam'),
(3, N'Váy Nữ'),
(4, N'Áo Nữ'),
(5, N'Đồ Thể Thao'),
(6, N'Phụ Kiện');
SET IDENTITY_INSERT [Categories] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 2: [Discounts] (Mã giảm giá)
-- ==================================================================================
SET IDENTITY_INSERT [Discounts] ON;
INSERT INTO [Discounts] ([DiscountID], [Code], [DiscountType], [DiscountValue], [Quantity], [UsedCount], [ExpirationDate]) VALUES
(1, 'NEVA20', 1, 20.00, 100, 25, '2026-12-31 23:59:59'),
(2, 'NEVA10', 1, 10.00, 200, 45, '2026-12-31 23:59:59'),
(3, 'KHAIXUAN', 0, 50000.00, 50, 20, '2026-06-30 23:59:59'),
(4, 'TRIAN', 0, 100000.00, 100, 15, '2026-08-31 23:59:59'),
(5, 'WINTER26', 1, 15.00, 150, 12, '2026-03-31 23:59:59');
SET IDENTITY_INSERT [Discounts] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 3: [Suppliers] (Nhà cung cấp)
-- ==================================================================================
SET IDENTITY_INSERT [Suppliers] ON;
INSERT INTO [Suppliers] ([SupplierID], [SupplierName], [Phone], [City], [Country], [ContactInfo]) VALUES
(1, N'Xưởng may thời trang NEVA HN', '0987654321', N'Hà Nội', N'Việt Nam', 'neva.hn@gmail.com'),
(2, N'Tổng kho sỉ quần áo Sài Gòn', '0912345678', N'TP Hồ Chí Minh', N'Việt Nam', 'khosi.sg@gmail.com'),
(3, N'Nhà máy Dệt may Việt Tiến', '02838640081', N'TP Hồ Chí Minh', N'Việt Nam', 'viettien@viettien.com.vn'),
(4, N'Xưởng thiết kế & gia công Bella', '0933445566', N'Đà Nẵng', N'Việt Nam', 'bella.design@gmail.com');
SET IDENTITY_INSERT [Suppliers] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 4: [Users] (Tài khoản gồm Admins/Staff và 30 Customers)
-- ==================================================================================
SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([UserID], [FullName], [Phone], [Role], [Password], [Gender], [DateOfBirth], [Status]) VALUES
-- Admin (Role 0)
(1, N'Admin Neva', '0900000000', 0, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1990-01-01', 1),
-- Staff (Role 1)
(2, N'Trần Thị Thu', '0900000001', 1, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1995-05-05', 1),
-- Customers (Role 2 - 30 Khách hàng thực tế)
(3, N'Nguyễn Văn Nam', '0912345678', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1993-04-15', 1),
(4, N'Trần Thị Hương', '0987654321', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1998-11-22', 1),
(5, N'Lê Hoàng Long', '0905123456', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1991-08-05', 1),
(6, N'Phạm Minh Tuấn', '0945678901', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1994-02-18', 1),
(7, N'Vũ Thị Mai', '0934567890', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1997-09-30', 1),
(8, N'Hoàng Anh Đức', '0978123456', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1989-12-12', 1),
(9, N'Đỗ Huy Khánh', '0919876543', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1995-07-25', 1),
(10, N'Phan Thanh Hà', '0963112233', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '2001-03-08', 1),
(11, N'Ngô Quốc Anh', '0909112233', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1992-06-19', 1),
(12, N'Bùi Thị Lan', '0988223344', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1996-01-27', 1),
(13, N'Nguyễn Đình Huy', '0977334455', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1994-10-14', 1),
(14, N'Đặng Minh Triết', '0911445566', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1990-05-30', 1),
(15, N'Dương Thúy Hằng', '0944556677', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1999-08-14', 1),
(16, N'Võ Duy Mạnh', '0933667788', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1996-02-12', 1),
(17, N'Đinh Văn Hùng', '0966778899', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1993-07-04', 1),
(18, N'Lâm Mỹ Tâm', '0988778899', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '2000-10-20', 1),
(19, N'Lý Hải Nam', '0977889900', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1997-04-11', 1),
(20, N'Tạ Minh Quân', '0911889900', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1991-03-24', 1),
(21, N'Trịnh Phương Nam', '0944889900', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1995-12-05', 1),
(22, N'Phùng Kiến Quốc', '0933990011', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1988-09-08', 1),
(23, N'Cao Thanh Thảo', '0966990011', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1998-05-18', 1),
(24, N'Mai Ngọc Anh', '0988990011', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '2002-06-25', 1),
(25, N'Đào Quốc Bảo', '0977001122', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1993-01-29', 1),
(26, N'Hà Thị Cúc', '0911001122', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1996-08-08', 1),
(27, N'Lương Thế Thành', '0944001122', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1990-12-25', 1),
(28, N'Nghiêm Xuân Trường', '0933112244', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1995-10-10', 1),
(29, N'Quách Hoàng Diệu', '0966112244', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '2001-02-15', 1),
(30, N'Giang Hồng Ngọc', '0988112244', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1994-05-17', 1),
(31, N'Chu Văn Biên', '0977223355', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1992-09-21', 1),
(32, N'Đỗ Thùy Trang', '0911223355', 2, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1997-12-30', 1),
-- Bổ sung 3 Nhân viên mới (Role 1)
(33, N'Nguyễn Minh Triết', '0900000002', 1, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1996-03-12', 1),
(34, N'Phạm Thảo Vy', '0900000003', 1, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 0, '1998-07-20', 1),
(35, N'Lê Anh Tuấn', '0900000004', 1, '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', 1, '1994-11-05', 1);
SET IDENTITY_INSERT [Users] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG MỚI: [CustomerDetails] (Chi tiết khách hàng - điểm tích lũy)
-- ==================================================================================
INSERT INTO [CustomerDetails] ([UserID], [RewardPoints], [MembershipTier]) VALUES
(3, 150, N'Bạc'),
(4, 80, N'Đồng'),
(5, 320, N'Vàng'),
(6, 45, N'Đồng'),
(7, 120, N'Bạc'),
(8, 210, N'Bạc'),
(9, 60, N'Đồng'),
(10, 0, N'Thường'),
(11, 130, N'Bạc'),
(12, 90, N'Đồng'),
(13, 55, N'Đồng'),
(14, 240, N'Bạc'),
(15, 75, N'Đồng'),
(16, 110, N'Bạc'),
(17, 30, N'Đồng'),
(18, 190, N'Bạc'),
(19, 0, N'Thường'),
(20, 280, N'Bạc'),
(21, 40, N'Đồng'),
(22, 160, N'Bạc'),
(23, 95, N'Đồng'),
(24, 10, N'Đồng'),
(25, 205, N'Bạc'),
(26, 50, N'Đồng'),
(27, 140, N'Bạc'),
(28, 85, N'Đồng'),
(29, 0, N'Thường'),
(30, 125, N'Bạc'),
(31, 70, N'Đồng'),
(32, 310, N'Vàng');
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG MỚI: [StaffDetails] (Chi tiết nhân viên)
-- ==================================================================================
INSERT INTO [StaffDetails] ([UserID], [HireDate], [Salary]) VALUES
(2, '2023-05-15', 12000000.00),
(33, '2024-02-10', 10500000.00),
(34, '2024-06-01', 11000000.00),
(35, '2025-01-15', 9500000.00);
GO

-- ==================================================================================
SET IDENTITY_INSERT [StaffShifts] ON;
INSERT INTO [StaffShifts] ([ShiftID], [UserID], [ShiftType], [DayOfWeek]) VALUES
(1, 2, 1, N'Thứ Hai'),
(2, 2, 2, N'Thứ Tư'),
(3, 2, 1, N'Thứ Sáu'),
(4, 33, 2, N'Thứ Ba'),
(5, 33, 3, N'Thứ Năm'),
(6, 34, 1, N'Thứ Bảy'),
(7, 34, 2, N'Chủ Nhật'),
(8, 35, 3, N'Thứ Hai'),
(9, 35, 1, N'Thứ Tư');
SET IDENTITY_INSERT [StaffShifts] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 5: [Products] (Sản phẩm — Status: 0=chưa hiển thị, 1=đã hiển thị)
-- ==================================================================================
SET IDENTITY_INSERT [Products] ON;
INSERT INTO [Products] ([ProductID], [CategoryID], [ProductName], [Price], [ImageUrl], [Description], [Session], [Color], [Style], [Material], [Status]) VALUES
(1, 1, N'Áo sơ mi nam công sở NEVA', 350000.00, '/uploads/products/somi_nam.jpg', N'Áo sơ mi nam chất liệu bền đẹp, tôn dáng lịch lãm.', 1, N'Trắng', N'Slimfit', N'Cotton', 1),
(2, 1, N'Áo thun nam polo basic', 250000.00, '/uploads/products/polo_nam.jpg', N'Áo thun polo thoáng khí, co giãn tốt.', 2, N'Xanh Navy', N'Regular', N'Cá sấu', 1),
(3, 2, N'Quần tây nam baggy ống suông', 420000.00, '/uploads/products/quantay_nam.jpg', N'Quần tây nam sang trọng, trẻ trung phù hợp đi học, đi làm.', 1, N'Đen', N'Baggy', N'Tuyết mưa', 1),
(4, 2, N'Quần short nam kaki dạo phố', 220000.00, '/uploads/products/short_kaki.jpg', N'Quần short kaki dày dặn, năng động.', 2, N'Be', N'Shorts', N'Kaki', 1),
(5, 3, N'Váy hoa nhí vintage tiểu thư', 380000.00, '/uploads/products/vay_hoanhi.jpg', N'Váy hoa nhí dịu dàng, thướt tha đón nắng hè.', 2, N'Vàng hoa', N'Vintage', N'Chiffon', 1),
(6, 3, N'Đầm dạ hội lụa satin cao cấp', 750000.00, '/uploads/products/dam_dahoi.jpg', N'Đầm dạ hội quý phái tôn vinh nét quyến rũ.', 3, N'Đỏ rượu', N'Dạ hội', N'Lụa satin', 1),
(7, 4, N'Áo cardigan len mỏng Hàn Quốc', 290000.00, '/uploads/products/cardigan_nu.jpg', N'Áo cardigan len nhẹ nhàng cho mùa thu se lạnh.', 3, N'Hồng phấn', N'Cardigan', N'Len tăm', 1),
(8, 4, N'Áo thun croptop ôm dáng', 150000.00, '/uploads/products/croptop_nu.jpg', N'Áo croptop cotton co giãn cá tính.', 2, N'Trắng', N'Croptop', N'Cotton co giãn', 1),
(9, 5, N'Bộ thể thao nam active năng động', 480000.00, '/uploads/products/bo_thethao_nam.jpg', N'Bộ đồ thể thao nam thoáng khí tốt cho vận động.', 2, N'Xám', N'Sporty', N'Polyester', 1),
(10, 5, N'Set tập gym yoga nữ co giãn', 390000.00, '/uploads/products/set_gym_nu.jpg', N'Đồ tập gym yoga ôm dáng co giãn tối đa.', 2, N'Đen', N'Athletic', N'Spandex', 1),
(11, 6, N'Thắt lưng da bò nam NEVA', 290000.00, '/uploads/products/thatlung_da.jpg', N'Thắt lưng da thật bền bỉ, phong cách cổ điển.', 1, N'Nâu', N'Classic', N'Da bò', 0),
(12, 1, N'Áo khoác gió nam cản mưa nhẹ', 450000.00, '/uploads/products/khoacgio_nam.jpg', N'Áo khoác gió 2 lớp giữ ấm, cản gió nước nhẹ.', 4, N'Xanh rêu', N'Jacket', N'Nylon', 0),
(13, 4, N'Áo len cổ lọ ấm áp đại hàn', 320000.00, '/uploads/products/aoloclo_nu.jpg', N'Áo len cổ lọ chất liệu len lông cừu cực ấm.', 4, N'Kem', N'Oversize', N'Len cừu', 0),
(14, 2, N'Quần jeans nam streetwear rách gối', 490000.00, '/uploads/products/jean_nam.jpg', N'Quần jean phong cách đường phố cá tính.', 3, N'Xanh sáng', N'Streetwear', N'Denim', 0),
(15, 3, N'Chân váy chữ A công sở dáng lửng', 270000.00, '/uploads/products/chanvay_a.jpg', N'Chân váy chữ A lịch sự dễ phối đồ công sở.', 1, N'Đen', N'Chữ A', N'Kaki hàn', 0);
SET IDENTITY_INSERT [Products] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 6: [ProductSizes] (Bảng Size: S, M, L cho 15 sản phẩm)
-- ==================================================================================
SET IDENTITY_INSERT [ProductSizes] ON;
INSERT INTO [ProductSizes] ([SizeID], [ProductID], [SizeName], [StockQuantity], [MinimumStock]) VALUES
-- Áo sơ mi nam công sở NEVA (ID 1)
(1, 1, 'S', 25, 5), (2, 1, 'M', 35, 5), (3, 1, 'L', 20, 5),
-- Áo thun nam polo basic (ID 2)
(4, 2, 'S', 40, 5), (5, 2, 'M', 50, 5), (6, 2, 'L', 30, 5),
-- Quần tây nam baggy ống suông (ID 3)
(7, 3, 'S', 15, 5), (8, 3, 'M', 25, 5), (9, 3, 'L', 18, 5),
-- Quần short nam kaki dạo phố (ID 4)
(10, 4, 'S', 30, 5), (11, 4, 'M', 45, 5), (12, 4, 'L', 35, 5),
-- Váy hoa nhí vintage tiểu thư (ID 5)
(13, 5, 'S', 20, 5), (14, 5, 'M', 28, 5), (15, 5, 'L', 15, 5),
-- Đầm dạ hội lụa satin cao cấp (ID 6)
(16, 6, 'S', 12, 5), (17, 6, 'M', 18, 5), (18, 6, 'L', 10, 5),
-- Áo cardigan len mỏng Hàn Quốc (ID 7)
(19, 7, 'S', 22, 5), (20, 7, 'M', 30, 5), (21, 7, 'L', 25, 5),
-- Áo thun croptop ôm dáng (ID 8)
(22, 8, 'S', 50, 5), (23, 8, 'M', 60, 5), (24, 8, 'L', 40, 5),
-- Bộ thể thao nam active năng động (ID 9)
(25, 9, 'S', 18, 5), (26, 9, 'M', 22, 5), (27, 9, 'L', 20, 5),
-- Set tập gym yoga nữ co giãn (ID 10)
(28, 10, 'S', 25, 5), (29, 10, 'M', 35, 5), (30, 10, 'L', 30, 5),
-- Thắt lưng da bò nam NEVA (ID 11)
(31, 11, 'S', 15, 5), (32, 11, 'M', 20, 5), (33, 11, 'L', 15, 5),
-- Áo khoác gió nam cản mưa nhẹ (ID 12)
(34, 12, 'S', 28, 5), (35, 12, 'M', 32, 5), (36, 12, 'L', 24, 5),
-- Áo len cổ lọ ấm áp đại hàn (ID 13)
(37, 13, 'S', 20, 5), (38, 13, 'M', 25, 5), (39, 13, 'L', 15, 5),
-- Quần jeans nam streetwear rách gối (ID 14)
(40, 14, 'S', 30, 5), (41, 14, 'M', 35, 5), (42, 14, 'L', 25, 5),
-- Chân váy chữ A công sở dáng lửng (ID 15)
(43, 15, 'S', 22, 5), (44, 15, 'M', 28, 5), (45, 15, 'L', 20, 5);
SET IDENTITY_INSERT [ProductSizes] OFF;
GO

-- CHÈN DỮ LIỆU BẢNG 7: [InventoryReceipts] (Phiếu nhập kho - 10 phiếu phân bổ)
SET IDENTITY_INSERT [InventoryReceipts] ON;
INSERT INTO [InventoryReceipts] ([ReceiptID], [SupplierID], [ImportDate], [Status], [CreatedBy]) VALUES
(1, 1, '2025-11-15 09:30:00', 1, 2),    -- Tạo bởi Trần Thị Thu
(2, 2, '2025-12-05 14:15:00', 1, 33),   -- Tạo bởi Nguyễn Minh Triết
(3, 3, '2025-12-20 10:00:00', 1, 34),   -- Tạo bởi Phạm Thảo Vy
(4, 4, '2026-01-10 11:45:00', 1, 35),   -- Tạo bởi Lê Anh Tuấn
(5, 1, '2026-02-05 08:30:00', 1, 2),
(6, 2, '2026-02-25 15:00:00', 1, 33),
(7, 3, '2026-03-12 13:20:00', 1, 34),
(8, 4, '2026-04-01 10:15:00', 1, 35),
(9, 1, '2026-04-20 09:00:00', 1, 2),
(10, 2, '2026-05-10 14:30:00', 0, 33);  -- Phiếu mới nhất đang ở trạng thái Chờ duyệt
SET IDENTITY_INSERT [InventoryReceipts] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 8: [InventoryReceiptDetails] (Chi tiết nhập kho)
-- Dữ liệu thực tế: Giá vốn bằng 45-55% Giá bán
-- ==================================================================================
SET IDENTITY_INSERT [InventoryReceiptDetails] ON;
INSERT INTO [InventoryReceiptDetails] ([DetailID], [ReceiptID], [SizeID], [Quantity], [ImportPrice]) VALUES
-- Phiếu 1 (NCC 1) Nhập Áo sơ mi & Thắt lưng
(1, 1, 1, 30, 180000.00),
(2, 1, 2, 40, 180000.00),
(3, 1, 3, 30, 180000.00),
(4, 1, 31, 20, 140000.00),
(5, 1, 32, 20, 140000.00),
-- Phiếu 2 (NCC 2) Nhập Áo polo & Quần short
(6, 2, 4, 50, 130000.00),
(7, 2, 5, 50, 130000.00),
(8, 2, 6, 40, 130000.00),
(9, 2, 10, 30, 110000.00),
(10, 2, 11, 30, 110000.00),
-- Phiếu 3 (NCC 3) Nhập Quần tây & Đồ thể thao nam
(11, 3, 7, 20, 220000.00),
(12, 3, 8, 30, 220000.00),
(13, 3, 9, 20, 220000.00),
(14, 3, 25, 20, 240000.00),
(15, 3, 26, 25, 240000.00),
-- Phiếu 4 (NCC 4) Nhập Váy hoa nhí & Đầm dạ hội lụa
(16, 4, 13, 25, 190000.00),
(17, 4, 14, 30, 190000.00),
(18, 4, 16, 15, 380000.00),
(19, 4, 17, 20, 380000.00),
-- Phiếu 5 (NCC 1) Nhập Áo cardigan & Áo khoác gió
(20, 5, 19, 25, 145000.00),
(21, 5, 20, 30, 145000.00),
(22, 5, 34, 30, 230000.00),
(23, 5, 35, 30, 230000.00),
-- Phiếu 6 (NCC 2) Nhập Áo croptop & Set tập gym
(24, 6, 22, 50, 75000.00),
(25, 6, 23, 60, 75000.00),
(26, 6, 28, 30, 195000.00),
(27, 6, 29, 40, 195000.00),
-- Phiếu 7 (NCC 3) Nhập Quần jean nam
(28, 7, 40, 30, 250000.00),
(29, 7, 41, 40, 250000.00),
-- Phiếu 8 (NCC 4) Nhập Chân váy chữ A
(30, 8, 43, 25, 135000.00),
(31, 8, 44, 30, 135000.00),
-- Phiếu 9 (NCC 1) Nhập Áo len cổ lọ
(32, 9, 37, 25, 160000.00),
(33, 9, 38, 30, 160000.00),
-- Phiếu 10 (NCC 2) Nhập thêm Áo polo & Áo croptop bổ sung
(34, 10, 5, 20, 130000.00),
(35, 10, 23, 30, 75000.00);
SET IDENTITY_INSERT [InventoryReceiptDetails] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 9: [UserAddresses] (Địa chỉ khách hàng - 35 địa chỉ)
-- Dữ liệu thực tế: Một số khách hàng có nhiều địa chỉ nhận hàng
-- ==================================================================================
SET IDENTITY_INSERT [UserAddresses] ON;
INSERT INTO [UserAddresses] ([AddressID], [UserID], [Province_City], [DetailedAddress], [FullName], [Phone], [IsDefault]) VALUES
(1, 3, N'Hà Nội', N'Số 12 ngõ 45 Cầu Giấy', N'Nguyễn Văn Nam', '0912345678', 1),
(2, 3, N'Hải Phòng', N'Kiốt số 3 chợ Đổ, Hồng Bàng', N'Nguyễn Văn Nam', '0912345678', 1), 
(3, 4, N'TP Hồ Chí Minh', N'180/45 Nguyễn Thị Minh Khai, Q.3', N'Trần Thị Hương', '0987654321', 1),
(4, 5, N'Hà Nội', N'P.402 Chung cư HH2 Linh Đàm, Hoàng Mai', N'Lê Hoàng Long', '0905123456', 1),
(5, 5, N'Đà Nẵng', N'88 Lê Duẩn, Hải Châu', N'Lê Hoàng Long', '0905123456', 1), 
(6, 6, N'Cần Thơ', N'45 Đường 3/2, Ninh Kiều', N'Phạm Minh Tuấn', '0945678901', 1),
(7, 7, N'Hà Nội', N'Số 5 ngách 82 Yên Hòa, Cầu Giấy', N'Vũ Thị Mai', '0934567890', 1),
(8, 8, N'Đồng Nai', N'12/3 Biên Hòa', N'Hoàng Anh Đức', '0978123456', 1),
(9, 9, N'Hải Dương', N'88 Trần Hưng Đạo', N'Đỗ Huy Khánh', '0919876543', 1),
(10, 10, N'Hà Nội', N'Số 10 ngõ 102 Chùa Láng, Đống Đa', N'Phan Thanh Hà', '0963112233', 1),
(11, 11, N'TP Hồ Chí Minh', N'90 Lê Lợi, Bến Nghé, Quận 1', N'Ngô Quốc Anh', '0909112233', 1),
(12, 12, N'Hà Nội', N'Số 8 Trấn Vũ, Ba Đình', N'Bùi Thị Lan', '0988223344', 1),
(13, 13, N'Bắc Ninh', N'22 Ngô Gia Tự', N'Nguyễn Đình Huy', '0977334455', 1),
(14, 14, N'Quảng Ninh', N'102 Kênh Liêm, Hạ Long', N'Đặng Minh Triết', '0911445566', 1),
(15, 14, N'Hà Nội', N'55 Phố Huế, Hai Bà Trưng', N'Đặng Minh Triết', '0911445566', 1), 
(16, 15, N'Thừa Thiên Huế', N'15 Hùng Vương', N'Dương Thúy Hằng', '0944556677', 1),
(17, 16, N'Khánh Hòa', N'40 Trần Phú, Nha Trang', N'Võ Duy Mạnh', '0933667788', 1),
(18, 17, N'Hà Nội', N'Số 6 ngõ 8 Chùa Bộc, Đống Đa', N'Đinh Văn Hùng', '0966778899', 1),
(19, 18, N'Đà Nẵng', N'120 Nguyễn Văn Linh, Thanh Khê', N'Lâm Mỹ Tâm', '0988778899', 1),
(20, 19, N'Thanh Hóa', N'88 Lê Lai', N'Lý Hải Nam', '0977889900', 1),
(21, 20, N'Hà Nội', N'Số 12 ngõ 20 Cát Linh, Đống Đa', N'Tạ Minh Quân', '0911889900', 1),
(22, 20, N'Vĩnh Phúc', N'22 Mê Linh, Vĩnh Yên', N'Tạ Minh Quân', '0911889900', 1), 
(23, 21, N'Nghệ An', N'45 Quang Trung, Vinh', N'Trịnh Phương Nam', '0944889900', 1),
(24, 22, N'Hà Nội', N'P.1205 Tòa nhà CT3 Nam Cường, Bắc Từ Liêm', N'Phùng Kiến Quốc', '0933990011', 1),
(25, 23, N'Bình Dương', N'80 Đại lộ Bình Dương, Thủ Dầu Một', N'Cao Thanh Thảo', '0966990011', 1),
(26, 24, N'Hà Nội', N'Số 17 ngõ 233 Xuân Thủy, Cầu Giấy', N'Mai Ngọc Anh', '0988990011', 1),
(27, 25, N'TP Hồ Chí Minh', N'450 Cách Mạng Tháng 8, Quận 3', N'Đào Quốc Bảo', '0977001122', 1),
(28, 26, N'Thái Nguyên', N'12 Lương Ngọc Quyến', N'Hà Thị Cúc', '0911001122', 1),
(29, 27, N'Hà Nội', N'Số 88 Trần Duy Hưng, Cầu Giấy', N'Lương Thế Thành', '0944001122', 1),
(30, 28, N'Lâm Đồng', N'15 Bùi Thị Xuân, Đà Lạt', N'Nghiêm Xuân Trường', '0933112244', 1),
(31, 28, N'Hà Nội', N'Số 10 Hàng Gai, Hoàn Kiếm', N'Nghiêm Xuân Trường', '0933112244', 1), 
(32, 29, N'Hà Nội', N'Số 5 ngõ 18 Nguyễn Khánh Toàn, Cầu Giấy', N'Quách Hoàng Diệu', '0966112244', 1),
(33, 30, N'Hà Nội', N'P.809 Chung cư Times City, Hai Bà Trưng', N'Giang Hồng Ngọc', '0988112244', 1),
(34, 31, N'Nam Định', N'88 Trần Hưng Đạo', N'Chu Văn Biên', '0977223355', 1),
(35, 32, N'Hà Nội', N'Số 2 ngõ 45 Tây Sơn, Đống Đa', N'Đỗ Thùy Trang', '0911223355', 1);
SET IDENTITY_INSERT [UserAddresses] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 10: [Orders] (Đơn hàng - ĐÚNG 100 ĐƠN HÀNG THỰC TẾ)
-- Dữ liệu phân bổ trải dài từ tháng 11/2025 đến 05/2026 để khớp với dữ liệu phân tích doanh thu 6 tháng
-- Trạng thái: 0 = Hủy, 1 = Chờ xử lý, 2 = Đang giao, 3 = Đã giao
-- ==================================================================================
SET IDENTITY_INSERT [Orders] ON;
INSERT INTO [Orders] ([OrderID], [UserID], [DiscountID], [OrderDate], [ShippingAddress], [ShippingProvince], [Country], [ReceiverName], [ReceiverPhone], [TotalAmount], [Status], [RedemptionPoints]) VALUES
-- Tháng 11/2025 (15 đơn)
(1, 3, NULL, '2025-11-02 10:30:00', N'Số 12 ngõ 45 Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Nguyễn Văn Nam', '0912345678', 350000.00, 3, 0),
(2, 4, 1, '2025-11-04 15:20:00', N'180/45 Nguyễn Thị Minh Khai, Q.3', N'TP Hồ Chí Minh', N'Việt Nam', N'Trần Thị Hương', '0987654321', 200000.00, 3, 0), -- Giảm 20% của 250k
(3, 5, NULL, '2025-11-06 09:15:00', N'88 Lê Duẩn, Hải Châu', N'Đà Nẵng', N'Việt Nam', N'Lê Hoàng Long', '0905123456', 420000.00, 3, 0),
(4, 6, NULL, '2025-11-08 14:45:00', N'45 Đường 3/2, Ninh Kiều', N'Cần Thơ', N'Việt Nam', N'Phạm Minh Tuấn', '0945678901', 220000.00, 3, 0),
(5, 7, NULL, '2025-11-10 11:30:00', N'Số 5 ngách 82 Yên Hòa, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Vũ Thị Mai', '0934567890', 380000.00, 3, 0),
(6, 8, 2, '2025-11-12 16:10:00', N'12/3 Biên Hòa', N'Đồng Nai', N'Việt Nam', N'Hoàng Anh Đức', '0978123456', 675000.00, 3, 0), -- 750k giảm 10%
(7, 9, NULL, '2025-11-14 08:35:00', N'88 Trần Hưng Đạo', N'Hải Dương', N'Việt Nam', N'Đỗ Huy Khánh', '0919876543', 290000.00, 3, 0),
(8, 10, NULL, '2025-11-16 13:50:00', N'Số 10 ngõ 102 Chùa Láng, Đống Đa', N'Hà Nội', N'Việt Nam', N'Phan Thanh Hà', '0963112233', 150000.00, 3, 0),
(9, 11, NULL, '2025-11-18 10:20:00', N'90 Lê Lợi, Bến Nghé, Quận 1', N'TP Hồ Chí Minh', N'Việt Nam', N'Ngô Quốc Anh', '0909112233', 480000.00, 3, 0),
(10, 12, 3, '2025-11-20 15:40:00', N'Số 8 Trấn Vũ, Ba Đình', N'Hà Nội', N'Việt Nam', N'Bùi Thị Lan', '0988223344', 340000.00, 3, 0), -- 390k giảm 50k
(11, 13, NULL, '2025-11-22 09:10:00', N'22 Ngô Gia Tự', N'Bắc Ninh', N'Việt Nam', N'Nguyễn Đình Huy', '0977334455', 290000.00, 3, 0),
(12, 14, NULL, '2025-11-24 14:15:00', N'102 Kênh Liêm, Hạ Long', N'Quảng Ninh', N'Việt Nam', N'Đặng Minh Triết', '0911445566', 450000.00, 3, 0),
(13, 15, NULL, '2025-11-26 11:25:00', N'15 Hùng Vương', N'Thừa Thiên Huế', N'Việt Nam', N'Dương Thúy Hằng', '0944556677', 320000.00, 3, 0),
(14, 16, NULL, '2025-11-28 16:30:00', N'40 Trần Phú, Nha Trang', N'Khánh Hòa', N'Việt Nam', N'Võ Duy Mạnh', '0933667788', 490000.00, 0, 0), -- Đơn hủy
(15, 17, NULL, '2025-11-30 08:45:00', N'Số 6 ngõ 8 Chùa Bộc, Đống Đa', N'Hà Nội', N'Việt Nam', N'Đinh Văn Hùng', '0966778899', 270000.00, 3, 0),

-- Tháng 12/2025 (15 đơn)
(16, 18, NULL, '2025-12-02 14:20:00', N'120 Nguyễn Văn Linh, Thanh Khê', N'Đà Nẵng', N'Việt Nam', N'Lâm Mỹ Tâm', '0988778899', 350000.00, 3, 0),
(17, 19, NULL, '2025-12-04 10:15:00', N'88 Lê Lai', N'Thanh Hóa', N'Việt Nam', N'Lý Hải Nam', '0977889900', 250000.00, 3, 0),
(18, 20, 4, '2025-12-06 15:40:00', N'Số 12 ngõ 20 Cát Linh, Đống Đa', N'Hà Nội', N'Việt Nam', N'Tạ Minh Quân', '0911889900', 320000.00, 3, 20), -- 420k giảm 100k
(19, 21, NULL, '2025-12-08 09:30:00', N'45 Quang Trung, Vinh', N'Nghệ An', N'Việt Nam', N'Trịnh Phương Nam', '0944889900', 220000.00, 3, 0),
(20, 22, NULL, '2025-12-10 13:55:00', N'P.1205 Tòa nhà CT3 Nam Cường, Bắc Từ Liêm', N'Hà Nội', N'Việt Nam', N'Phùng Kiến Quốc', '0933990011', 380000.00, 3, 0),
(21, 23, NULL, '2025-12-12 11:20:00', N'80 Đại lộ Bình Dương, Thủ Dầu Một', N'Bình Dương', N'Việt Nam', N'Cao Thanh Thảo', '0966990011', 750000.00, 3, 0),
(22, 24, NULL, '2025-12-14 16:45:00', N'Số 17 ngõ 233 Xuân Thủy, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Mai Ngọc Anh', '0988990011', 290000.00, 3, 0),
(23, 25, NULL, '2025-12-16 08:30:00', N'450 Cách Mạng Tháng 8, Quận 3', N'TP Hồ Chí Minh', N'Việt Nam', N'Đào Quốc Bảo', '0977001122', 150000.00, 3, 0),
(24, 26, NULL, '2025-12-18 14:10:00', N'12 Lương Ngọc Quyến', N'Thái Nguyên', N'Việt Nam', N'Hà Thị Cúc', '0911001122', 480000.00, 3, 0),
(25, 27, NULL, '2025-12-20 10:25:00', N'Số 88 Trần Duy Hưng, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Lương Thế Thành', '0944001122', 390000.00, 3, 0),
(26, 28, 5, '2025-12-22 15:50:00', N'Số 10 Hàng Gai, Hoàn Kiếm', N'Hà Nội', N'Việt Nam', N'Nghiêm Xuân Trường', '0933112244', 246500.00, 3, 0), -- 290k giảm 15%
(27, 29, NULL, '2025-12-24 09:05:00', N'Số 5 ngõ 18 Nguyễn Khánh Toàn, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Quách Hoàng Diệu', '0966112244', 450000.00, 3, 0),
(28, 30, NULL, '2025-12-26 13:40:00', N'P.809 Chung cư Times City, Hai Bà Trưng', N'Hà Nội', N'Việt Nam', N'Giang Hồng Ngọc', '0988112244', 320000.00, 3, 0),
(29, 31, NULL, '2025-12-28 11:15:00', N'88 Trần Hưng Đạo', N'Nam Định', N'Việt Nam', N'Chu Văn Biên', '0977223355', 490000.00, 3, 0),
(30, 32, NULL, '2025-12-30 16:35:00', N'Số 2 ngõ 45 Tây Sơn, Đống Đa', N'Hà Nội', N'Việt Nam', N'Đỗ Thùy Trang', '0911223355', 270000.00, 3, 0),

-- Tháng 01/2026 (20 đơn)
(31, 3, NULL, '2026-01-02 08:45:00', N'Số 12 ngõ 45 Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Nguyễn Văn Nam', '0912345678', 350000.00, 3, 0),
(32, 4, NULL, '2026-01-03 14:10:00', N'180/45 Nguyễn Thị Minh Khai, Q.3', N'TP Hồ Chí Minh', N'Việt Nam', N'Trần Thị Hương', '0987654321', 250000.00, 3, 0),
(33, 5, NULL, '2026-01-04 10:20:00', N'88 Lê Duẩn, Hải Châu', N'Đà Nẵng', N'Việt Nam', N'Lê Hoàng Long', '0905123456', 420000.00, 3, 0),
(34, 6, NULL, '2026-01-06 15:35:00', N'45 Đường 3/2, Ninh Kiều', N'Cần Thơ', N'Việt Nam', N'Phạm Minh Tuấn', '0945678901', 220000.00, 3, 0),
(35, 7, NULL, '2026-01-08 09:10:00', N'Số 5 ngách 82 Yên Hòa, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Vũ Thị Mai', '0934567890', 380000.00, 3, 0),
(36, 8, NULL, '2026-01-10 14:50:00', N'12/3 Biên Hòa', N'Đồng Nai', N'Việt Nam', N'Hoàng Anh Đức', '0978123456', 750000.00, 3, 0),
(37, 9, NULL, '2026-01-11 11:25:00', N'88 Trần Hưng Đạo', N'Hải Dương', N'Việt Nam', N'Đỗ Huy Khánh', '0919876543', 290000.00, 3, 0),
(38, 10, NULL, '2026-01-13 16:40:00', N'Số 10 ngõ 102 Chùa Láng, Đống Đa', N'Hà Nội', N'Việt Nam', N'Phan Thanh Hà', '0963112233', 150000.00, 3, 0),
(39, 11, NULL, '2026-01-15 08:30:00', N'90 Lê Lợi, Bến Nghé, Quận 1', N'TP Hồ Chí Minh', N'Việt Nam', N'Ngô Quốc Anh', '0909112233', 480000.00, 3, 0),
(40, 12, NULL, '2026-01-17 14:15:00', N'Số 8 Trấn Vũ, Ba Đình', N'Hà Nội', N'Việt Nam', N'Bùi Thị Lan', '0988223344', 390000.00, 3, 0),
(41, 13, NULL, '2026-01-18 10:05:00', N'22 Ngô Gia Tự', N'Bắc Ninh', N'Việt Nam', N'Nguyễn Đình Huy', '0977334455', 290000.00, 3, 0),
(42, 14, NULL, '2026-01-20 15:30:00', N'102 Kênh Liêm, Hạ Long', N'Quảng Ninh', N'Việt Nam', N'Đặng Minh Triết', '0911445566', 450000.00, 3, 0),
(43, 15, NULL, '2026-01-22 09:20:00', N'15 Hùng Vương', N'Thừa Thiên Huế', N'Việt Nam', N'Dương Thúy Hằng', '0944556677', 320000.00, 3, 0),
(44, 16, NULL, '2026-01-24 13:55:00', N'40 Trần Phú, Nha Trang', N'Khánh Hòa', N'Việt Nam', N'Võ Duy Mạnh', '0933667788', 490000.00, 3, 0),
(45, 17, NULL, '2026-01-25 11:10:00', N'Số 6 ngõ 8 Chùa Bộc, Đống Đa', N'Hà Nội', N'Việt Nam', N'Đinh Văn Hùng', '0966778899', 270000.00, 3, 0),
(46, 18, NULL, '2026-01-27 16:45:00', N'120 Nguyễn Văn Linh, Thanh Khê', N'Đà Nẵng', N'Việt Nam', N'Lâm Mỹ Tâm', '0988778899', 350000.00, 3, 0),
(47, 19, NULL, '2026-01-28 08:35:00', N'88 Lê Lai', N'Thanh Hóa', N'Việt Nam', N'Lý Hải Nam', '0977889900', 250000.00, 3, 0),
(48, 20, NULL, '2026-01-29 14:05:00', N'Số 12 ngõ 20 Cát Linh, Đống Đa', N'Hà Nội', N'Việt Nam', N'Tạ Minh Quân', '0911889900', 420000.00, 3, 0),
(49, 21, NULL, '2026-01-30 10:20:00', N'45 Quang Trung, Vinh', N'Nghệ An', N'Việt Nam', N'Trịnh Phương Nam', '0944889900', 220000.00, 0, 0), -- Đơn hủy
(50, 22, NULL, '2026-01-31 15:50:00', N'P.1205 Tòa nhà CT3 Nam Cường, Bắc Từ Liêm', N'Hà Nội', N'Việt Nam', N'Phùng Kiến Quốc', '0933990011', 380000.00, 3, 0),

-- Tháng 02/2026 (20 đơn)
(51, 23, NULL, '2026-02-01 09:15:00', N'80 Đại lộ Bình Dương, Thủ Dầu Một', N'Bình Dương', N'Việt Nam', N'Cao Thanh Thảo', '0966990011', 750000.00, 3, 0),
(52, 24, NULL, '2026-02-02 13:40:00', N'Số 17 ngõ 233 Xuân Thủy, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Mai Ngọc Anh', '0988990011', 290000.00, 3, 0),
(53, 25, NULL, '2026-02-03 11:05:00', N'450 Cách Mạng Tháng 8, Quận 3', N'TP Hồ Chí Minh', N'Việt Nam', N'Đào Quốc Bảo', '0977001122', 150000.00, 3, 0),
(54, 26, NULL, '2026-02-05 16:30:00', N'12 Lương Ngọc Quyến', N'Thái Nguyên', N'Việt Nam', N'Hà Thị Cúc', '0911001122', 480000.00, 3, 0),
(55, 27, NULL, '2026-02-06 08:25:00', N'Số 88 Trần Duy Hưng, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Lương Thế Thành', '0944001122', 390000.00, 3, 0),
(56, 28, NULL, '2026-02-08 14:10:00', N'Số 10 Hàng Gai, Hoàn Kiếm', N'Hà Nội', N'Việt Nam', N'Nghiêm Xuân Trường', '0933112244', 290000.00, 3, 0),
(57, 29, NULL, '2026-02-09 10:20:00', N'Số 5 ngõ 18 Nguyễn Khánh Toàn, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Quách Hoàng Diệu', '0966112244', 450000.00, 3, 0),
(58, 30, NULL, '2026-02-10 15:45:00', N'P.809 Chung cư Times City, Hai Bà Trưng', N'Hà Nội', N'Việt Nam', N'Giang Hồng Ngọc', '0988112244', 320000.00, 3, 0),
(59, 31, NULL, '2026-02-12 09:10:00', N'88 Trần Hưng Đạo', N'Nam Định', N'Việt Nam', N'Chu Văn Biên', '0977223355', 490000.00, 3, 0),
(60, 32, NULL, '2026-02-13 13:50:00', N'Số 2 ngõ 45 Tây Sơn, Đống Đa', N'Hà Nội', N'Việt Nam', N'Đỗ Thùy Trang', '0911223355', 270000.00, 3, 0),
(61, 3, NULL, '2026-02-15 11:25:00', N'Số 12 ngõ 45 Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Nguyễn Văn Nam', '0912345678', 600000.00, 3, 0), -- Combo 1 sơ mi + 1 polo
(62, 4, NULL, '2026-02-16 16:40:00', N'180/45 Nguyễn Thị Minh Khai, Q.3', N'TP Hồ Chí Minh', N'Việt Nam', N'Trần Thị Hương', '0987654321', 250000.00, 3, 0),
(63, 5, NULL, '2026-02-18 08:30:00', N'88 Lê Duẩn, Hải Châu', N'Đà Nẵng', N'Việt Nam', N'Lê Hoàng Long', '0905123456', 420000.00, 3, 0),
(64, 6, NULL, '2026-02-20 14:15:00', N'45 Đường 3/2, Ninh Kiều', N'Cần Thơ', N'Việt Nam', N'Phạm Minh Tuấn', '0945678901', 220000.00, 3, 0),
(65, 7, NULL, '2026-02-22 10:05:00', N'Số 5 ngách 82 Yên Hòa, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Vũ Thị Mai', '0934567890', 380000.00, 3, 0),
(66, 8, NULL, '2026-02-24 15:30:00', N'12/3 Biên Hòa', N'Đồng Nai', N'Việt Nam', N'Hoàng Anh Đức', '0978123456', 750000.00, 3, 0),
(67, 9, NULL, '2026-02-25 09:20:00', N'88 Trần Hưng Đạo', N'Hải Dương', N'Việt Nam', N'Đỗ Huy Khánh', '0919876543', 290000.00, 3, 0),
(68, 10, NULL, '2026-02-26 13:55:00', N'Số 10 ngõ 102 Chùa Láng, Đống Đa', N'Hà Nội', N'Việt Nam', N'Phan Thanh Hà', '0963112233', 150000.00, 3, 0),
(69, 11, NULL, '2026-02-27 11:10:00', N'90 Lê Lợi, Bến Nghé, Quận 1', N'TP Hồ Chí Minh', N'Việt Nam', N'Ngô Quốc Anh', '0909112233', 480000.00, 3, 0),
(70, 12, NULL, '2026-02-28 16:45:00', N'Số 8 Trấn Vũ, Ba Đình', N'Hà Nội', N'Việt Nam', N'Bùi Thị Lan', '0988223344', 390000.00, 3, 0),

-- Tháng 03/2026 (15 đơn)
(71, 13, NULL, '2026-03-02 08:35:00', N'22 Ngô Gia Tự', N'Bắc Ninh', N'Việt Nam', N'Nguyễn Đình Huy', '0977334455', 290000.00, 3, 0),
(72, 14, NULL, '2026-03-04 14:05:00', N'102 Kênh Liêm, Hạ Long', N'Quảng Ninh', N'Việt Nam', N'Đặng Minh Triết', '0911445566', 450000.00, 3, 0),
(73, 15, NULL, '2026-03-06 10:20:00', N'15 Hùng Vương', N'Thừa Thiên Huế', N'Việt Nam', N'Dương Thúy Hằng', '0944556677', 320000.00, 3, 0),
(74, 16, NULL, '2026-03-08 15:50:00', N'40 Trần Phú, Nha Trang', N'Khánh Hòa', N'Việt Nam', N'Võ Duy Mạnh', '0933667788', 490000.00, 3, 0),
(75, 17, NULL, '2026-03-10 09:15:00', N'Số 6 ngõ 8 Chùa Bộc, Đống Đa', N'Hà Nội', N'Việt Nam', N'Đinh Văn Hùng', '0966778899', 270000.00, 3, 0),
(76, 18, NULL, '2026-03-12 13:40:00', N'120 Nguyễn Văn Linh, Thanh Khê', N'Đà Nẵng', N'Việt Nam', N'Lâm Mỹ Tâm', '0988778899', 350000.00, 3, 0),
(77, 19, NULL, '2026-03-14 11:05:00', N'88 Lê Lai', N'Thanh Hóa', N'Việt Nam', N'Lý Hải Nam', '0977889900', 250000.00, 3, 0),
(78, 20, NULL, '2026-03-16 16:30:00', N'Số 12 ngõ 20 Cát Linh, Đống Đa', N'Hà Nội', N'Việt Nam', N'Tạ Minh Quân', '0911889900', 420000.00, 3, 0),
(79, 21, NULL, '2026-03-18 08:25:00', N'45 Quang Trung, Vinh', N'Nghệ An', N'Việt Nam', N'Trịnh Phương Nam', '0944889900', 220000.00, 3, 0),
(80, 22, NULL, '2026-03-20 14:10:00', N'P.1205 Tòa nhà CT3 Nam Cường, Bắc Từ Liêm', N'Hà Nội', N'Việt Nam', N'Phùng Kiến Quốc', '0933990011', 380000.00, 3, 0),
(81, 23, NULL, '2026-03-22 10:20:00', N'80 Đại lộ Bình Dương, Thủ Dầu Một', N'Bình Dương', N'Việt Nam', N'Cao Thanh Thảo', '0966990011', 750000.00, 3, 0),
(82, 24, NULL, '2026-03-24 15:45:00', N'Số 17 ngõ 233 Xuân Thủy, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Mai Ngọc Anh', '0988990011', 290000.00, 3, 0),
(83, 25, NULL, '2026-03-26 09:10:00', N'450 Cách Mạng Tháng 8, Quận 3', N'TP Hồ Chí Minh', N'Việt Nam', N'Đào Quốc Bảo', '0977001122', 150000.00, 3, 0),
(84, 26, NULL, '2026-03-28 13:50:00', N'12 Lương Ngọc Quyến', N'Thái Nguyên', N'Việt Nam', N'Hà Thị Cúc', '0911001122', 480000.00, 3, 0),
(85, 27, NULL, '2026-03-30 11:25:00', N'Số 88 Trần Duy Hưng, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Lương Thế Thành', '0944001122', 390000.00, 3, 0),

-- Tháng 04/2026 (10 đơn)
(86, 28, NULL, '2026-04-02 16:40:00', N'Số 10 Hàng Gai, Hoàn Kiếm', N'Hà Nội', N'Việt Nam', N'Nghiêm Xuân Trường', '0933112244', 290000.00, 3, 0),
(87, 29, NULL, '2026-04-05 08:30:00', N'Số 5 ngõ 18 Nguyễn Khánh Toàn, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Quách Hoàng Diệu', '0966112244', 450000.00, 3, 0),
(88, 30, NULL, '2026-04-08 14:15:00', N'P.809 Chung cư Times City, Hai Bà Trưng', N'Hà Nội', N'Việt Nam', N'Giang Hồng Ngọc', '0988112244', 320000.00, 3, 0),
(89, 31, NULL, '2026-04-10 10:05:00', N'88 Trần Hưng Đạo', N'Nam Định', N'Việt Nam', N'Chu Văn Biên', '0977223355', 490000.00, 3, 0),
(90, 32, NULL, '2026-04-12 15:30:00', N'Số 2 ngõ 45 Tây Sơn, Đống Đa', N'Hà Nội', N'Việt Nam', N'Đỗ Thùy Trang', '0911223355', 270000.00, 3, 0),
(91, 3, NULL, '2026-04-15 09:20:00', N'Số 12 ngõ 45 Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Nguyễn Văn Nam', '0912345678', 350000.00, 3, 0),
(92, 4, NULL, '2026-04-18 13:55:00', N'180/45 Nguyễn Thị Minh Khai, Q.3', N'TP Hồ Chí Minh', N'Việt Nam', N'Trần Thị Hương', '0987654321', 250000.00, 3, 0),
(93, 5, NULL, '2026-04-20 11:10:00', N'88 Lê Duẩn, Hải Châu', N'Đà Nẵng', N'Việt Nam', N'Lê Hoàng Long', '0905123456', 420000.00, 3, 0),
(94, 6, NULL, '2026-04-22 16:45:00', N'45 Đường 3/2, Ninh Kiều', N'Cần Thơ', N'Việt Nam', N'Phạm Minh Tuấn', '0945678901', 220000.00, 3, 0),
(95, 7, NULL, '2026-04-25 08:35:00', N'Số 5 ngách 82 Yên Hòa, Cầu Giấy', N'Hà Nội', N'Việt Nam', N'Vũ Thị Mai', '0934567890', 380000.00, 3, 0),

-- Tháng 05/2026 (5 đơn gần đây - phục vụ hiển thị đơn mới)
(96, 8, NULL, '2026-05-02 14:05:00', N'12/3 Biên Hòa', N'Đồng Nai', N'Việt Nam', N'Hoàng Anh Đức', '0978123456', 750000.00, 3, 0),
(97, 9, NULL, '2026-05-05 10:20:00', N'88 Trần Hưng Đạo', N'Hải Dương', N'Việt Nam', N'Đỗ Huy Khánh', '0919876543', 290000.00, 2, 0), -- Đang giao
(98, 10, NULL, '2026-05-10 15:50:00', N'Số 10 ngõ 102 Chùa Láng, Đống Đa', N'Hà Nội', N'Việt Nam', N'Phan Thanh Hà', '0963112233', 150000.00, 2, 0), -- Đang giao
(99, 11, NULL, '2026-05-15 09:15:00', N'90 Lê Lợi, Bến Nghé, Quận 1', N'TP Hồ Chí Minh', N'Việt Nam', N'Ngô Quốc Anh', '0909112233', 480000.00, 1, 0), -- Chờ xử lý
(100, 12, NULL, '2026-05-18 13:40:00', N'Số 8 Trấn Vũ, Ba Đình', N'Hà Nội', N'Việt Nam', N'Bùi Thị Lan', '0988223344', 390000.00, 1, 0); -- Chờ xử lý
SET IDENTITY_INSERT [Orders] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 11: [OrderDetails] (SizeID = size M của sản phẩm: (ProductID-1)*3+2)
-- ==================================================================================
SET IDENTITY_INSERT [OrderDetails] ON;
INSERT INTO [OrderDetails] ([OrderDetailID], [OrderID], [SizeID], [Quantity], [UnitPrice]) VALUES
-- Orders 1 - 20 (Tháng 11 & 12/2025: Mỗi đơn 2 sản phẩm = 40 dòng)
(1, 1, 2, 1, 350000.00), (2, 1, 5, 1, 250000.00),
(3, 2, 5, 1, 250000.00), (4, 2, 14, 1, 380000.00),
(5, 3, 8, 1, 420000.00), (6, 3, 11, 1, 220000.00),
(7, 4, 11, 1, 220000.00), (8, 4, 23, 1, 150000.00),
(9, 5, 14, 1, 380000.00), (10, 5, 20, 1, 290000.00),
(11, 6, 17, 1, 750000.00), (12, 6, 32, 1, 290000.00),
(13, 7, 20, 1, 290000.00), (14, 7, 44, 1, 270000.00),
(15, 8, 23, 2, 150000.00), (16, 8, 38, 1, 320000.00),
(17, 9, 26, 1, 480000.00), (18, 9, 29, 1, 390000.00),
(19, 10, 29, 1, 390000.00), (20, 10, 2, 1, 350000.00),
(21, 11, 32, 1, 290000.00), (22, 11, 5, 1, 250000.00),
(23, 12, 35, 1, 450000.00), (24, 12, 8, 1, 420000.00),
(25, 13, 38, 1, 320000.00), (26, 13, 11, 1, 220000.00),
(27, 14, 41, 1, 490000.00), (28, 14, 14, 1, 380000.00),
(29, 15, 44, 1, 270000.00), (30, 15, 17, 1, 750000.00),
(31, 16, 2, 1, 350000.00), (32, 16, 23, 1, 150000.00),
(33, 17, 5, 1, 250000.00), (34, 17, 26, 1, 480000.00),
(35, 18, 8, 1, 420000.00), (36, 18, 29, 1, 390000.00),
(37, 19, 11, 1, 220000.00), (38, 19, 32, 1, 290000.00),
(39, 20, 14, 1, 380000.00), (40, 20, 35, 1, 450000.00),

-- Orders 21 - 40 (Tháng 12/2025 & 01/2026: Mỗi đơn 2 sản phẩm = 40 dòng)
(41, 21, 17, 1, 750000.00), (42, 21, 38, 1, 320000.00),
(43, 22, 20, 1, 290000.00), (44, 22, 41, 1, 490000.00),
(45, 23, 23, 1, 150000.00), (46, 23, 44, 1, 270000.00),
(47, 24, 26, 1, 480000.00), (48, 24, 2, 1, 350000.00),
(49, 25, 29, 1, 390000.00), (50, 25, 5, 1, 250000.00),
(51, 26, 32, 1, 290000.00), (52, 26, 8, 1, 420000.00),
(53, 27, 35, 1, 450000.00), (54, 27, 11, 1, 220000.00),
(55, 28, 38, 1, 320000.00), (56, 28, 14, 1, 380000.00),
(57, 29, 41, 1, 490000.00), (58, 29, 17, 1, 750000.00),
(59, 30, 44, 1, 270000.00), (60, 30, 20, 1, 290000.00),
(61, 31, 2, 1, 350000.00), (62, 31, 23, 1, 150000.00),
(63, 32, 5, 1, 250000.00), (64, 32, 26, 1, 480000.00),
(65, 33, 8, 1, 420000.00), (66, 33, 29, 1, 390000.00),
(67, 34, 11, 1, 220000.00), (68, 34, 32, 1, 290000.00),
(69, 35, 14, 1, 380000.00), (70, 35, 35, 1, 450000.00),
(71, 36, 17, 1, 750000.00), (72, 36, 38, 1, 320000.00),
(73, 37, 20, 1, 290000.00), (74, 37, 41, 1, 490000.00),
(75, 38, 23, 1, 150000.00), (76, 38, 44, 1, 270000.00),
(77, 39, 26, 1, 480000.00), (78, 39, 2, 1, 350000.00),
(79, 40, 29, 1, 390000.00), (80, 40, 5, 1, 250000.00),

-- Orders 41 - 60 (Tháng 01 & 02/2026: Mỗi đơn 3 sản phẩm = 60 dòng)
(81, 41, 32, 1, 290000.00), (82, 41, 8, 1, 420000.00), (83, 41, 2, 1, 350000.00),
(84, 42, 35, 1, 450000.00), (85, 42, 11, 1, 220000.00), (86, 42, 5, 1, 250000.00),
(87, 43, 38, 1, 320000.00), (88, 43, 14, 1, 380000.00), (89, 43, 8, 1, 420000.00),
(90, 44, 41, 1, 490000.00), (91, 44, 17, 1, 750000.00), (92, 44, 11, 1, 220000.00),
(93, 45, 44, 1, 270000.00), (94, 45, 20, 1, 290000.00), (95, 45, 14, 1, 380000.00),
(96, 46, 2, 1, 350000.00), (97, 46, 23, 1, 150000.00), (98, 46, 17, 1, 750000.00),
(99, 47, 5, 1, 250000.00), (100, 47, 26, 1, 480000.00), (101, 47, 20, 1, 290000.00),
(102, 48, 8, 1, 420000.00), (103, 48, 29, 1, 390000.00), (104, 48, 23, 1, 150000.00),
(105, 49, 11, 1, 220000.00), (106, 49, 32, 1, 290000.00), (107, 49, 26, 1, 480000.00),
(108, 50, 14, 1, 380000.00), (109, 50, 35, 1, 450000.00), (110, 50, 29, 1, 390000.00),
(111, 51, 17, 1, 750000.00), (112, 51, 38, 1, 320000.00), (113, 51, 32, 1, 290000.00),
(114, 52, 20, 1, 290000.00), (115, 52, 41, 1, 490000.00), (116, 52, 35, 1, 450000.00),
(117, 53, 23, 1, 150000.00), (118, 53, 44, 1, 270000.00), (119, 53, 38, 1, 320000.00),
(120, 54, 26, 1, 480000.00), (121, 54, 2, 1, 350000.00), (122, 54, 41, 1, 490000.00),
(123, 55, 29, 1, 390000.00), (124, 55, 5, 1, 250000.00), (125, 55, 44, 1, 270000.00),
(126, 56, 32, 1, 290000.00), (127, 56, 8, 1, 420000.00), (128, 56, 2, 1, 350000.00),
(129, 57, 35, 1, 450000.00), (130, 57, 11, 1, 220000.00), (131, 57, 5, 1, 250000.00),
(132, 58, 38, 1, 320000.00), (133, 58, 14, 1, 380000.00), (134, 58, 8, 1, 420000.00),
(135, 59, 41, 1, 490000.00), (136, 59, 17, 1, 750000.00), (137, 59, 11, 1, 220000.00),
(138, 60, 44, 1, 270000.00), (139, 60, 20, 1, 290000.00), (140, 60, 14, 1, 380000.00),

-- Orders 61 - 80 (Tháng 02 & 03/2026: Mỗi đơn 2 sản phẩm = 40 dòng)
(141, 61, 2, 1, 350000.00), (142, 61, 5, 1, 250000.00),
(143, 62, 5, 1, 250000.00), (144, 62, 23, 1, 150000.00),
(145, 63, 8, 1, 420000.00), (146, 63, 26, 1, 480000.00),
(147, 64, 11, 1, 220000.00), (148, 64, 29, 1, 390000.00),
(149, 65, 14, 1, 380000.00), (150, 65, 32, 1, 290000.00),
(151, 66, 17, 1, 750000.00), (152, 66, 35, 1, 450000.00),
(153, 67, 20, 1, 290000.00), (154, 67, 38, 1, 320000.00),
(155, 68, 23, 1, 150000.00), (156, 68, 41, 1, 490000.00),
(157, 69, 26, 1, 480000.00), (158, 69, 44, 1, 270000.00),
(159, 70, 29, 1, 390000.00), (160, 70, 2, 1, 350000.00),
(161, 71, 32, 1, 290000.00), (162, 71, 5, 1, 250000.00),
(163, 72, 35, 1, 450000.00), (164, 72, 8, 1, 420000.00),
(165, 73, 38, 1, 320000.00), (166, 73, 11, 1, 220000.00),
(167, 74, 41, 1, 490000.00), (168, 74, 14, 1, 380000.00),
(169, 75, 44, 1, 270000.00), (170, 75, 17, 1, 750000.00),
(171, 76, 2, 1, 350000.00), (172, 76, 20, 1, 290000.00),
(173, 77, 5, 1, 250000.00), (174, 77, 23, 1, 150000.00),
(175, 78, 8, 1, 420000.00), (176, 78, 26, 1, 480000.00),
(177, 79, 11, 1, 220000.00), (178, 79, 29, 1, 390000.00),
(179, 80, 14, 1, 380000.00), (180, 80, 32, 1, 290000.00),

-- Orders 81 - 100 (Tháng 03, 04 & 05/2026: Mỗi đơn 2 sản phẩm = 40 dòng)
(181, 81, 17, 1, 750000.00), (182, 81, 35, 1, 450000.00),
(183, 82, 20, 1, 290000.00), (184, 82, 38, 1, 320000.00),
(185, 83, 23, 1, 150000.00), (186, 83, 41, 1, 490000.00),
(187, 84, 26, 1, 480000.00), (188, 84, 44, 1, 270000.00),
(189, 85, 29, 1, 390000.00), (190, 85, 2, 1, 350000.00),
(191, 86, 32, 1, 290000.00), (192, 86, 5, 1, 250000.00),
(193, 87, 35, 1, 450000.00), (194, 87, 8, 1, 420000.00),
(195, 88, 38, 1, 320000.00), (196, 88, 11, 1, 220000.00),
(197, 89, 41, 1, 490000.00), (198, 89, 14, 1, 380000.00),
(199, 90, 44, 1, 270000.00), (200, 90, 17, 1, 750000.00),
(201, 91, 2, 1, 350000.00), (202, 91, 20, 1, 290000.00),
(203, 92, 5, 1, 250000.00), (204, 92, 23, 1, 150000.00),
(205, 93, 8, 1, 420000.00), (206, 93, 26, 1, 480000.00),
(207, 94, 11, 1, 220000.00), (208, 94, 29, 1, 390000.00),
(209, 95, 14, 1, 380000.00), (210, 95, 32, 1, 290000.00),
(211, 96, 17, 1, 750000.00), (212, 96, 35, 1, 450000.00),
(213, 97, 20, 1, 290000.00), (214, 97, 38, 1, 320000.00),
(215, 98, 23, 1, 150000.00), (216, 98, 41, 1, 490000.00),
(217, 99, 26, 1, 480000.00), (218, 99, 44, 1, 270000.00),
(219, 100, 29, 1, 390000.00), (220, 100, 2, 1, 350000.00);
SET IDENTITY_INSERT [OrderDetails] OFF;
GO

-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 12: [CartItems] (Giỏ hàng — SizeID + UnitPrice)
-- ==================================================================================
SET IDENTITY_INSERT [CartItems] ON;
INSERT INTO [CartItems] ([CartID], [UserID], [SizeID], [Quantity], [UnitPrice], [AdID]) VALUES
(1, 3, 2, 1, 350000.00, NULL),
(2, 3, 14, 1, 380000.00, NULL),
(3, 4, 5, 2, 250000.00, NULL),
(4, 5, 23, 1, 150000.00, NULL),
(5, 7, 38, 1, 320000.00, NULL),
(6, 10, 29, 1, 390000.00, NULL),
(7, 12, 44, 1, 270000.00, NULL),
(8, 14, 8, 1, 420000.00, NULL),
(9, 15, 17, 1, 750000.00, NULL),
(10, 18, 35, 1, 450000.00, NULL),
(11, 20, 5, 1, 250000.00, NULL),
(12, 22, 26, 1, 480000.00, NULL),
(13, 25, 20, 2, 290000.00, NULL),
(14, 28, 11, 1, 220000.00, NULL),
(15, 30, 41, 1, 490000.00, NULL);
SET IDENTITY_INSERT [CartItems] OFF;
GO

-- ==================================================================================
-- TỰ ĐỘNG CẬP NHẬT LẠI TOTALAMOUNT CHO CÁC ĐƠN HÀNG [Orders] DỰA TRÊN [OrderDetails] THỰC TẾ
-- ==================================================================================
UPDATE o
SET o.TotalAmount = ROUND(
    ISNULL((SELECT SUM(od.Quantity * od.UnitPrice) FROM [OrderDetails] od WHERE od.OrderID = o.OrderID), 0) 
    - CASE 
        WHEN o.DiscountID = 1 THEN ISNULL((SELECT SUM(od.Quantity * od.UnitPrice) FROM [OrderDetails] od WHERE od.OrderID = o.OrderID), 0) * 0.20
        WHEN o.DiscountID = 2 THEN ISNULL((SELECT SUM(od.Quantity * od.UnitPrice) FROM [OrderDetails] od WHERE od.OrderID = o.OrderID), 0) * 0.10
        WHEN o.DiscountID = 3 THEN 50000.00
        WHEN o.DiscountID = 4 THEN 100000.00
        WHEN o.DiscountID = 5 THEN ISNULL((SELECT SUM(od.Quantity * od.UnitPrice) FROM [OrderDetails] od WHERE od.OrderID = o.OrderID), 0) * 0.15
        ELSE 0.00
    END, 2)
FROM [Orders] o;
GO

-- IN THÔNG BÁO HOÀN THÀNH
-- ==================================================================================
-- CHÈN DỮ LIỆU BẢNG 13: [Advertisements] (Quảng cáo)
-- ==================================================================================
SET IDENTITY_INSERT [Advertisements] ON;
INSERT INTO [Advertisements] ([AdID], [Title], [ImageUrl], [Position], [IsActive], [CreatedDate], [ProductID], [DiscountType], [DiscountValue], [StartDate], [EndDate]) VALUES
(1, N'Sale Hè Rực Rỡ - Giảm 20%', '/images/ads/summer-sale.jpg', 'popup', 1, '2026-05-01 00:00:00', 1, 1, 20.00, '2026-05-01 00:00:00', '2026-12-31 23:59:59'),
(2, N'Bộ Sưu Tập Công Sở - Giảm 15%', '/images/ads/office-collection.jpg', 'sidebar', 1, '2026-05-10 00:00:00', 3, 1, 15.00, '2026-05-10 00:00:00', '2026-12-31 23:59:59'),
(3, N'Đầm Dạ Hội Cao Cấp - Giảm 10%', '/images/ads/evening-dress.jpg', 'popup', 1, '2026-05-15 00:00:00', 6, 1, 10.00, '2026-05-15 00:00:00', '2026-12-31 23:59:59');
SET IDENTITY_INSERT [Advertisements] OFF;
GO

PRINT N'ĐÃ CHÈN THÀNH CÔNG DỮ LIỆU MẪU CHO TẤT CẢ 16 BẢNG THÀNH CÔNG!';
PRINT N'- 6 Danh mục sản phẩm (Categories)';
PRINT N'- 5 Mã giảm giá (Discounts)';
PRINT N'- 4 Nhà cung cấp (Suppliers)';
PRINT N'- 35 Người dùng (Users: 1 Admin + 4 Staff + 30 Customers)';
PRINT N'- 30 Chi tiết khách hàng (CustomerDetails - chuyển RewardPoints)';
PRINT N'- 4 Chi tiết nhân viên (StaffDetails)';
PRINT N'- 9 Ca làm việc của nhân viên (StaffShifts)';
PRINT N'- 15 Sản phẩm thực tế (Products — 10 Status=1 hiển thị, 5 Status=0 chờ publish)';
PRINT N'- 45 Size sản phẩm (ProductSizes)';
PRINT N'- 10 Phiếu nhập kho (InventoryReceipts)';
PRINT N'- 35 Chi tiết nhập kho (InventoryReceiptDetails)';
PRINT N'- 35 Địa chỉ nhận hàng (UserAddresses)';
PRINT N'- 100 Đơn hàng thực tế phân bổ 6 tháng qua (Orders)';
PRINT N'- 220 Chi tiết đơn hàng (OrderDetails)';
PRINT N'- 15 Giỏ hàng hiện tại (CartItems)';
PRINT N'- 3 Quảng cáo (Advertisements)';
