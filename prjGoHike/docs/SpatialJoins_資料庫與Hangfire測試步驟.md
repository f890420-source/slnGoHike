# 空間關聯：實際資料庫與 Hangfire 測試步驟

本文件交付需要實際啟動 Hangfire、直接連線讀寫 SQL Server 的測試步驟。依本次決定，這些測試留供後續執行，本輪測試環節結束；下列完整整合測試尚未執行，不能視為已通過。

API 契約、設定與同步規則見 [SpatialJoins_api.md](SpatialJoins_api.md)。所有 shell 指令都從 repository 根目錄執行。

## 已完成的驗證

| 項目 | 結果與範圍 |
| --- | --- |
| 主專案與 SpatialJoinTests build | 通過；主專案有既有警告，無錯誤 |
| SpatialJoinTests 預設測試 | 83 項通過：HTTP、JWT、驗證、排程流程、補排與 OpenAPI；資料存取／Hangfire 使用替身 |
| GoHikeSafeApiTests | 400 項通過：既有 API 回歸與 EF 查詢轉譯 |
| 目前資料庫唯讀盤點 | 已完成：schema、約束、索引、來源筆數與缺少 Segment 的來源 |
| 目前 SQL Server 暫存表測試 | 34 項通過：使用真實 geography 與交易；只寫 session-local 暫存表，未改業務資料／schema，未啟動 Hangfire |
| 業務表寫入、實際 Hangfire 入列／重試／補排 | 本輪未執行；操作方式如下 |

2026-10-04 盤點的資料庫為 `GoHikeData0917v5`：119 條步道、4 個指標，TrailSegments／IndicatorSegments 分別 515／70 筆；TrailIndicators、BackgroundJobRuns 均為 0 筆，尚無 HangFire 資料表。6 條已發布步道、1 個啟用指標缺少 Segment。

來源缺少 Segment 時，正常結果是整批 `Failed`，不會跳過缺失來源。若要在目前資料庫驗證 `Succeeded`，需先由資料維護流程補齊有效來源；測試不會自動修補目前資料。

## 一、執行前核對目前資料庫

連線讀取主專案 appsettings、Development 設定、User Secrets 與環境變數；業務連線鍵為 `ConnectionStrings:GoHikeDataContext`。帳密透過既有本機設定提供，不另寫入測試檔案或 Git。

```bash
dotnet build prjGoHike/prjGoHike.csproj
dotnet build tests/SpatialJoinTests/SpatialJoinTests.csproj
dotnet run --project tests/SpatialJoinTests --no-build -- --current-db-readonly
```

確認輸出的 server／database 是預期目標、`EvaluatedScore` 允許 NULL、BackgroundJobRuns 約束與索引啟用，以及目前來源缺失筆數。完整模式會拒絕已有非終止 SpatialJoin Run 的資料庫；執行期間也應停止其他空間同步與來源管理寫入。

若只要再次檢查 SQL 引擎、保留目前業務資料，可執行已通過的暫存表模式：

```bash
dotnet run --project tests/SpatialJoinTests --no-build -- --current-db-temp
```

此模式不涵蓋業務表的實際鎖定／寫入、正式表外鍵與約束，或 Hangfire 儲存與 Worker。

## 二、目前資料庫完整自動測試

這個模式適用於上述盤點狀態：來源仍有缺少 Segment，且候選關聯為空。測試對這些前提有斷言；資料之後改變時，應調整測試預期，或改用下一節的獨立資料庫測試成功路徑。

執行帳號需要業務表讀寫、交易應用程式鎖，以及建立 HangFire schema／資料表的權限；測試也會在回滾交易內暫時調整 Segment 約束並建立測試 trigger。選定可以接受下列異動的測試時段後執行：

```bash
dotnet run --project tests/SpatialJoinTests --no-build -- --current-db
```

測試自行啟動本機臨時 HTTP host、測試 Admin JWT、Hangfire Worker 與每分鐘恢復工作，不需先啟動主專案或提供正式管理員 token。此模式刻意將 Hangfire 儲存設為目前業務資料庫，並開啟 `PrepareSchemaIfNecessary`；不使用另外設定的 `ConnectionStrings:Hangfire`。

### 測試順序與預期結果

1. 取得 `GoHike:SpatialJoin` 應用程式鎖，在 Serializable 交易中建立來源／候選 fixture，暫時切換原來源啟用狀態，執行下表八種同步情境。
2. 每個情境結束回滾；比對原 TrailIndicators、來源狀態、TrailFeatures 與約束啟用／信任狀態。
3. 透過實際 SpatialJoinStore 同時建立 Run，只允許一個；核對不同 Job ID 無法搶占、終態無法重入，以及鎖等待逾時確實中止。
4. 部署 HangFire schema，啟動實際 Worker。POST 回 `202` 與 Location，GET 輪詢至 `Failed`；目前缺少 Segment 的錯誤應經過三次重試才終止，Run 保留第一次 StartedAt 並填入 FinishedAt。
5. 比對失敗前後 TrailIndicators 完全一致，核對 Run 清單與候選查詢 API。
6. 使用測試用入列失敗替身建立實際持久化的 Pending，將該測試 Run 的 CreatedAt 移至兩分鐘前；第二次 POST 回 `409`。呼叫正式恢復服務後確認綁定 Job ID、實際 Worker 執行及最終 `Failed`。
7. 確認固定 ID `gohike-spatial-join-recovery` 已註冊。測試 Pending 補排會直接呼叫恢復服務，不等待下一分鐘的排程 tick。

| SQL 情境 | 預期結果 |
| --- | --- |
| normal | 每組來源取全部 Segment 最短距離；點／線與面相交距離為 0；目前權重覆寫未評分候選 |
| wide | 使用較大門檻後納入較遠候選 |
| empty | 合法空來源清除未評分候選，仍保留所有已評分資料 |
| missing | 已發布／啟用來源缺少 Segment，整批失敗 |
| srid | SRID 非 4326，整批失敗 |
| empty-shape | 空圖形，整批失敗 |
| unsupported | 不支援的來源圖形型別，整批失敗 |
| rollback | 在更新成功狀態時注入失敗，關聯異動與成功狀態整批回滾 |

normal／wide 同時核對：原始約 100.004 公尺的距離不能因四捨五入而通過 100 公尺門檻；RawScore、OverlapRatio、EvaluatedScore 保持 NULL；0 分與非 0 分的已評分列所有欄位都保留。

測試專用 filter 會加速重試，仍核對 Hangfire 公開狀態 `Failed` 與 `RetryCount=3`；正式服務沒有這個加速 filter，需等待正常重試間隔。

### 實際留下的異動

- fixture、暫時來源狀態、測試 trigger 與約束調整在各自交易中回滾；identity 編號可能跳號。
- HangFire schema、排程／工作紀錄及 Store／HTTP 測試建立的 BackgroundJobRuns **會保留**，不會整體回滾。
- Pending 恢復案例會永久調整該測試 Run 的 CreatedAt；不代表一般 API 可修改 Run 時間。
- 腳本失敗或程序中斷後，可能留下非終止 Run 或尚未完成的 Hangfire 工作；先確認並處理該 Run 的實際工作狀態，再安排下一次測試。不要直接刪除狀態紀錄讓第二批工作開始。

## 三、獨立空資料庫完整整合測試

若要驗證有效來源的真實 Hangfire 成功路徑，可使用全新空 SQL Server 資料庫。名稱必須以 `GoHikeSpatialJoinTests` 開頭，帳號需能建立測試表與 HangFire schema；資料庫由操作者事先建立，測試不會自動建庫。

以下以互動輸入提供連線字串，避免將帳密直接寫進文件或 shell 歷史：

```bash
read -r -s -p '測試資料庫連線字串：' GOHIKE_SPATIAL_TEST_CONNECTION
export GOHIKE_SPATIAL_TEST_CONNECTION
dotnet run --project tests/SpatialJoinTests --no-build
unset GOHIKE_SPATIAL_TEST_CONNECTION
```

預設 83 項檢查完成後，會接續真實 SQL／Hangfire 整合測試。預期涵蓋成功提交、未評分候選新增／更新／刪除、已評分保留、來源驗證、同步鎖／併發、成功狀態更新失敗回滾，以及 Worker 成功、三次重試耗盡與 Pending 補排。

此模式建立業務 schema 的相關欄位／型別子集，並非目前完整資料庫的副本。測試資料、Run 與 HangFire 表保留供檢查；每次使用新的空資料庫，不可重複使用有表的資料庫，也不可將此環境變數指向現有業務資料庫。

## 四、使用正式應用程式人工驗收

此流程實際透過 API 執行同步，成功時會提交 TrailIndicators 候選異動。先依 [部署設定](SpatialJoins_api.md#啟用與部署) 準備 Hangfire 儲存及執行帳號；主程式不會在未設定儲存時自動借用業務連線。

```bash
dotnet user-secrets set "ConnectionStrings:Hangfire" "<選定的 Hangfire 儲存連線字串>" --project prjGoHike
dotnet user-secrets set "Hangfire:Enabled" "true" --project prjGoHike
dotnet user-secrets set "Hangfire:PrepareSchemaIfNecessary" "false" --project prjGoHike
dotnet run --project prjGoHike --launch-profile http
```

若尚未部署 HangFire schema，先執行官方 schema 部署；或在可進行 schema 建置的環境，一次性使用 `PrepareSchemaIfNecessary=true` 啟動建立，之後改回 false 並重啟。部署完成後以正常執行權限操作。

開啟 `http://localhost:5204/swagger`，使用既有登入流程取得含 Admin 角色的 JWT 並 Authorize。依序驗收：

1. POST `/api/admin/spatial-joins`，body 為 `{ "distanceMeters": 100.00 }`。應回 `202`、Run ID、statusUrl 與 Location。
2. GET statusUrl 每數秒輪詢至終態；有效來源應為 `Succeeded`，目前已知缺少 Segment 的來源應為 `Failed`，回應只有受控中文錯誤，不含 SQL／原始例外。記錄第一次 StartedAt，確認重試不改變該值。
3. 非終止 Run 存在時再次 POST，應回 `409` 並指出原 Run；工作若很快完成，不應要求後續 POST 仍回 409。GET `/api/admin/spatial-joins?page=1&pageSize=20` 應依時間由新到舊列出紀錄。
4. GET `/api/admin/spatial-joins/associations?pageSize=20`，核對目前未評分候選、名稱、距離、權重與 UTC evaluatedAt。翻頁時同時帶上一頁末列的 afterTrailId／afterIndicatorId；此清單是目前候選，不是某個 Run 的歷史明細。
5. 成功情境比對同步前後 SQL 資料：新增／更新列 RawScore、OverlapRatio、EvaluatedScore 為 NULL；已評分列（含 0 分）與 TrailFeatures 不變。失敗情境所有關聯都不變。可用下列唯讀查詢核對，並在執行前後保存結果。

```sql
SELECT Id, JobType, Status, DistanceMeters, CreatedAt, StartedAt, FinishedAt,
       HangfireJobId, ErrorMessage
FROM dbo.BackgroundJobRuns
WHERE JobType = 'SpatialJoin'
ORDER BY Id DESC;

SELECT * FROM dbo.TrailIndicators ORDER BY Trail_Id, IndicatorId;
SELECT * FROM dbo.TrailFeatures ORDER BY FeatureId;
```

另核對未登入 401、非 Admin 403、非法距離（例如 100.004）400、未知正整數 Run ID 404。將本機 `Hangfire:Enabled` 設 false 並重啟後，POST 應回 503，Run 查詢仍可讀；恢復啟用也需重啟。

要人工驗證每分鐘補排，可在獨立 Hangfire 儲存暫時不可用、業務資料庫仍可讀寫的情況建立 Run；它應保留 Pending。恢復儲存後保持 Web／Worker 常駐，等待 Pending 超過 60 秒及後續每分鐘恢復工作，確認 Job ID 綁定與終態。若兩種儲存共用同一資料庫，關閉該資料庫無法測到此情境；使用第二節的自動案例即可。

## 五、記錄與收尾

保存測試日期、目標 database、commit／版本、指令、exit code、Run ID 與最終狀態；伺服器日誌可用 RunId／JobId 對照。標明哪一種測試模式，不將暫存表或替身測試記成真實 Worker 通過。

完整模式結束後先確認沒有未完成的測試工作，再停止測試 host。保留測試 Run／Hangfire 紀錄供追查；如需清理，由資料庫管理流程針對記錄的測試 ID 處理。不要直接刪除整個 HangFire schema 或業務表。

人工驗收若修改了本機 User Secrets，結束後恢復原值；原本沒有該鍵時使用 `dotnet user-secrets remove "<設定鍵>" --project prjGoHike`。本次版本中的 Hangfire 預設仍為停用，schema 自動部署預設亦為停用。
