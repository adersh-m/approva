using System.Data;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using ExpenseApp.API.Application.Interfaces;
using ExpenseApp.API.Application.Services;
using ExpenseApp.API.Infrastructure.Cache;
using ExpenseApp.API.Infrastructure.Persistence;
using ExpenseApp.API.Infrastructure.ReadRepositories;
using ExpenseApp.API.Middleware;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
               .Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));

builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddScoped<IDbConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IExpenseReadRepository, ExpenseReadRepository>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();

builder.Services.AddSingleton(new ServiceBusClient(
    builder.Configuration.GetConnectionString("ServiceBus")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
