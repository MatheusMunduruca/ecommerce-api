using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bogus;

namespace ECommerceApi.IntegrationTests.Helpers;

/// <summary>Auxiliares para registrar usuários e autenticar o HttpClient nos testes.</summary>
public static class ApiHelpers
{
    private static readonly Faker Faker = new("pt_BR");

    public static object NewUser(string? email = null) => new
    {
        name = Faker.Name.FullName(),
        email = email ?? $"user_{Guid.NewGuid():N}@teste.com",
        password = "Senha123!"
    };

    /// <summary>Registra um usuário e devolve a resposta de autenticação (com token).</summary>
    public static async Task<AuthResult> RegisterAsync(HttpClient client, string? email = null)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/register", NewUser(email));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AuthResult>())!;
    }

    /// <summary>Adiciona o token Bearer ao cabeçalho do HttpClient.</summary>
    public static void Authenticate(HttpClient client, string token)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
