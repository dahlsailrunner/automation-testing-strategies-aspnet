namespace CarvedRock.InnerLoop.Tests.Utilities;

public abstract class BaseTest 
{
    public HttpClient Client { get; private set; }

    protected SharedFixture SharedContext { get; }

    protected BaseTest(CustomApiFactory factory)
    {
        SharedContext = factory.SharedFixture;

        Client = factory.CreateClient();
        
        var wireMockProxyUrl = SharedContext.ProxyAndRecordApiCalls(Client.BaseAddress!);
        Client.BaseAddress = new Uri(wireMockProxyUrl);
    }
}
