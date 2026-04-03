using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApi.Data;
using WatchStoreApi.Models;

namespace WatchStoreApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ShoppingCartItemsController : ControllerBase
{
    private readonly ApiDbContext _dbContext;

    public ShoppingCartItemsController(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userEmail = User.Claims.FirstOrDefault(s => s.Type == ClaimTypes.Email)?.Value;
        var user = await _dbContext.Users.FirstOrDefaultAsync(s => s.Email == userEmail);
        if (user == null)
        {
            return Unauthorized();
        }
        var cartItems = await _dbContext.ShoppingCartItems.Where(s => s.UserId == user.Id).Include(s => s.Product).Select(s => new
        {
            Id = s.Id,
            Qty = s.Qty,
            UnitPrice = s.UnitPrice,
            TotalAmount = s.TotalAmount,
            ProductId = s.ProductId,
            ProductName = s.Product.Name,
            ImageUrl = s.Product.ImageUrl
        }).ToListAsync();
        return Ok(cartItems);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Post(ShoppingCartItem shoppingCartItem)
    {
        var existCartItem = await _dbContext.ShoppingCartItems.FirstOrDefaultAsync(s => s.ProductId == shoppingCartItem.ProductId && s.UserId == shoppingCartItem.UserId);
        if (existCartItem != null)
        {
            existCartItem.Qty += shoppingCartItem.Qty;
            existCartItem.TotalAmount = existCartItem.UnitPrice * existCartItem.Qty;
        }
        else
        {
            var productRecord = await _dbContext.Products.FindAsync(shoppingCartItem.ProductId);

            var newCartItem = new ShoppingCartItem
            {
                ProductId = shoppingCartItem.ProductId,
                UserId = shoppingCartItem.UserId,
                Qty = shoppingCartItem.Qty,
                UnitPrice = productRecord.Price,
                TotalAmount = productRecord.Price * shoppingCartItem.Qty
            };
            await _dbContext.ShoppingCartItems.AddAsync(newCartItem);
        }
        await _dbContext.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created);
    }


    [HttpPut]
    public async Task<IActionResult> Put([FromQuery] int productId, [FromQuery] string action)
    {
        var userEmail = User.Claims.FirstOrDefault(s => s.Type == ClaimTypes.Email)?.Value;
        var user = await _dbContext.Users.FirstOrDefaultAsync(s => s.Email == userEmail);
        if (user == null)
        {
            return Unauthorized();
        }
        var cartItem = await _dbContext.ShoppingCartItems.FirstOrDefaultAsync(s => s.UserId == user.Id && s.ProductId == productId);
        if (cartItem == null)
        {
            return NotFound("Cart item not found.");
        }
        switch (action.ToLower())
        {
            case "increase":
                cartItem.Qty += 1;
                cartItem.TotalAmount = cartItem.UnitPrice * cartItem.Qty;
                break;
            case "decrease":
                if (cartItem.Qty > 1)
                {
                    cartItem.Qty -= 1;
                }
                else
                {
                    _dbContext.ShoppingCartItems.Remove(cartItem);
                }
                break;
            default:
                return BadRequest("Invalid action");
        }
        cartItem.TotalAmount = cartItem.UnitPrice * cartItem.Qty;
        await _dbContext.SaveChangesAsync();
        return Ok("Cart item updated successfully.");
    }

    [HttpDelete("remove/{productId}")]
    public async Task<IActionResult> Delete(int productId)
    {
        var userEmail = User.Claims.FirstOrDefault(s => s.Type == ClaimTypes.Email)?.Value;
        var user = await _dbContext.Users.FirstOrDefaultAsync(s => s.Email == userEmail);
        if (user == null)
        {
            return Unauthorized();
        }
        var cartItem = await _dbContext.ShoppingCartItems.FirstOrDefaultAsync(s => s.UserId == user.Id && s.ProductId == productId);
        if (cartItem == null)
        {
            return NotFound("Cart item not found.");
        }
        _dbContext.ShoppingCartItems.Remove(cartItem);
        await _dbContext.SaveChangesAsync();
        return Ok("Cart item delete successfully.");
    }

}
