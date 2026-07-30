# Mini ERP - Fase 6: Consolidação Profissional

Use `Proxima_Fase_Consolidacao_Profissional_Mini_ERP.md` como referência principal para evoluir este repositório. O objetivo da fase é tornar o Mini ERP organizado, testável, compreensível, revisável e seguro para manutenção.

## Fluxo Git solicitado pelo usuário

- Para evoluções relevantes, trabalhe em uma branch própria, abra um Pull Request, aguarde a revisão e faça o merge somente depois da aprovação.
- Commit direto na `master` só deve acontecer quando o usuário solicitar isso explicitamente.
- Quando o usuário pedir commit e push sem indicar outra branch, confirme a branch de destino e execute as validações adequadas antes de publicar.
- O template de Pull Request e o checklist de evidências devem ser mantidos e usados nas alterações relevantes.

## Arquitetura atual

- O frontend fica em `miniErpWeb/` e usa HTML, CSS e JavaScript sem framework.
- `miniErpWeb/js/api.js` deve concentrar chamadas HTTP; controllers não devem espalhar URLs ou `fetch`.
- `ui.js` atualiza a interface; controllers coordenam eventos, estado e chamadas da API.
- A API fica em `MiniErp.Api/` e usa ASP.NET Core Minimal API, Entity Framework Core e SQLite.
- `Program.cs` deve priorizar configuração, injeção de dependências e mapeamento de rotas. Regras de negócio pertencem aos services.
- Produtos, categorias e movimentações são persistidos em `MiniErp.Api/Dados/mini-erp.db`.
- `MiniErp.Api.Tests/` usa xUnit e SQLite em memória para testar regras sem alterar o banco local.
- O GitHub Actions deve continuar executando a suíte de testes em push e pull request.

## Requisitos obrigatórios da Fase 6

### README e documentação

- O README deve permitir que uma pessoa nova execute o projeto do zero.
- Manter seções sobre: visão geral, arquitetura, tecnologias, API, frontend, testes, SQLite, regras de negócio, endpoints, testes existentes, fluxo de revisão e próximos passos.
- Incluir a seção `Arquitetura da aplicação` com o fluxo:

  `Usuário -> HTML/CSS/JavaScript -> api.js/fetch -> API ASP.NET Core -> Services -> Entity Framework Core -> SQLite`

- Explicar a responsabilidade de frontend, `api.js`, API, services, Entity Framework Core e SQLite.
- Criar ou atualizar `docs/fixacao-fase-6.md` com respostas, em linguagem simples, para as perguntas de arquitetura, frontend, backend, regras de ERP, testes e Git descritas no MD.
- Atualizar o README sempre que uma mudança afetar execução, arquitetura, regras ou testes.

### Frontend

- Manter `api.js` responsável apenas por chamadas HTTP e tratamento padrão de respostas.
- Usar uma função comum para tratar respostas HTTP, incluindo JSON de erro, respostas `204 No Content` e mensagens padrão.
- Controllers devem usar funções de `api.js`, não chamar `fetch` diretamente.
- Preservar separação entre manipulação de tela, estado do controller e comunicação HTTP.
- Escolher e documentar claramente o papel do `localStorage`:
  - opção preferencial: remover do fluxo principal e usar API/SQLite como fonte única;
  - se mantido por motivo didático, ele nunca pode ser fallback silencioso e a interface deve avisar que dados locais não foram salvos no servidor.
- Quando a API estiver indisponível, mostrar mensagem compreensível, sem expor `Failed to fetch`, `TypeError` ou mensagens técnicas.
- Padronizar mensagens para falha de conexão, código duplicado, categoria inexistente, saldo insuficiente e erro inesperado.
- Manter JavaScript legível, com nomes claros, sem duplicação desnecessária e sem blocos grandes quando uma extração simples melhorar a manutenção.

### Caracteres e codificação

- Usar UTF-8 e revisar arquivos antes de commitar.
- Não introduzir caracteres Unicode invisíveis ou bidirecionais.
- Corrigir qualquer alerta de caracteres ocultos apontado pelo GitHub.
- Validar sintaxe JavaScript com `node --check` nos arquivos alterados ou em toda a pasta `miniErpWeb/js`.

### Backend e DTOs

- Criar DTOs simples em `MiniErp.Api/DTOs/` para dados recebidos pela API, pelo menos `ProdutoRequest` e `CategoriaRequest`.
- Endpoints de criação e edição devem receber DTOs, não entidades persistidas diretamente, quando possível.
- Mapear DTOs para entidades nos limites da API ou em services claros.
- Manter regras de produto no `ProdutoService`, regras de categoria no `CategoriaService` e regras de entrada, saída, saldo e histórico no service de estoque/movimentação.
- Não permitir código de produto duplicado, preço menor ou igual a zero, quantidade negativa, estoque mínimo negativo, categoria ausente ou categoria inexistente.
- Não permitir remover categoria vinculada a produtos.
- Alterações de saldo devem ocorrer exclusivamente por movimentações de entrada e saída, com histórico e bloqueio de saldo negativo.
- Preservar respostas HTTP coerentes: `200`, `201`, `204`, `400`, `404` e `409` conforme o caso.

### Testes

- Manter testes unitários com xUnit e SQLite em memória.
- A meta minima da Fase 6 e pelo menos 10 testes automatizados relevantes.
- Cobrir no mínimo: cadastro válido, código duplicado, produto sem categoria, categoria inexistente, preço inválido, quantidade negativa, entrada, saída válida, saída acima do saldo, histórico de movimentação, estoque mínimo negativo e produto com categoria.
- Usar estrutura Arrange, Act e Assert, com nomes de teste descritivos.
- Rodar `dotnet test MiniErp.Api.Tests/MiniErp.Api.Tests.csproj` antes de concluir alterações de backend ou regras de negócio.

### Checklist e revisão

- Criar e manter `.github/pull_request_template.md` com resumo, tipo de alteração, como testar, evidências e checklist.
- O checklist deve cobrir revisão própria, testes locais, README quando necessário, ausência de caracteres ocultos, console do navegador e terminal da API sem erros.
- Preferir commits pequenos e com uma única responsabilidade.
- Para alterações significativas, registrar comandos de validação executados e o impacto esperado.

## Ordem de execução da Fase 6

1. Completar README, arquitetura e instruções de execução.
2. Definir e implementar o comportamento do `localStorage`.
3. Organizar `api.js`, controllers e mensagens de erro.
4. Revisar Unicode e codificação.
5. Introduzir DTOs e reduzir regras no `Program.cs`.
6. Ampliar a suíte para pelo menos 10 testes.
7. Criar template de PR e documento de fixação.
8. Validar projeto do zero: build, testes, API, frontend e banco.
9. Preparar apresentação técnica de 15 a 20 minutos sobre arquitetura, regras, persistência, testes, decisões e próximos passos.

## Evolução após a Fase 6

- Prioridade funcional sugerida pelo MD: cadastro de fornecedores, com código e documento únicos, status ativo/inativo, validação de e-mail, relacionamento opcional com produto e testes próprios.
- Alternativa menor: relatório de estoque baixo com filtro por categoria, ordenação por menor saldo, destaque para produto sem estoque e testes de listagem.

## Comandos de validação

```powershell
dotnet build .\MiniErp.slnx
dotnet test .\MiniErp.Api.Tests\MiniErp.Api.Tests.csproj
Get-ChildItem .\miniErpWeb\js\*.js | ForEach-Object { node --check $_.FullName }
git diff --check
```