using LoLChampSelectService;
using PoniLCU;
using static PoniLCU.LeagueClient;

LeagueClient leagueClient = new(credentials.cmd);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(leagueClient);
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
