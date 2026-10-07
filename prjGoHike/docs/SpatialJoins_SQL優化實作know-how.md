# 空間關聯 SQL 優化實作 know-how

對應 [Synchronize.sql](../Services/SpatialJoins/Synchronize.sql)。核心做法是先用便宜的空間資訊篩選，再對候選的原始圖形計算距離，最後彙總與寫入。

## 1. 先定位成本

`STDistance` 的成本與兩側圖形的複雜度有關，不能只看 Segment 配對數。這次來源為 515 個步道 Segment、70 個指標 Segment，其中指標圖形最多有 15,018 個座標點。執行中 CPU 時間接近總耗時，且持續停在距離計算語句，支持優先減少昂貴的圖形距離運算。

## 2. 把計算拆成可重用的結果

```text
來源驗證 → 包覆圓 → #SegmentCandidates → #SegmentDistances
        → #PairDistances → #Candidates → 關聯與成功狀態寫入
```

- 先驗證 SRID 4326、有效性、空圖形與允許型別，再計算包覆圓；來源暫存表保留 Segment 主鍵，候選才能準確接回原始 Shape。
- 每個 Segment 的 `EnvelopeCenter()`、`EnvelopeAngle()` 各計算一次。全部配對仍會比較中心點距離，但只有留下的候選才計算複雜圖形的 `STDistance`。
- `CROSS APPLY` 本身不建立可重用的實體結果。先將候選主鍵寫入 `#SegmentCandidates`，再將精確距離寫入 `#SegmentDistances`，讓後續 `MIN`、`COUNT_BIG` 直接讀取已儲存的值。

## 3. 粗篩要放寬，最終門檻維持精確

[包覆角與中心](https://learn.microsoft.com/en-us/sql/t-sql/spatial-geography/envelopeangle-geography-data-type)描述圖形的包覆圓。本實作使用以下保留條件：

```text
R = 包覆角 × PI / 180 × 6500000 公尺
中心距離 <= (步道 R + 指標 R + 距離門檻) × 1.01 + 1 公尺
```

6,500,000 公尺採較寬鬆的 WGS84 角度／距離換算；1% 與 1 公尺另提供近似計算餘裕。[STDistance 本身有近似誤差](https://learn.microsoft.com/en-us/sql/t-sql/spatial-geography/stdistance-geography-data-type)，因此粗篩允許多留候選，避免門檻附近的配對被過早排除。包覆角大於 90 度、包覆資訊或中心距離為 NULL 時，一律保留配對。

最後仍以原始 `Shape.STDistance` 彙總每組步道／指標的最小距離，先比較原始浮點距離，再轉成 `decimal(12,2)`。例如 100.004 公尺在 100 公尺門檻下必須排除，不能先捨入成 100.00 再判定。粗篩半徑與餘裕都不寫入最終距離。

## 4. 保留交易規則，驗證完整成本

未評分關聯才更新／刪除；已評分列包含 0 分完整保留。來源驗證、關聯異動與 `Succeeded` 仍在呼叫端同一個 Serializable 交易內提交，失敗一起回滾；新增暫存表於 SQL 結尾清除。本次沒有變更交易邊界，狀態查詢仍可能被同步交易鎖住。

驗證重點包括門檻內外、捨入邊界、零距離、多 Segment、Polygon 孔洞、跨日期變更線、極區、大範圍圖形、NULL 包覆資訊、空來源與回滾。效能量測需包含驗證與包覆圓成本；這次 100 公尺門檻下，36,050 組配對留下 177 組精確計算，產生 15 筆候選，候選計算約 3.8 秒。此時間不包含正式關聯寫入與提交，也不是固定效能保證。

SQL 是組件的 `EmbeddedResource`，修改後必須重新建置並重啟 API。既有 SQL 暫存表回歸測試可用：

```bash
dotnet build tests/SpatialJoinTests/SpatialJoinTests.csproj --no-restore
dotnet run --project tests/SpatialJoinTests --no-build -- --current-db-temp
```

測試使用既有資料庫連線設定，在暫存表驗證同步流程；其他測試方式見 [資料庫與 Hangfire 測試步驟](SpatialJoins_資料庫與Hangfire測試步驟.md)。
