# Validacao tecnica da Fase 6

Este documento registra as evidencias tecnicas da Fase 6 do Mini ERP. O escopo considera o funcionamento, a estrutura, a documentacao, os testes, a API, o frontend, o SQLite, o CI e o fluxo de revisao. A apresentacao e a fixacao de conhecimento ficam fora deste checklist por decisao do projeto.

## Checklist de planejamento inicial

- [x] Atualizar README e instrucoes de execucao.
- [x] Revisar a arquitetura e a separacao de responsabilidades.
- [x] Organizar chamadas HTTP e mensagens do frontend.
- [x] Revisar regras e testes do backend.
- [x] Implementar e testar fornecedores.
- [x] Implementar e testar relatorios de estoque.
- [x] Preparar checklist, evidencias e rascunho de Pull Request.

## Criterios tecnicos

- [x] README com visao geral, arquitetura, tecnologias, API, frontend, SQLite, regras, endpoints, testes, fluxo de revisao e proximos passos.
- [x] Frontend usando a API e o SQLite como fonte principal.
- [x] `localStorage` fora do fluxo principal.
- [x] `api.js` concentrando as chamadas HTTP e o tratamento padrao de respostas.
- [x] DTOs usados nos endpoints de criacao e edicao.
- [x] Services concentrando as regras de negocio.
- [x] Migrations versionando o esquema do SQLite.
- [x] Pelo menos 10 testes automatizados relevantes. O estado atual possui 29 testes.
- [x] Workflow de testes executando em `push` e `pull_request`.
- [x] Template de Pull Request com checklist, comandos e evidencias.
- [x] Varredura de caracteres Unicode ocultos e bidirecionais sem ocorrencias.
- [x] Validacao completa do projeto do zero registrada abaixo.
- [x] Prints das telas novas capturados e anexados como evidencia visual.
- [ ] Pull Request aberto, revisado e aprovado.

## Comandos de validacao

Executar na raiz do repositorio:

```powershell
C:\Progra~1\dotnet\dotnet.exe build .\MiniErp.slnx
C:\Progra~1\dotnet\dotnet.exe test .\MiniErp.Api.Tests\MiniErp.Api.Tests.csproj
Get-ChildItem .\miniErpWeb\js\*.js | ForEach-Object { node --check $_.FullName }
git diff --check
```

Resultado da ultima execucao:

- `dotnet build .\MiniErp.slnx`: aprovado.
- `dotnet test .\MiniErp.Api.Tests\MiniErp.Api.Tests.csproj`: 29 testes aprovados.
- `node --check`: todos os arquivos JavaScript aprovados.
- `git diff --check`: aprovado, sem erros de whitespace.
- Varredura de Unicode oculto e bidirecional: nenhuma ocorrencia encontrada.

Para preparar o banco em uma maquina nova:

```powershell
dotnet tool install --global dotnet-ef
dotnet restore .\MiniErp.slnx
dotnet ef database update --project .\MiniErp.Api --startup-project .\MiniErp.Api
```

## Validacao manual

- [x] API iniciada em `http://localhost:5208`.
- [x] Frontend servido em `http://127.0.0.1:5500`.
- [x] Categorias carregadas pela API.
- [x] Produto cadastrado com categoria valida.
- [x] Entrada e saida registradas com historico.
- [x] Fornecedor cadastrado com telefone e e-mail vazio.
- [x] Fornecedor inativado e removido das novas opcoes de vinculo.
- [x] Relatorio de estoque baixo exibindo saldo menor ou igual ao minimo.
- [x] Filtro do relatorio por categoria funcionando.
- [x] Produto sem estoque destacado na tabela.
- [x] Mensagem compreensivel exibida quando a API fica indisponivel.
- [x] Console do navegador sem erros com a API ativa.
- [x] Terminal da API sem erros.

Resultado da integracao com dados temporarios:

- Cadastro de produto: `201 Created`.
- Entrada de estoque: `201 Created`.
- Saida de estoque: `201 Created`.
- Historico: 2 movimentacoes retornadas.
- Estoque baixo: produto encontrado.
- Filtro por categoria: produto correto encontrado.
- Produtos sem estoque: produto com saldo zero encontrado.
- Inativacao de fornecedor: status alterado para inativo.
- E-mail vazio: aceito sem erro.
- Registros temporarios removidos ao final do teste.
- Com a API desligada, o frontend exibiu mensagem compreensivel; o evento de rede recusada foi tratado sem expor erro tecnico na interface.

## Evidencias visuais

Os prints devem ser mantidos junto do projeto e anexados ao Pull Request:

![Tela de fornecedores](../miniErpWeb/assets/evidencias/fornecedores.png)

![Tela de produtos](../miniErpWeb/assets/evidencias/produtos.png)

![Relatorio de estoque baixo](../miniErpWeb/assets/evidencias/estoque-baixo.png)

## Evidencias do CI

- [Workflow de testes](https://github.com/Fernando-0904/mini---erp/actions/workflows/tests.yml)
- [Workflow do GitHub Pages](https://github.com/Fernando-0904/mini---erp/actions/workflows/pages.yml)
- Commits publicados da funcionalidade: `5f213be` e `85c2191`.

## Roteiro do Pull Request

1. Descrever o objetivo e o impacto da alteracao.
2. Marcar o tipo de alteracao no template.
3. Informar os comandos executados.
4. Anexar os prints e o resultado dos testes.
5. Marcar o checklist somente apos revisar o codigo.
6. Solicitar revisao antes do merge.
