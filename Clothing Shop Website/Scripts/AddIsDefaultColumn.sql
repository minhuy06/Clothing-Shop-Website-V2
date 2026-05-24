-- Chạy script này trong SQL Server nếu chưa restart được app để tự migrate
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'UserAddresses') AND name = 'IsDefault'
)
BEGIN
    ALTER TABLE UserAddresses ADD IsDefault bit NOT NULL CONSTRAINT DF_UserAddresses_IsDefault DEFAULT 0;
END

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory
    WHERE MigrationId = N'20260521130000_AddIsDefaultToUserAddress'
)
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260521130000_AddIsDefaultToUserAddress', N'5.0.17');
END
