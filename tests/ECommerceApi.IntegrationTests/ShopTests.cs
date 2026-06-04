using System.Net;
using System.Net.Http.Json;
using ECommerceApi.IntegrationTests.Fixtures;
using ECommerceApi.IntegrationTests.Helpers;
using FluentAssertions;

namespace ECommerceApi.IntegrationTests;

public class ShopTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<ProductDto> GetProductByNameAsync(HttpClient client, string name)
    {
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        return products!.Single(p => p.Name == name);
    }

    private async Task<HttpClient> AuthedClientAsync(string? email = null)
    {
        var client = _factory.CreateClient();
        var auth = await ApiHelpers.RegisterAsync(client, email);
        ApiHelpers.Authenticate(client, auth.Token);
        return client;
    }

    // ---------- Catálogo ----------

    [Fact]
    public async Task GetProducts_SemAutenticacao_RetornaListaSemeada()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/products");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var products = await resp.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCategories_RetornaCategoriasSemeadas()
    {
        var client = _factory.CreateClient();

        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        categories.Should().Contain(c => c.Name == "Pocoes")
                  .And.Contain(c => c.Name == "Ingredientes");
    }

    [Fact]
    public async Task GetProducts_ComDesconto_ExpoePrecoFinalReduzido()
    {
        var client = _factory.CreateClient();

        var produto = await GetProductByNameAsync(client, "Pocao com Desconto");

        produto.Price.Should().Be(100);
        produto.DiscountPercent.Should().Be(25);
        produto.FinalPrice.Should().Be(75);
    }

    // ---------- Carrinho ----------

    [Fact]
    public async Task GetCart_SemToken_Retorna401()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/cart");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdicionarItemComDesconto_CalculaTotalComPrecoFinal()
    {
        var client = await AuthedClientAsync();
        var produto = await GetProductByNameAsync(client, "Pocao com Desconto");

        await client.PostAsJsonAsync("/api/cart/items", new { productId = produto.Id, quantity = 1 });
        var cart = await client.GetFromJsonAsync<CartDto>("/api/cart");

        cart!.Items.Should().HaveCount(1);
        cart.Items[0].UnitPrice.Should().Be(75);
        cart.Total.Should().Be(75);
    }

    [Fact]
    public async Task AdicionarAlemDoEstoque_Retorna400()
    {
        var client = await AuthedClientAsync();
        var erva = await GetProductByNameAsync(client, "Erva da Lua"); // estoque = 2

        var resp = await client.PostAsJsonAsync("/api/cart/items", new { productId = erva.Id, quantity = 5 });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- Pedido / checkout ----------

    [Fact]
    public async Task Checkout_CriaPedidoEDebitaEstoque()
    {
        var client = await AuthedClientAsync();
        var pocao = await GetProductByNameAsync(client, "Pocao de Cura Menor"); // estoque 10, preço 50

        await client.PostAsJsonAsync("/api/cart/items", new { productId = pocao.Id, quantity = 2 });

        var resp = await client.PostAsync("/api/orders", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await resp.Content.ReadFromJsonAsync<OrderDto>();
        order!.TotalAmount.Should().Be(100); // 50 * 2
        order.Items.Should().ContainSingle(i => i.ProductId == pocao.Id && i.Quantity == 2);

        // estoque debitado: 10 - 2 = 8
        var depois = await GetProductByNameAsync(client, "Pocao de Cura Menor");
        depois.StockQuantity.Should().Be(8);
    }

    [Fact]
    public async Task Checkout_ComCarrinhoVazio_Retorna400()
    {
        var client = await AuthedClientAsync();

        var resp = await client.PostAsync("/api/orders", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- Refresh administrativo ----------

    [Fact]
    public async Task RefreshStock_ContaNormal_Retorna403()
    {
        var client = await AuthedClientAsync("comum@teste.com");

        var resp = await client.PostAsync("/api/products/refresh-stock", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RefreshStock_ContaAdmin_Retorna200()
    {
        var client = await AuthedClientAsync("chefe@adm");

        var resp = await client.PostAsync("/api/products/refresh-stock", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
