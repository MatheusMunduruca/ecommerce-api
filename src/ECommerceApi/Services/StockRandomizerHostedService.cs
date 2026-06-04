namespace ECommerceApi.Services;

/// <summary>
/// Enquanto a API está rodando, verifica a cada 10 minutos se a janela de 6h
/// virou e re-sorteia o estoque. A primeira verificação ocorre logo na
/// inicialização (cobre o caso de a API ter ficado fechada na virada da janela).
/// </summary>
public class StockRandomizerHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<StockRandomizerHostedService> _logger;

    public StockRandomizerHostedService(
        IServiceProvider services,
        ILogger<StockRandomizerHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var stock = scope.ServiceProvider.GetRequiredService<RudolfStockService>();
                await stock.EnsureCurrentAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao randomizar o estoque de Rudolf.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
