using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreApi.Api.Common;
using StoreApi.Application.Common.Interfaces;
using StoreApi.Application.Common.Models;
using StoreApi.Application.Orders;
using StoreApi.Application.Services;
using StoreApi.Domain.Constants;
using StoreApi.Domain.Enums;

namespace StoreApi.Api.Controllers;

/// <summary>Order placement, tracking and admin management.</summary>
[Authorize]
public sealed class OrdersController(
    IOrderService orderService,
    ICurrentUserService currentUser) : BaseApiController
{
    private int UserId => currentUser.UserId
                          ?? throw new InvalidOperationException("Order endpoints require an authenticated user.");

    private bool IsAdmin => currentUser.IsAdmin;

    /// <summary>Creates an order from the current customer's cart.</summary>
    [HttpPost("checkout")]
    [Authorize(Roles = UserRoles.Customer)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Checkout(
        [FromBody] CheckoutDto request,
        CancellationToken cancellationToken)
    {
        var order = await orderService.CheckoutAsync(UserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, order);
    }

    /// <summary>Lists the current customer's orders.</summary>
    [HttpGet("my")]
    [Authorize(Roles = UserRoles.Customer)]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetMyOrders(
        [FromQuery] OrderQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(await orderService.GetMyOrdersAsync(UserId, query, cancellationToken));

    /// <summary>Gets a single order by id. Customers can only view their own orders.</summary>
    [HttpGet("{id:int}", Name = "GetOrderById")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await orderService.GetByIdAsync(UserId, id, IsAdmin, cancellationToken));

    /// <summary>Lists all orders (admin only), optionally filtered by status.</summary>
    [HttpGet]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetAllOrders(
        [FromQuery] OrderQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(await orderService.GetAllAsync(query, cancellationToken));

    /// <summary>Updates an order's status (admin only). Only valid transitions are allowed.</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(
        int id,
        [FromBody] UpdateOrderStatusDto request,
        CancellationToken cancellationToken) =>
        Ok(await orderService.UpdateStatusAsync(id, request, cancellationToken));

    /// <summary>Cancels a pending order (customer only, own order).</summary>
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = UserRoles.Customer)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> CancelOrder(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await orderService.CancelAsync(UserId, id, cancellationToken));
}
