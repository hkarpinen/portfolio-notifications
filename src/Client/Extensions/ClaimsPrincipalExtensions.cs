using System.Security.Claims;

namespace Client.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("sub")
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("Missing user identifier claim.");

        if (!Guid.TryParse(raw, out var userId))
            throw new InvalidOperationException("User identifier claim is not a valid GUID.");

        return userId;
    }
}
