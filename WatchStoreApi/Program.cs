using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WatchStoreApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<ApiDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});


//添加JWT认证服务
// 1. 注册【认证服务】，指定默认使用 JWT Bearer 认证方案
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>  // 2. 配置 JWT Bearer 认证的具体参数
        {
            // 3. 核心：JWT 令牌验证规则（校验Token是否合法）
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // ✅ 验证【签发者】（Issuer）：是否是我信任的服务器签发的Token
                ValidateIssuer = true,
                // ✅ 验证【接收者】（Audience）：是否是发给当前程序使用的Token
                ValidateAudience = true,
                // ✅ 验证【过期时间】：Token过期了直接拒绝访问
                ValidateLifetime = true,
                // ✅ 验证【签名密钥】：防止Token被篡改、伪造
                ValidateIssuerSigningKey = true,

                // 🔑 从配置文件读取：合法的【签发者】名称
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                // 🔑 从配置文件读取：合法的【接收者】名称
                ValidAudience = builder.Configuration["Jwt:Audience"],
                // 🔑 从配置文件读取密钥，生成【签名秘钥】（验签用，最核心）
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };
        });

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseStaticFiles();

//添加认证中间件
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
