using ECommerceApi.Models;
using FluentAssertions;

namespace ECommerceApi.UnitTests;

/// <summary>Testes unitários puros da regra de preço final com desconto.</summary>
public class ProductTests
{
    [Theory]
    [InlineData(100, 0, 100)]
    [InlineData(100, 5, 95)]
    [InlineData(100, 10, 90)]
    [InlineData(100, 25, 75)]
    [InlineData(50, 25, 37.5)]
    [InlineData(280, 10, 252)]
    public void FinalPrice_AplicaDescontoCorretamente(decimal price, int discount, decimal expected)
    {
        var product = new Product { Price = price, DiscountPercent = discount };

        product.FinalPrice.Should().Be(expected);
    }

    [Fact]
    public void FinalPrice_SemDesconto_IgualAoPreco()
    {
        var product = new Product { Price = 199.90m, DiscountPercent = 0 };

        product.FinalPrice.Should().Be(199.90m);
    }

    [Fact]
    public void FinalPrice_Arredonda_ParaDuasCasas()
    {
        // 99.99 * 0,90 = 89.991 → 89.99
        var product = new Product { Price = 99.99m, DiscountPercent = 10 };

        product.FinalPrice.Should().Be(89.99m);
    }
}
