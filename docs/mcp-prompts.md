# MCP Playwright no Mini ERP

## 1. Objetivo
Este documento padroniza o uso do MCP Playwright para testar o frontend do Mini ERP com roteiros repetíveis, evidências visuais e checklist de regressão antes do push.

## 2. Pré-requisitos
1. API em execução na porta 5208.
2. Frontend servido em http://localhost:5500.
3. MCP Playwright configurado e habilitado no VS Code.
4. Base de dados local com dados mínimos para os testes funcionais.

## 3. Como usar
1. Abra o Chat do Copilot.
2. Copie um prompt desta lista e envie.
3. Aguarde o agente executar os passos no navegador.
4. Salve as evidências solicitadas (capturas de tela + resumo).
5. Marque o checklist de regressão ao final.

## 4. Prompts prontos

### Prompt 1 - Teste de fumaça geral
Use este prompt para validar a navegação e o carregamento das páginas principais.

```text
Abra http://localhost:5500/index.html e execute um teste de fumaça do Mini ERP.
Passos:
1) Verificar se a página Painel abre sem erro.
2) Navegar para Produtos, Categorias, Fornecedores, Movimentações e Estoque baixo.
3) Em cada página, validar que os elementos principais estão visíveis (título, formulário/tabela, mensagem).
4) Confirmar que não existe mensagem técnica em inglês na interface.
5) Tirar uma captura de tela por página validada.
Entregar um resumo final: páginas OK, páginas com erro e ação recomendada.
```

### Prompt 2 - Fluxo de Produtos
Use este prompt para validar cadastro e listagem.

```text
Abra http://localhost:5500/produtos.html e execute um fluxo de produto.
Passos:
1) Preencher o formulário com um produto de teste (código único, nome, preço, quantidade, estoque mínimo e categoria válida).
2) Salvar produto.
3) Validar mensagem de sucesso.
4) Validar se o produto aparece na tabela.
5) Tirar uma captura de tela da tabela com o produto visível.
6) Remover o produto criado para limpar ambiente.
Entregar resumo: resultado de cada etapa e qualquer erro encontrado.
```

### Prompt 3 - Fluxo de movimentação
Use este prompt para validar entrada, saída e histórico.

```text
Abra http://localhost:5500/movimentacoes.html e execute um teste de movimentação.
Passos:
1) Fazer uma entrada para um produto existente.
2) Validar mensagem de sucesso.
3) Fazer uma saída válida para o mesmo produto.
4) Validar mensagem de sucesso.
5) Buscar o histórico do produto.
6) Validar se a entrada e a saída aparecem no histórico.
7) Tirar uma captura de tela da tabela de histórico.
Entregar resumo: saldo inicial, saldo final e consistência do histórico.
```

### Prompt 4 - Erros de UX
Use este prompt para validar mensagens amigáveis.

```text
Abra http://localhost:5500/produtos.html e valide mensagens de erro amigáveis.
Passos:
1) Tentar cadastrar produto com código duplicado (usar um código existente).
2) Validar mensagem amigável em português.
3) Tentar cadastrar produto com categoria inválida.
4) Validar mensagem amigável em português.
5) Ir para Movimentações e tentar uma saída acima do saldo.
6) Validar mensagem amigável (sem stack trace, sem TypeError e sem Failed to fetch).
7) Tirar capturas de tela de cada erro validado.
Entregar resumo com as mensagens exibidas e informar se estão padronizadas.
```

### Prompt 5 - Relatório de estoque baixo e gráfico
Use este prompt para validar a tabela e o gráfico.

```text
Abra http://localhost:5500/estoque-baixo.html.
Passos:
1) Validar o carregamento da tabela e do gráfico.
2) Selecionar uma categoria no filtro e validar a atualização da tabela.
3) Confirmar que o texto de resumo do gráfico foi atualizado.
4) Tirar uma captura da tela inteira.
5) Voltar para Todas as categorias e validar comportamento novamente.
Entregar resumo: estado do filtro, quantidade de itens e consistência visual do gráfico.
```

## 5. Checklist de regressão antes do push
Marque este checklist após executar os prompts.

- [ ] Prompt 1 (teste de fumaça geral) executado.
- [ ] Prompt 2 (Fluxo de Produtos) executado.
- [ ] Prompt 3 (fluxo de movimentação) executado.
- [ ] Prompt 4 (Erros de UX) executado.
- [ ] Prompt 5 (estoque baixo e gráfico) executado.
- [ ] Todas as mensagens ao usuário estão em português.
- [ ] Nenhum erro técnico cru está exposto na interface.
- [ ] Evidências salvas.

## 6. Evidências recomendadas
Sugestão de pasta para guardar arquivos:

- miniErpWeb/assets/evidencias/mcp/

Sugestão de nomes:

- mcp-smoke-painel.png
- mcp-smoke-produtos.png
- mcp-smoke-categorias.png
- mcp-smoke-fornecedores.png
- mcp-smoke-movimentacoes.png
- mcp-smoke-estoque-baixo.png
- mcp-produto-fluxo.png
- mcp-movimentacao-historico.png
- mcp-erros-ux.png
- mcp-estoque-baixo-grafico.png

## 7. Problemas comuns e ação rápida
1. API fora do ar:
- Sintoma: telas sem dados ou erro de conexão.
- Ação: iniciar a API em http://localhost:5208.

2. Frontend fora do ar:
- Sintoma: a página não abre em localhost:5500.
- Ação: iniciar o servidor HTTP na pasta miniErpWeb.

3. Dados de teste insuficientes:
- Sintoma: fluxo de movimentação ou estoque baixo sem itens.
- Ação: cadastrar dados básicos (categoria e produto) antes de executar os prompts.

4. Erros intermitentes por cache:
- Sintoma: resultado diferente entre execuções.
- Ação: recarregar a página e repetir o prompt.

## 8. Resultado esperado para esta branch
Ao final desta branch, o time deve ter:
1. Padrão único para testes com MCP Playwright.
2. Prompts reutilizáveis por fluxo.
3. Checklist de regressão antes do push.
4. Evidências visuais para PR e revisão técnica.
