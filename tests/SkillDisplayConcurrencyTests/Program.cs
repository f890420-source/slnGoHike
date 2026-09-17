using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using prjGoHike.APIControllers.User;
using prjGoHike.DTO.User;
using prjGoHike.Models;

// Run with: dotnet run --project tests/SkillDisplayConcurrencyTests
// Uses only a disposable LocalDB database, never the application's database.
var databaseName = "SkillDisplayReview_" + Guid.NewGuid().ToString("N");
var masterConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true";
var testConnectionString = new SqlConnectionStringBuilder(masterConnectionString)
{
    InitialCatalog = databaseName
}.ConnectionString;

await using var master = new SqlConnection(masterConnectionString);
await master.OpenAsync();
await Execute(master, $"CREATE DATABASE [{databaseName}]");
try
{
    await using var setup = new SqlConnection(testConnectionString);
    await setup.OpenAsync();
    await Execute(setup, """
        CREATE TABLE dbo.users (user_id bigint NOT NULL PRIMARY KEY);
        CREATE TABLE dbo.user_skill_tags (
            user_id bigint NOT NULL,
            tag_id bigint NOT NULL,
            source varchar(20) NOT NULL,
            is_displayed bit NOT NULL,
            PRIMARY KEY (user_id, tag_id)
        );
        INSERT INTO dbo.users VALUES (1);
        INSERT INTO dbo.user_skill_tags VALUES
            (1, 1, 'test', 1), (1, 2, 'test', 1),
            (1, 3, 'test', 0), (1, 4, 'test', 0),
            (1, 5, 'test', 0), (1, 6, 'test', 0);
        """);

    // Build the EF model before timing concurrent requests.
    await using (var warmup = new TestDataContext(testConnectionString))
        _ = warmup.Model;

    for (var iteration = 0; iteration < 10; iteration++)
    {
        await Execute(setup, "UPDATE dbo.user_skill_tags SET is_displayed = CASE WHEN tag_id <= 2 THEN 1 ELSE 0 END");

        // A request must wait for this user's lock before checking the limit.
        // This also makes the regression deterministic if that lock is removed.
        await using var blocker = new SqlConnection(testConnectionString);
        await blocker.OpenAsync();
        using var blockingTransaction = blocker.BeginTransaction();
        using (var command = blocker.CreateCommand())
        {
            command.Transaction = blockingTransaction;
            command.CommandText = "SELECT user_id FROM dbo.users WITH (UPDLOCK, HOLDLOCK) WHERE user_id = 1";
            await command.ExecuteNonQueryAsync();
        }

        var requests = Enumerable.Range(3, 4)
            .Select(tagId => SetDisplayed(tagId, true)).ToArray();
        await Task.Delay(500);
        var completedBeforeUnlock = requests.Any(request => request.IsCompleted);
        blockingTransaction.Commit();
        var results = await Task.WhenAll(requests);

        Check(!completedBeforeUnlock, "Display requests must wait for the user's transaction lock.");
        Check(results.Count(result => result is NoContentResult) == 1, "Only one of four requests may fill the last display slot.");
        Check(results.Count(result => result is BadRequestObjectResult) == 3, "The other three requests must receive 400.");
        await using var verification = new TestDataContext(testConnectionString);
        Check(await verification.UserSkillTags.CountAsync(x => x.IsDisplayed) == 3, "Displayed skills must never exceed three.");
        Check(await verification.UserSkillTags.CountAsync() == 6, "All six unlocked skills must remain available.");
    }

    await using (var verification = new TestDataContext(testConnectionString))
    {
        var displayedId = await verification.UserSkillTags.Where(x => x.IsDisplayed).Select(x => x.TagId).FirstAsync();
        var hiddenId = await verification.UserSkillTags.Where(x => !x.IsDisplayed).Select(x => x.TagId).FirstAsync();
        Check(await SetDisplayed(displayedId, true) is NoContentResult, "Showing an already displayed skill must be idempotent.");
        Check(await SetDisplayed(displayedId, false) is NoContentResult, "Hiding a skill must free a slot.");
        Check(await SetDisplayed(hiddenId, true) is NoContentResult, "Another unlocked skill can replace the hidden skill.");
        Check(await SetDisplayed(999, true) is NotFoundObjectResult, "A skill that is not unlocked must return 404.");
        Check(await verification.UserSkillTags.CountAsync(x => x.IsDisplayed) == 3, "Replacing a displayed skill must preserve the limit.");
    }

    Console.WriteLine("PASS: 10 concurrent batches, three-skill limit, six unlocked skills, repeated requests, replacement and missing skills.");
}
finally
{
    SqlConnection.ClearAllPools();
    await Execute(master, $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]");
}

async Task<ActionResult> SetDisplayed(long tagId, bool displayed)
{
    await using var context = new TestDataContext(testConnectionString);
    var controller = new UserSkillTagsController(context)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "1")], "test"))
            }
        }
    };
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
    return await controller.SetDisplayed(tagId, new SetSkillTagDisplayRequest { IsDisplayed = displayed }, timeout.Token);
}

static async Task Execute(SqlConnection connection, string sql)
{
    using var command = connection.CreateCommand();
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync();
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class TestDataContext(string connectionString) : GoHikeDataContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlServer(connectionString, options => options.UseNetTopologySuite());
}
