namespace CarvedRock.EndToEnd.Tests.Utilities;
public class BaseTest : PageTest
{
	public string WebUrl { get; private set; } = null!;

	public BaseTest()
	{
		WebUrl = Environment.GetEnvironmentVariable("WEB_URL") ?? "https://localhost:9999/";
	}
}
