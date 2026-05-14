USE ClothingShopWebsiteDB;
GO
IF COL_LENGTH('dbo.Products', 'OriginalPrice') IS NULL
BEGIN
    ALTER TABLE dbo.Products ADD OriginalPrice DECIMAL(18,2) NULL;
END
GO
