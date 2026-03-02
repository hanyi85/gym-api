using gym_api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using gym_api.Services;

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
//  NewebPay（藍新）設定
// =======================
var np = builder.Configuration.GetSection("NewebPay").Get<NewebPayOptions>();
if (np is null)
{
    throw new Exception("NewebPay 設定不存在，請確認 appsettings.json 是否有 NewebPay 區塊");
}

builder.Services.AddSingleton(np);
builder.Services.AddScoped<NewebPayService>();

// =======================
//  Swagger
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseStaticFiles();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowVue");

app.UseAuthorization();

//圖片
app.UseStaticFiles();


app.MapControllers();

app.Run();
