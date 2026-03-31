using Microsoft.EntityFrameworkCore;
using WatchStoreApi.Models;

namespace WatchStoreApi.Data;

public class ApiDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }

    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {
        
    }
}
