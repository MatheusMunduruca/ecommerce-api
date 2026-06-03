using ECommerceApi.Models;

namespace ECommerceApi.Services.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderFromCartAsync(int userId);
    Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId);
    Task<Order?> GetOrderByIdAsync(int orderId, int userId);
    Task CancelOrderAsync(int orderId, int userId);
}
