using Microsoft.EntityFrameworkCore;
using NexusLogix.Application.Common.Interfaces;
using NexusLogix.Infrastructure.Persistence;
using NexusLogix.Infrastructure.Interfaces;
using NexusLogix.Infrastructure.Services;
//For JWT Authentication <BEGIN>
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
//For JWT Authentication <END>


var builder = WebApplication.CreateBuilder(args);

// 1. Fetch the connection string securely from our local User Secrets
var connectionString = builder.Configuration.GetConnectionString("SqlDatabase");

// 2. Register ApplicationDbContext with Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Bind the IApplicationDbContext interface to our actual DbContext class
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// Required to allow HttpTenantProvider to inspect incoming web headers
builder.Services.AddHttpContextAccessor();

// Register our Multi-Tenant provider service
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();

// For CORS Middleware <BEGIN>
// // 4. Configure a CORS policy so our React app can talk to this API locally
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("CorsPolicy", policy =>
//     {
//         policy.WithOrigins("http://localhost:5173") // Standard local React/Vite port
//               .AllowAnyHeader()
//               .AllowAnyMethod();
//     });
// });
// For CORS Middleware <END>

// Fetch JWT configuration <BEGIN>
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

builder.Services.AddAuthorization();
// Fetch JWT configuration <END>

var app = builder.Build();

// 5. Turn on CORS and Redirection
//app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


//Token API <Begin>
app.MapPost("/auth/token", (string tenantId) =>
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "DemoUser"),
            new Claim("tenant_id", tenantId) // Embeds tenant context securely
        }),
        Expires = DateTime.UtcNow.AddHours(2),
        Issuer = jwtSettings["Issuer"],
        Audience = jwtSettings["Audience"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { token = tokenHandler.WriteToken(token) });
});
//Token API <End>

//Login API <BEGIN>
app.MapPost("/auth/login", (LoginRequest request) =>
{
    // Simple credential check mapping users to tenants
    string? tenantId = (request.Username, request.Password) switch
    {
        ("user1", "user1") => "Tenant01",
        ("user2", "user2") => "Tenant02",
        _ => null
    };

    if (tenantId == null)
    {
        return Results.Unauthorized();
    }

    // Mint the JWT token containing the corresponding tenant claim
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim("tenant_id", tenantId)
        }),
        Expires = DateTime.UtcNow.AddHours(4),
        Issuer = jwtSettings["Issuer"],
        Audience = jwtSettings["Audience"],
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature
        )
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);

    return Results.Ok(new
    {
        token = tokenHandler.WriteToken(token),
        username = request.Username,
        tenantId = tenantId
    });
});
//Login API <END>

// 6. Define GET Endpoint: Fetch all drivers
app.MapGet("/drivers", async (IApplicationDbContext context) =>
{
    var drivers = await context.Drivers.ToListAsync();
    return Results.Ok(drivers);
}).RequireAuthorization();

// 7. Define POST Endpoint: Register a new driver
app.MapPost("/drivers", async (NexusLogix.Domain.Entities.Driver driver, IApplicationDbContext context) =>
{
    driver.Id = Guid.NewGuid();
    driver.CreatedAt = DateTime.UtcNow;

    context.Drivers.Add(driver);
    await context.SaveChangesAsync(CancellationToken.None);

    return Results.Created($"/drivers/{driver.Id}", driver);
}).RequireAuthorization();

app.Run();