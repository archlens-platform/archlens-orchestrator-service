# language: pt-BR
Funcionalidade: Métricas administrativas do Orchestrator
  Como um administrador da plataforma ArchLens
  Eu quero consultar as métricas de análises
  Para monitorar a saúde e performance do sistema

  Cenário: Consultar métricas com role Admin
    Dado que eu sou um usuário autenticado com role "Admin"
    E que existem métricas de sagas disponíveis
    Quando eu consultar as métricas administrativas de sagas
    Então a resposta deve ter status code 200
    E a resposta deve conter as métricas de sagas

  Cenário: Consultar métricas com role User
    Dado que eu sou um usuário autenticado com role "User"
    E que existem métricas de sagas disponíveis
    Quando eu consultar as métricas administrativas de sagas
    Então a resposta deve ter status code 200

  Cenário: Consultar métricas sem autenticação
    Dado que eu não estou autenticado
    E que existem métricas de sagas disponíveis
    Quando eu consultar as métricas administrativas de sagas
    Então a resposta deve ter status code 401

  Cenário: Métricas retornam contadores corretos
    Dado que eu sou um usuário autenticado com role "Admin"
    E que existem métricas com 10 análises totais
    Quando eu consultar as métricas administrativas de sagas
    Então a resposta deve ter status code 200
    E a resposta deve conter total de análises igual a 10

  Cenário: Métricas retornam análises recentes
    Dado que eu sou um usuário autenticado com role "Admin"
    E que existem métricas de sagas disponíveis
    Quando eu consultar as métricas administrativas de sagas
    Então a resposta deve ter status code 200
    E a resposta deve conter análises recentes
