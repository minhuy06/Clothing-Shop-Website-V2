-- Bổ sung cột SupplierID cho bảng Products (nếu chưa có)
-- Chạy trên ClothingShopWebsiteDB khi gặp lỗi: Invalid column name 'SupplierID'

USE [ClothingShopWebsiteDB];
GO

IF COL_LENGTH('dbo.Products', 'SupplierID') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [SupplierID] INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_SupplierID' AND object_id = OBJECT_ID(N'dbo.Products'))
        CREATE INDEX [IX_Products_SupplierID] ON [dbo].[Products]([SupplierID]);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Products_Suppliers_SupplierID')
        ALTER TABLE [dbo].[Products] ADD CONSTRAINT [FK_Products_Suppliers_SupplierID]
            FOREIGN KEY ([SupplierID]) REFERENCES [dbo].[Suppliers]([SupplierID]) ON DELETE SET NULL;

    PRINT N'Đã thêm cột Products.SupplierID.';
END
ELSE
    PRINT N'Cột Products.SupplierID đã tồn tại.';

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260523120000_AddProductSupplier')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523120000_AddProductSupplier', N'5.0.17');
    PRINT N'Đã ghi nhận migration AddProductSupplier.';
END
GO
