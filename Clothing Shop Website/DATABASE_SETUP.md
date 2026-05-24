# Thiết lập database (Code First — EF Core)

## Yêu cầu

- SQL Server (LocalDB / Express / Developer)
- Visual Studio với **Package Manager Console**
- .NET 5 SDK

## Connection string

Chỉnh `appsettings.json` nếu cần:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost; Database=ClothingShopWebsiteDB; Trusted_Connection=True; TrustServerCertificate=True"
}
```

EF sẽ **tự tạo database** `ClothingShopWebsiteDB` khi chạy migration lần đầu (nếu chưa tồn tại).

## Người mới clone project

Repo đã có sẵn migration **`InitialCreate`** (schema đầy đủ, gồm cột `Products.Status`).

1. Mở solution, **Stop** debug / IIS Express nếu đang chạy.
2. **Tools → NuGet Package Manager → Package Manager Console**
3. **Default project:** `Clothing Shop Website`
4. Chạy:

```powershell
Update-Database
```

5. (Tuỳ chọn) Nạp dữ liệu mẫu trong SSMS:

```text
DataWarehouse_Scripts/Seed_Realistic_Data.sql
```

6. F5 chạy web. Đăng nhập admin mẫu: SĐT `0900000000`, mật khẩu `123456` (hash trong seed).

## Khi nào dùng Add-Migration?

**Không** chạy `Add-Migration InitialCreate` trên repo đã có migration — sẽ báo trùng tên.

Chỉ dùng khi **bạn sửa model C#** (`Models/*.cs`):

```powershell
Add-Migration TenThayDoiMoTa
Update-Database
```

## Tạo lại database từ đầu (dev)

```powershell
Drop-Database
Update-Database
```

Sau đó chạy lại `Seed_Realistic_Data.sql` nếu cần dữ liệu mẫu.

## Auto migrate khi F5

`appsettings.json` → `"Database": { "AutoMigrate": true }` — app tự gọi `Migrate()` khi khởi động (tương đương `Update-Database`).
