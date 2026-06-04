using System.Net;
using System.Net.Http.Json;
using ECommerceApi.BddTests.Support;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace ECommerceApi.BddTests.Steps;

[Binding]
public class ShopSteps
{
    private readonly ScenarioState _state;

    public ShopSteps(ScenarioState state) => _state = state;

    [Given(@"que estou autenticado como viajante")]
    public async Task DadoAutenticadoComoViajante()
        => await _state.RegisterAndAuthenticateAsync();

    [Given(@"que estou autenticado como administrador")]
    public async Task DadoAutenticadoComoAdministrador()
        => await _state.RegisterAndAuthenticateAsync("chefe@adm");

    [When(@"eu adiciono (\d+) unidades de ""(.*)"" a bolsa")]
    public async Task QuandoAdicionoItem(int quantidade, string produto)
    {
        var p = await _state.GetProductAsync(produto);
        _state.LastResponse = await _state.Client.PostAsJsonAsync(
            "/api/cart/items", new { productId = p.Id, quantity = quantidade });
    }

    [When(@"eu selo o pedido")]
    public async Task QuandoSeloOPedido()
        => _state.LastResponse = await _state.Client.PostAsync("/api/orders", null);

    [When(@"eu solicito a renovacao do estoque")]
    public async Task QuandoSolicitoRenovacao()
        => _state.LastResponse = await _state.Client.PostAsync("/api/products/refresh-stock", null);

    [Then(@"o pedido e criado com sucesso")]
    public void EntaoPedidoCriado()
        => _state.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

    [Then(@"o estoque de ""(.*)"" passa a ser (\d+)")]
    public async Task EntaoEstoquePassaASer(string produto, int esperado)
    {
        var p = await _state.GetProductAsync(produto);
        p.StockQuantity.Should().Be(esperado);
    }

    [Then(@"a operacao e recusada")]
    public void EntaoOperacaoRecusada()
        => _state.LastResponse!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    [Then(@"a renovacao e autorizada")]
    public void EntaoRenovacaoAutorizada()
        => _state.LastResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

    [Then(@"a renovacao e negada")]
    public void EntaoRenovacaoNegada()
        => _state.LastResponse!.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
