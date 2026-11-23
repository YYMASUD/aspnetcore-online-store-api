using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StoreApi.Api.Common;
using StoreApi.Application.Auth;
using StoreApi.Domain.Constants;

namespace StoreApi.Api.Controllers;

/// <summary>Registration and sign-in endpoints issuing JWT access tokens.</summary>
public sealed class AuthController(IAuthService authService) : BaseApiController
{
    /// <summary>Registers a new customer account and returns an access token.</summary>
    /// <response code="201">The account was created.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="409">The email or username is already registered.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterDto request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Authenticates a user and returns an access token.</summary>
    /// <response code="200">Credentials accepted.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The email or password is incorrect.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Placeholder guarded by the Customer role; proves role policies are wired.</summary>
    [HttpGet("me")]
    [Authorize(Roles = UserRoles.Customer + "," + UserRoles.Admin)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult<object> Me() => Ok(new { userId = CurrentUserId, username = User.Identity?.Name });
}
