INSERT INTO dbo.PurchaseCodes (Code, IsActive, CreatedAt)
VALUES ('CODE', 1, CAST(GETUTCDATE() AT TIME ZONE 'UTC' AT TIME ZONE 'GMT Standard Time' AS datetime2));