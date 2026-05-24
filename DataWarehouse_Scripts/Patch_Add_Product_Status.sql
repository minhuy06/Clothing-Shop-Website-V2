-- DB-first: thêm cột Status vào Products (chạy trên ClothingShopWebsiteDB)
-- Lỗi "Invalid column name Status" trong Error List SSMS thường là cache IntelliSense — xem hướng dẫn cuối file.

USE [ClothingShopWebsiteDB];
GO

IF COL_LENGTH('dbo.Products', 'Status') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products]
        ADD [Status] INT NOT NULL CONSTRAINT [DF_Products_Status] DEFAULT (0);

  -- Dùng dynamic SQL để SSMS không báo đỏ khi parse (cột chưa có lúc mở file)
    EXEC(N'UPDATE [dbo].[Products] SET [Status] = 1 WHERE [Status] = 0;');

    PRINT N'Đã thêm cột Products.Status (mặc định 0, dữ liệu cũ gán 1).';
END
ELSE
    PRINT N'Cột Products.Status đã tồn tại — bỏ qua ALTER.';
GO

-- Kiểm tra cột thật sự có trên DB (chạy xong phải thấy 1 dòng)
SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Products')
  AND c.name = N'Status';
GO

SELECT TOP (5) ProductID, ProductName, Status FROM dbo.Products ORDER BY ProductID;
GO

/*
  Nếu query trên chạy OK nhưng Error List vẫn báo "Invalid column name Status":
  → Đó là IntelliSense cache, KHÔNG phải lỗi DB.
  SSMS: Edit → IntelliSense → Refresh Local Cache  (Ctrl+Shift+R)
  Hoặc đóng/mở lại file .sql và reconnect server.
*/
