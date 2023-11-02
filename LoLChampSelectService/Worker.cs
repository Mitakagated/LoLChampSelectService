using System.Collections;
using System.Text.Json;
using PoniLCU;

namespace LoLChampSelectService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly LeagueClient _leagueClient;
    private string _gameFlowState = string.Empty;

    public Worker(ILogger<Worker> logger, LeagueClient leagueClient)
    {
        _logger = logger;
        _leagueClient = leagueClient;
        _leagueClient.Subscribe("/lol-gameflow/v1/gameflow-phase", GameFlowPhase);
    }
    void GameFlowPhase(OnWebsocketEventArgs obj)
    {
        _logger.LogInformation($"Gameflow Phase: {obj.Data}");
        _gameFlowState = obj.Data;
    }

    async Task<List<string>> SummonerNamesList()
    {
        var nameList = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var data = await _leagueClient.Request(LeagueClient.requestMethod.GET, $"/lol-champ-select/v1/summoners/{i}");
                using (JsonDocument document = JsonDocument.Parse(data))
                {
                    var summonerId = document.RootElement.GetProperty("championName").GetString();
                    /*var summonerData = await _leagueClient.Request(LeagueClient.requestMethod.GET, $"/lol-summoner/v1/summoners/{summonerId}");
                    var summonerName = JsonDocument.Parse(summonerData).RootElement.GetProperty("displayName").GetString();*/
                    nameList.Add(summonerId);
                    _logger.LogInformation("Current Summoner Id: {SummonerId}, Summoner Name: name", summonerId);
                }
            }
            catch (Exception)
            {
                //The code will search continuously for a valid Id
            }
            await Task.Delay(250);
        }
        return nameList;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_gameFlowState is "ChampSelect")
            {
                var summonerNames = await SummonerNamesList();
                _logger.LogInformation("Names: {names}", string.Join(",", summonerNames));
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
            else
            {
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
