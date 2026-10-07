# 空間關聯背景工作 API

本功能以 Hangfire 1.8.25 執行同步，使用 SQL Server geography 計算距離，將目前候選關聯儲存在 `TrailIndicators`。所有入口都要求 JWT 的 `Admin` 角色。

## 啟用與部署

1. 業務資料庫先套用既有空間關聯 schema 腳本與 EF Model。`EvaluatedScore` 必須允許 NULL，並存在 `BackgroundJobRuns`。
2. 透過 User Secrets 或部署環境設定 `ConnectionStrings:Hangfire`；它可指向獨立 Hangfire 資料庫，或同一 SQL Server 資料庫的 HangFire schema。不要將密碼加入版本控制。
3. 在 Hangfire 儲存部署 1.8.25 的官方 SQL schema。部署帳號與日常執行帳號分開；應用帳號需有業務表的讀寫權限、取得 `public` principal 應用程式鎖的權限，以及 Hangfire 儲存的日常執行權限。
4. 設定 `Hangfire:Enabled=true` 後重啟服務。預設為 false；停用時建立工作回 503，既有紀錄仍可查詢。`PrepareSchemaIfNecessary` 預設 false；只有刻意授權自動部署 schema 的環境才設 true。

範例：

```json
{
  "Hangfire": {
    "Enabled": true,
    "PrepareSchemaIfNecessary": false,
    "CommandTimeoutSeconds": 30,
    "SyncCommandTimeoutSeconds": 300,
    "LockTimeoutMilliseconds": 5000
  }
}
```

命令 timeout 必須大於鎖等待時間。服務啟用時會啟動一個 Hangfire Worker，並持續註冊固定 ID `gohike-spatial-join-recovery`、UTC 每分鐘的補排／校對工作。Hangfire 儲存暫時失聯時，註冊會每分鐘重試。Web／Worker 必須常駐，停機期間不承諾執行時限。本功能不公開 Hangfire Dashboard。

套件與儲存部署參考：[Hangfire ASP.NET Core 整合](https://docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html)、[SQL Server 儲存](https://docs.hangfire.io/en/latest/configuration/using-sql-server.html)。

## 建立工作與輪詢

`POST /api/admin/spatial-joins`

```json
{ "distanceMeters": 100.00 }
```

距離必填，允許 `0.01～10000.00` 公尺，原始 decimal scale 最多兩位。`100.004` 或 `1.000` 都會被拒絕，不先四捨五入。前端只能提供門檻，不能指定 Run ID、狀態、權重或 Hangfire Job ID。

建立回 `202 Accepted`，`Location` 指向 `/api/admin/spatial-joins/{id}`，回應沿用 `ApiResponse`：

```json
{
  "success": true,
  "message": "已接受空間關聯同步請求。",
  "data": {
    "id": 12,
    "status": "Queued",
    "distanceMeters": 100.00,
    "createdAt": "2026-10-04T04:00:00Z",
    "startedAt": null,
    "finishedAt": null,
    "errorMessage": null,
    "statusUrl": "/api/admin/spatial-joins/12"
  }
}
```

Angular 儲存回傳 ID 或 `statusUrl`，每數秒 `GET /api/admin/spatial-joins/{id}`，直到 `Succeeded` 或 `Failed`。202 只表示請求已持久化，Status 也可能為 Pending／Processing。Hangfire 入列失敗會保留 Pending，由補排恢復。

| 狀態 | 意義 |
| --- | --- |
| Pending | 已持久化，尚未確認入列 |
| Queued | 已綁定執行者、等待處理 |
| Processing | 正在執行；中斷後可由原工作恢復 |
| RetryPending | 已失敗一次，仍在 Hangfire 重試流程中 |
| Succeeded | 關聯同步與成功狀態已於同一交易提交 |
| Failed | 重試耗盡或工作已不存在；修正問題後建立新 Run |

所有 Run 時間為 UTC。StartedAt 保留第一次開始時間；FinishedAt 只在終態設定。DTO 不包含 Hangfire Job ID 或原始例外。

未登入／非 Admin 回 401／403；請求驗證失敗回 400；不存在的 Run 回 404。已有非終止 Run 時 POST 回 409，Data 提供該 Run 與 `statusUrl`。功能停用或無法持久化／讀取時回 503，錯誤回應不暴露 SQL、內部路徑或連線資訊。

## 後台查詢

- `GET /api/admin/spatial-joins?page=1&pageSize=20`：Run 紀錄由新到舊，每頁最多 100 筆。Data 為 `{ "items": [...], "hasMore": true }`。
- `GET /api/admin/spatial-joins/associations?pageSize=20`：目前 `EvaluatedScore IS NULL` 的候選關聯，以 `(trailId, indicatorId)` 排序；可用 `trailId`、`indicatorId` 篩選。
- 候選關聯下一頁同時提供上一頁最後一列的 `afterTrailId` 與 `afterIndicatorId`；一頁最多 100 筆。Data 同樣含 items／hasMore。

候選 DTO 包含 trailId、trailName、indicatorId、indicatorName、distanceMeters、indicatorWeightSnapshot、evaluatedAt。讀取期間其他 Run 可能重新同步，分頁不是固定快照。

候選查詢回傳目前資料，不屬於指定 Run 的歷史明細。現有 schema 不保存結果版本，也不將每列關聯綁定 RunId。已評分關聯仍保留在資料表，但不列於此候選 endpoint，也不保證符合最新門檻。

## 同步與恢復規則

來源為已發布 Trails 與啟用 Indicators 的全部 Segment。每組步道／指標取 `MIN(STDistance)`，先比較原始公尺距離，再轉 `decimal(12,2)`。來源缺少 Segment、圖形無效／空值／空圖形、SRID 非 4326 或型別不支援時，整批失敗。合法空結果會移除未評分候選。

僅更新／刪除未評分關聯；新列使用伺服器目前權重，RawScore、OverlapRatio、EvaluatedScore 都是 NULL，EvaluatedAt 顯式寫 UTC。所有已評分列包含 0 分完整保留；不更動 TrailFeatures。

建立 Run、綁定執行者、狀態校對與同步都使用 `GoHike:SpatialJoin` SQL 交易應用程式鎖。同步使用 Serializable，來源讀取、驗證、關聯更新與 Succeeded 一次提交。同步期間可能阻擋來源的管理寫入，正式啟用前應量測資料量與執行時間。目前先以包覆圓中心距離與包覆角粗篩 Segment 配對，再物化候選的原始 Shape 精確距離供彙總；中心點仍做全配對比較，不保證利用原表空間索引。原理與量測範圍見 [SQL 優化實作 know-how](SpatialJoins_SQL優化實作know-how.md)。

Hangfire 採三次自動重試。狀態通知只觸發讀取目前 Hangfire 公開狀態，沒有在 Hangfire 未提交交易內寫入業務狀態；遺失或過早通知由每分鐘校對補回。Pending 超過 60 秒可補排，同 Run 的不同 Job ID 只選定一個執行者。終止 Run 不重入，舊 Failed Job 的手動重試不會更改結果。

監控 Run 最久 Pending 時間、Processing／RetryPending 持續時間、最後成功時間與 Failed 次數；伺服器日誌含 RunId／JobId 與鎖／入列／校對錯誤。停機取消交給 Hangfire 恢復，不直接視為最終失敗。

## 驗證

```bash
dotnet build prjGoHike/prjGoHike.csproj
dotnet run --project tests/SpatialJoinTests
dotnet run --project tests/GoHikeSafeApiTests
```

SpatialJoinTests 的 HTTP／流程測試使用替身，不能證明 SQL geography 或交易正確。若設定 `GOHIKE_SPATIAL_TEST_CONNECTION`，同一測試會使用真實 SQL Server 與 Hangfire 執行整合測試；未設定時明確印出 SKIP。

整合測試要求**全新空資料庫**，名稱以 `GoHikeSpatialJoinTests` 開頭，且帳號可建立其測試資料表與 HangFire schema。測試使用業務 schema 的相關欄位與型別子集，包含空間來源、主外鍵、nullable 分數與工作狀態約束；用來源資料缺陷測試 Job 驗證，因此 Segment 測試表刻意不限制 SRID／型別。測試資料保留供檢查，每次使用新資料庫，不得指向既有業務資料庫。

整合測試涵蓋 Segment 最短距離、點／面相交、100.004 公尺門檻、評分保留、空結果、來源驗證、同時建立 Run、應用程式鎖逾時、成功狀態更新失敗時整批回滾，以及真實 Hangfire 成功、三次重試耗盡與 Pending 補排。本輪不執行完整整合測試，依本次決定交付 [實際資料庫與 Hangfire 測試步驟](SpatialJoins_資料庫與Hangfire測試步驟.md)，測試環節結束。

### 使用目前資料庫

使用與主專案相同的 appsettings、User Secrets 與環境變數取得連線，不在測試檔案另外保存帳密。以下模式必須從 repository 根目錄執行：

```bash
# 唯讀核對目前 schema、來源筆數與缺少 Segment 的主表
dotnet run --project tests/SpatialJoinTests -- --current-db-readonly

# 只建立 session-local 暫存表，使用目前 SQL Server 的 geography 引擎測試
dotnet run --project tests/SpatialJoinTests -- --current-db-temp

# 已明確授權目前資料庫 schema／測試紀錄異動時，執行完整測試
dotnet run --project tests/SpatialJoinTests -- --current-db
```

暫存表模式複製目前來源／關聯表的欄位型別到 tempdb，僅替換同步 SQL 的表名稱，驗證最短距離、門檻、未評分新增／更新／移除、已評分保留、來源驗證及交易回滾。它不建立 Hangfire 工作，也不寫入業務表或更改來源狀態。

完整模式會在交易內建立來源測試資料與暫時切換來源狀態，最後回滾並核對原資料與約束狀態；identity 號碼可能跳號。另在目前資料庫部署官方 HangFire schema，啟動本機 Admin API／Worker，保留業務 Run 與 Hangfire 測試紀錄，驗證真實入列、重試耗盡、狀態查詢與 Pending 補排。開始時要求沒有非終止 SpatialJoin Run，避免與其他同步工作併行。

目前 `GoHikeData0917v5` 唯讀盤點：119 條步道、4 個指標、515／70 筆 Segment；有 6 條已發布步道、1 個啟用指標缺少 Segment。來源缺失的正常結果是整批 Failed，而非略過後宣稱成功。暫存表模式已完成 34 項真實 SQL 引擎檢查；需要業務表讀寫或實際 Hangfire 的測試僅交付上述文件，未執行。
