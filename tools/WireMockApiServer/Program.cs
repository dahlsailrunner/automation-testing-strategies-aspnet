using WireMock.Net.StandAlone;
using WireMock.Settings;

var server = StandAloneApp.Start(new WireMockServerSettings
{
    ReadStaticMappings = true
});

Console.WriteLine("WireMock.Net server listening at {0}", server.Urls[0]);

Console.WriteLine("Press any key to stop the server");
Console.ReadKey();
