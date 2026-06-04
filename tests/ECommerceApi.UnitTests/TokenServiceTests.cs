using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECommerceApi.Models;
using ECommerceApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ECommerceApi.UnitTests;

/// <summary>Testes do TokenService usando Moq para a configuração (Jwt).</summary>
public class TokenServiceTests
{
    private static TokenService CreateService()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Jwt:Key"]).Returns("chave-de-teste-bem-longa-para-hmacsha256-1234567890");
        config.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        config.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        return new TokenService(config.Object);
    }

    [Fact]
    public void GenerateToken_RetornaJwtValido_ComTresPartes()
    {
        var service = CreateService();
        var user = new User { Id = 7, Name = "Rudolf", Email = "rudolf@adm" };

        var token = service.GenerateToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3); // header.payload.signature
    }

    [Fact]
    public void GenerateToken_IncluiClaimsDeId_Email_E_Nome()
    {
        var service = CreateService();
        var user = new User { Id = 42, Name = "Matheus", Email = "matheus@adm" };

        var token = service.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "42");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "matheus@adm");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "Matheus");
    }

    [Fact]
    public void GenerateToken_UsaIssuerEAudienceDaConfiguracao()
    {
        var service = CreateService();
        var user = new User { Id = 1, Name = "Teste", Email = "t@adm" };

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateToken(user));

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
    }
}
