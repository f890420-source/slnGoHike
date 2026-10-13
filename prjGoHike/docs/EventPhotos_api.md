# 揪團活動圖片

選圖時由 Angular `FileReader` 預覽；送出活動後，後端使用既有 `CloudinaryService`
上傳到 `gohike/events`，將 HTTPS 圖片網址存進 `EventData.ActivityPhoto`。
清單、詳情、編輯預覽與地圖照片直接使用此網址，不需要拼接後端位址。

## API 契約

| 方法 | 路徑 | 圖片行為 |
| --- | --- | --- |
| POST | `/api/EventDataAPI` | 可選的 `ActivityPhoto` 檔案；未選圖時省略欄位 |
| PUT | `/api/EventDataAPI/{eventid}` | 可選的新圖片；省略欄位保留原圖 |
| DELETE | `/api/EventDataAPI/{eventid}` | 活動儲存刪除成功後清理圖片 |
| GET | `/api/EventDataAPI` | `data[].activityPhoto` 回傳圖片網址；無圖時使用前端預設圖 |

POST／PUT 使用 `multipart/form-data` 並帶 JWT Bearer token；前端不要自行設定
`Content-Type`，讓瀏覽器產生 boundary。成功維持 HTTP 200 與既有
`{ success, message, data }` 包裝，`data` 包含 `eventId`、`eventName`、`activityPhoto`。
圖片保留 JPG、JPEG、PNG、GIF 格式及 10 MB 上限；不支援格式或過大回傳 400。
Cloudinary 上傳失敗回傳 503 與「圖片上傳失敗，請稍後再試」。
Cloudinary 網址超過資料欄位的 255 字元限制時清理新圖並回傳 500。

替換圖片時，先上傳新圖，活動儲存成功才清理舊圖。資料庫儲存失敗會嘗試清理新圖。
圖片清理失敗記錄 warning，不覆蓋活動操作的成功或原始錯誤結果。
既有 `/assets/JoinGroup_Images/` 圖片繼續相容，不批次搬移；替換／刪除時只清理此目錄
對應檔案。Cloudinary 圖片透過服務刪除並要求快取失效。

## 後端設定

本機沿用後端 User Secrets 的 `Cloudinary:CloudName`、`Cloudinary:ApiKey`、
`Cloudinary:ApiSecret`。部署環境需將下列變數連結到 Container App secrets：

- `Cloudinary__CloudName`
- `Cloudinary__ApiKey`
- `Cloudinary__ApiSecret`

User Secrets 不會進入 Docker 映像；需在實際部署環境檢查三項變數均已配置。
憑證只放後端，不放 Angular environment、Git 或 Dockerfile。

## 驗證

```bash
dotnet run --project tests/EventPhotoApiTests
```

測試以 HTTP multipart、JWT、資料庫與 Cloudinary test doubles 驗證新增、換圖、
刪除、既有本機檔案相容、格式／大小限制、上傳失敗、儲存失敗與清理順序，不連正式資料庫。

配置好本機後端 Cloudinary 憑證後，可由 repository 根目錄執行：

```bash
dotnet run --project tests/EventPhotoApiTests -- --live-cloudinary
```

此模式額外透過活動 API 上傳一張暫時圖片到真實 Cloudinary，再 GET 活動清單，
讀取圖片 HTTPS URL，最後刪除圖片。活動資料使用記憶體 test double；
此測試不驗證正式 SQL 儲存或實際瀏覽器畫面。

前端 `/home/r2/Projects/frdev/` 的相關驗證：

```bash
npm test -- --watch=false --include=src/app/features/groupjoin/groupjoin.spec.ts
npm run build
```

部署後用一筆測試活動選圖並送出，確認列表、詳情與地圖顯示圖片，重新整理仍可顯示，
換圖後顯示新圖，刪除活動後確認 Cloudinary 圖片已清理。
