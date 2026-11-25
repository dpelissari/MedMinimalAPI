namespace PiraRemumeApi.Servicos;

public class SincronismoHostedService : BackgroundService
{
    private static readonly TimeSpan IntervaloVarredura = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan EsperaInicial = TimeSpan.FromSeconds(45);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SincronismoHostedService> _logger;

    public SincronismoHostedService(IServiceScopeFactory scopes, ILogger<SincronismoHostedService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(EsperaInicial, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var sincronismo = scope.ServiceProvider.GetRequiredService<SincronismoFonteService>();
                await sincronismo.VerificarSeDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no job de sincronismo da REMUME.");
            }

            try
            {
                await Task.Delay(IntervaloVarredura, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
