using System.Text.Json;
using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.Orchestrator.Tests.BDD.Hooks;
using FluentAssertions;
using NSubstitute;
using Reqnroll;

namespace ArchLens.Orchestrator.Tests.BDD.StepDefinitions;

[Binding]
public class MetricasSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MetricasSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _client = scenarioContext.Get<HttpClient>("HttpClient");
    }

    [Given(@"que existem métricas de sagas disponíveis")]
    public void DadoQueExistemMetricasDeSagasDisponiveis()
    {
        var metrics = new AdminMetricsResponse(
            TotalAnalyses: 25,
            Completed: 20,
            Failed: 3,
            Processing: 2,
            AverageProcessingTimeMs: 1500.5,
            AnalysesByState: new Dictionary<string, int>
            {
                { "Completed", 20 },
                { "Failed", 3 },
                { "Processing", 2 }
            },
            RecentAnalyses:
            [
                new RecentAnalysisDto(Guid.NewGuid(), Guid.NewGuid(), "Completed", "file1.png", 1200, DateTime.UtcNow),
                new RecentAnalysisDto(Guid.NewGuid(), Guid.NewGuid(), "Processing", "file2.png", null, DateTime.UtcNow)
            ]);

        TestHooks.MockSagaRepo
            .GetAdminMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(metrics);
    }

    [Given(@"que existem métricas com (.*) análises totais")]
    public void DadoQueExistemMetricasComAnalisesTotais(int total)
    {
        var metrics = new AdminMetricsResponse(
            TotalAnalyses: total,
            Completed: total - 2,
            Failed: 1,
            Processing: 1,
            AverageProcessingTimeMs: 1000.0,
            AnalysesByState: new Dictionary<string, int>
            {
                { "Completed", total - 2 },
                { "Failed", 1 },
                { "Processing", 1 }
            },
            RecentAnalyses:
            [
                new RecentAnalysisDto(Guid.NewGuid(), Guid.NewGuid(), "Completed", "file.png", 1000, DateTime.UtcNow)
            ]);

        TestHooks.MockSagaRepo
            .GetAdminMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(metrics);
    }

    [When(@"eu consultar as métricas administrativas de sagas")]
    public async Task QuandoEuConsultarAsMetricasAdministrativasDeSagas()
    {
        var response = await _client.GetAsync("/saga/admin/metrics");
        _scenarioContext["Response"] = response;
        _scenarioContext["ResponseBody"] = await response.Content.ReadAsStringAsync();
    }

    [Then(@"a resposta deve conter as métricas de sagas")]
    public void EntaoARespostaDeveConterAsMetricasDeSagas()
    {
        var body = _scenarioContext.Get<string>("ResponseBody");
        var metrics = JsonSerializer.Deserialize<AdminMetricsResponse>(body, JsonOptions);
        metrics.Should().NotBeNull();
        metrics!.TotalAnalyses.Should().BeGreaterThan(0);
        metrics.AnalysesByState.Should().NotBeEmpty();
    }

    [Then(@"a resposta deve conter total de análises igual a (.*)")]
    public void EntaoARespostaDeveConterTotalDeAnalisesIgualA(int total)
    {
        var body = _scenarioContext.Get<string>("ResponseBody");
        var metrics = JsonSerializer.Deserialize<AdminMetricsResponse>(body, JsonOptions);
        metrics.Should().NotBeNull();
        metrics!.TotalAnalyses.Should().Be(total);
    }

    [Then(@"a resposta deve conter análises recentes")]
    public void EntaoARespostaDeveConterAnalisesRecentes()
    {
        var body = _scenarioContext.Get<string>("ResponseBody");
        var metrics = JsonSerializer.Deserialize<AdminMetricsResponse>(body, JsonOptions);
        metrics.Should().NotBeNull();
        metrics!.RecentAnalyses.Should().NotBeEmpty();
    }
}
