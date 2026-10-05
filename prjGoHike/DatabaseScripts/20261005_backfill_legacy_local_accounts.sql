/* 僅用於本機先前已執行舊版驗證欄位腳本的開發資料庫。
   本機首次加入驗證程式碼的時間為 2026-10-05 14:50 UTC；此後註冊者仍須驗證。 */
UPDATE dbo.users
SET email_verified_at = SYSUTCDATETIME()
WHERE email_verified_at IS NULL
  AND password_hash LIKE '$2%'
  AND created_at < '2026-10-05T14:50:00';
