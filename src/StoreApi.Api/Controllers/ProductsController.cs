using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreApi.Api.Common;
using StoreApi.Application.Catalog;
using StoreApi.Application.Common.Models;
using StoreApi.Application.Services;
using StoreApi.Domain.Constants;

namespace StoreApi.Api.Controllers;

/// <summary>Public catalog browsing and admin-only product management.</summary>
public sealed class ProductsController(IProductService productService) : BaseApiController
{
    /// <summary>Lists active products with search, filtering, sorting and pagination.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] ProductQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(await productService.GetPagedAsync(query, cancellationToken));

    /// <summary>Gets a single active product by id.</summary>
    [HttpGet("{id:int}", Name = "GetProductById")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await productService.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates a product.</summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] ProductCreateDto request,
        CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetProductById", new { id = product.Id }, product);
    }

    /// <summary>Updates an existing product, including reactivation.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        int id,
        [FromBody] ProductUpdateDto request,
        CancellationToken cancellationToken) =>
        Ok(await productService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Soft-deletes a product (sets IsActive to false).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        await productService.SoftDeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
