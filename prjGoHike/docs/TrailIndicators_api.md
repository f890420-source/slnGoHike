# 指定步道的關聯指標 API

新增公開 `GET /api/trails/{id}/indicators`，供 Angular 在步道頁單獨載入關聯指標。既有 `GET /api/trails/{id}` 仍回傳 `TrailPublicDto`；清單與明細的既有欄位不變。

採用獨立 endpoint 的原因是步道明細包含路段 GeoJSON，指標摘要可以分開載入／刷新，且不用為既有 Client 更換 DTO。新 DTO 僅回傳步道 ID、有無可見關聯與文字／數值摘要；需要指標圖形時，可再呼叫既有 `GET /api/indicators/{id}`。

## 呼叫與回應

不需登入，範例：

```http
GET /api/trails/12/indicators
```

```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    "trailId": 12,
    "hasIndicators": true,
    "indicators": [
      {
        "indicatorId": 3,
        "indicatorName": "落石風險",
        "indicatorType": "Risk",
        "indicatorLevel": 3,
        "distanceMeters": 12.34,
        "evaluatedScore": null
      }
    ]
  }
}
```

Client 應判斷 HTTP 狀態及 success／data。

| 欄位 | 意義 |
| --- | --- |
| trailId | 指定的已發布步道 ID |
| hasIndicators | 目前回傳的啟用指標清單是否非空，由清單計算 |
| indicators | 依 indicatorId 遞增排序的關聯摘要，無關聯時為 `[]` |
| indicatorId／indicatorName／indicatorType | 指標 ID、名稱、分類 |
| indicatorLevel | 指標目前等級；NULL 表示未提供 |
| distanceMeters | TrailIndicators 儲存的距離（公尺）；NULL 表示未知，不代換為 0 |
| evaluatedScore | TrailIndicators 儲存的評分；NULL 表示未評分，0 是有效已評分值 |

已發布步道沒有可見指標時仍回 `200`：

```json
{
  "success": true,
  "message": "操作成功",
  "data": { "trailId": 12, "hasIndicators": false, "indicators": [] }
}
```

| HTTP 狀態 | 條件 |
| --- | --- |
| 200 | 已發布步道，包括沒有關聯或關聯全為停用指標 |
| 400 | id ≤ 0；非 long 格式的路由不匹配此 endpoint |
| 404 | 步道不存在或未發布；即使管理員呼叫也不公開未發布步道 |
| 500 | 查詢失敗，回受控錯誤並記錄伺服器日誌 |

## 關聯與同步語意

資料取自 `Trail.TrailIndicators → TrailIndicator.Indicator` 導覽屬性，使用 EF `AsNoTracking` 與 DTO 投影後一次 materialize，不讀取路段／Shape，也不將 Entity 或循環導覽屬性交給 JSON serializer。此 GET 不寫入資料、不觸發 Hangfire、不重新計算距離。

只公開已發布步道的啟用指標，包含未評分候選及已評分關聯（含 0 分），沒有 `EvaluatedScore IS NULL` 的限制。這與管理員 [SpatialJoins 候選查詢](SpatialJoins_api.md#後台查詢) 只列未評分資料的用途不同。

Spatial Join 會更新 TrailIndicators；此 API 顯示目前已持久化的關聯。現有 schema 沒有關聯來源旗標或 RunId，無法區分手動／歷史關聯，也無法聲稱每列都是最近一次成功 Run 產生。已評分列在同步時受到保護，可能不符合最新門檻；這裡仍會顯示其啟用指標。

`hasIndicators=false` 只表示目前沒有可見關聯，**不代表同步已完成或沒有風險**。尚未同步、同步失敗、來源尚未維護或所有指標停用，都可能得到空清單。Admin 可用 SpatialJoins API 查詢 Run 狀態，資料同步操作見 [Hangfire／資料庫測試步驟](SpatialJoins_資料庫與Hangfire測試步驟.md)。

同步成功後，Angular 重新呼叫此 GET 即可顯示新的持久化結果。指標停用後立即從公開清單排除，無須先刪除其 TrailIndicators 關聯。

## Angular 串接範例

```typescript
interface TrailRelatedIndicator {
  indicatorId: number;
  indicatorName: string;
  indicatorType: string;
  indicatorLevel: number | null;
  distanceMeters: number | null;
  evaluatedScore: number | null;
}

interface TrailIndicators {
  trailId: number;
  hasIndicators: boolean;
  indicators: TrailRelatedIndicator[];
}

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: string[] | null;
}

// http 為既有 HttpClient，apiBaseUrl 為 API 伺服器網址。
function getTrailIndicators(trailId: number) {
  return http.get<ApiResponse<TrailIndicators>>(
    `${apiBaseUrl}/api/trails/${trailId}/indicators`
  );
}
```

Component 透過既有 async pipe 或 subscribe 訂閱回傳的 Observable。空清單可顯示「目前沒有已關聯的啟用指標」。距離與評分使用明確的 `value === null` 判斷，避免以 truthy 判斷漏掉 0；讀取失敗不可當作空清單。此 endpoint 回傳單一步道的完整指標摘要，不分頁。

## 精簡自動驗證與後續人工測試

本次沿用既有測試 host，新增精簡的 HTTP／DTO 檢查及正式 SQL Server provider 的查詢轉譯檢查：

```bash
dotnet build prjGoHike/prjGoHike.csproj
dotnet run --project tests/GoHikeSafeApiTests
```

自動檢查涵蓋匿名存取、指定步道隔離、啟用篩選、順序、未評分／0 分／非 0 分、未知距離／等級、空清單、未發布／不存在／非法 ID、受控查詢錯誤、GET 不儲存資料、既有步道明細契約與 OpenAPI。資料存取使用替身；另對**正式查詢本身**執行 `ToQueryString`，核對導覽關聯、篩選及不查路段／圖形。

本次 build 通過（既有 66 個警告、0 錯誤），API 測試共 428 項通過，本次新增 28 項檢查。未連線或異動業務資料庫，未啟動真實 Hangfire Worker。

以下實際資料庫／Hangfire 與 Angular 人工測試留作文件，本輪不執行：

1. 使用已完成同步且已發布的步道，先唯讀核對 TrailIndicators JOIN Indicators（限定 IsActive=1）的 ID、距離與評分，再以匿名 HTTP 呼叫新 endpoint，逐筆核對結果。來源目前缺少 Segment 時，先按既有資料維護流程處理，不能將 Failed 視為成功同步。
2. 依 [Hangfire／資料庫測試步驟](SpatialJoins_資料庫與Hangfire測試步驟.md) 建立成功 Run，完成後重新讀取 endpoint，確認未評分候選反映新增／更新／移除，已評分（包含 0）仍存在。Run 失敗時應維持原關聯結果。
3. 在選定的測試資料切換 Indicator.IsActive，核對公開清單與 hasIndicators；切換 Trail.IsPublished，核對 404。測試後恢復原狀；這些是實際寫入，不屬於唯讀驗收。
4. 使用存在的已發布步道與空關聯、全停用關聯各驗證一次 `200 + false + []`，並確認 Angular 不將其呈現為「沒有風險」或「同步成功」。
5. Angular 確認 loading／empty／error 分開顯示、NULL 顯示未評分或未知、0 不被隱藏；切換步道時取消或忽略舊請求，避免前一條步道的晚到回應覆蓋畫面。
6. 對較多關聯的步道量測回應大小與時間，透過 SQL 日誌確認查詢不產生逐指標的 N+1 存取；本次未做正式資料量效能測試。
