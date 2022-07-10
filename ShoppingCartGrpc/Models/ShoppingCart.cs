namespace ShoppingCartGrpc.Models;

public class ShoppingCart
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public List<ShoppingCartItems> Items { get; set; } = new List<ShoppingCartItems>();

    public ShoppingCart()
    {

    }

    public ShoppingCart(string userName)
    {

    }

    public float TotalPrice
    {
        get
        {
            float totalprice = 0;
            foreach(var item in Items)
            {
                totalprice += item.Price * item.Quantity;
            }

            return totalprice;
        }
    }
}
