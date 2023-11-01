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
        _leagueClient.Subscribe("/lol-gameflow/v1/gameflow-phase", GameFlowPhase);
    }
    void GameFlowPhase(OnWebsocketEventArgs obj)
    {
        _logger.LogInformation($"Gameflow Phase: {obj.Data}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var data = await _leagueClient.Request(LeagueClient.requestMethod.GET, "/lol-champ-select/v1/summoners/0");
                using (JsonDocument document = JsonDocument.Parse(data))
                {
                    var summonerId = document.RootElement.GetProperty("summonerId");
                    var summonerData = await _leagueClient.Request(LeagueClient.requestMethod.GET, $"/lol-summoner/v1/summoners/{summonerId}");
                    var summonerName = JsonDocument.Parse(summonerData).RootElement.GetProperty("displayName").GetString();
                    _logger.LogInformation($"Current Summoner Id: {summonerId}, Summoner Name: {summonerName}");
                }
            }
            catch (Exception)
            {
                //The code will search for a valid Id
            }
            await Task.Delay(3000, stoppingToken);
        }
    }
}
