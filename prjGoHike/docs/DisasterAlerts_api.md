# 災害警示 API 與 SignalR 即時通知

後端沿用 `/api/disasteralerts` 取得警示資料；API 與 MVC 管理頁成功儲存警示後，透過既有 `/eventHub` 廣播 `AlertsChanged`。通知僅表示資料已變更，前端重新呼叫 API 取得完整清單，再更新 MapLibre 圖層。

## 警示資料來源

```http
GET /api/disasteralerts
```

公開清單不需登入，回傳 `ApiResponse<List<DisAlertDto>>`，JSON 使用 camelCase。清單依 `alertId` 遞增排序，包含 `IsActive=true` 的警示及其 `alertSegments`；無啟用警示時回 `200` 與 `data: []`。

警示 DTO 包含 `alertId`、`alertType`、`alertTitle`、`alertDescription`、`severityLevel`、`effectiveFrom`、`effectiveTo`、`sourceAgency`、`sourceUrl`、`isActive`、`alertSegments`。每段的 `shape` 為 GeoJSON Geometry，座標順序為 `[經度, 緯度]`；警示可以沒有路段。

目前公開查詢只篩選 `IsActive`，**未依現在時間過濾 `EffectiveFrom`／`EffectiveTo`**。HTTP CRUD 路由、Admin 權限、GeoJSON 驗證及回應格式沿用 [API 契約](README_api.md#gohikesafe-crud)；新增與更新本文範例見 [使用方法](GoHikeSafe_使用方法與開發原理.md#3-步道指標與災害警示-crud)。

## 即時通知契約

| 項目 | 契約 |
| --- | --- |
| Hub 路徑 | `/eventHub`，HTTP 開發網址為 `http://localhost:5204/eventHub` |
| 事件名稱 | `AlertsChanged` |
| 訊息參數 | 一個物件，僅包含 `alertId` |
| 發送對象 | `Clients.All`，所有已連線的 EventHub client |
| 接收權限 | 目前 EventHub 沒有套用 `[Authorize]`；API 寫入仍須 Admin |
| 資料重新取得 | `GET /api/disasteralerts` |

Callback 收到的單一參數範例：

```json
{ "alertId": 123 }
```

這個物件沒有 `ApiResponse` 包裝，也不包含警示內容、GeoJSON、異動類型、版本或時間戳記。新增、修改、停用與刪除均使用同一事件；既有 `EventDataChanged` 事件維持原樣。

| 寫入入口 | 發送條件 |
| --- | --- |
| API `POST /api/disasteralerts` | 儲存成功後，使用伺服器產生的 ID |
| API `PUT /api/disasteralerts/{id}` | 儲存成功後；包含一般欄位、路段及 `IsActive` 異動 |
| API `DELETE /api/disasteralerts/{id}` | 刪除成功後，使用被刪除的 ID |
| MVC `AdminDisAlertController.Create / Edit` | 儲存成功後 |
| MVC `AdminDisAlertController.DeleteConfirmed` | 實際找到警示並成功刪除後；查無資料不通知 |

每個成功寫入入口呼叫一次推播服務；這不代表 client 一定收到或已完成重載。API 驗證失敗、權限拒絕、資源不存在、刪除關聯衝突及儲存失敗均不通知。新增停用警示也會通知，但它不會出現在公開清單。

## 前端接收與地圖更新

```text
API / MVC 警示寫入
  → SaveChangesAsync 成功
  → EventHub: AlertsChanged({ alertId })
  → 前端 GET /api/disasteralerts
  → 將 alertSegments.shape 組成 GeoJSON FeatureCollection
  → MapLibre source.setData 更新圖層
```

以下示意接到前端既有 HubConnection 的方式；`reloadActiveAlerts` 應呼叫 API、檢查 HTTP 狀態與 `success`，成功後以完整清單替換地圖資料。前端尚未在本次後端功能中實作。

```typescript
interface AlertsChanged {
  alertId: number;
}

hub.on('AlertsChanged', (_change: AlertsChanged) => {
  void reloadActiveAlerts();
});

hub.onreconnected(() => {
  void reloadActiveAlerts();
});
```

串接順序與資料處理：

1. 先註冊事件與重連 callback，首次進頁面即呼叫 API；即使 Hub 暫時連線失敗，也能顯示 API 資料。
2. Hub 初次連線成功後再重載一次，補齊首次讀取至連線完成間可能漏掉的異動；之後收到事件及重新連線時均重載。
3. 用回傳的完整清單替換目前資料，不要只追加收到的 ID。刪除或停用後，公開單筆 GET 可能回 404；完整清單可同時移除已不可見的警示。
4. 每個 `alertSegments.shape` 可組成一個 Feature，將警示 ID、嚴重度、標題等放入 properties。`shape` 已是 Geometry；沒有路段的警示不會產生地圖 Feature。MapLibre source 存在且地圖樣式已載入後，再呼叫 `setData`。
5. 前端合併短時間內的多個重載通知，並使用 `switchMap` 或請求序號避免舊回應覆蓋新資料。讀取失敗應保留既有圖層並顯示錯誤；有效的 `data: []` 才清空圖層。頁面銷毀時清理 handler 與所屬連線。

SignalR 傳送成功不等同 client 已收妥；離線期間沒有補送紀錄。首次載入與重連重載可恢復目前狀態，仍不能視為可靠訊息佇列。

## 後端實作與部署

[DisasterAlertRealtimeService](../Services/DisasterAlertRealtimeService.cs) 透過建構式注入 `IHubContext<EventHub>` 與 Logger，由 [Program.cs](../Program.cs) 註冊 scoped 服務。API 與 MVC 各寫入入口只在 `SaveChangesAsync` 成功後呼叫 `PublishChangedAsync(alertId)`。

服務使用獨立的 **5 秒取消逾時**，不沿用 HTTP request token。HTTP caller 在儲存完成後中斷，不會取消已保存資料的通知。發送例外或逾時會記錄 Warning、警示 ID 與例外，不重新拋出：API 新增維持 201，更新與刪除維持 200；MVC 維持導向 Index。

本功能不新增 NuGet 套件、資料表、欄位或 migration。SignalR 使用既有 ASP.NET Core 支援，EF Core、SQL Server 空間資料與 GeoJSON 套件均沿用既有依賴。部署需要發布更新後的後端並重啟，不需要為此功能啟用 Hangfire。現有 CORS 開發來源為 `http://localhost:4200`；其他前端來源需依部署環境調整既有 CORS 設定。

直接 SQL 修改或其他未接入此服務的寫入不會觸發通知；沒有生效／到期時間掃描、通知歷史、已讀狀態或可靠補送。本次也未新增警示 Spatial Join，CRUD 不會自動建立 `AlertsTrails`；既有空間同步仍只處理步道與指標。

## 驗證與證據範圍

從 repository 根目錄執行：

```bash
dotnet build prjGoHike/prjGoHike.csproj --no-restore
dotnet run --project tests/GoHikeSafeApiTests
```

[AlertRealtimeChecks](../../tests/GoHikeSafeApiTests/AlertRealtimeChecks.cs) 使用實際推播服務與可記錄／模擬失敗的 `IHubContext<EventHub>` 替身。API 經測試 HTTP pipeline 呼叫，MVC 則直接呼叫 action。檢查涵蓋：

- 成功新增、更新、停用及刪除各通知一次；事件、單一參數、ID 與儲存成功後的時機正確。
- 驗證、授權、資源不存在、關聯衝突及儲存失敗時不通知；MVC 查無資料的刪除也不通知。
- 推播失敗仍保留 API 成功狀態及 MVC 導向結果，並記錄警示 ID 與例外。
- 儲存後取消 HTTP request token 不影響通知；等待中的發送會由獨立 5 秒逾時取消並記錄。

本功能實作時，後端及測試專案建置成功，`GoHikeSafeApiTests` 全專案 **795 項檢查通過**；此數字包含既有 API 與 SQL 轉譯檢查，不是單指警示新增測試。未連線或異動業務資料庫，未驗證真實 SignalR client 收送、MVC HTTP／防偽流程或 Angular／MapLibre 畫面。此次文件更新僅核對程式與文件，未重新執行程式測試。
