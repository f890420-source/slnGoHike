# GoHike API 開發文件

專案使用 .NET 10，需先安裝 .NET 10 SDK。

```bash
dotnet restore slnGoHike.slnx
dotnet build slnGoHike.slnx --no-restore
dotnet run --project prjGoHike --launch-profile https
```

HTTPS 開發環境需信任本機開發憑證（`dotnet dev-certs https --trust`）。
亦可使用 `--launch-profile http`，透過 `http://localhost:5204` 開啟相同路徑。

## API 文件

- Swagger UI：<https://localhost:7285/swagger>
- OpenAPI JSON：<https://localhost:7285/openapi/v1.json>

兩個入口僅在 `Development` 環境啟用。文件名稱為 `v1`，標題為 `GoHike API`，
只收錄 `/api/` 路由，不包含 MVC 頁面。首頁啟動設定維持原樣。

文件在執行時從 API 中繼資料產生，讀取文件不會查詢資料庫；應用程式啟動仍需存在
`ConnectionStrings:GoHikeDataContext` 設定。
Swagger UI 的 **Try it out** 會實際呼叫 API；步道查詢需要有效的 SQL Server 連線與既有資料表。
本機連線可透過 User Secrets 設定，請勿將帳密提交至版本控制：

```bash
dotnet user-secrets set "ConnectionStrings:GoHikeDataContext" "<本機 SQL Server 連線字串>" --project prjGoHike
```

`GET /api/trails` 回傳已發布步道，200 回應為 `ApiResponse<List<TrailPublicDto>>`，
查無資料時 `data` 為空陣列；400／404 文件採 `ApiResponse<object>`。
路段 `geometry` 以 GeoJSON 傳輸，文件包含 LineString 範例。

## 新增 API 的文件標註

在 `APIControllers` 下繼承 `BaseController`，使用 `/api/` 路由與明確的 HTTP 動詞。
加入中文 `EndpointSummary`、`EndpointDescription`，並以 `ProducesResponseType` 標註實際狀態碼
及完整回應包裝型別（例如 `ApiResponse<MyDto>`），避免僅有 `IActionResult` 而缺少模型說明。
可參考 `TrailsApiController.List`；新增後重新啟動並確認 Swagger UI 顯示正確。

幾何模型的文件由 `GeoJsonSchemaTransformer` 定義；HTTP JSON 設定與既有 MVC 設定
使用相同的 GeoJSON converter，使文件產生器不會展開 NetTopologySuite 內部欄位。

## GoHikeSafe CRUD

三個資源的基底路徑：

| 模組 | 路徑 | 寫入／管理回應 DTO | PUT 本文 ID |
| --- | --- | --- | --- |
| Trails | `/api/trails` | `TrailAdminDto` | `id` |
| Indicators | `/api/indicators` | `IndicatorDto` | `id` |
| DisasterAlerts | `/api/disasteralerts` | `DisAlertDto` | `alertId` |

每個資源均提供以下端點：

| 方法與相對路徑 | 功能 | 權限 |
| --- | --- | --- |
| `GET /` | 已發布步道／啟用指標／啟用警示清單 | 公開 |
| `GET /{id}` | 指定已發布／啟用資源 | 公開 |
| `GET /admin` | 包含未發布／停用資源的完整管理清單 | Admin |
| `GET /admin/{id}` | 指定資源完整管理資料 | Admin |
| `POST /` | 新增資源與路段 | Admin |
| `PUT /{id}` | 更新資源與路段 | Admin |
| `DELETE /{id}` | 刪除資源及其路段 | Admin |

管理與寫入端點使用既有 JWT Bearer 的 `Admin` 角色。PUT 路由 ID 與本文 ID 必須一致且大於 0。
新增回傳 201；查詢、更新、刪除成功回傳 200，沿用 `ApiResponse<T>` 包裝。
無效欄位／ID／GeoJSON／警示起訖時間回傳 400；不存在或未公開的資源回傳 404。
有其他關聯資料的資源回傳 409，需先解除關聯再刪除；未登入與非管理員分別回傳 401、403。

路段欄位分別為 `trailSegDtos`、`indicatorSegments`、`alertSegments`，其中 `shape` 使用 GeoJSON。
新增步道須至少一段有效 LineString 或 MultiLineString；指標與警示可不提供路段。
更新時省略路段或傳入 null 會保留原路段；提供陣列會替換原路段，指標與警示可用空陣列清除路段。
路段 ID 由伺服器產生，替換路段會產生新 ID。警示區域存放於 `alertSegments`，與資料庫模型一致。

步道管理 DTO 包含 `estimatedHours`。指標管理 DTO 包含權重、等級、來源、描述與啟用狀態。
警示 DTO 包含類型、標題、描述、嚴重等級、有效起訖時間、來源與啟用狀態。
公開指標的清單與明細維持既有 `IndicatorTextInfoDto`、`IndicatorPublicDto` 格式。

例如新增指標：

```http
POST /api/indicators
Authorization: Bearer <管理員 token>
Content-Type: application/json
```

```json
{
  "indicatorName": "落石風險",
  "indicatorType": "Risk",
  "weight": 1.5,
  "indicatorLevel": 3,
  "indicatorDescription": "落石注意區域",
  "dataSource": "人工調查",
  "isActive": true,
  "indicatorSegments": [
    {
      "segmentName": "注意點",
      "shape": { "type": "Point", "coordinates": [121.0, 24.0] }
    }
  ]
}
```

## GoHikeSafe 驗證

```bash
dotnet build prjGoHike/prjGoHike.csproj --no-restore
dotnet run --project tests/GoHikeSafeApiTests
```

測試以真正的 ASP.NET Core HTTP pipeline 驗證三個模組的 CRUD、JWT 授權、JSON 欄位驗證、
公開／管理查詢、路段保留／替換／清除、刪除衝突與受控錯誤回應。
資料存取使用測試替身；另使用正式 EF Core SQL Server 模型檢查查詢轉譯。
不連線到應用程式資料庫，未涵蓋真實 SQL Server 寫入及外鍵約束的整合測試。
