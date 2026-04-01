using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApi.Data;
using WatchStoreApi.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WatchStoreApi.Controllers;


[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ApiDbContext _dbContext;

    public CategoriesController(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var categories = await _dbContext.Categories.ToListAsync();
        return Ok(categories);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Category category)
    {
        if (category == null)
        {
            return BadRequest("Category cannot be null.");
        }
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();
        return Created();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, [FromBody] Category category)
    {
        var existingCategory = await _dbContext.Categories.FindAsync(id);
        if (existingCategory == null)
        {
            return NotFound();
        }

        existingCategory.Name = category.Name;

        await _dbContext.SaveChangesAsync();
        return Ok("Category updated successfully.");
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existingCategory = await _dbContext.Categories.FindAsync(id);
        if (existingCategory == null)
        {
            return NotFound();
        }
        _dbContext.Categories.Remove(existingCategory);
        await _dbContext.SaveChangesAsync();
        return Ok("Category deleted successfully.");
    }
}
