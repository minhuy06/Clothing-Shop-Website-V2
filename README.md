# 🛍️ Clothing Shop Website (V2) & Hệ Thống Phân Tích Dữ Liệu

Dự án này không chỉ là một trang web thương mại điện tử (bán quần áo) thông thường, mà còn tích hợp một hệ thống phân tích dữ liệu chuyên sâu. Để tối ưu hóa hiệu năng và đáp ứng các yêu cầu khác nhau, dự án được thiết kế với kiến trúc sử dụng **2 cơ sở dữ liệu (Database) riêng biệt**, đi kèm với quy trình **ETL** và **Cube (SSAS)**.

Dưới đây là phần giải thích ngắn gọn và dễ hiểu về kiến trúc này:

---

## 1. Tại sao lại cần 2 Database khác nhau?

Hệ thống của chúng ta phục vụ hai mục đích hoàn toàn khác biệt, do đó việc tách ra làm 2 cơ sở dữ liệu là tiêu chuẩn thiết kế tối ưu nhất:

- **Database 1 (OLTP - Hệ thống giao dịch):** Đây là cơ sở dữ liệu chính chạy trang web. Nhiệm vụ của nó là xử lý các thao tác diễn ra liên tục hàng ngày như: thêm sản phẩm, tạo tài khoản, đặt hàng, cập nhật giỏ hàng. Cấu trúc của DB này được tối ưu để ghi và đọc dữ liệu *nhanh, nhỏ lẻ*.
- **Database 2 (OLAP/Data Warehouse - Kho dữ liệu phân tích):** Đây là cơ sở dữ liệu phục vụ cho việc thống kê, báo cáo và dự đoán. Nó chứa dữ liệu lịch sử khổng lồ. Nếu chúng ta chạy các câu truy vấn phức tạp để phân tích doanh thu trên Database 1, trang web bán hàng sẽ bị chậm hoặc sập (quá tải). Do đó, dữ liệu được đẩy sang Database 2 để xử lý riêng biệt mà không ảnh hưởng đến trải nghiệm mua sắm của khách hàng.

---

## 2. Nhiệm vụ của tiến trình ETL (Extract, Transform, Load)

ETL là cây cầu nối giữa hai cơ sở dữ liệu trên. Quy trình này diễn ra định kỳ (ví dụ: mỗi đêm) với 3 bước:
- **Extract (Trích xuất):** Lấy dữ liệu mới nhất từ Database 1 (OLTP).
- **Transform (Biến đổi):** Làm sạch, chuẩn hóa và tính toán lại dữ liệu (chuyển đổi ngày tháng, gộp nhóm sản phẩm, lọc các đơn hàng bị hủy,...).
- **Load (Tải lên):** Lưu dữ liệu đã được làm sạch vào Database 2 (Kho dữ liệu).

*Nhờ có ETL, kho dữ liệu phân tích luôn có nguồn thông tin chính xác và sẵn sàng để khai thác.*

---

## 3. Tại sao lại cần Cube (SSAS)?

Dù đã có Database 2 (Data Warehouse), việc truy vấn dữ liệu vẫn có thể tốn thời gian nếu số lượng bản ghi lên tới hàng triệu. Đây là lúc **Cube (Khối dữ liệu đa chiều)** phát huy tác dụng.

- **Tốc độ siêu tốc:** Thay vì tính toán dữ liệu mỗi khi bạn mở báo cáo (Ví dụ: Tổng doanh thu áo thun mùa hè năm 2023), Cube đã tính toán sẵn và lưu trữ tất cả các tổ hợp kết quả có thể xảy ra. Khi bạn hoặc giảng viên cần xem báo cáo, kết quả sẽ hiện ra gần như ngay lập tức.
- **Hỗ trợ dự báo (Prediction):** Việc áp dụng các thuật toán dự đoán (ví dụ: dự báo nhập hàng) hoặc khai phá dữ liệu (Data Mining) trên cấu trúc đa chiều của Cube sẽ dễ dàng, nhanh chóng và chính xác hơn rất nhiều so với bảng dữ liệu quan hệ (Relational DB) truyền thống.

---

## 💡 Tóm lại (Dành cho báo cáo/giảng viên)
Kiến trúc của dự án được thiết kế chuẩn chỉnh theo mô hình Doanh nghiệp thực tế:
1. **Web DB** lo việc bán hàng (nhanh, nhẹ).
2. **ETL** đóng vai trò người vận chuyển và dọn dẹp dữ liệu.
3. **Data Warehouse (DB thứ 2)** lưu trữ dữ liệu lịch sử để phân tích.
4. **Cube** giúp truy xuất siêu tốc và cung cấp nền tảng cho các thuật toán dự đoán.

Sự tách bạch này chứng minh nhóm đã xem xét kỹ lưỡng đến yếu tố **hiệu năng (Performance)** và **khả năng mở rộng (Scalability)** của hệ thống khi đưa vào thực tế.
