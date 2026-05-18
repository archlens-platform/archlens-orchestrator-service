using System.Text.Json;
using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.Orchestrator.Tests.BDD.Hooks;
using FluentAssertions;
using NSubstitute;
using Reqnroll;

namespace ArchLens.Orchestrator.Tests.BDD.StepDefinitions;

[Binding]
public class SagaSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SagaSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _client = scenarioContext.Get<HttpClient>("HttpClient");
    }

    [Given(@"que existe uma saga para o diagrama ""(.*)""")]
    public void DadoQueExisteUmaSagaParaODiagrama(string diagramId)
    {
        var guid = Guid.Parse(diagramId);
        var response = new SagaStatusResponse(
            Guid.NewGuid(), guid, Guid.NewGuid(), "Completed",
            "diagram.png", 0, null, Guid.NewGuid(), 1500,
            DateTime.UtcNow, DateTime.UtcNow);

        TestHooks.MockSagaRepo
            .GetByDiagramIdAsync(guid, Arg.Any<CancellationToken>())
            .Returns(response);
    }

    [Given(@"que não existe saga para o diagrama ""(.*)""")]
    public void DadoQueNaoExisteSagaParaODiagrama(string diagramId)
    {
        var guid = Guid.Parse(diagramId);
        TestHooks.MockSagaRepo
            .GetByDiagramIdAsync(guid, Arg.Any<CancellationToken>())
            .Returns((SagaStatusResponse?)null);
    }

    [Given(@"que existe uma saga para a análise ""(.*)""")]
    public void DadoQueExisteUmaSagaParaAAnalise(string analysisId)
    {
        var guid = Guid.Parse(analysisId);
        var response = new SagaStatusResponse(
            Guid.NewGuid(), Guid.NewGuid(), guid, "Processing",
            "analysis.png", 1, null, null, null,
            DateTime.UtcNow, DateTime.UtcNow);

        TestHooks.MockSagaRepo
            .GetByAnalysisIdAsync(guid, Arg.Any<CancellationToken>())
            .Returns(response);
    }

    [Given(@"que não existe saga para a análise ""(.*)""")]
    public void DadoQueNaoExisteSagaParaAAnalise(string analysisId)
    {
        var guid = Guid.Parse(analysisId);
        TestHooks.MockSagaRepo
            .GetByAnalysisIdAsync(guid, Arg.Any<CancellationToken>())
            .Returns((SagaStatusResponse?)null);
    }

    [Given(@"que existem sagas cadastradas")]
    public void DadoQueExistemSagasCadastradas()
    {
        var sagas = new List<SagaStatusResponse>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Completed",
                "file1.png", 0, null, Guid.NewGuid(), 1200,
                DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Processing",
                "file2.png", 0, null, null, null,
                DateTime.UtcNow, DateTime.UtcNow)
        };

        TestHooks.MockSagaRepo
            .ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(sagas.AsReadOnly());
    }

    [When(@"eu consultar a saga pelo diagrama ""(.*)""")]
    public async Task QuandoEuConsultarASagaPeloDiagrama(string diagramId)
    {
        var response = await _client.GetAsync($"/saga/diagram/{diagramId}");
        _scenarioContext["Response"] = response;
        _scenarioContext["ResponseBody"] = await response.Content.ReadAsStringAsync();
    }

    [When(@"eu consultar a saga pela análise ""(.*)""")]
    public async Task QuandoEuConsultarASagaPelaAnalise(string analysisId)
    {
        var response = await _client.GetAsync($"/saga/analysis/{analysisId}");
        _scenarioContext["Response"] = response;
        _scenarioContext["ResponseBody"] = await response.Content.ReadAsStringAsync();
    }

    [When(@"eu listar as sagas")]
    public async Task QuandoEuListarAsSagas()
    {
        var response = await _client.GetAsync("/saga");
        _scenarioContext["Response"] = response;
        _scenarioContext["ResponseBody"] = await response.Content.ReadAsStringAsync();
    }

    [When(@"eu listar as sagas com página (.*) e tamanho (.*)")]
    public async Task QuandoEuListarAsSagasComPaginaETamanho(int page, int pageSize)
    {
        var response = await _client.GetAsync($"/saga?page={page}&pageSize={pageSize}");
        _scenarioContext["Response"] = response;
        _scenarioContext["ResponseBody"] = await response.Content.ReadAsStringAsync();
    }

    [Then(@"a resposta deve conter o diagrama ""(.*)""")]
    public void EntaoARespostaDeveConterODiagrama(string diagramId)
    {
        var body = _scenarioContext.Get<string>("ResponseBody");
        body.Should().Contain(diagramId.ToLowerInvariant());
    }

    [Then(@"a resposta deve conter a análise ""(.*)""")]
    public void EntaoARespostaDeveConterAAnalise(string analysisId)
    {
        var body = _scenarioContext.Get<string>("ResponseBody");
        body.Should().Contain(analysisId.ToLowerInvariant());
    }
}
