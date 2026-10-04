# 步道周圍特徵回報 API

基底路徑：`/api/trailfeatures`。JSON 欄位使用 camelCase。
互動文件在開發環境的 `/swagger`，OpenAPI 在 `/openapi/v1.json`。
API 沿用 `GoHikeSafe` 的回應包裝、JWT 與管理員權限，資料直接儲存在既有 `TrailFeatures` 表。

## 功能範圍與前端流程

1. 用 `GET /api/trails` 選擇已發布步道，取得其 `id` 作為 `trailId`。
2. 用 `GET /api/trailfeatures?trailId={id}` 顯示該步道可用特徵及地圖標記。
3. 登入後送出 `POST /api/trailfeatures`，成功顯示「回報成功，待管理員確認」。請使用 POST 回傳的資料；新回報尚無法透過公開明細再次取得。
4. 管理員用 `GET /api/trailfeatures/admin?isAvailable=false` 查看不可用記錄，再用管理明細開啟編輯表單。
5. 管理員以 PUT 儲存完整欄位，確認位置、描述與可信度後將 `isAvailable` 設為 `true`。只有所屬步道也已發布時才會出現在公開 API。
6. 要隱藏或標示已不可用的特徵，PUT 設為 `false`；要永久移除記錄則 DELETE。

`isAvailable=false` 同時涵蓋新回報與後來停用的特徵，**不是獨立審核狀態**，前端應標示「不可用／待確認」，不可推定所有 false 都是新回報。
`reliabilityLevel` 是管理員手動維護的 1～5 分，數字越高代表可信度越高；新回報固定為 1，沒有自動計算或投票。

本版依照現有欄位取捨：

| 支援 | 暫不支援與原因 |
| --- | --- |
| 步道關聯、類型、名稱、點位置、描述、可信度、可用狀態、來源 | 回報者、建立／更新／發現時間：沒有對應欄位 |
| 登入者新增，管理員編輯／刪除，匿名公開查詢 | 我的回報、會員編輯／撤回：沒有回報者外鍵，無法驗證擁有權 |
| 用可用狀態控制公開 | 獨立審核狀態、審核原因與歷程、通知：沒有對應欄位 |
| 一次回報建立一筆特徵記錄 | 照片／附件、留言、按讚、投票、多次觀察歷程與自動去重：沒有對應欄位或關聯表 |

位置代表使用者發現的點；本版沒有半徑查詢或距離門檻，也不檢查點是否真的位於步道路線附近。
清單不分頁，按 `featureId` 升冪排列；請優先以 `trailId` 篩選。重複送出 POST 會建立不同記錄；送出中應停用按鈕，網路中斷時不要自動重送。
此版不修改 entity／資料庫 schema，不將缺少的欄位塞入 `dataSource` 或描述。

## 路由與權限

需要登入的 API 使用 `Authorization: Bearer <accessToken>`。Admin 是 JWT 中既有的授權角色。

| 方法 | 路徑 | 權限 | 成功狀態與 data |
| --- | --- | --- | --- |
| GET | `/api/trailfeatures` | 公開 | 200，特徵陣列；只含可用且步道已發布的資料 |
| GET | `/api/trailfeatures/{id}` | 公開 | 200，單筆特徵；不可用／步道未發布／不存在回 404 |
| POST | `/api/trailfeatures` | 已登入，須有有效的使用者 ID claim | 201，新回報，固定不可用 |
| GET | `/api/trailfeatures/admin` | Admin | 200，特徵陣列，包含所有可用狀態與未發布步道 |
| GET | `/api/trailfeatures/admin/{id}` | Admin | 200，單筆完整特徵 |
| PUT | `/api/trailfeatures/{id}` | Admin | 200，更新後完整特徵 |
| DELETE | `/api/trailfeatures/{id}` | Admin | 200，`data` 為空字串，永久刪除 |

`id` 是特徵的 `featureId`，須為大於 0 的整數，與步道 `trailId` 不同。
PUT 以路由 ID 指定目標，本文不需也不使用 `featureId`。

兩種清單都可組合下列 query，未指定的條件不限制結果：

| 參數 | 型別 | 規則 |
| --- | --- | --- |
| `trailId` | integer/int64，可選 | 大於 0；不存在的步道回 200 與空陣列 |
| `featureType` | string，可選 | 非空值須符合下方類型格式，以相等條件查詢；大小寫是否相等取決於既有資料庫 collation，前端請使用一致代碼 |
| `isAvailable` | boolean，可選，**僅管理清單** | `true` 或 `false`；省略則不限制狀態 |

例如 `GET /api/trailfeatures/admin?trailId=12&featureType=WaterSource&isAvailable=false`。

## 新增回報 POST

```http
POST /api/trailfeatures
Authorization: Bearer <accessToken>
Content-Type: application/json
```

```json
{
  "trailId": 12,
  "featureType": "WaterSource",
  "featureName": "路旁山泉",
  "location": { "type": "Point", "coordinates": [121.5, 24.5] },
  "featureDescription": "位於叉路前，水質及飲用安全仍需確認。"
}
```

| 欄位 | 型別 | 規則 |
| --- | --- | --- |
| `trailId` | integer/int64，必填 | 大於 0，且已存在、已發布；不符存在／發布條件回 404 |
| `featureType` | string，必填 | 1～20 個 ASCII 字元，符合 `^[A-Za-z][A-Za-z0-9_-]{0,19}$`，首字母為英文字母，後續可用字母、數字、`_`、`-` |
| `featureName` | string，必填 | 最多 120 字元，不能只有空白，存入時去除頭尾空白 |
| `location` | GeoJSON Geometry，必填 | 本版回報限有效、非空 `Point`，詳見下方座標規則 |
| `featureDescription` | string/null，可選 | 最多 1000 字元，省略／null 存為 null，存入時去除頭尾空白 |

`featureType` 在 schema 中是 varchar(20)，使用 ASCII 代碼避免中文字元轉碼問題。
沒有既有 enum 或類型字典；可使用 `WaterSource`（水源）、`RestArea`（休息點）、`Viewpoint`（景觀點）、`Hazard`（需注意地點）、`Other`（其他）作為前端選項。
這些是建議代碼，API 接受任何符合格式的類型，不檢查 enum。

位置格式：

- WGS 84 經緯度，陣列順序為 **[經度, 緯度]**，可帶第三個數值為高度，例如 `[121.5, 24.5, 1200]`。
- 經度 -180～180、緯度 -90～90，所有數值必須有限；只接受 2 或 3 個數值。
- `type` 大小寫固定為 `Point`。不接受空圖形、LineString、Polygon、MultiPoint、Feature／FeatureCollection 包裝或 `crs`。
- 沿用現有 GeoJSON 原始 JSON 驗證。可選 `bbox` 必須涵蓋座標且維度一致；`bbox` 及一般擴充成員不保存。儲存使用 SRID 4326，不做投影轉換。

POST 不接收可信度、可用狀態或來源；即使本文附上 `featureId`、`reliabilityLevel`、`isAvailable`、`dataSource` 等非 request 欄位，也不會套用。
伺服器固定 `reliabilityLevel=1`、`isAvailable=false`、`dataSource="Member report"`，ID 由資料庫產生。
`dataSource` 是資料來源文字，不代表回報者，也無法用來識別擁有權。
管理員也使用同一個 POST，初始狀態相同，再透過 PUT 維護。

201 回應範例（ID 由伺服器產生）：

```json
{
  "success": true,
  "message": "回報成功，待管理員確認。",
  "data": {
    "featureId": 87,
    "trailId": 12,
    "featureType": "WaterSource",
    "featureName": "路旁山泉",
    "location": { "type": "Point", "coordinates": [121.5, 24.5] },
    "featureDescription": "位於叉路前，水質及飲用安全仍需確認。",
    "reliabilityLevel": 1,
    "isAvailable": false,
    "dataSource": "Member report"
  },
  "errors": null
}
```

## 管理員更新 PUT

```http
PUT /api/trailfeatures/87
Authorization: Bearer <adminAccessToken>
Content-Type: application/json
```

```json
{
  "trailId": 12,
  "featureType": "WaterSource",
  "featureName": "路旁山泉",
  "location": { "type": "Point", "coordinates": [121.5, 24.5] },
  "featureDescription": "現地確認位置，飲用前仍須處理。",
  "reliabilityLevel": 4,
  "isAvailable": true,
  "dataSource": "管理員現地確認"
}
```

PUT 是完整替換，需提供 POST 的所有必填欄位，另加：

| 欄位 | 型別 | 規則 |
| --- | --- | --- |
| `reliabilityLevel` | integer，必填 | 1～5，省略會回 400 |
| `isAvailable` | boolean，必填 | `true` 或 `false`，省略／null 會回 400 |
| `dataSource` | string/null，可選 | 最多 200 字元，存入時去除頭尾空白 |

`featureDescription`、`dataSource` 省略／null 都會清為 null，不保留舊值。
可改變 `trailId`，目標步道必須存在；管理員可指定未發布步道，但該特徵不會公開。
PUT 不增加另一筆記錄，也不保存修改歷程。成功回 200、`message="操作成功"`，`data` 為更新後 DTO。

## 回應 DTO 與空結果

公開／管理／新增／更新共用相同特徵 DTO，欄位就是上述 201 範例的 `data`。
`featureId`、`trailId` 為 integer/int64；`featureDescription`、`dataSource` 可為 null。
`location` 輸出為 GeoJSON，保留輸入的可選高度，不輸出 EF navigation 或 NTS 內部欄位。
既有資料若本來存有其他 geometry，讀取會保留原 geometry；新回報及 PUT 僅接受 Point。

清單無符合資料時：

```json
{ "success": true, "message": "操作成功", "data": [], "errors": null }
```

刪除成功：

```json
{ "success": true, "message": "刪除資料成功！", "data": "", "errors": null }
```

## 錯誤處理

前端先讀 HTTP status，不要只檢查 `success`；錯誤格式沿用專案的兩種行為：

| HTTP | 情況 | 前端處理 |
| --- | --- | --- |
| 400 | 欄位／query／JSON／GeoJSON 無效，或 ID 不大於 0 | 顯示欄位 errors 或 message，保留表單 |
| 401 | 沒有 token、過期／無效 token；POST 的使用者 ID claim 無效 | 提示登入／重新登入；body 可能是空的 |
| 403 | 已登入但非 Admin 呼叫管理、PUT、DELETE | 顯示沒有權限；body 可能是空的 |
| 404 | 明細不存在或被公開條件隱藏；POST 步道不存在／未發布；PUT 目標步道不存在 | 顯示不存在或請重新選擇步道 |
| 409 | 寫入時步道外鍵已變更，或刪除遇到外鍵衝突 | 重新取得資料後再操作 |
| 500 | 資料庫或其他伺服器錯誤 | 顯示受控錯誤訊息；不自動重送 POST |

Controller 回傳的錯誤是 `ApiResponse<object>`，例如：

```json
{ "success": false, "message": "找不到已發布的步道。", "data": null, "errors": null }
```

ASP.NET Core 自動 Model Validation／JSON 解析錯誤是 `ValidationProblemDetails`，不是 ApiResponse；
`errors` 是欄位名稱對應訊息陣列，欄位路徑可能含 `$`，訊息文字與 `type`／`title` 不應作為前端判斷條件。範例節錄：

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "$.location": ["shape.coordinates：經度必須介於 -180～180，緯度必須介於 -90～90。"] }
}
```

## 前端呼叫範例（TypeScript）

```ts
type PointLocation = { type: 'Point'; coordinates: [number, number] | [number, number, number] };
type FeatureReport = {
  trailId: number;
  featureType: string;
  featureName: string;
  location: PointLocation;
  featureDescription?: string | null;
};
type FeatureUpdate = FeatureReport & {
  reliabilityLevel: number;
  isAvailable: boolean;
  dataSource?: string | null;
};

async function reportFeature(apiBase: string, accessToken: string, form: FeatureReport) {
  const response = await fetch(`${apiBase}/api/trailfeatures`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
    body: JSON.stringify(form),
  });
  const text = await response.text();
  const body = text ? JSON.parse(text) : null;
  if (!response.ok) {
    // errors 若為物件可轉成欄位提示；401/403 可能沒有 body。
    throw { status: response.status, message: body?.message ?? body?.title, errors: body?.errors };
  }
  return body.data; // 保存回傳資料，顯示待確認；不要立即呼叫公開明細。
}
```

若前端與 API 同站，`apiBase` 可設為空字串；本機 Angular 既有允許來源為 `http://localhost:4200`。
PUT 使用 `FeatureUpdate`、相同 JSON header 與管理員 token，method 改為 `PUT` 並使用 `/api/trailfeatures/{featureId}`。

## 驗證

```bash
dotnet build prjGoHike/prjGoHike.csproj --no-restore
dotnet run --project tests/GoHikeSafeApiTests --no-restore
```

HTTP 測試涵蓋登入回報、越權／overposting、欄位與 Point 驗證、步道存在／發布、管理更新／刪除、
公開資料隔離與篩選、GeoJSON 回傳及受控儲存錯誤；另檢查正式 EF Core SQL Server 模型的查詢轉譯與 geography 型別。
測試使用資料存取替身，不連線正式資料庫，未驗證真實 SQL Server 的寫入與外鍵競態。
