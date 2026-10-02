using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;

namespace prjGoHike.Services;

/// <summary>
/// 解鎖會員帳號流程中的一次性成就。成就定義由資料庫管理；缺少定義時不新增資料。
/// </summary>
public sealed class MemberAchievementService(GoHikeDataContext context)
{
    public Task UnlockRegistrationAsync(long userId, CancellationToken cancellationToken = default) =>
        UnlockOnceAsync(userId, "初來乍到", "完成註冊", cancellationToken);

    public Task UnlockFirstLoginAsync(long userId, CancellationToken cancellationToken = default) =>
        UnlockOnceAsync(userId, "山友報到", "登入次數", cancellationToken);

    public Task UnlockFirstProfileUpdateAsync(long userId, CancellationToken cancellationToken = default) =>
        UnlockOnceAsync(userId, "認識彼此", "完成個人資料", cancellationToken);

    private async Task UnlockOnceAsync(
        long userId,
        string name,
        string conditionType,
        CancellationToken cancellationToken)
    {
        // 依成就名稱和條件查找，避免不同資料庫的 achievement_id 不同。
        // 複合主鍵及鎖定檢查確保並行請求也不會重複解鎖。
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.user_achievements (user_id, achievement_id, unlocked_at)
            SELECT {userId}, achievement_id, GETUTCDATE()
            FROM dbo.achievements
            WHERE name = {name}
              AND condition_type = {conditionType}
              AND condition_value = '1'
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.user_achievements WITH (UPDLOCK, HOLDLOCK)
                  WHERE user_id = {userId}
                    AND achievement_id = achievements.achievement_id
              )
            """, cancellationToken);
    }
}
