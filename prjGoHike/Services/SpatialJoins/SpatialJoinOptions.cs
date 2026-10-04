using System.ComponentModel.DataAnnotations;

namespace prjGoHike.Services.SpatialJoins;

public sealed class SpatialJoinOptions
{
    public const string SectionName = "Hangfire";
    public bool Enabled { get; set; }
    public bool PrepareSchemaIfNecessary { get; set; }

    [Range(5, 1800)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    [Range(5, 1800)]
    public int SyncCommandTimeoutSeconds { get; set; } = 300;

    [Range(100, 60000)]
    public int LockTimeoutMilliseconds { get; set; } = 5000;
}

public static class SpatialJoinRules
{
    public const string JobType = "SpatialJoin";
    public const string LockResource = "GoHike:SpatialJoin";
    public const string RecoveryJobId = "gohike-spatial-join-recovery";

    public static bool IsValidDistance(decimal value) =>
        value is >= 0.01m and <= 10000.00m && ((decimal.GetBits(value)[3] >> 16) & 0xff) <= 2;

    public static bool IsTerminal(string status) => status is "Succeeded" or "Failed";

    public static string? StatusForHangfireState(string? state) => state switch
    {
        "Enqueued" => "Queued",
        "Processing" => "Processing",
        "Scheduled" => "RetryPending",
        "Failed" or "Deleted" or "Missing" or "Succeeded" => "Failed",
        _ => null
    };
}

public sealed class SpatialJoinLockException(int returnCode) : Exception("無法取得空間關聯交易鎖。")
{
    public int ReturnCode { get; } = returnCode;
}

public sealed class SpatialJoinUnavailableException() : Exception("空間關聯背景工作尚未啟用。");
