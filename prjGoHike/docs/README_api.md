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
只收錄 `/api/v1/` 路由，不包含 MVC 頁面。首頁啟動設定維持原樣。

文件在執行時從 API 中繼資料產生，讀取文件不會查詢資料庫；應用程式啟動仍需存在
`ConnectionStrings:GoHikeDataContext` 設定。
Swagger UI 的 **Try it out** 會實際呼叫 API；步道查詢需要有效的 SQL Server 連線與既有資料表。
本機連線可透過 User Secrets 設定，請勿將帳密提交至版本控制：

```bash
dotnet user-secrets set "ConnectionStrings:GoHikeDataContext" "<本機 SQL Server 連線字串>" --project prjGoHike
```

`GET /api/v1/trails` 回傳已發布步道，200 回應為 `ApiResponse<List<TrailPublicDto>>`，
查無資料時 `data` 為空陣列；400／404 文件採 `ApiResponse<object>`。
路段 `geometry` 以 GeoJSON 傳輸，文件包含 LineString 範例。

## 新增 API 的文件標註

在 `APIControllers` 下繼承 `BaseController`，使用 `/api/v1/` 路由與明確的 HTTP 動詞。
加入中文 `EndpointSummary`、`EndpointDescription`，並以 `ProducesResponseType` 標註實際狀態碼
及完整回應包裝型別（例如 `ApiResponse<MyDto>`），避免僅有 `IActionResult` 而缺少模型說明。
可參考 `TrailsController.List`；新增後重新啟動並確認 Swagger UI 顯示正確。

幾何模型的文件由 `GeoJsonSchemaTransformer` 定義；HTTP JSON 設定與既有 MVC 設定
使用相同的 GeoJSON converter，使文件產生器不會展開 NetTopologySuite 內部欄位。
