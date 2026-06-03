using AutoMapper;
using ECommerceApi.DTOs;
using ECommerceApi.Models;

namespace ECommerceApi.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Category, CategoryResponse>();

        CreateMap<Product, ProductResponse>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name));

        CreateMap<CartItem, CartItemResponse>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.Product.Price))
            .ForMember(d => d.Subtotal, o => o.MapFrom(s => s.Product.Price * s.Quantity));

        CreateMap<Cart, CartResponse>()
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items))
            .ForMember(d => d.Total, o => o.MapFrom(s => s.Items.Sum(i => i.Product.Price * i.Quantity)));

        CreateMap<OrderItem, OrderItemResponse>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.Subtotal, o => o.MapFrom(s => s.UnitPrice * s.Quantity));

        CreateMap<Order, OrderResponse>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));

        CreateMap<Payment, PaymentResponse>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
    }
}
