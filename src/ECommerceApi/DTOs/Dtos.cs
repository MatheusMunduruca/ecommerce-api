using ECommerceApi.Enums;

namespace ECommerceApi.DTOs;

// Auth
public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Name, string Email, decimal GoldBalance, string? WelcomeDialogue = null);
public record DeductGoldRequest(decimal Amount);

// Category
public record CategoryResponse(int Id, string Name);
public record CreateCategoryRequest(string Name);

// Product
public record ProductResponse(int Id, string Name, string Description, decimal Price, int StockQuantity, int CategoryId, string CategoryName);
public record CreateProductRequest(string Name, string Description, decimal Price, int StockQuantity, int CategoryId);
public record UpdateProductRequest(string Name, string Description, decimal Price, int StockQuantity, int CategoryId);

// Cart
public record CartResponse(int Id, int UserId, List<CartItemResponse> Items, decimal Total);
public record CartItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);
public record AddCartItemRequest(int ProductId, int Quantity);
public record UpdateCartItemRequest(int Quantity);

// Order
public record OrderResponse(int Id, int UserId, string Status, decimal TotalAmount, DateTime CreatedAt, List<OrderItemResponse> Items);
public record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

// Payment
public record PaymentResponse(int Id, int OrderId, string Status, DateTime CreatedAt);
