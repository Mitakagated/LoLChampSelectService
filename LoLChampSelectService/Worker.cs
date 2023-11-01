using System.Text.Json;
using PoniLCU;

namespace LoLChampSelectService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly LeagueClient _leagueClient;

    public Worker(ILogger<Worker> logger, LeagueClient leagueClient)
    {
        _logger = logger;
        _leagueClient = leagueClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var data = await _leagueClient.Request(LeagueClient.requestMethod.GET, "/lol-summoner/v1/current-summoner");
            using (JsonDocument document = JsonDocument.Parse(data))
            {
                var JsonDeserialized = document.RootElement.GetProperty("displayName").ToString();
                _logger.LogInformation($"Current Summoner Name: {JsonDeserialized}");
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
