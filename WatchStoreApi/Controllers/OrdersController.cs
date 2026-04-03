using System.Net;
using System.Net.NetworkInformation;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApi.Data;
using WatchStoreApi.Models;

namespace WatchStoreApi.Controllers;

[Authorize()]
[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly ApiDbContext _dbContext;

    public OrdersController(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAllOrdersForAdmin(
        int pageNumber = 1,
        int pageSize = 10,
        [FromQuery] string status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string user = null)
    {
        var query = _dbContext.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);
        if (!string.IsNullOrEmpty(user))
            query = query.Where(o => o.User.Name.Contains(user) || o.User.Email.Contains(user));
        var orders = await query.OrderByDescending(o => o.OrderDate).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(o => new
            {
                Id = o.Id,
                UserName = o.User.Name,
                OrderdDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                Address = o.Address,
            }).ToListAsync();
        return Ok(orders);
    }

    // api/orders/admin/pending
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/pending")]
    public async Task<IActionResult> GetPendingOrdersForAdmin(int pagerNumber = 1, int pageSize = 10)
    {
        var pendingOrders = await _dbContext.Orders.Where(o => o.Status == "Pending")
        .OrderByDescending(o => o.OrderDate).Skip((pagerNumber - 1) * pageSize).Take(pageSize)
        .Select(o => new
        {
            Id = o.Id,
            UserName = o.User.Name,
            OrderdDate = o.OrderDate,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            Address = o.Address,
        }).ToListAsync();
        return Ok(pendingOrders);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{orderId:int}/admindetails")]
    public async Task<IActionResult> GetOrderDetailsForAdmin(int orderId)
    {
        var orderDetails = await _dbContext.OrderDetails.Where(od => od.OrderId == orderId)
        .Include(od => od.Product).Select(od => new
        {
            Id = od.Id,
            Qty = od.Qty,
            TotalAmount = od.TotalAmount,
            ProductId = od.ProductId,
            ProductName = od.Product.Name,
            ProductImageUrl = od.Product.ImageUrl,
            ProductPrice = od.Product.Price,
        }).ToListAsync();
        return Ok(orderDetails);
    }

    // PuT:/api/orders/123/status?orderStatus=completed
    [HttpPut("{orderId:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromQuery] string orderStatus)
    {
        if (orderStatus != "Completed" && orderStatus != "Cancelled")
            return BadRequest("Invalid status . Allowed values completed or cancelled");
        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order == null)
            return NotFound("Order with Id not found...");
        order.Status = orderStatus;
        await _dbContext.SaveChangesAsync();
        return Ok("Order status updated...");
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetOrdersForCurrentUser()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value; var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            return Unauthorized();
        }
        var
        userOrders = await _dbContext.Orders.Where(o => o.UserId == user.Id)
        .OrderByDescending(o => o.OrderDate).Select(o => new
        {
            o.Id,
            o.TotalAmount,
            o.OrderDate
        }).ToListAsync();
        return Ok(userOrders);
    }

    [HttpGet("{orderId:int}/details")]
    public async Task<IActionResult> GetOrderDetailsForUser(int orderId)
    {
        var orderDetails = await _dbContext.OrderDetails
        .Where(o => o.OrderId == orderId).Include(o => o.Product)
        .Select(o => new
        {
            Id = o.Id,
            Qty = o.Qty,
            TotalAmount = o.TotalAmount,
            ProductName = o.Product.Name,
            ProductImageUrl = o.Product.ImageUrl,
            ProductPrice = o.Product.Price,
        }).ToListAsync();
        return Ok(orderDetails);
    }


    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        await _dbContext.AddAsync(order);
        await _dbContext.SaveChangesAsync();
        var cartItems = await _dbContext.ShoppingCartItems.Where(s => s.UserId == order.UserId).ToListAsync();
        order.OrderDate = DateTime.UtcNow;
        order.Status = "Pending";
        order.TotalAmount = cartItems.Sum(c => c.TotalAmount);
        foreach (var carItem in cartItems)
        {
            var orderDetail = new OrderDetail
            {
                UnitPrice = carItem.UnitPrice,
                TotalAmount = carItem.TotalAmount,
                Qty = carItem.Qty,
                ProductId = carItem.ProductId,
                OrderId = order.Id
            };
            await _dbContext.AddAsync(orderDetail);
        }
        await _dbContext.SaveChangesAsync();
        _dbContext.ShoppingCartItems.RemoveRange(cartItems);
        await _dbContext.SaveChangesAsync();
        return Ok("订单创建成功");
    }

}
