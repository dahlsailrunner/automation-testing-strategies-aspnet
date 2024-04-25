using CarvedRock.Data.Entities;
using CarvedRock.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.MsSql;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using RestEase;
using WireMock.Client;
using WireMock.Server;
using WireMock.Settings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Bogus;
using CarvedRock.Core;

namespace CarvedRock.InnerLoop.Tests.Utilities;

public class SharedFixture : IAsyncLifetime
{
    public readonly Faker<NewProductModel> NewProductFaker = new Faker<NewProductModel>()
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
        .RuleFor(p => p.Category, f => f.PickRandom("boots", "equip", "kayak"))
        .RuleFor(p => p.Price, (f, p) =>
                p.Category == "boots" ? f.Random.Double(50, 300) :
                p.Category == "equip" ? f.Random.Double(20, 150) :
                p.Category == "kayak" ? f.Random.Double(100, 500) : 0)
        .RuleFor(p => p.ImgUrl, f => f.Image.PicsumUrl());

    // see sqlite docs for more options
    public const string DatabaseName = "InMemTestDb;Mode=Memory;Cache=Shared;";
    public string PostgresConnectionString => _dbContainer.GetConnectionString();
    public string SqlConnectionString => _sqlContainer.GetConnectionString();

    private string? _mockServerUrl;

    public List<Product>? OriginalProducts { get; private set; }

    private LocalContext? _dbContext;
    private readonly PostgreSqlContainer _dbContainer = 
        new PostgreSqlBuilder()
        .WithDatabase("carvedrock")        
        .WithUsername("carvedrock")
        .WithPassword("innerloop-ftw!")
        .Build();

    private readonly MsSqlContainer _sqlContainer =
        new MsSqlBuilder()
        .WithPassword("1nnerLoop-ftw!")
        .Build();

    // Custom SQL Server -------------------------------------------------------    
    private readonly IContainer _customSqlContainer = new ContainerBuilder()
        .WithImage("localhost/carvedrock/sqlserver") 
        .WithEnvironment("SA_PASSWORD", "Custom1zationRocks!")
        .WithPortBinding(1433, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Starting up database 'CarvedRock'."))
        .WithCleanUp(true)
        .Build();

    public string CustomSqlConnectionString =>
        $"Server=127.0.0.1,{_customSqlContainer.GetMappedPublicPort(1433)};" +
        "Database=carvedrock;" +
        "User=sa;" +
        "Password=Custom1zationRocks!;" +
        "MultipleActiveResultSets=true;" +
        "TrustServerCertificate=true;";
    // ----------------------------------------------------

    public async Task InitializeAsync()
    {
        // Postgres -----------------------------
        //await _dbContainer.StartAsync();

        //var optionsBuilder = new DbContextOptionsBuilder<LocalContext>()
        //    .UseNpgsql(PostgresConnectionString);
        //_dbContext = new LocalContext(optionsBuilder.Options);
        //---------------------------------------

        //SQLite --------------------------------
        var options = new DbContextOptionsBuilder<LocalContext>()
            .UseSqlite($"Data Source={DatabaseName}")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        _dbContext = new LocalContext(options);

        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();
        await _dbContext.Database.OpenConnectionAsync();
        //---------------------------------------

        // SQL Server ---------------------------
        //await _sqlContainer.StartAsync(); // built-in testcontainer sql
        //var optionsBuilder = new DbContextOptionsBuilder<LocalContext>()
        //    .UseSqlServer(SqlConnectionString);

        //await _customSqlContainer.StartAsync(); // custom sql
        //var optionsBuilder = new DbContextOptionsBuilder<LocalContext>()
        //    .UseSqlServer(CustomSqlConnectionString);

        //_dbContext = new LocalContext(optionsBuilder.Options);
        //---------------------------------------

        await _dbContext.Database.MigrateAsync();
        _dbContext.InitializeTestData(50);

        OriginalProducts = await _dbContext.Products.ToListAsync();
    }

    public string ProxyAndRecordApiCalls(Uri apiBaseUrl)
    {
        var server = WireMockServer.StartWithAdminInterface();
        _mockServerUrl = server.Url!;

        server.Given(Request.Create().WithPath("*"))
            .RespondWith(Response.Create().WithProxy(
                new ProxyAndRecordSettings
                {
                    Url = apiBaseUrl.ToString(),
                    SaveMapping = true,
                    SaveMappingToFile = true
                }));

        return _mockServerUrl;
    }

    public async Task DisposeAsync()
    {
        if (!string.IsNullOrEmpty(_mockServerUrl))
        {
            var api = RestClient.For<IWireMockAdminApi>(_mockServerUrl);
            var getMappingsResult = await api.GetMappingsAsync();
        }

        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }
    }
}

[CollectionDefinition(nameof(InnerLoopCollection))]
public class InnerLoopCollection : ICollectionFixture<SharedFixture>
{
    // This class has no code, and is never created. Its purpose is simply to be the place
    // to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}