/* 可重複執行。首次新增欄位時，既有密碼帳號沿用原有登入資格。 */
IF COL_LENGTH('dbo.users', 'email_verified_at') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD email_verified_at datetime2 NULL;

    /* 只在首次啟用驗證時回填既有密碼帳號。後續新註冊仍為未驗證。 */
    EXEC sp_executesql N'
    UPDATE dbo.users
    SET email_verified_at = SYSUTCDATETIME()
    WHERE email_verified_at IS NULL AND password_hash LIKE ''$2%'';
    ';
END;

/* Google-only 帳號已由 Google 驗證；動態 SQL 避免新增欄位後的編譯問題。 */
EXEC sp_executesql N'
UPDATE dbo.users
SET email_verified_at = SYSUTCDATETIME()
WHERE email_verified_at IS NULL AND password_hash LIKE ''GOOGLE[_]ONLY[_]%'';
';
