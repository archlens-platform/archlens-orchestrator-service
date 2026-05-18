using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Context;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Reqnroll;

namespace ArchLens.Orchestrator.Tests.BDD.Hooks;

[Binding]
public sealed class TestHooks
{
    private static BddWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    internal static ISagaStateRepository MockSagaRepo { get; private set; } = null!;

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__PostgreSQL",
            "Host=localhost;Database=bdd_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable("RabbitMQ__Host", "localhost");
        Environment.SetEnvironmentVariable("RabbitMQ__Username", "guest");
        Environment.SetEnvironmentVariable("RabbitMQ__Password", "guest");
        Environment.SetEnvironmentVariable("Jwt__Key", "bdd-test-jwt-secret-key-minimum-32-characters!");

        MockSagaRepo = Substitute.For<ISagaStateRepository>();
        _factory = new BddWebApplicationFactory(MockSagaRepo);
        _client = _factory.CreateClient();

        // Ensure InMemory DB schema is created
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>();
        db.Database.EnsureCreated();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        BddTestAuthHandler.Reset();
        MockSagaRepo.ClearReceivedCalls();
        scenarioContext.Set(_client, "HttpClient");
        scenarioContext.Set(_factory, "Factory");
    }
}

public sealed class BddWebApplicationFactory : WebApplicationFactory<ArchLens.Orchestrator.Api.Program>
{
    private readonly ISagaStateRepository _mockSagaRepo;

    public BddWebApplicationFactory(ISagaStateRepository mockSagaRepo)
    {
        _mockSagaRepo = mockSagaRepo;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext and EF provider registrations
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<SagaDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || (d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition().FullName?.Contains("EntityFrameworkCore") == true)
                    || d.ServiceType.FullName?.Contains("Npgsql") == true
                    || d.ServiceType.FullName?.Contains("EntityFrameworkCore.Relational") == true
                    || d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                .ToList();
            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            // Add InMemory DB
            services.AddDbContext<SagaDbContext>(options =>
                options.UseInMemoryDatabase("OrchestratorBddTests"));

            // Replace MassTransit with TestHarness
            var massTransitDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("MassTransit") == true)
                .ToList();
            foreach (var descriptor in massTransitDescriptors)
                services.Remove(descriptor);

            services.AddMassTransitTestHarness();

            // Remove HostedServices
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                .ToList();
            foreach (var descriptor in hostedServices)
                services.Remove(descriptor);

            // Replace ISagaStateRepository with mock
            var repoDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(ISagaStateRepository));
            if (repoDescriptor is not null)
                services.Remove(repoDescriptor);
            services.AddSingleton(_mockSagaRepo);

            // Replace auth with test handler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, BddTestAuthHandler>("Test", _ => { });

            services.AddAuthorization();
        });
    }
}

public sealed class BddTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private static bool _isAuthenticated;
    private static string _role = "User";

    public BddTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    public static void SetAuthenticated(string role = "User")
    {
        _isAuthenticated = true;
        _role = role;
    }

    public static void Reset()
    {
        _isAuthenticated = false;
        _role = "User";
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!_isAuthenticated)
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "bdd-test-user"),
            new Claim(ClaimTypes.Name, "BDD Test User"),
            new Claim(ClaimTypes.Email, "bdd@test.com"),
            new Claim(ClaimTypes.Role, _role),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
