using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace prjGoHike.APIControllers.User;

public abstract class UserApiControllerBase : ControllerBase
{
    protected bool TryGetCurrentUserId(out long userId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
