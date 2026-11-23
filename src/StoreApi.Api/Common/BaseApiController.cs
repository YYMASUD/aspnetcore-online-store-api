using Microsoft.AspNetCore.Mvc;

namespace StoreApi.Api.Common;

/// <summary>Shared controller base carrying the route prefix and common helpers.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>Reads the authenticated user's id from the validated principal.</summary>
    /// <exception cref="InvalidOperationException">The principal carries no usable id (a programming error).</exception>
    protected int CurrentUserId =>
        int.TryParse(User.FindFirst("sub")?.Value, out var id)
            ? id
            : throw new InvalidOperationException("The current user has no usable subject claim.");
}
