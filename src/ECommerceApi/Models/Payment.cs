using ECommerceApi.Enums;

namespace ECommerceApi.Models;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}
