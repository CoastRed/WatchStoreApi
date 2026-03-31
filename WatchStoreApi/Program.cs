using Microsoft.EntityFrameworkCore;
using WatchStoreApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<ApiDbContext>(options =>
{
    options.UseSqlServer("Data Source=localhost;Initial Catalog=WatchStoreDb;Integrated Security=True;TrustServerCertificate=True;");
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
