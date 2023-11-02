using System.Diagnostics;
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
        _gameFlowState = obj.Data;
    }

    async Task<List<string>> SummonerNamesListAsync()
    {
        var nameList = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            try
            {
                //Going through the ally team slots in the champion select
                var data = await _leagueClient.Request(LeagueClient.requestMethod.GET, $"/lol-champ-select/v1/summoners/{i}");
                using (JsonDocument document = JsonDocument.Parse(data))
                {
                    var summonerId = document.RootElement.GetProperty("summonerId").GetString();
                    var summonerData = await _leagueClient.Request(LeagueClient.requestMethod.GET, $"/lol-summoner/v1/summoners/{summonerId}");
                    var summonerName = JsonDocument.Parse(summonerData).RootElement.GetProperty("displayName").GetString();
                    nameList.Add(summonerId);
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

    async Task OpenBrowserAsync(List<string> summonerNames)
    {
        var region = string.Empty;
        var data = await _leagueClient.Request(LeagueClient.requestMethod.GET, $"/riotclient/region-locale");
        using (JsonDocument document = JsonDocument.Parse(data))
        {
            region = document.RootElement.GetProperty("region").GetString();
        }
        switch (region)
        {
            case "EUNE":
                region = "eun1";
                break;
            case "EUW":
                region = "euw1";
                break;
            case "NA":
                region = "na1";
                break;
            case "KR":
                region = "kr";
                break;
            case "BR":
                region = "br1";
                break;
            case "JP":
                region = "jp1";
                break;
        }
        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://u.gg/multisearch?summoners={string.Join(",", summonerNames)}&region={region}",
            UseShellExecute = true
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_gameFlowState is "ChampSelect")
            {
                var summonerNames = await SummonerNamesListAsync();
                await OpenBrowserAsync(summonerNames);
                //Giving 3 minutes delay to wait for the state to change
                await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
            }
            else
            {
                //Checks every 3 seconds if the Gameflow state is changed to "ChampSelect"
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
