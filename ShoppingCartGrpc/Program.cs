using DiscountGrpc.Protos;
using Microsoft.EntityFrameworkCore;
using ShoppingCartGrpc.Data;
using ShoppingCartGrpc.Services;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>
    (o => o.Address = new Uri(builder.Configuration["GrpcConfigs:DiscountUrl"]));
builder.Services.AddScoped<DiscountService>();

//Product - https://localhost:7138
//ShoppingCart - https://localhost:7049
//Discount - https://localhost:7097

builder.Services.AddDbContext<ShoppingCartContext>(options =>
    options.UseInMemoryDatabase("ShoppingCart"));
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opt => {
        opt.Authority = "https://localhost:7085";
        opt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

SeedDatabase();

// Configure the HTTP request pipeline.
app.MapGrpcService<ShoppingCartServices>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();


void SeedDatabase() //can be placed at the very bottom under app.Run()
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var shoppingCartContext = services.GetRequiredService<ShoppingCartContext>();
    ShoppingCartContextSeed.SeedAsync(shoppingCartContext);

}
