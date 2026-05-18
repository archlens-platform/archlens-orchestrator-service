# language: pt-BR
Funcionalidade: Consulta de Sagas
  Como um usuário da plataforma ArchLens
  Eu quero consultar o status das sagas de análise
  Para acompanhar o progresso das minhas análises

  Cenário: Consultar saga por diagrama com sucesso
    Dado que eu sou um usuário autenticado com role "User"
    E que existe uma saga para o diagrama "11111111-1111-1111-1111-111111111111"
    Quando eu consultar a saga pelo diagrama "11111111-1111-1111-1111-111111111111"
    Então a resposta deve ter status code 200
    E a resposta deve conter o diagrama "11111111-1111-1111-1111-111111111111"

  Cenário: Consultar saga por diagrama inexistente
    Dado que eu sou um usuário autenticado com role "User"
    E que não existe saga para o diagrama "99999999-9999-9999-9999-999999999999"
    Quando eu consultar a saga pelo diagrama "99999999-9999-9999-9999-999999999999"
    Então a resposta deve ter status code 404

  Cenário: Consultar saga por análise com sucesso
    Dado que eu sou um usuário autenticado com role "User"
    E que existe uma saga para a análise "22222222-2222-2222-2222-222222222222"
    Quando eu consultar a saga pela análise "22222222-2222-2222-2222-222222222222"
    Então a resposta deve ter status code 200
    E a resposta deve conter a análise "22222222-2222-2222-2222-222222222222"

  Cenário: Consultar saga por análise inexistente
    Dado que eu sou um usuário autenticado com role "User"
    E que não existe saga para a análise "99999999-9999-9999-9999-999999999999"
    Quando eu consultar a saga pela análise "99999999-9999-9999-9999-999999999999"
    Então a resposta deve ter status code 404

  Cenário: Listar sagas com sucesso
    Dado que eu sou um usuário autenticado com role "User"
    E que existem sagas cadastradas
    Quando eu listar as sagas
    Então a resposta deve ter status code 200

  Cenário: Consultar saga por diagrama sem autenticação
    Dado que eu não estou autenticado
    E que existe uma saga para o diagrama "11111111-1111-1111-1111-111111111111"
    Quando eu consultar a saga pelo diagrama "11111111-1111-1111-1111-111111111111"
    Então a resposta deve ter status code 401

  Cenário: Listar sagas com paginação
    Dado que eu sou um usuário autenticado com role "User"
    E que existem sagas cadastradas
    Quando eu listar as sagas com página 1 e tamanho 5
    Então a resposta deve ter status code 200
