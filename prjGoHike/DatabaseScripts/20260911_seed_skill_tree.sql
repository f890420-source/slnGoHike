/*
  能力樹種子資料：不變更 Schema，可重複執行。
  skill_tags 已有 parent_tag_id 自我參照，可直接表達多層技能樹。
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

/* 已執行過舊版種子資料時，保留既有 ID 與會員關聯，只更新名稱。 */
UPDATE skill_tags SET tag_name = N'植物學家', unlock_condition = N'先解鎖爬山'
WHERE category = N'爬山' AND tag_name = N'步態與節奏';

UPDATE child SET tag_name = N'植物辨識', unlock_condition = N'先解鎖植物學家'
FROM skill_tags child
INNER JOIN skill_tags parent ON parent.tag_id = child.parent_tag_id
WHERE child.category = N'爬山' AND child.tag_name = N'上坡換氣' AND parent.tag_name = N'植物學家';

UPDATE child SET tag_name = N'有毒植物避險', unlock_condition = N'先解鎖植物學家'
FROM skill_tags child
INNER JOIN skill_tags parent ON parent.tag_id = child.parent_tag_id
WHERE child.category = N'爬山' AND child.tag_name = N'下坡緩衝' AND parent.tag_name = N'植物學家';

DECLARE @Roots TABLE (Category nvarchar(20), TagName nvarchar(50), UnlockCondition nvarchar(50));
INSERT INTO @Roots VALUES
(N'野炊', N'野炊', N'點擊開始野炊技能線'),
(N'爬山', N'爬山', N'點擊開始爬山技能線'),
(N'醫療', N'醫療', N'點擊開始醫療技能線');

INSERT INTO skill_tags (category, tag_name, parent_tag_id, unlock_condition)
SELECT r.Category, r.TagName, NULL, r.UnlockCondition
FROM @Roots r
WHERE NOT EXISTS (
    SELECT 1 FROM skill_tags s
    WHERE s.category = r.Category AND s.tag_name = r.TagName
);

DECLARE @Skills TABLE (
    Category nvarchar(20),
    TagName nvarchar(50),
    ParentName nvarchar(50),
    UnlockCondition nvarchar(50),
    Depth int
);

INSERT INTO @Skills VALUES
(N'野炊', N'火源管理', N'野炊', N'先解鎖野炊', 1),
(N'野炊', N'炊具運用', N'野炊', N'先解鎖野炊', 1),
(N'野炊', N'山野料理', N'野炊', N'先解鎖野炊', 1),
(N'爬山', N'植物學家', N'爬山', N'先解鎖爬山', 1),
(N'爬山', N'路線判讀', N'爬山', N'先解鎖爬山', 1),
(N'爬山', N'裝備管理', N'爬山', N'先解鎖爬山', 1),
(N'醫療', N'傷口處理', N'醫療', N'先解鎖醫療', 1),
(N'醫療', N'扭傷處理', N'醫療', N'先解鎖醫療', 1),
(N'醫療', N'緊急應變', N'醫療', N'先解鎖醫療', 1),
(N'野炊', N'防風生火', N'火源管理', N'先解鎖火源管理', 2),
(N'野炊', N'安全滅火', N'火源管理', N'先解鎖火源管理', 2),
(N'野炊', N'爐具檢查', N'炊具運用', N'先解鎖炊具運用', 2),
(N'野炊', N'燃料估算', N'炊具運用', N'先解鎖炊具運用', 2),
(N'野炊', N'一鍋料理', N'山野料理', N'先解鎖山野料理', 2),
(N'野炊', N'食材保存', N'山野料理', N'先解鎖山野料理', 2),
(N'爬山', N'植物辨識', N'植物學家', N'先解鎖植物學家', 2),
(N'爬山', N'有毒植物避險', N'植物學家', N'先解鎖植物學家', 2),
(N'爬山', N'可食植物概論', N'植物學家', N'先解鎖植物學家', 2),
(N'爬山', N'地圖定位', N'路線判讀', N'先解鎖路線判讀', 2),
(N'爬山', N'迷途應變', N'路線判讀', N'先解鎖路線判讀', 2),
(N'爬山', N'背包配重', N'裝備管理', N'先解鎖裝備管理', 2),
(N'爬山', N'天候分層穿著', N'裝備管理', N'先解鎖裝備管理', 2),
(N'醫療', N'清潔包紮', N'傷口處理', N'先解鎖傷口處理', 2),
(N'醫療', N'止血加壓', N'傷口處理', N'先解鎖傷口處理', 2),
(N'醫療', N'固定與冰敷', N'扭傷處理', N'先解鎖扭傷處理', 2),
(N'醫療', N'傷勢評估', N'扭傷處理', N'先解鎖扭傷處理', 2),
(N'醫療', N'低體溫處置', N'緊急應變', N'先解鎖緊急應變', 2),
(N'醫療', N'求援通報', N'緊急應變', N'先解鎖緊急應變', 2);

DECLARE @Depth int = 1;
WHILE @Depth <= 2
BEGIN
    INSERT INTO skill_tags (category, tag_name, parent_tag_id, unlock_condition)
    SELECT x.Category, x.TagName, p.tag_id, x.UnlockCondition
    FROM @Skills x
    INNER JOIN skill_tags p
        ON p.category = x.Category AND p.tag_name = x.ParentName
    WHERE x.Depth = @Depth
      AND NOT EXISTS (
          SELECT 1 FROM skill_tags s
          WHERE s.category = x.Category AND s.tag_name = x.TagName
      );

    SET @Depth += 1;
END;

COMMIT TRANSACTION;
