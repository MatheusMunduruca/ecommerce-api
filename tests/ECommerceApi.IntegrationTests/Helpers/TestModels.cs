namespace ECommerceApi.IntegrationTests.Helpers;

// DTOs mínimos para desserializar as respostas da API (JSON camelCase).
public record AuthResult(string Token, string Name, string Email, decimal GoldBalance, string? WelcomeDialogue);
public record ProductDto(int Id, string Name, decimal Price, int StockQuantity, int DiscountPercent, decimal FinalPrice, string CategoryName);
public record CategoryDto(int Id, string Name);
public record CartItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);
public record CartDto(int Id, int UserId, List<CartItemDto> Items, decimal Total);
public record OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);
public record OrderDto(int Id, int UserId, string Status, decimal TotalAmount, List<OrderItemDto> Items);
