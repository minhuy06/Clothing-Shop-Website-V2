-- Mở rộng Dim_Product cho cube / MDX (mùa: Xuân, Hạ, Thu, Đông — khớp Session trên web).
USE DB_ShopQuanAo_DataWarehouse;
GO
IF COL_LENGTH('dbo.Dim_Product', 'Season') IS NULL
BEGIN
    ALTER TABLE dbo.Dim_Product ADD Season NVARCHAR(20) NULL;
END
GO
