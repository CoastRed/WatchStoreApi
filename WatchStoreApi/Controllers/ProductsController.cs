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
public class ProductsController : ControllerBase
{
    private readonly ApiDbContext _dbContext;

    public ProductsController(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet()]
    public async Task<IActionResult> Get([FromQuery] string search, [FromQuery] int? CategoryId, [FromQuery] string material, [FromQuery] string gender, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
    {
        var query = _dbContext.Products.AsQueryable();
        if (string.IsNullOrWhiteSpace(search) == false)
        {
            query = query.Where(s => s.Name.ToLower().Contains(search.ToLower()) || s.Description.ToLower().Contains(search.ToLower()));
        }
        if (string.IsNullOrWhiteSpace(material) == false)
        {
            query = query.Where(s => s.Material.ToLower() == material.ToLower());
        }
        if (string.IsNullOrWhiteSpace(gender) == false)
        {
            query = query.Where(s => s.Gender.ToLower() == gender.ToLower());
        }
        if (minPrice.HasValue)
        {
            query = query.Where(s => s.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(s => s.Price <= maxPrice.Value);
        }
        if (CategoryId.HasValue)
        {
            query = query.Where(s => s.CategoryId == CategoryId.Value);
        }
        var products = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return products == null ? NotFound() : Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(s => s.Id == id);
        return product == null ? NotFound() : Ok(product);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Post([FromForm] Product product)
    {
        if (product == null)
        {
            return BadRequest("Product is null");
        }
        var guid = Guid.NewGuid();
        var filePath = Path.Combine("wwwroot", guid + ".jpg");
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await product.Image.CopyToAsync(stream);
        }
        // 将文件路径保存到数据库中，去掉 "wwwroot//" 前缀
        product.ImageUrl = filePath.Substring(8);
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created);
    }


    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromForm] Product product)
    {
        var existingProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (existingProduct != null)
        {
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.CategoryId = product.CategoryId;
            if (product.Image != null)
            {
                if (!string.IsNullOrWhiteSpace(existingProduct.ImageUrl))
                {
                    var oldFilePath = Path.Combine("wwwroot", existingProduct.ImageUrl);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
                var guid = Guid.NewGuid();
                var filePath = Path.Combine("wwwroot", guid + ".jpg");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await product.Image.CopyToAsync(stream);
                }
                existingProduct.ImageUrl = filePath.Substring(8);
            }
            await _dbContext.SaveChangesAsync();
            return Ok("修改成功");
        }
        return NotFound();
    }


    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existingProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (existingProduct != null)
        {
            if (!string.IsNullOrWhiteSpace(existingProduct.ImageUrl))
            {
                var oldFilePath = Path.Combine("wwwroot", existingProduct.ImageUrl);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }
            _dbContext.Products.Remove(existingProduct);
            await _dbContext.SaveChangesAsync();
            return Ok("删除成功");
        }
        return NotFound();
    }

}
