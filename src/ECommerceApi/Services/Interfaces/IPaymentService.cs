using ECommerceApi.Models;

namespace ECommerceApi.Services.Interfaces;

public interface IPaymentService
{
    Task<Payment> ProcessPaymentAsync(int orderId, int userId);
    Task<Payment?> GetPaymentByOrderIdAsync(int orderId, int userId);
}
