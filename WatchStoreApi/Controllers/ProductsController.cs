using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApi.Data;
using WatchStoreApi.Models;

namespace WatchStoreApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly ApiDbContext _dbContext;

    public ProductsController(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var products = await _dbContext.Products.ToListAsync();
        if(products == null)
        {
            return NotFound();
        }
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(s => s.Id == id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Product product)
    {
        if(product == null)
        {
            return BadRequest("Product is null");
        }
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] Product product)
    {
        var existingProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if(existingProduct != null)
        {
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            await _dbContext.SaveChangesAsync();
            return Ok("修改成功");
        }
        return NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existingProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (existingProduct != null)
        {
            _dbContext.Products.Remove(existingProduct);
            await _dbContext.SaveChangesAsync();
            return Ok("删除成功");
        }
        return NotFound();
    }

}
