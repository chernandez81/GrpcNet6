using Microsoft.EntityFrameworkCore;
using ProductGrpc.Dara;
using ProductGrpc.Services;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc(opt =>
{
    opt.EnableDetailedErrors = true;
});
builder.Services.AddDbContext<ProductContext>(options =>
    options.UseInMemoryDatabase("Products")
);
builder.Services.AddAutoMapper(typeof(Program));

var app = builder.Build();

SeedDatabase();

// Configure the HTTP request pipeline.
app.MapGrpcService<ProductService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();

void SeedDatabase() //can be placed at the very bottom under app.Run()
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var productsContext = services.GetRequiredService<ProductContext>();
    ProductContextSeed.SeedAsync(productsContext);
    
}
