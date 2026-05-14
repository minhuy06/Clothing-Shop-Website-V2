USE DB_ShopQuanAo_DataWarehouse; -- Thay bằng tên DB của bạn
GO
DELETE FROM Fact_Sales;
DELETE FROM Fact_Inventory;
DELETE FROM Dim_Time;
-- Bạn có thể DELETE luôn Dim_Product, Dim_Customer nếu muốn làm lại sạch sẽ
DELETE FROM Dim_Product
DELETE FROM Dim_Customer