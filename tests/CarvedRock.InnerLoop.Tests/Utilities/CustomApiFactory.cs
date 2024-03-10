using CarvedRock.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;

namespace CarvedRock.InnerLoop.Tests.Utilities;

public class CustomApiFactory(SharedFixture fixture) : WebApplicationFactory<Program>
{
    public SharedFixture SharedFixture => fixture;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("innerloop-test");

        builder.ConfigureTestServices(services => services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LocalContext>));
            services.Remove(dbContextDescriptor!);

            var ctx = services.SingleOrDefault(d => d.ServiceType == typeof(LocalContext));
            services.Remove(ctx!);

            // add back the container-based dbContext

            // SQLite --------------------------------
            services.AddDbContext<LocalContext>(opts =>
                opts.UseSqlite($"Data Source={SharedFixture.DatabaseName}")
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

            // Postgres -----------------------------
            //services.AddDbContext<LocalContext>(options => options
            //    .UseNpgsql(SharedFixture.PostgresConnectionString)
            //    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

            // SQL Server ---------------------------
            //services.AddDbContext<LocalContext>(options => options
            //    .UseSqlServer(SharedFixture.SqlConnectionString)
            //    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

            // Custom SQL Server ---------------------
            //services.AddDbContext<LocalContext>(options => options
            //    .UseSqlServer(SharedFixture.CustomSqlConnectionString)
            //    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        });
    }
}
