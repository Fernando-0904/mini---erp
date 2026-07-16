# Mini ERP - Fase 6: Consolidacao Profissional

Use `Proxima_Fase_Consolidacao_Profissional_Mini_ERP.md` como referencia principal para evoluir este repositorio. O objetivo da fase e tornar o Mini ERP organizado, testavel, compreensivel, revisavel e seguro para manutencao.

## Fluxo Git solicitado pelo usuario

- Trabalhe diretamente na branch `master`.
- Nao crie branches, pull requests ou fluxos de merge, a menos que o usuario solicite isso explicitamente.
- Quando o usuario pedir commit e push, publique diretamente na `master` depois das validacoes adequadas.
- O MD pede uma pratica de PR. Implemente o template e documente o fluxo, mas nao force o uso de PR contra a preferencia atual do usuario.

## Arquitetura atual

- O frontend fica em `miniErpWeb/` e usa HTML, CSS e JavaScript sem framework.
- `miniErpWeb/js/api.js` deve concentrar chamadas HTTP; controllers nao devem espalhar URLs ou `fetch`.
- `ui.js` atualiza a interface; controllers coordenam eventos, estado e chamadas da API.
- A API fica em `MiniErp.Api/` e usa ASP.NET Core Minimal API, Entity Framework Core e SQLite.
- `Program.cs` deve priorizar configuracao, injecao de dependencias e mapeamento de rotas. Regras de negocio pertencem aos services.
- Produtos, categorias e movimentacoes sao persistidos em `MiniErp.Api/Dados/mini-erp.db`.
- `MiniErp.Api.Tests/` usa xUnit e SQLite em memoria para testar regras sem alterar o banco local.
- O GitHub Actions deve continuar executando a suite de testes em push e pull request.

## Requisitos obrigatorios da Fase 6

### README e documentacao

- O README deve permitir que uma pessoa nova execute o projeto do zero.
- Manter secoes sobre: visao geral, arquitetura, tecnologias, API, frontend, testes, SQLite, regras de negocio, endpoints, testes existentes, fluxo de revisao e proximos passos.
- Incluir a secao `Arquitetura da aplicacao` com o fluxo:

  `Usuario -> HTML/CSS/JavaScript -> api.js/fetch -> API ASP.NET Core -> Services -> Entity Framework Core -> SQLite`

- Explicar a responsabilidade de frontend, `api.js`, API, services, Entity Framework Core e SQLite.
- Criar ou atualizar `docs/fixacao-fase-6.md` com respostas, em linguagem simples, para as perguntas de arquitetura, frontend, backend, regras de ERP, testes e Git descritas no MD.
- Atualizar o README sempre que uma mudanca afetar execucao, arquitetura, regras ou testes.

### Frontend

- Manter `api.js` responsavel apenas por chamadas HTTP e tratamento padrao de respostas.
- Usar uma funcao comum para tratar respostas HTTP, incluindo JSON de erro, respostas `204 No Content` e mensagens padrao.
- Controllers devem usar funcoes de `api.js`, nao chamar `fetch` diretamente.
- Preservar separacao entre manipulacao de tela, estado do controller e comunicacao HTTP.
- Escolher e documentar claramente o papel do `localStorage`:
  - opcao preferencial: remover do fluxo principal e usar API/SQLite como fonte unica;
  - se mantido por motivo didatico, ele nunca pode ser fallback silencioso e a interface deve avisar que dados locais nao foram salvos no servidor.
- Quando a API estiver indisponivel, mostrar mensagem compreensivel, sem expor `Failed to fetch`, `TypeError` ou mensagens tecnicas.
- Padronizar mensagens para falha de conexao, codigo duplicado, categoria inexistente, saldo insuficiente e erro inesperado.
- Manter JavaScript legivel, com nomes claros, sem duplicacao desnecessaria e sem blocos grandes quando uma extracao simples melhorar a manutencao.

### Caracteres e codificacao

- Usar UTF-8 e revisar arquivos antes de commitar.
- Nao introduzir caracteres Unicode invisiveis ou bidirecionais.
- Corrigir qualquer alerta de caracteres ocultos apontado pelo GitHub.
- Validar sintaxe JavaScript com `node --check` nos arquivos alterados ou em toda a pasta `miniErpWeb/js`.

### Backend e DTOs

- Criar DTOs simples em `MiniErp.Api/DTOs/` para dados recebidos pela API, pelo menos `ProdutoRequest` e `CategoriaRequest`.
- Endpoints de criacao e edicao devem receber DTOs, nao entidades persistidas diretamente, quando possivel.
- Mapear DTOs para entidades nos limites da API ou em services claros.
- Manter regras de produto no `ProdutoService`, regras de categoria no `CategoriaService` e regras de entrada, saida, saldo e historico no service de estoque/movimentacao.
- Nao permitir codigo de produto duplicado, preco menor ou igual a zero, quantidade negativa, estoque minimo negativo, categoria ausente ou categoria inexistente.
- Nao permitir remover categoria vinculada a produtos.
- Alteracoes de saldo devem ocorrer exclusivamente por movimentacoes de entrada e saida, com historico e bloqueio de saldo negativo.
- Preservar respostas HTTP coerentes: `200`, `201`, `204`, `400`, `404` e `409` conforme o caso.

### Testes

- Manter testes unitarios com xUnit e SQLite em memoria.
- A meta minima da Fase 6 e pelo menos 10 testes automatizados relevantes.
- Cobrir no minimo: cadastro valido, codigo duplicado, produto sem categoria, categoria inexistente, preco invalido, quantidade negativa, entrada, saida valida, saida acima do saldo, historico de movimentacao, estoque minimo negativo e produto com categoria.
- Usar estrutura Arrange, Act e Assert, com nomes de teste descritivos.
- Rodar `dotnet test MiniErp.Api.Tests/MiniErp.Api.Tests.csproj` antes de concluir alteracoes de backend ou regras de negocio.

### Checklist e revisao

- Criar e manter `.github/pull_request_template.md` com resumo, tipo de alteracao, como testar, evidencias e checklist.
- O checklist deve cobrir revisao propria, testes locais, README quando necessario, ausencia de caracteres ocultos, console do navegador e terminal da API sem erros.
- Preferir commits pequenos e com uma unica responsabilidade.
- Para alteracoes significativas, registrar comandos de validacao executados e o impacto esperado.

## Ordem de execucao da Fase 6

1. Completar README, arquitetura e instrucoes de execucao.
2. Definir e implementar o comportamento do `localStorage`.
3. Organizar `api.js`, controllers e mensagens de erro.
4. Revisar Unicode e codificacao.
5. Introduzir DTOs e reduzir regras no `Program.cs`.
6. Ampliar a suite para pelo menos 10 testes.
7. Criar template de PR e documento de fixacao.
8. Validar projeto do zero: build, testes, API, frontend e banco.
9. Preparar apresentacao tecnica de 15 a 20 minutos sobre arquitetura, regras, persistencia, testes, decisoes e proximos passos.

## Evolucao apos a Fase 6

- Prioridade funcional sugerida pelo MD: cadastro de fornecedores, com codigo e documento unicos, status ativo/inativo, validacao de email, relacionamento opcional com produto e testes proprios.
- Alternativa menor: relatorio de estoque baixo com filtro por categoria, ordenacao por menor saldo, destaque para produto sem estoque e testes de listagem.

## Comandos de validacao

```powershell
dotnet build .\MiniErp.slnx
dotnet test .\MiniErp.Api.Tests\MiniErp.Api.Tests.csproj
Get-ChildItem .\miniErpWeb\js\*.js | ForEach-Object { node --check $_.FullName }
git diff --check
```