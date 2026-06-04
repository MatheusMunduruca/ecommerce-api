using System.Net;
using System.Net.Http.Json;
using ECommerceApi.IntegrationTests.Fixtures;
using ECommerceApi.IntegrationTests.Helpers;
using FluentAssertions;

namespace ECommerceApi.IntegrationTests;

public class AuthTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AuthTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Register_ComDadosValidos_Retorna200ComTokenE1000Gold()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/register", ApiHelpers.NewUser());

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await resp.Content.ReadFromJsonAsync<AuthResult>();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.GoldBalance.Should().Be(1000);
        auth.WelcomeDialogue.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_ComEmailDuplicado_Retorna409()
    {
        var user = ApiHelpers.NewUser("duplicado@teste.com");
        (await _client.PostAsJsonAsync("/api/auth/register", user))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var segunda = await _client.PostAsJsonAsync("/api/auth/register", user);

        segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_Retorna200ComToken()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new { name = "Login OK", email = "login.ok@teste.com", password = "Senha123!" });

        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "login.ok@teste.com", password = "Senha123!" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await resp.Content.ReadFromJsonAsync<AuthResult>();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_ComSenhaErrada_Retorna401()
    {
        await _client.PostAsJsonAsync("/api/auth/register",
            new { name = "Senha Errada", email = "senha.errada@teste.com", password = "Senha123!" });

        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "senha.errada@teste.com", password = "ERRADA" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
