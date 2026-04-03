using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApi.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WatchStoreApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly ApiDbContext _dbContext;

    public AdminController(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    [HttpGet("revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRevenue([FromQuery] string range = "monthly")
    {
        var now = DateTime.UtcNow;
        var result = new List<object>();
        for (int i = 6; i >= 0; i--)
        {
            DateTime start, end;
            string period;
            if (range == "yearly")
            {
                var year = now.Year - i;
                start = new DateTime(year, 1, 1);
                end = start.AddYears(1);
                period = year.ToString();
            }
            else if (range == "monthly")
            {
                var date = now.AddMonths(-i);
                start = new DateTime(date.Year, date.Month, 1);
                end = start.AddMonths(1);
                period = $"{date.Year}-{date.Month: D2}";
            }
            else if (range == "weekly")
            {
                var weekStart = now.Date.AddDays(-7 * i);
                start = weekStart.AddDays(-(int)weekStart.DayOfWeek);
                end = start.AddDays(7);
                period = start.ToString("yyyy-MM-dd");
            }
            else
            {
                return BadRequest("Use range = yearly . monthly or weekly");
            }
            decimal revenue = await _dbContext.Orders.Where(o => o.Status == "completed" && o.OrderDate >= start && o.OrderDate < end)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            result.Add(new { Revenue = revenue, Period = period });
        }
        return Ok(result);
    }


    // api/admin/dashboard
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminDashboardSummary()
    {
        var totalOrders = await _dbContext.Orders.CountAsync();
        var pendingOrders = await _dbContext.Orders.CountAsync(o => o.Status == "Pending");
        var totalRevenue = await _dbContext.Orders.Where(o => o.Status == "completed").SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        var totalProducts = await _dbContext.Products.CountAsync();
        var totalCategories = await _dbContext.Categories.CountAsync();
        var result = new
        {
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            TotalRevenue = totalRevenue,
            TotalProducts = totalProducts,
            TotalCategories = totalCategories
        };
        return Ok(result);
    }

}
