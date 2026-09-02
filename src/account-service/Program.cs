// import the classes
using AccountService.Models;
using AccountService.Dtos;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var accounts = new List <Account>{

     new Account
    {
        Id = 1,
        Owner = "Varnesh",
        Balance = 100.00m
    },
    new Account
    {
        Id = 2,
        Owner = "John",
        Balance = 250.50m
    },
    new Account
    {
        Id = 3,
        Owner = "Sarah",
        Balance = 500.75m
    }
};

app.MapGet("/accounts", ()=> accounts);
app.MapGet("/accounts/{id}", (int id) =>
{
    var a = accounts.FirstOrDefault(a => a.Id == id);

    if (a == null)
    {
        return Results.NotFound("Account not found");
    }

    return Results.Ok(a);
});

app.MapPost("/accounts/{id}/debit", (int id, AmountRequest req) =>{
    // create a 404 if account doesnt exist
    var activeAccount = accounts.FirstOrDefault(activeAccount => activeAccount.Id == id);
    
    if (activeAccount == null){
        return Results.NotFound("Account not Found");
    }
    // create a 400 if amount is less than 0 since we cannot deposit negative amounts
    if (req.Amount <= 0 ){
        return Results.BadRequest("Debit must be greater than 0");
    }
    if (activeAccount.Balance < req.Amount){
        return Results.BadRequest("Insufficient funds");
    }
    activeAccount.Balance -= req.Amount;
    return Results.Ok(activeAccount);

});

app.MapPost("/accounts/{id}/credit", (int id, AmountRequest req) =>{
    var accountCredit = accounts.FirstOrDefault(accountCredit => accountCredit.Id == id);
    if (accountCredit == null){
        return Results.NotFound("Account not found");
    }
    if (req.Amount <=0){
        return Results.BadRequest("Cannot deposit negative amount");
    }
    accountCredit.Balance += req.Amount;
    return Results.Ok(accountCredit);
});

app.Run();
