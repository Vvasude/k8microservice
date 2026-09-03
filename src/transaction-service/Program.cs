using TransactionService.Clients;
using TransactionService.Dtos;
using TransactionService.Models;
using TransactionService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=transactions;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
var accountServiceUrl = builder.Configuration["AccountService:BaseUrl"] ?? "http://localhost:5111";
builder.Services.AddHttpClient<AccountClient>(client =>
{
    client.BaseAddress = new Uri(accountServiceUrl);
});
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
app.MapPost("/transfers", async (TransferRequest req, AccountClient accounts, AppDbContext db) =>
{
    if (req.Amount <= 0)
    {
        return Results.BadRequest("Amount must be positive");
    }

    if (req.FromAccountId == req.ToAccountId) 
    {
        return Results.BadRequest("Cannot Transfer Between Same Account");
    }

    var from = await accounts.GetAccountAsync(req.FromAccountId);
    var to   = await accounts.GetAccountAsync(req.ToAccountId);
    
    if (from is null || to is null) 
    { 
        return Results.NotFound("...");
    }

    if (from.Balance < req.Amount) 
    {
        return Results.BadRequest("Insufficient funds"); 
    }

    var debited = await accounts.DebitAsync(req.FromAccountId, req.Amount);
    
    if (!debited) 
    {
        return Results.BadRequest("Debit failed");
    }

    var credited = await accounts.CreditAsync(req.ToAccountId, req.Amount);
    
    if (!credited) 
    {
        await accounts.CreditAsync(req.FromAccountId, req.Amount); 
        return Results.Problem("Transfer failed, funds restored");
    }
    db.Transactions.Add(new Transaction
{
    FromAccountId = req.FromAccountId,
    ToAccountId = req.ToAccountId,
    Amount = req.Amount,
    Status = "completed",
    CreatedAt = DateTime.UtcNow
});
    await db.SaveChangesAsync();
    return Results.Ok(new { req.FromAccountId, req.ToAccountId, req.Amount, status = "completed" });
});

app.MapGet("/transfers", async (AppDbContext db) =>
    await db.Transactions.OrderByDescending(t => t.CreatedAt).ToListAsync());

app.Run();
