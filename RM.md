# RM — Tóm tắt các hàm/luồng hiện tại

Tài liệu này mô tả nhanh các **controller actions**, **helpers**, **view + JS/CSS** quan trọng đang có trong project `Clothing Shop Website` (ASP.NET Core MVC .NET 5).

## 1) Quảng cáo (Advertisements)

### Admin — quản lý quảng cáo
- **File**: `Clothing Shop Website/Controllers/AdminController.cs`
  - **`Advertisements()`**: trang quản lý quảng cáo (popup + sidebar).
  - **`SearchProductsForAd(string? q)`**: API tìm sản phẩm để gắn quảng cáo.
    - **Chỉ trả về sản phẩm đang hiển thị**: `Products.Status == 1`.
  - **`AddAdvertisement(...)`**: tạo quảng cáo.
    - Validate `position` (`popup|sidebar`), validate ảnh, validate `ProductID` phải tồn tại & `Status == 1`.
    - Lưu các trường khuyến mãi: `DiscountType`, `DiscountValue`, `StartDate`, `EndDate`.
  - **`ToggleAdActive(int adId)`**: ẩn/hiện quảng cáo.
  - **`DeleteAdvertisement(int adId)`**: xóa quảng cáo.

- **File**: `Clothing Shop Website/Views/Admin/Advertisements.cshtml`
  - UI quản lý quảng cáo + modal tạo quảng cáo.
  - Chọn sản phẩm bằng ô search (AJAX), chọn vị trí, giảm giá, ngày bắt đầu/kết thúc, upload & cắt ảnh.

- **File**: `Clothing Shop Website/wwwroot/js/admin-ad-product.js`
  - Autocomplete tìm sản phẩm cho form thêm quảng cáo.

- **File**: `Clothing Shop Website/wwwroot/js/admin-ad-image.js`
  - Upload + crop ảnh theo vị trí:
    - `popup`: 4:5
    - `sidebar`: 16:10

- **File**: `Clothing Shop Website/wwwroot/js/admin-ads.js`
  - Mở/đóng modal tạo quảng cáo, sync vị trí, validate form cơ bản.

- **File**: `Clothing Shop Website/wwwroot/css/admin-ads.css`
  - CSS riêng cho trang quảng cáo admin (cards + modal + crop overlay).

### FE — quảng cáo trang chủ
- **File**: `Clothing Shop Website/Controllers/HomeController.cs`
  - **`Index()`**: lấy danh sách popup ads đang active (có ảnh, nằm trong khoảng ngày) -> `HomeIndexViewModel.PopupAds`.

- **File**: `Clothing Shop Website/ViewModels/HomeIndexViewModel.cs`
  - **`PopupAds`**: danh sách popup ads.

- **File**: `Clothing Shop Website/Views/Home/Index.cshtml`
  - Render popup quảng cáo theo `PopupAds` (có thể nhiều popup xếp chồng).
  - Link quảng cáo -> `/Product?highlight=<ProductID>` để **đẩy sản phẩm lên đầu** trong trang bộ sưu tập.

- **File**: `Clothing Shop Website/wwwroot/js/trang-chu.js`
  - Hiển thị popup (xếp chồng), đóng từng cái (lưu sessionStorage theo `AdID`).

- **File**: `Clothing Shop Website/wwwroot/css/trang-chu.css`
  - CSS popup quảng cáo + hiệu ứng “stack”.

### FE — quảng cáo sidebar (widget góc phải)
- **File**: `Clothing Shop Website/ViewComponents/NavSidebarAdsViewComponent.cs`
  - **`InvokeAsync()`**: lấy danh sách quảng cáo `Position == "sidebar"` (đang active, trong khoảng ngày, có ảnh).

- **File**: `Clothing Shop Website/Views/Shared/Components/NavSidebarAds/Default.cshtml`
  - Render **danh sách** quảng cáo sidebar (không stack, không nút bỏ qua).
  - Link quảng cáo -> `/Product?highlight=<ProductID>`.

- **File**: `Clothing Shop Website/wwwroot/js/sidebar-ads.js`
  - Thu gọn/mở lại widget quảng cáo sidebar.

- **File**: `Clothing Shop Website/wwwroot/css/neva-base.css`
  - CSS widget quảng cáo sidebar (`.sidebar-ad-panel`, `.sidebar-ad-item`, …).

## 2) Khuyến mãi theo quảng cáo (giá sale)

- **File**: `Clothing Shop Website/Models/Advertisement.cs`
  - `DiscountType` (0=VNĐ, 1=%), `DiscountValue`, `StartDate`, `EndDate`, `ProductID`, `Position`, `IsActive`.

- **File**: `Clothing Shop Website/Helper/AdPromotionHelper.cs`
  - **`IsPromotionActive(Advertisement?)`**: kiểm tra ad hợp lệ theo `IsActive`, `DiscountValue`, `ProductID`, `StartDate/EndDate`.
  - **`GetSalePrice(originalPrice, ad)`**: tính giá sau giảm (clamp 0..original).

## 3) Trang sản phẩm (Bộ sưu tập)

- **File**: `Clothing Shop Website/Controllers/ProductController.cs`
  - **`Index(..., int? highlight)`**:
    - Lọc sản phẩm `Status == 1`, áp filter (category/season/price/search).
    - Sort theo `sort`.
    - Nếu có `highlight`: **đẩy sản phẩm đó lên đầu**.
    - Load map quảng cáo đang active theo `ProductID` -> `ViewBag.ActiveAdMap` để view hiển thị giá sale.
  - **`GetSizes(int productId)`**: trả JSON size + tồn kho (cho FE modal).
  - **`Detail(int id)`**: action tồn tại nhưng hiện tại **không dùng** trong flow quảng cáo (link đã chuyển sang `/Product?highlight=`).

- **File**: `Clothing Shop Website/Views/Product/Index.cshtml`
  - Card sản phẩm hiển thị:
    - **Giá sale** (`salePrice`)
    - **Giá gốc gạch ngang** nếu có giảm (`.price-old`)
  - Inject `ALL_PRODUCTS` JSON để JS mở modal xem nhanh (price/old được set theo quảng cáo).

- **File**: `Clothing Shop Website/wwwroot/js/san-pham-page.js`
  - Modal “xem nhanh”, chọn size, giới hạn số lượng theo tồn kho.
  - Thêm giỏ gọi `POST /Cart/Add` với `sizeId` + `quantity`.
  - Link trong modal chuyển sang `/Product?highlight=<id>`.

- **File**: `Clothing Shop Website/wwwroot/css/san-pham.css`
  - Style hiển thị giá & giá gốc gạch ngang (`.price-old`).

## 4) Giỏ hàng (Cart) — giá sau giảm

- **File**: `Clothing Shop Website/Models/CartItem.cs`
  - `UnitPrice`: **giá chốt tại thời điểm add vào giỏ** (sau giảm nếu có).
  - `AdID`: quảng cáo áp dụng (nullable).

- **File**: `Clothing Shop Website/Controllers/CartController.cs`
  - **`Index()`**:
    - Load cart items.
    - **Backfill `UnitPrice` cho item cũ** nếu null/0 (tính theo quảng cáo active hiện tại).
  - **`Add(int sizeId, int quantity)`**:
    - Tính `unitPrice` theo quảng cáo active của sản phẩm (nếu có) rồi lưu `CartItem.UnitPrice`.
  - **`ValidateCoupon(code)`**:
    - Tính subtotal theo `(UnitPrice ?? Product.Price) * qty`, rồi áp coupon.
  - **`SetCheckoutPromo(coupon, usePoints)`**:
    - Lưu lựa chọn coupon/points vào session để qua trang checkout.

- **File**: `Clothing Shop Website/Views/Cart/Index.cshtml`
  - Subtotal/đơn giá/tổng dòng đều dựa trên `UnitPrice` (fallback giá gốc nếu null).

- **File**: `Clothing Shop Website/wwwroot/js/gio-hang.js`
  - UI áp coupon/điểm và tính tổng dựa trên `data-subtotal` từ server (đã là subtotal theo `UnitPrice`).

## 5) Thanh toán & đặt hàng (Order)

- **File**: `Clothing Shop Website/Helper/OrderPricingHelper.cs`
  - **`CalcSubtotal(items)`**: dùng `(UnitPrice ?? Product.Price) * qty`.
  - **`CalculateForCheckoutAsync(...)`**: tính tổng tiền cuối (shipping + coupon + points + tier).

- **File**: `Clothing Shop Website/Controllers/OrderController.cs`
  - **`Checkout()`**: đọc cart -> tính pricing -> render checkout.
  - **`PlaceOrder(...)`**:
    - Trừ kho.
    - Tạo `OrderDetails` với `UnitPrice = cartItem.UnitPrice ?? Product.Price`.
    - Xóa cart.

- **File**: `Clothing Shop Website/Views/Order/Checkout.cshtml`
  - Hiển thị từng dòng theo `UnitPrice` (fallback giá gốc).

## 6) Ghi chú kỹ thuật

- **Build lỗi DLL locked**: nếu build báo `MSB3027/MSB3021` (file bị lock) → stop IIS Express/Debugger rồi build lại.
- **Popup/Sidebar ads link**: hiện tại thống nhất link về `/Product?highlight=...` (không còn phụ thuộc view `Product/Detail.cshtml`).

