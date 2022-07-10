using ShoppingCartGrpc.Models;

namespace ShoppingCartGrpc.Data;

public class ShoppingCartContextSeed
{
    public static void SeedAsync(ShoppingCartContext shoppingCartContext)
    {
        if (!shoppingCartContext.ShoppingCart.Any())
        {
            var shoppingCarts = new List<ShoppingCart> {
                new ShoppingCart
                {
                    UserName = "swn2",
                    Items = new List<ShoppingCartItems>
                    {
                        new ShoppingCartItems
                        {
                            Quantity = 2,
                            Color = "Black",
                            Price = 699,
                            ProductId = 1,
                            ProductName = "Mi10T"
                        },
                        new ShoppingCartItems
                        {
                            Quantity = 3,
                            Color = "Red",
                            Price = 899,
                            ProductId = 2,
                            ProductName = "P40"
                        }
                    }
                }
            };

            shoppingCartContext.ShoppingCart.AddRange(shoppingCarts);
            shoppingCartContext.SaveChanges();
        }
    }
}
