CREATE VIEW v_DataMining_TimeSeries AS
SELECT 
    -- Tạo một "Trục thời gian" liên tục bằng cách ghép Năm và Tháng (Ví dụ: 202601, 202602)
    (dt.[Year] * 100) + dt.[Month] AS TimeIndex, 
    
    dp.CategoryName,
    
    -- Tổng số lượng bán ra của danh mục đó trong tháng
    SUM(fs.Quantity) AS TotalQty
FROM Fact_Sales fs
JOIN Dim_Time dt ON fs.TimeKey = dt.TimeKey
JOIN Dim_Product dp ON fs.ProductKey = dp.ProductKey
GROUP BY 
    (dt.[Year] * 100) + dt.[Month], 
    dp.CategoryName;