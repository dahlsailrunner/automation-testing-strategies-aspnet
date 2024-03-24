using System.IdentityModel.Tokens.Jwt;
using CarvedRock.Data;
using CarvedRock.Domain;
using CarvedRock.Api;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using CarvedRock.Domain.Mapping;
using FluentValidation;
using CarvedRock.Core;
using Microsoft.AspNetCore.Authentication;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LocalContext>();

builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, loggerConfig) => {
    loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console()
    .Enrich.WithExceptionDetails()
    .Enrich.FromLogContext()
    .Enrich.With<ActivityEnricher>()
    .WriteTo.Seq("http://localhost:5341");
});

builder.Services.AddProblemDetails(opts => // built-in problem details support
    opts.CustomizeProblemDetails = (ctx) =>
    {
        if (!ctx.ProblemDetails.Extensions.ContainsKey("traceId"))
        {
            string? traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
            ctx.ProblemDetails.Extensions.Add(new KeyValuePair<string, object?>("traceId", traceId));
        }
        var exception = ctx.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (ctx.ProblemDetails.Status == 500)
        {
            ctx.ProblemDetails.Detail = "An error occurred in our API. Use the trace id when contacting us.";
        }
    }
);   

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        options.Audience = "api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "email"
        };
    });
builder.Services.AddScoped<IClaimsTransformation, CarvedRockTransformer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerOptions>();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProductLogic, ProductLogic>();

// SQLite --------------------------------
var filename = Path.Join(
           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
           builder.Configuration.GetConnectionString("CarvedRock"));

builder.Services.AddDbContext<LocalContext>(options => options
        .UseSqlite($"Data Source={filename}")
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

// Postgres -----------------------------
//builder.Services.AddDbContext<LocalContext>(options => options
//    .UseNpgsql(builder.Configuration.GetConnectionString("CarvedRockPostgres"))
//    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

// SQL Server ---------------------------
//builder.Services.AddDbContext<LocalContext>(options => options
//    .UseSqlServer(builder.Configuration.GetConnectionString("CarvedRockSqlServer"))
//    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.Services.AddScoped<ICarvedRockRepository, CarvedRockRepository>();

builder.Services.AddAutoMapper(typeof(ProductMappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<NewProductValidator>();

var app = builder.Build();

app.UseExceptionHandler();  

if (app.Environment.IsDevelopment())
{
    SetupDevelopment(app);
}

app.MapFallback(() => Results.Redirect("/swagger"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

app.MapHealthChecks("health").AllowAnonymous();

app.Run();

[ExcludeFromCodeCoverage]
static void SetupDevelopment(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<LocalContext>();
        context.MigrateAndCreateData();
    }

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("interactive.public.short");
        options.OAuthAppName("CarvedRock API");
        options.OAuthUsePkce();
    });
}

public partial class Program { } // used for integration tests
