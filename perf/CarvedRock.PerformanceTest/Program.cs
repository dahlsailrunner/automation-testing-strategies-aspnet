using IdentityModel.Client;
using NBomber.CSharp;
using NBomber.Data;
using NBomber.Data.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;
using NBomber.Sinks.InfluxDB;

using var httpClient = new HttpClient();

string _globalJwt = string.Empty;
IDataFeed<string> categories = DataFeed.Random(new List<string> { "all", "boots", "equip", "kayak" });
IDataFeed<int> productIds = DataFeed.Random(new List<int> { 1, 2, 3, 4, 5, 6 });
InfluxDBSink _influxDbSink = new();

var listScenario = Scenario.Create("get_product_list", async context =>
{
    var category = categories.GetNextItem(context.ScenarioInfo);
    var request = Http.CreateRequest("GET", $"https://localhost:7213/product?category={category}");                

    return await Http.Send(httpClient, request);
})
.WithoutWarmUp()
.WithLoadSimulations(
    Simulation.RampingInject(rate: 5000,
        interval: TimeSpan.FromSeconds(1),
        during: TimeSpan.FromSeconds(120))
);

var singleScenario = Scenario.Create("get_single_product", async context =>
{
    var id = productIds.GetNextItem(context.ScenarioInfo);
    var request = Http.CreateRequest("GET", $"https://localhost:7213/product/{id}")
                      .WithHeader("Authorization", $"Bearer {_globalJwt}");

    return await Http.Send(httpClient, request);
})
.WithInit(async context =>
{
    var token = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
    {
        Address = "https://demo.duendesoftware.com/connect/token",
        ClientId = "m2m",
        ClientSecret = "secret",
        Scope = "api",
    });
    _globalJwt = token.AccessToken!;
})
.WithoutWarmUp()
.WithLoadSimulations(
    Simulation.Inject(rate: 5000,
        interval: TimeSpan.FromSeconds(1),
        during: TimeSpan.FromSeconds(120))
); 

NBomberRunner
    .RegisterScenarios(listScenario, singleScenario)
    .WithWorkerPlugins(new HttpMetricsPlugin())
    .LoadInfraConfig("infra-config.json")
    .WithReportingInterval(TimeSpan.FromSeconds(5))
    .WithReportingSinks(_influxDbSink)
    .WithTestSuite("CarvedRock")
    .WithTestName("Get_requests")
    .Run();
