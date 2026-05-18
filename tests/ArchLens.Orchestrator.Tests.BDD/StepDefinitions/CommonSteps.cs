using FluentAssertions;
using ArchLens.Orchestrator.Tests.BDD.Hooks;
using Reqnroll;

namespace ArchLens.Orchestrator.Tests.BDD.StepDefinitions;

[Binding]
public class CommonSteps
{
    private readonly ScenarioContext _scenarioContext;

    public CommonSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"que eu sou um usuário autenticado com role ""(.*)""")]
    public void DadoQueEuSouUmUsuarioAutenticadoComRole(string role)
    {
        BddTestAuthHandler.SetAuthenticated(role);
    }

    [Given(@"que eu não estou autenticado")]
    public void DadoQueEuNaoEstouAutenticado()
    {
        BddTestAuthHandler.Reset();
    }

    [Then(@"a resposta deve ter status code (.*)")]
    public void EntaoARespostaDeveTerStatusCode(int statusCode)
    {
        var response = _scenarioContext.Get<HttpResponseMessage>("Response");
        ((int)response.StatusCode).Should().Be(statusCode);
    }

    [Then(@"a resposta deve conter a mensagem ""(.*)""")]
    public void EntaoARespostaDeveConterAMensagem(string mensagem)
    {
        var body = _scenarioContext.Get<string>("ResponseBody");
        body.Should().Contain(mensagem);
    }
}
