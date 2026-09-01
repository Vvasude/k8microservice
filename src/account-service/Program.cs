var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var accounts = new List <Account>{

    new Account(1, "Varnesh", 100.00M),
    new Account(2, "John", 250.50M),
    new Account(3, "Sarah", 500.75M)

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


app.Run();
public record Account(int Id, string Owner, decimal Balance);