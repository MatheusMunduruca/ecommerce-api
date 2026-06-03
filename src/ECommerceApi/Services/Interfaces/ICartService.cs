using ECommerceApi.Models;

namespace ECommerceApi.Services.Interfaces;

public interface ICartService
{
    Task<Cart> GetCartByUserIdAsync(int userId);
    Task AddItemAsync(int userId, int productId, int quantity);
    Task UpdateItemAsync(int userId, int productId, int quantity);
    Task RemoveItemAsync(int userId, int productId);
    Task ClearCartAsync(int userId);
}
