using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreApi.Api.Common;
using StoreApi.Application.Cart;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Application.Services;
using StoreApi.Domain.Constants;

namespace StoreApi.Api.Controllers;

/// <summary>Shopping cart endpoints, scoped to the authenticated customer.</summary>
[Authorize(Roles = UserRoles.Customer)]
public sealed class CartController(
    ICartService cartService,
    ICurrentUserService currentUser) : BaseApiController
{
    private int UserId => currentUser.UserId
                          ?? throw new InvalidOperationException("Cart endpoints require an authenticated customer.");

    /// <summary>Returns the current user's cart with totals.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken) =>
        Ok(await cartService.GetAsync(UserId, cancellationToken));

    /// <summary>Adds a product to the cart; merges into an existing line for the same product.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItemDto>> AddToCart(
        [FromBody] AddToCartDto request,
        CancellationToken cancellationToken)
    {
        var item = await cartService.AddAsync(UserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, item);
    }

    /// <summary>Changes the quantity of a cart line; zero removes it.</summary>
    [HttpPut("{itemId:int}")]
    [ProducesResponseType(typeof(CartItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartItemDto>> UpdateCartQuantity(
        int itemId,
        [FromBody] UpdateCartDto request,
        CancellationToken cancellationToken) =>
        Ok(await cartService.UpdateQuantityAsync(UserId, itemId, request, cancellationToken));

    /// <summary>Removes a single item from the cart.</summary>
    [HttpDelete("{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCartItem(
        int itemId,
        CancellationToken cancellationToken)
    {
        await cartService.RemoveAsync(UserId, itemId, cancellationToken);
        return NoContent();
    }

    /// <summary>Empties the cart.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        await cartService.ClearAsync(UserId, cancellationToken);
        return NoContent();
    }
}
