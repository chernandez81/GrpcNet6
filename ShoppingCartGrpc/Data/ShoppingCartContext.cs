using Microsoft.EntityFrameworkCore;
using ShoppingCartGrpc.Models;

namespace ShoppingCartGrpc.Data;

public class ShoppingCartContext : DbContext
{
    public ShoppingCartContext(DbContextOptions<ShoppingCartContext> options)
        : base(options) 
    {

    }

    public DbSet<ShoppingCart> ShoppingCart { get; set; }
    public DbSet<ShoppingCartItems> ShoppingCartItem { get; set; }
}
