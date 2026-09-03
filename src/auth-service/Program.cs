using AuthService.Data;
using AuthService.Dtos;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=users;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? "dev-only-secret-change-me-min-32-bytes-long!!";

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();


app.MapPost("/auth/register", async (AuthRequest req, AppDbContext db) =>
{
    // If a user with that Username already exists 
    // (await db.Users.AnyAsync(u => u.Username == req.Username)) → Results.Conflict("Username taken").
    if(await db.Users.AnyAsync(u => u.Username == req.Username)){
        return Results.Conflict("Sorry, Username Already Taken");
    }
    
    //build the user
    var user = new User
    {
    Username = req.Username,
    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
    };
    //add the user
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(new {user.Id, user.Username}); // never return the hash here
});

app.MapPost("/auth/login", async (AuthRequest req, AppDbContext db) =>
{
    // Load the user
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
    if(user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)){
        return Results.Unauthorized();
    }
    
     var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
    };
    
    //Generate JWT for the user
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var jwt = new JwtSecurityToken(
        issuer: "auth-service",
        audience: "bank-api",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);
    var token = new JwtSecurityTokenHandler().WriteToken(jwt);
    return Results.Ok(new { token });
});

app.Run();
