// import the classes
using AccountService.Models;
using AccountService.Dtos;
using AccountService.Data;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=accounts;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Accounts.Any())
    {
        db.Accounts.AddRange(
            new Account { Id = 1, Owner = "Varnesh", Balance = 100.00m },
            new Account { Id = 2, Owner = "John",    Balance = 250.50m },
            new Account { Id = 3, Owner = "Sarah",   Balance = 500.75m }
        );
        db.SaveChanges();
    }
}

app.MapGet("/accounts", async (AppDbContext db) => await db.Accounts.OrderBy(a => a.Id).ToListAsync());
app.MapGet("/accounts/{id}", async (int id, AppDbContext db) =>
{
    var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
    return account is null ? Results.NotFound("Account not found") : Results.Ok(account);
});

app.MapPost("/accounts/{id}/debit", async (int id, AmountRequest req, AppDbContext db) =>
{
    var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
    if (account is null)
        return Results.NotFound("Account not found");

    if (req.Amount <= 0)
        return Results.BadRequest("Debit must be greater than 0");

    if (account.Balance < req.Amount)
        return Results.BadRequest("Insufficient funds");

    account.Balance -= req.Amount;
    await db.SaveChangesAsync();          // <-- persists the change
    return Results.Ok(account);
});

app.MapPost("/accounts/{id}/credit", async (int id, AmountRequest req, AppDbContext db) =>
{
    var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
    if (account is null)
        return Results.NotFound("Account not found");

    if (req.Amount <= 0)
        return Results.BadRequest("Cannot credit a negative amount");

    account.Balance += req.Amount;
    await db.SaveChangesAsync();
    return Results.Ok(account);
});

app.Run();
