namespace ECommerceApi.Models;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }   // referencia Users em todo_db (sem FK no BD)

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
