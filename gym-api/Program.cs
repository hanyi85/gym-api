using gym_api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// =======================
// 資料庫設定
// =======================
builder.Services.AddDbContext<dbFitness2Context>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Fitness2Connection")
    );
});

// =======================
//  CORS 設定（給 Vue 用）
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVue",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173" // Vite
                                   )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// =======================
// Controller & JSON 設定
// =======================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 忽略循環參照（EF 關聯很重要）
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // JSON 深度
        options.JsonSerializerOptions.MaxDepth = 64;

        // 保留 C# 屬性命名（不轉 camelCase）
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// =======================
//  Swagger
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowVue");

app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();

app.Run();
