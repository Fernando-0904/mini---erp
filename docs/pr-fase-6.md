# PR - Consolidação técnica da Fase 6

## Resumo

Consolida a estrutura técnica do Mini ERP e fecha os fluxos de fornecedores e estoque.

## Alterações

- Atualiza a documentação de execução, arquitetura, SQLite, endpoints e testes.
- Corrige os exemplos de requisição em `MiniErp.Api.http` para os DTOs atuais.
- Adiciona a consulta `GET /produtos/sem-estoque`.
- Padroniza a mensagem de erro inesperado no frontend.
- Registra o checklist técnico e as evidências da Fase 6.
- Mantém fornecedores com telefone, e-mail opcional e inativação.
- Mantém o relatório de estoque baixo com filtro por categoria e destaque para saldo zero.

## Como testar

```powershell
C:\Progra~1\dotnet\dotnet.exe build .\MiniErp.slnx
C:\Progra~1\dotnet\dotnet.exe test .\MiniErp.Api.Tests\MiniErp.Api.Tests.csproj
Get-ChildItem .\miniErpWeb\js\*.js | ForEach-Object { node --check $_.FullName }
git diff --check
```

Validação manual:

1. Aplicar as migrations e iniciar a API em `http://localhost:5208`.
2. Servir `miniErpWeb` em `http://127.0.0.1:5500`.
3. Cadastrar um fornecedor com telefone e e-mail vazio.
4. Inativar o fornecedor e confirmar a remoção das novas opções de vínculo.
5. Cadastrar produto com estoque baixo e produto sem estoque.
6. Abrir o relatório, filtrar por categoria e confirmar o destaque do saldo zero.
7. Conferir o console do navegador e o terminal da API.

## Resultado

- Build: aprovado.
- Testes: 55 aprovados.
- Sintaxe JavaScript: aprovada.
- Unicode oculto/bidirecional: nenhuma ocorrência.
- Integração manual: aprovada.

## Evidências visuais

![Fornecedores](../miniErpWeb/assets/evidencias/fornecedores.png)

![Produtos](../miniErpWeb/assets/evidencias/produtos.png)

![Estoque baixo](../miniErpWeb/assets/evidencias/estoque-baixo.png)

## Checklist

- [x] Código revisado.
- [x] Testes locais executados.
- [x] README atualizado.
- [x] Migrations aplicadas.
- [x] API e frontend validados.
- [x] Capturas de tela anexadas.
- [x] Nenhum caractere Unicode oculto encontrado.
- [x] Console do navegador sem erros com a API ativa.
- [ ] Revisão de outro colaborador.

## Critérios de aceite da Fase 6

- [x] O README permite rodar o projeto do zero.
- [x] A arquitetura está documentada.
- [x] O frontend consome a API como fonte principal.
- [x] O papel do `localStorage` está claro e ele não participa do fluxo principal.
- [x] A auditoria de caracteres ocultos foi concluída sem ocorrências.
- [x] O JavaScript está organizado e legível.
- [x] O backend possui responsabilidades separadas entre rotas, services e persistência.
- [x] DTOs simples são usados nos endpoints principais.
- [x] Existem 55 testes automatizados relevantes.
- [x] O GitHub Actions executa os testes com sucesso.
- [x] Existe template de Pull Request.
- [ ] O PR da fase foi aberto, recebeu as evidências e passou por revisão.

O item 12 fica reservado para a abertura manual do Pull Request a partir desta branch e para a revisão externa antes do merge.
