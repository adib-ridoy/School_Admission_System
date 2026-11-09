using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
var connectionString = builder.Configuration["DATABASE_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("SchoolAdmission");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The DATABASE_CONNECTION_STRING environment variable or SchoolAdmission connection string is required.");
}

builder.Services.AddDbContext<SchoolAdmissionDbContext>(options =>
    options.UseNpgsql(connectionString));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.UseCors("Frontend");

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchoolAdmissionDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
