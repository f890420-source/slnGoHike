# GoHikeSafe 使用方法、開發原理與技術亮點

本文依 `APIControllers/GoHikeSafe` 的五個 Controller，以及其 DTO、EF Model、GeoJSON converter、SpatialJoins 服務與災害警示即時通知流程整理。課堂基礎的比較依據為 repository 根目錄的 [AGENTS.md](../../AGENTS.md)；該文件是開發準則，不能據此斷言某項技術實際上未在課堂教授。以下將「基礎要求」與「本專案針對地理資料、背景工作、即時通知所做的延伸」對照。

## 1. 功能範圍與閱讀入口

| 模組 | 用途 | 原始碼 |
| --- | --- | --- |
| 步道 | 公開已發布步道與路線；管理員維護步道與路段 | [TrailsApiController](../APIControllers/GoHikeSafe/TrailsApiController.cs) |
| 指標 | 公開啟用指標；管理員維護權重、等級與空間範圍 | [IndicatorApiController](../APIControllers/GoHikeSafe/IndicatorApiController.cs) |
| 災害警示 | 公開啟用警示；管理員維護有效期間與空間範圍，API／MVC 儲存成功後廣播變更通知 | [DisasterAlertsApiController](../APIControllers/GoHikeSafe/DisasterAlertsApiController.cs) |
| 步道特徵 | 登入者回報周圍特徵，管理員確認後公開 | [TrailFeaturesApiController](../APIControllers/GoHikeSafe/TrailFeaturesApiController.cs) |
| 空間關聯 | 管理員排入背景同步，計算步道與指標的距離候選，查詢工作狀態 | [SpatialJoinsApiController](../APIControllers/GoHikeSafe/SpatialJoinsApiController.cs) |

進一步串接與部署文件：

- [API 啟動與 CRUD 契約](README_api.md)
- [步道關聯指標](TrailIndicators_api.md)
- [步道特徵回報完整欄位與前端範例](TrailFeatures_api.md)
- [災害警示 API 與 SignalR 即時通知](DisasterAlerts_api.md)
- [SpatialJoins API 與部署](SpatialJoins_api.md)
- [SQL 優化原理與既有量測](SpatialJoins_SQL優化實作know-how.md)
- [實際資料庫／Hangfire 驗收步驟](SpatialJoins_資料庫與Hangfire測試步驟.md)

目前空間同步只處理 `Trails ↔ Indicators`。災害警示 CRUD 不會自動建立 `AlertsTrails`，特徵回報也不會自動產生指標。現有 `EvaluatedScore` 是讀取／保護既存評分的欄位，這組 API 沒有實作評分計算或評分寫入端點。PMTiles 導入規劃不屬於此資料夾已完成的功能。

## 2. 啟動與共通呼叫方式

從 repository 根目錄操作，需 .NET 10 SDK、可用的 SQL Server 及對應業務 schema。連線設定使用 User Secrets 或部署環境設定：

```bash
dotnet user-secrets set "ConnectionStrings:GoHikeDataContext" "<SQL Server 連線字串>" --project prjGoHike
dotnet restore slnGoHike.slnx
dotnet build prjGoHike/prjGoHike.csproj --no-restore
dotnet run --project prjGoHike --launch-profile http
```

HTTP 開發網址為 `http://localhost:5204`。Development 環境提供 `/swagger` 與 `/openapi/v1.json`；HTTPS profile 使用 `https://localhost:7285`，憑證設定見 [README_api](README_api.md)。讀取 OpenAPI 不查詢業務資料庫；實際 API 查詢需要資料庫連線。

公開 GET 不需登入。登入回報使用有效 Access Token；管理端點需 token 的 `Admin` 角色：

```http
Authorization: Bearer <Access Token>
Content-Type: application/json
```

Swagger 的 Authorize 欄位只貼 token，不需自行加 `Bearer` 前綴。步道寫入另外要求可解析成 long 的 `NameIdentifier` claim；特徵新增要求該 claim 為正整數。不能只在前端顯示管理按鈕就取得權限。

Controller 主動回應通常使用 `ApiResponse<T>`，JSON 欄位為 camelCase：

```json
{
  "success": true,
  "message": "操作成功",
  "data": [],
  "errors": null
}
```

`errors` 是可選錯誤列表。不要假設所有錯誤都有這個包裝：目前 `[ApiController]` 的自動模型驗證仍採框架的 ValidationProblemDetails；JWT 的 401／403 也不保證有 `ApiResponse` 本文。Client 應先處理 HTTP 狀態，再解析對應回應。

| 狀態碼 | 使用語意 |
| --- | --- |
| 200 | 讀取、更新、刪除成功；公開清單查無資料仍回空陣列 |
| 201 | CRUD 或特徵回報新增成功；共用 CreatedResponse 未設定 Location |
| 202 | 空間同步請求已持久化；含狀態網址與 Location，不代表完成 |
| 400 | 欄位、ID、GeoJSON 或查詢參數不符合規則 |
| 401／403 | 未通過認證／缺少所需角色 |
| 404 | 資源不存在；公開明細也會隱藏未發布／停用資源 |
| 409 | 刪除有關聯資料、特徵寫入時外鍵關聯變更，或已有未終止同步工作 |
| 500 | 一般資料操作失敗，Controller 回受控訊息並記錄日誌 |
| 503 | 空間背景工作停用，或其資料存取／鎖／逾時失敗 |

路由使用 `{id:long}`；非 long 的文字通常不匹配路由，不能當成已進入 Controller 的 400 驗證。

## 3. 步道、指標與災害警示 CRUD

三個基底路徑分別為 `/api/trails`、`/api/indicators`、`/api/disasteralerts`：

| 方法與相對路徑 | 使用方法 | 權限 |
| --- | --- | --- |
| GET `/` | 步道只看 IsPublished；指標／警示只看 IsActive | 公開 |
| GET `/{id}` | 讀取單筆公開資料 | 公開 |
| GET `/admin` | 讀取包含未發布／停用的完整管理資料 | Admin |
| GET `/admin/{id}` | 取得編輯用資料 | Admin |
| POST `/` | 新增資源，ID 由伺服器產生 | Admin |
| PUT `/{id}` | 更新一般欄位及選擇性替換路段 | Admin |
| DELETE `/{id}` | 刪除主表與所屬路段；有其他關聯時拒絕 | Admin |

公開步道回 `TrailPublicDto`（名稱、區域、難度、距離及路段）；公開指標清單只回 `IndicatorTextInfoDto`（ID、名稱、類型），明細回 `IndicatorPublicDto` 與 `indiSegments`。管理步道／指標使用較完整 DTO；警示公開與管理均使用 `DisAlertDto`，包含警示期間、來源及 `alertSegments`。

警示公開查詢目前只依 `IsActive` 篩選，**沒有依現在時間過濾 EffectiveFrom／EffectiveTo**。時間欄位用於資料維護與回傳，Client 若需要「現在有效」需另依需求判斷，不能將啟用清單直接等同於當下有效警示。

### 新增本文範例

以下各 JSON 分別送往對應 POST 路徑。`shape` 是 Geometry 本身，座標順序為 `[經度, 緯度]`：

`POST /api/trails`：

```json
{
  "trailName": "示範步道",
  "region": "臺中",
  "difficultyLevel": 2,
  "distanceKm": 1.2,
  "estimatedHours": 0.8,
  "permitRequired": false,
  "guideRequired": false,
  "regulationNote": null,
  "isPublished": true,
  "trailSegDtos": [
    { "shape": { "type": "LineString", "coordinates": [[121.0, 24.0], [121.01, 24.01]] } }
  ]
}
```

`POST /api/indicators`：

```json
{
  "indicatorName": "落石注意點",
  "indicatorType": "Risk",
  "weight": 1.5,
  "indicatorLevel": 3,
  "indicatorDescription": "人工調查的注意區域",
  "dataSource": "人工調查",
  "isActive": true,
  "indicatorSegments": [
    { "segmentName": "注意點", "shape": { "type": "Point", "coordinates": [121.0, 24.0] } }
  ]
}
```

`POST /api/disasteralerts`：

```json
{
  "alertType": "Rockfall",
  "alertTitle": "示範落石警示",
  "alertDescription": "請留意路況",
  "severityLevel": 3,
  "effectiveFrom": "2026-10-05T00:00:00Z",
  "effectiveTo": "2026-10-06T00:00:00Z",
  "sourceAgency": "人工巡查",
  "sourceUrl": null,
  "isActive": true,
  "alertSegments": [
    { "segmentName": "警示位置", "shape": { "type": "Point", "coordinates": [121.0, 24.0] } }
  ]
}
```

一般欄位使用 DTO 的 Required、StringLength、Range、Url 驗證，加上 Controller 的空白名稱與警示時間檢查。步道難度、指標／路段等級、警示嚴重度均限定 1～5；指標權重範圍為 0～999.999。警示起始時間不可為 default，結束時間可為 null，提供時不可早於起始時間。完整規則以 [DTO 原始碼](../DTO/GoHikeSafe) 為準。

### 更新與刪除注意事項

PUT 先取得管理明細，再送出需要保留的一般欄位。步道／指標本文 `id`、警示本文 `alertId` 必須與路由一致且大於 0。一般欄位會依 DTO 更新；省略 bool 等值型別可能使用預設值，不適合當作任意欄位的 PATCH。

| 路段本文 | 步道 `trailSegDtos` | 指標 `indicatorSegments`／警示 `alertSegments` |
| --- | --- | --- |
| 省略或 null | 保留原路段 | 保留原路段 |
| 非空陣列 | 替換，限 LineString／MultiLineString | 替換，每段需有效 Shape |
| 空陣列 | 拒絕，回 400 | 清除路段 |

路段替換採刪除舊資料、新增新資料，產生新路段 ID，不依 Client 提供的路段 ID 做逐段更新。API 新增／替換的步道路段 Source 固定為 `User Uploaded`。

步道刪除會檢查警示關聯、登山紀錄明細、特徵、指標關聯、訂閱及行程回報；指標檢查 TrailIndicators，警示檢查 AlertsTrails。Controller 先以 AnyAsync 提供 409 訊息，再捕捉 SQL Server 外鍵違反（547）處理檢查後發生的競爭。刪除成功回 200、`data: ""`。

### 災害警示即時變更通知

警示 API 與 `AdminDisAlertController` 的新增、更新及實際刪除，在儲存成功後透過既有 `/eventHub` 廣播 `AlertsChanged`，單一參數為 `{ "alertId": 123 }`。停用警示也會通知；驗證、權限、資源存在或關聯檢查未通過，以及儲存失敗時不通知。MVC 查無資料的刪除不通知。

前端首次載入、收到通知及重新連線後，重新 GET `/api/disasteralerts`，以完整清單替換圖層資料，才能反映刪除與停用。通知本身不帶 GeoJSON，也沒有可靠補送紀錄；API／DB 是警示資料來源。Hub 的接收流程與地圖更新建議見 [串接文件](DisasterAlerts_api.md)。

共用服務採獨立 5 秒逾時，不使用 HTTP request token；推播失敗記錄 Warning、警示 ID 與例外，保留已成功寫入的 HTTP 回應或 MVC 導向結果。本功能不需要新增套件或資料庫結構，也不需要啟用 Hangfire；沒有時間邊界自動通知或警示 Spatial Join。

## 4. 特徵回報與確認流程

1. 公開 `GET /api/trailfeatures?trailId=12&featureType=Water` 查詢指定步道特徵，僅顯示 `IsAvailable=true` 且所屬步道已發布的資料。`GET /api/trailfeatures/{id}` 使用相同公開條件。
2. 登入者以 POST 回報已發布步道；下面的 `trailId` 請換成實際存在的 ID。
3. Admin 用 `GET /api/trailfeatures/admin?isAvailable=false` 找出尚未開放的回報，或以 `/admin/{id}` 讀取單筆。
4. Admin 用 PUT `/{id}` 完整送出回報欄位與可信度、可用狀態、來源，確認後設 `isAvailable=true`。目標步道允許未發布，但此時仍不會公開。
5. Admin 可 DELETE `/{id}` 刪除特徵。一般會員沒有修改／刪除回報端點。

```http
POST /api/trailfeatures
Authorization: Bearer <登入者 Access Token>
Content-Type: application/json
```

```json
{
  "trailId": 12,
  "featureType": "Water",
  "featureName": "步道旁水源",
  "location": { "type": "Point", "coordinates": [121.0, 24.0] },
  "featureDescription": "現場觀察，待確認"
}
```

`featureType` 使用最長 20 字元的英文字首代碼，後續可用英數、底線、連字號；目前不是固定 enum。位置只接受有效 Point。新增回 201，伺服器固定 `reliabilityLevel=1`、`isAvailable=false`、`dataSource="Member report"`；回報 DTO 沒有這些管理欄位。

Admin PUT 使用相同五個回報欄位，再加 `reliabilityLevel`（1～5）、必填 `isAvailable`、可選 `dataSource`，本文不需 `featureId`。這是以 PUT 維護可信度與公開狀態，尚未另設確認工作流、回報者歸屬欄位或審核歷程。完整範例見 [TrailFeatures_api](TrailFeatures_api.md)。

## 5. 空間同步與公開關聯查詢

Hangfire 預設停用。要執行同步，需業務表包含 nullable `EvaluatedScore` 與 `BackgroundJobRuns`，部署 Hangfire SQL schema、設定 `ConnectionStrings:Hangfire`，再設定 `Hangfire:Enabled=true` 並重啟。預設不自動建立 Hangfire schema，部署細節見 [SpatialJoins_api](SpatialJoins_api.md#啟用與部署)。

```http
POST /api/admin/spatial-joins
Authorization: Bearer <Admin Access Token>
Content-Type: application/json

{ "distanceMeters": 100.00 }
```

距離必填，允許 0.01～10000.00 公尺，decimal 原始 scale 最多兩位；`100.004` 與 `1.000` 均拒絕，不先四捨五入。

收到 202 後，取 `data.id` 或 `data.statusUrl`，每數秒 GET `/api/admin/spatial-joins/{id}`，直到 Succeeded／Failed。若 POST 回 409，回應 data 是既有未終止 Run，可直接輪詢它。狀態查詢、紀錄與候選端點均限 Admin。

| 工作狀態 | 意義 |
| --- | --- |
| Pending | 業務 Run 已提交，尚未確認入列 |
| Queued | 已綁定 Hangfire 執行者，等待執行 |
| Processing | 執行中，中斷可恢復 |
| RetryPending | 單次失敗後等待 Hangfire 重試 |
| Succeeded | 關聯異動與成功狀態已在同一交易提交 |
| Failed | 重試耗盡或工作遺失等終止情況；修正後建立新 Run |

Run 時間為 UTC，回應不公開 Hangfire Job ID／原始例外。功能停用時不能新建工作，但仍可讀取既有業務紀錄。

- `GET /api/admin/spatial-joins?page=1&pageSize=20`：由新到舊查 Run，pageSize 最大 100，data 含 `items`、`hasMore`。
- `GET /api/admin/spatial-joins/associations?pageSize=20`：只看目前未評分候選；可用 trailId／indicatorId 篩選。下一頁同時提供上一頁末列的 afterTrailId 與 afterIndicatorId。
- `GET /api/trails/{id}/indicators`：公開已發布步道的啟用指標摘要，包含未評分與已評分關聯；不查圖形、不重新計算距離、不排背景工作。

公開摘要包含 `trailId`、`hasIndicators`、`indicators`；每筆有 indicatorId、indicatorName、indicatorType、indicatorLevel、distanceMeters、evaluatedScore。`null` 是未知／未評分，`0` 是有效數值。無可見關聯回 200、false、空清單；不代表沒有風險，也不代表已完成同步。同步成功後 Client 重新 GET 取得目前結果。

候選查詢沒有 RunId 歷史版本，分頁期間結果可能被下一次同步更新；公開摘要也不能保證每列都來自最近成功的 Run。已評分列受保護，可能超出最新門檻。

## 6. 開發原理：從 HTTP 到空間資料

### 6.1 一般請求與資料關係

```text
HTTP request
  → JWT／角色驗證（受保護端點）
  → JSON converter／Model Binding／DataAnnotations
  → Controller guard clauses 與資源存在檢查
  → EF 查詢或追蹤 Entity 異動
  → ToListAsync／FirstOrDefaultAsync／SaveChangesAsync
  → DTO + HTTP 狀態碼 + JSON
```

[GoHikeDataContext](../Models/GoHikeDataContext.cs) 由 DI 管理 scoped 生命週期，在 OnConfiguring 啟用 SQL Server NetTopologySuite。Trail、Indicator、DisasterAlert 各有多筆 Segment；TrailFeature 是所屬步道的一個位置；TrailIndicator 以 `(TrailId, IndicatorId)` 複合主鍵保存關聯距離、權重快照與評分欄位。

公開步道與指標使用 Select 投影需要的 DTO 欄位，步道關聯摘要抽成 [TrailIndicatorQuery](../Services/TrailIndicatorQuery.cs)。其他管理／警示查詢用 Include 取得路段，再於 materialize 後映射 DTO，避免將 EF 導覽屬性直接交給 serializer。唯讀 Entity 查詢多使用 AsNoTracking；寫入則先載入追蹤 Entity，更新允許欄位後呼叫 SaveChangesAsync。

CRUD 邏輯目前多在 Controller 內；較複雜的背景同步有 Service／Store 邊界，警示推播則由 [DisasterAlertRealtimeService](../Services/DisasterAlertRealtimeService.cs) 集中處理，由 API 與 MVC 共用。沒有另加通用 Repository 或 CQRS。TrailGeometryService 等 MVC 檔案處理不屬於這組 API 的 JSON 寫入流程。

### 6.2 GeoJSON 的三層驗證

```text
原始 Geometry JSON
  → GeoJsonGeometryValidator.ValidateJson
  → NTS GeoJSON 解析（SRID 4326）
  → ValidateAndOrient：拓樸檢查與 Polygon 方向處理
  → Controller 的業務型別限制
  → SQL Server geography 儲存
```

相關實作：[Geometry converter](../Services/GeoJsonGeometryConverter.cs)、[驗證器](../Services/GeoJsonGeometryValidator.cs)、[Point converter](../Services/GeoJsonPointConverter.cs)。

- 原始 JSON 階段檢查 type、coordinates、點數、封閉環、有限數值、2D／3D 座標與經緯度範圍；不接受 Feature／FeatureCollection 或舊式 crs。
- NTS 階段拒絕空圖形、空子圖形、無效拓樸。合法 Polygon 外環調為逆時針、內環順時針，保留高度，不自動修復自交。
- API 的 Geometry converter 可接受七種 Geometry 型別；步道路段額外只接受 LineString／MultiLineString，特徵 location 只接受 Point。
- 可選 bbox 會檢查維度、範圍與座標涵蓋性；bbox 及一般 foreign members 不保存。所有輸入採 SRID 4326，沒有投影轉換或跨日期變更線自動切割。

這不是把 GeoJSON 當作任意 JSON 字串存入資料庫。C# 端用 NTS Geometry／Point，資料庫用 geography。NTS 驗證不代表涵蓋 SQL Server geography 的全部儲存限制；來源也可能由 MVC 或其他流程匯入，所以背景 SQL 執行前仍再次驗證。

### 6.3 空間同步的計算流程與效能設計

[Synchronize.sql](../Services/SpatialJoins/Synchronize.sql) 是組件的 EmbeddedResource，由 Store 在交易內執行，修改後需重新 build／重啟。

```text
已發布步道／啟用指標的 Segment
  → 驗證來源並物化暫存表
  → 每段計算 EnvelopeCenter／EnvelopeAngle
  → 中心點距離 + 包覆角粗篩 #SegmentCandidates
  → 原始 Shape.STDistance → #SegmentDistances
  → 每組步道／指標 MIN(Meters) → #PairDistances
  → 原始距離與門檻比較，再轉 decimal(12,2) → #Candidates
  → 更新／新增／移除未評分關聯 + 寫入 Succeeded
```

不同於依相同 ID JOIN，這裡根據空間距離建立關係。一個步道／指標可以有多個 Segment，需先彙總最短距離，避免把每段配對變成重複關聯。距離由 SQL geography 計算，程式使用公尺門檻，不在 NTS 端把經緯度的平面距離當成公尺。

目前粗篩的保留條件為：

```text
中心距離 ≤ ((步道包覆角 + 指標包覆角) × PI / 180 × 6500000
            + 距離門檻) × 1.01 + 1
```

這些常數是實作中的粗篩餘裕；最終仍以原始 Shape 的 STDistance 判定，不以包覆圓距離當結果。包覆角大於 90 度、包覆資訊或中心距離為 null 時保留配對。候選與距離分別物化，讓後續彙總重用已計算值。仍會做中心點的全配對比較，不保證使用原表空間索引；效能依來源量與圖形複雜度而變，既有量測範圍見 [SQL know-how](SpatialJoins_SQL優化實作know-how.md)。

距離需先比較再捨入，例如 100.004 公尺在 100.00 公尺門檻下不可納入。來源缺 Segment、錯誤 SRID、空／無效圖形或不支援型別時整批失敗；背景 SQL 的指標來源只允許六種型別，**不接受 GeometryCollection**，即使 CRUD Geometry converter 可接受它。

同步僅更新／刪除 `EvaluatedScore IS NULL` 的關聯；已評分列包含 0 分完全保留。新增權重快照由伺服器讀取 Indicator.Weight，RawScore／OverlapRatio／EvaluatedScore 保持 null；未評分既有列的 RawScore／OverlapRatio 也會清為 null。合法空候選會清除未評分關聯。EvaluatedAt 由同步 SQL 顯式寫入 UTC，不能據此推論其他寫入途徑的資料也都使用 UTC。

### 6.4 背景工作的一致性與恢復

HTTP 背景入口走 `Controller → SpatialJoinService → ISpatialJoinStore`。Store 使用參數化 SqlCommand、明確 decimal precision／scale、await using 釋放連線與交易，客戶端只能提供距離，不能指定工作狀態或 Job ID。

1. 在業務資料庫交易內取得 `GoHike:SpatialJoin` exclusive `sp_getapplock`，確認沒有未終止 Run，再提交 Pending。
2. Pending 持久化後呼叫 Hangfire 入列並綁定 Job ID；若入列／綁定失敗，保留待恢復資料。HTTP 取消不會抹掉已提交的請求。
3. Worker 執行前再次以交易鎖確認 Run 與 Job ID。重複入列只有綁定的執行者能進入同步，Succeeded／Failed 不再重入。
4. 同步使用 Serializable，來源讀取、驗證、關聯異動與 Succeeded 在同一交易提交；任何步驟失敗都回滾，因此不會留下部分更新卻宣告成功。
5. Hangfire 設定三次自動重試。單次失敗記錄受控錯誤摘要後重新拋出，由 Hangfire 決定重試／終止；取消交給背景工作恢復。
6. 狀態 observer 透過容量 256 的 Channel 通知校對，再讀取 Hangfire 目前公開狀態；不直接在 Hangfire 尚未提交的交易內寫業務狀態。每分鐘 recovery 補排超過 60 秒仍未綁定的 Pending，並校對既有非終止 Run，補回遺失或過早通知。

這是以持久化 Run、重試、執行者檢查與定期校對處理兩個儲存系統的一致性；業務 DB 與 Hangfire 入列並非同一個原子交易，也不保證每個工作只投遞一次。單一 Worker 減少本機併行，跨執行個體仍由 SQL 交易鎖保護。

Worker／Web 需常駐；同步 Serializable 交易可能阻擋來源管理寫入與狀態查詢，202 不保證後續輪詢立即返回。功能沒有公開 Hangfire Dashboard，管理入口由 Admin API 提供。

### 6.5 OpenAPI 與驗證方式

[Program.cs](../Program.cs) 使用 AddOpenApi 產生 `/api/` 路由文件，依授權 metadata 加上 Bearer scheme 與 operation security；[GeoJsonSchemaTransformer](../Services/GeoJsonSchemaTransformer.cs) 將 Geometry 顯示為 GeoJSON schema 與範例，避免展開 NTS 內部物件。Schema 描述與真正的輸入 converter 分工，Swagger 本身不會取代伺服器驗證。

可從 repository 根目錄執行既有聚焦檢查：

```bash
dotnet build prjGoHike/prjGoHike.csproj --no-restore
dotnet run --project tests/GoHikeSafeApiTests
dotnet run --project tests/SpatialJoinTests
```

GoHikeSafeApiTests 啟動實際 ASP.NET Core HTTP pipeline，檢查路由、JWT、JSON 驗證、CRUD、GeoJSON、特徵確認、關聯摘要與錯誤回應，資料存取使用替身；另以正式 SQL Server EF provider 檢查 SQL 轉譯。警示即時通知檢查使用實際服務與 SignalR 記錄替身，涵蓋 API／MVC 儲存後通知、失敗隔離、逾時及 HTTP request 取消；MVC 是直接呼叫 action，不是 MVC HTTP／防偽驗收。SpatialJoinTests 包含背景流程與恢復替身測試，真實 SQL／Hangfire 測試需額外設定；未設定 `GOHIKE_SPATIAL_TEST_CONNECTION` 時會明確跳過相關整合測試。

HTTP 替身測試與 ToQueryString 不足以證明真實 geography 運算、外鍵、交易回滾或 Hangfire 儲存正確。實際整合測試的資料庫要求與可能寫入內容，依 [資料庫／Hangfire 測試文件](SpatialJoins_資料庫與Hangfire測試步驟.md) 操作。本次整理文件僅核對原始碼與文件連結，未執行上述程式測試或業務資料庫異動。

警示即時通知功能實作時，後端及測試專案建置成功，GoHikeSafeApiTests 全專案 795 項檢查通過，沒有連線或異動業務資料庫。SignalR 替身測試不代表真實 client 與 Angular／MapLibre 的端到端測試已完成，完整證據範圍見 [警示通知驗證](DisasterAlerts_api.md#驗證與證據範圍)。

## 7. 相較於 AGENTS.md 基礎的技術亮點

| AGENTS.md 的基礎要求 | 本功能的具體延伸 | 解決的問題與說明重點 |
| --- | --- | --- |
| DTO、JSON 與伺服器驗證 | 自訂 GeoJSON converter，先驗原始結構再驗 NTS 拓樸與環方向 | 地理資料合法性不能只靠 Required；還需座標、型別、封閉環與拓樸驗證 |
| EF Core 查詢與關聯表 | SQL Server geography 的距離關聯、多 Segment 最短距離彙總 | 用空間條件找出相近資源，超出單純主外鍵查詢 |
| 避免重複查詢與不必要成本 | 包覆圓粗篩、分階段暫存表、精確距離物化 | 減少複雜圖形 STDistance 次數，同時保留原始門檻判定 |
| POST 執行操作、正確 HTTP 狀態 | 202 + Location／statusUrl + 持久化 Run + 輪詢 | 長時間同步離開 HTTP request 生命週期，接受與完成分開表達 |
| SaveChanges 與持久化狀態 | Hangfire 重試、Pending 補排、observer 與每分鐘校對 | 入列中斷或通知遺失時仍可恢復，不能依賴記憶體旗標 |
| 資源存在／狀態驗證 | Serializable + sp_getapplock + Run／Job ID 執行者檢查 | 防止同時建立同步、重複執行與部分提交；成功狀態與結果一致 |
| Server authoritative／避免 overposting | 回報 DTO 排除可信度與公開旗標；同步只收距離，權重由伺服器產生快照 | 會員送觀察資料，管理狀態與工作結果由伺服器決定 |
| API 契約與只公開必要欄位 | 公開／管理 DTO、獨立指標摘要、GeoJSON OpenAPI schema | 地圖圖形與文字摘要可分開讀取，維持既有明細契約 |
| SaveChanges、受控錯誤與伺服器資料來源 | 儲存成功後發送 SignalR 變更通知，獨立逾時並隔離發送失敗 | 即時通知提醒 client 重讀 API；傳送失敗不把已成功寫入回報成失敗 |
| 適當狀態碼、受控錯誤與測試 | FK 競爭回 409、工作儲存失敗回 503、HTTP pipeline 與 SQL 轉譯檢查 | 區分可處理的衝突與服務失敗，並說清楚替身測試的證據範圍 |

報告或示範時可依「會員回報後不可公開 → Admin 確認 → 公開特徵可見」展示伺服器控制，再依「POST 收到 202 → 輪詢 Run → 成功後 GET 步道指標摘要」展示背景工作。開發原理則用「多 Segment 最短距離、先比較後捨入、只改未評分資料、結果與 Succeeded 同交易」說明正確性，而非僅列出套件名稱。
