using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ECommerceApi.BddTests.Support;

/// <summary>
/// Estado compartilhado entre os passos de um cenário (injetado pelo SpecFlow,
/// um por cenário). Cria a API em memória e o HttpClient, e guarda a última resposta.
/// </summary>
public class ScenarioState : IDisposable
{
    public BddWebApplicationFactory Factory { get; } = new();
    public HttpClient Client { get; }
    public HttpResponseMessage? LastResponse { get; set; }

    public ScenarioState() => Client = Factory.CreateClient();

    public async Task RegisterAndAuthenticateAsync(string? email = null)
    {
        var resp = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "BDD User",
            email = email ?? $"bdd_{Guid.NewGuid():N}@teste.com",
            password = "Senha123!"
        });
        resp.EnsureSuccessStatusCode();
        var auth = await resp.Content.ReadFromJsonAsync<BddAuth>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    public async Task<BddProduct> GetProductAsync(string name)
    {
        var products = await Client.GetFromJsonAsync<List<BddProduct>>("/api/products");
        return products!.Single(p => p.Name == name);
    }

    public void Dispose()
    {
        LastResponse?.Dispose();
        Client.Dispose();
        Factory.Dispose();
    }

    public record BddAuth(string Token);
    public record BddProduct(int Id, string Name, int StockQuantity);
}
