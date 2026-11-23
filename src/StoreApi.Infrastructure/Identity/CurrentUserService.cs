using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Domain.Constants;

namespace StoreApi.Infrastructure.Identity;

/// <summary>Reads the caller identity from <see cref="HttpContext.User"/>.</summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue("sub");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirstValue("name");

    public bool IsAdmin => Principal?.IsInRole(UserRoles.Admin) ?? false;
}
