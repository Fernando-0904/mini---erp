# Semana 2: Consultas SQL aplicadas ao MiniERP

## Objetivo
Praticar consultas SQL diretamente sobre o banco SQLite do MiniERP, entendendo como os dados podem ser listados, filtrados, relacionados e agrupados para apoiar rotinas de um ERP.

As consultas usam os nomes reais definidos pelo Entity Framework Core. No modelo atual, a chave primária de `Produtos` é `Codigo`, a chave estrangeira de `MovimentacoesEstoque` é `ProdutoCodigo` e a data da movimentação fica em `DataMovimentacaoUtc`.

O campo `PrecoUnitario` é armazenado como `TEXT` pelo SQLite. Por isso, as consultas que comparam ou calculam valores monetários usam `CAST(PrecoUnitario AS REAL)`.

## Produtos

### Consulta 1 - Listar todos os produtos

**Objetivo:** visualizar todos os dados cadastrados na tabela de produtos.

```sql
SELECT *
FROM Produtos;
```

**Resultado esperado:** todos os produtos, com todas as colunas da tabela.

**Uso no ERP:** conferência geral dos cadastros ou investigação inicial de dados de produtos.

### Consulta 2 - Selecionar os campos principais dos produtos

**Objetivo:** retornar somente os campos mais importantes para uma listagem resumida.

```sql
SELECT Codigo, Nome, PrecoUnitario, QuantidadeEstoque
FROM Produtos;
```

**Resultado esperado:** código, nome, preço unitário e saldo de cada produto.

**Uso no ERP:** montar uma listagem operacional sem carregar informações desnecessárias.

### Consulta 3 - Ordenar produtos por nome

**Objetivo:** listar os produtos em ordem alfabética.

```sql
SELECT Codigo, Nome
FROM Produtos
ORDER BY Nome;
```

**Resultado esperado:** produtos ordenados pelo nome, do menor para o maior em ordem alfabética.

**Uso no ERP:** facilitar a localização visual de produtos em cadastros e relatórios.

### Consulta 4 - Listar produtos abaixo do estoque mínimo

**Objetivo:** identificar produtos cujo saldo está menor ou igual ao estoque mínimo.

```sql
SELECT Codigo, Nome, QuantidadeEstoque, EstoqueMinimo
FROM Produtos
WHERE QuantidadeEstoque <= EstoqueMinimo
ORDER BY QuantidadeEstoque, Nome;
```

**Resultado esperado:** somente produtos que precisam de atenção para reposição, começando pelos menores saldos.

**Uso no ERP:** alimentar alertas de estoque e apoiar o planejamento de compras.

### Consulta 5 - Listar produtos sem estoque

**Objetivo:** localizar produtos com saldo igual a zero.

```sql
SELECT Codigo, Nome
FROM Produtos
WHERE QuantidadeEstoque = 0
ORDER BY Nome;
```

**Resultado esperado:** somente produtos sem unidades disponíveis.

**Uso no ERP:** identificar itens indisponíveis para venda ou separação.

### Consulta 6 - Listar produtos com preço acima de 100

**Objetivo:** filtrar produtos por um valor mínimo de preço.

```sql
SELECT Codigo, Nome, PrecoUnitario
FROM Produtos
WHERE CAST(PrecoUnitario AS REAL) > 100
ORDER BY CAST(PrecoUnitario AS REAL) DESC;
```

**Resultado esperado:** produtos com preço superior a 100, ordenados do maior preço para o menor.

**Uso no ERP:** apoiar análises por faixa de preço ou conferências de produtos de maior valor.

### Consulta 7 - Buscar produtos por parte do nome

**Objetivo:** localizar produtos cujo nome contenha a letra ou o trecho informado.

```sql
SELECT Codigo, Nome
FROM Produtos
WHERE Nome LIKE '%a%'
ORDER BY Nome;
```

**Resultado esperado:** produtos que contenham `a` em qualquer posição do nome.

**Uso no ERP:** permitir uma busca flexível quando o usuário não conhece o nome completo do produto.

## Relacionamentos

### Consulta 8 - Listar produtos com suas categorias

**Objetivo:** relacionar cada produto com a sua categoria obrigatória.

```sql
SELECT
    p.Codigo,
    p.Nome AS Produto,
    c.Nome AS Categoria
FROM Produtos p
INNER JOIN Categorias c ON c.Id = p.CategoriaId
ORDER BY p.Nome;
```

**Resultado esperado:** produtos que possuem uma categoria válida, acompanhados do nome da categoria.

**Uso no ERP:** gerar listagens classificadas e conferir vínculos obrigatórios do cadastro.

### Consulta 9 - Listar produtos com seus fornecedores

**Objetivo:** exibir o fornecedor de cada produto, mantendo também os produtos sem fornecedor.

```sql
SELECT
    p.Codigo,
    p.Nome AS Produto,
    COALESCE(f.Nome, 'Sem fornecedor') AS Fornecedor
FROM Produtos p
LEFT JOIN Fornecedores f ON f.Id = p.FornecedorId
ORDER BY p.Nome;
```

**Resultado esperado:** todos os produtos; quando não existir vínculo, a coluna exibirá `Sem fornecedor`.

**Uso no ERP:** analisar abastecimento sem ocultar produtos cujo fornecedor ainda não foi definido.

### Consulta 10 - Listar produtos sem fornecedor

**Objetivo:** identificar produtos que não possuem fornecedor vinculado.

```sql
SELECT p.Codigo, p.Nome
FROM Produtos p
WHERE p.FornecedorId IS NULL
ORDER BY p.Nome;
```

**Resultado esperado:** somente produtos cujo `FornecedorId` está nulo.

**Uso no ERP:** localizar cadastros que precisam ser completados pelo setor de compras.

## Agrupamentos e valores

### Consulta 11 - Contar produtos por categoria

**Objetivo:** saber quantos produtos existem em cada categoria.

```sql
SELECT
    c.Nome AS Categoria,
    COUNT(p.Codigo) AS QuantidadeProdutos
FROM Categorias c
LEFT JOIN Produtos p ON p.CategoriaId = c.Id
GROUP BY c.Id, c.Nome
ORDER BY QuantidadeProdutos DESC, c.Nome;
```

**Resultado esperado:** uma linha por categoria, inclusive categorias sem produtos, com a quantidade correspondente.

**Uso no ERP:** analisar a distribuição do catálogo e identificar categorias vazias ou muito concentradas.

### Consulta 12 - Calcular o valor em estoque por produto

**Objetivo:** multiplicar o preço unitário pelo saldo de cada produto.

```sql
SELECT
    Codigo,
    Nome,
    PrecoUnitario,
    QuantidadeEstoque,
    ROUND(CAST(PrecoUnitario AS REAL) * QuantidadeEstoque, 2) AS ValorTotal
FROM Produtos
ORDER BY Nome;
```

**Resultado esperado:** cada produto acompanhado de seu valor total armazenado em estoque.

**Uso no ERP:** entender quanto do capital está concentrado em cada item.

### Consulta 13 - Calcular o valor total do estoque

**Objetivo:** somar o valor em estoque de todos os produtos.

```sql
SELECT
    ROUND(
        COALESCE(SUM(CAST(PrecoUnitario AS REAL) * QuantidadeEstoque), 0),
        2
    ) AS ValorTotalEstoque
FROM Produtos;
```

**Resultado esperado:** uma linha com o valor total do estoque; se não houver produtos, o valor será zero.

**Uso no ERP:** apoiar avaliações financeiras e conferências do patrimônio armazenado.

### Consulta 14 - Calcular o valor do estoque por categoria

**Objetivo:** agrupar o valor dos produtos de acordo com a categoria.

```sql
SELECT
    c.Nome AS Categoria,
    ROUND(
        COALESCE(SUM(CAST(p.PrecoUnitario AS REAL) * p.QuantidadeEstoque), 0),
        2
    ) AS ValorTotal
FROM Categorias c
LEFT JOIN Produtos p ON p.CategoriaId = c.Id
GROUP BY c.Id, c.Nome
ORDER BY ValorTotal DESC, c.Nome;
```

**Resultado esperado:** uma linha por categoria com o valor total correspondente, incluindo categorias sem produtos com valor zero.

**Uso no ERP:** mostrar quais grupos de produtos concentram maior valor financeiro.

## Movimentações de estoque

### Consulta 15 - Listar o histórico de movimentações por produto

**Objetivo:** relacionar as movimentações aos produtos que tiveram o estoque alterado.

```sql
SELECT
    p.Codigo,
    p.Nome,
    m.Tipo,
    m.Quantidade,
    m.SaldoAnterior,
    m.SaldoNovo,
    m.DataMovimentacaoUtc
FROM MovimentacoesEstoque m
INNER JOIN Produtos p ON p.Codigo = m.ProdutoCodigo
ORDER BY m.DataMovimentacaoUtc DESC;
```

**Resultado esperado:** histórico do mais recente para o mais antigo, com produto, tipo, quantidade e saldos.

**Uso no ERP:** investigar de onde veio o saldo atual e rastrear entradas e saídas.

### Consulta 16 - Listar as últimas 10 movimentações

**Objetivo:** limitar o histórico às dez alterações mais recentes.

```sql
SELECT
    p.Nome AS Produto,
    m.Tipo,
    m.Quantidade,
    m.SaldoAnterior,
    m.SaldoNovo,
    m.DataMovimentacaoUtc
FROM MovimentacoesEstoque m
INNER JOIN Produtos p ON p.Codigo = m.ProdutoCodigo
ORDER BY m.DataMovimentacaoUtc DESC
LIMIT 10;
```

**Resultado esperado:** no máximo dez movimentações, ordenadas da mais recente para a mais antiga.

**Uso no ERP:** exibir atividade recente em um painel ou durante uma análise rápida.

### Consulta 17 - Somar entradas por produto

**Objetivo:** calcular o total de unidades que entraram no estoque de cada produto.

```sql
SELECT
    p.Codigo,
    p.Nome,
    SUM(m.Quantidade) AS TotalEntradas
FROM MovimentacoesEstoque m
INNER JOIN Produtos p ON p.Codigo = m.ProdutoCodigo
WHERE m.Tipo = 'Entrada'
GROUP BY p.Codigo, p.Nome
ORDER BY TotalEntradas DESC;
```

**Resultado esperado:** produtos que possuem entradas, acompanhados da soma das quantidades recebidas.

**Uso no ERP:** analisar recebimentos e produtos com maior volume de reposição.

### Consulta 18 - Somar saídas por produto

**Objetivo:** calcular o total de unidades que saíram do estoque de cada produto.

```sql
SELECT
    p.Codigo,
    p.Nome,
    SUM(m.Quantidade) AS TotalSaidas
FROM MovimentacoesEstoque m
INNER JOIN Produtos p ON p.Codigo = m.ProdutoCodigo
WHERE m.Tipo = 'Saida'
GROUP BY p.Codigo, p.Nome
ORDER BY TotalSaidas DESC;
```

**Resultado esperado:** produtos que possuem saídas, acompanhados da soma das quantidades retiradas.

**Uso no ERP:** identificar itens com maior movimentação de saída e apoiar decisões de reposição.

## Fornecedores

### Consulta 19 - Listar fornecedores ativos

**Objetivo:** retornar somente fornecedores que podem ser usados em novos vínculos.

```sql
SELECT Codigo, Nome, Documento, Email, Telefone
FROM Fornecedores
WHERE Ativo = 1
ORDER BY Nome;
```

**Resultado esperado:** fornecedores cujo campo `Ativo` está armazenado como verdadeiro no SQLite.

**Uso no ERP:** preencher opções de fornecedor sem apresentar cadastros inativados.

### Consulta 20 - Listar fornecedores sem produtos vinculados

**Objetivo:** encontrar fornecedores que não estão relacionados a nenhum produto.

```sql
SELECT f.Codigo, f.Nome
FROM Fornecedores f
LEFT JOIN Produtos p ON p.FornecedorId = f.Id
WHERE p.Codigo IS NULL
ORDER BY f.Nome;
```

**Resultado esperado:** somente fornecedores para os quais o `LEFT JOIN` não encontrou produto.

**Uso no ERP:** revisar cadastros sem utilização e avaliar inativação ou correção de vínculos.

## Evidências de execução

As 20 consultas foram executadas novamente em 30 de julho de 2026 com `Microsoft.Data.Sqlite` 10.0.10. O banco `MiniErp.Api/Dados/mini-erp.db` foi aberto em modo somente leitura.

Validação realizada:
- execução das 20 consultas diretamente contra o SQLite local;
- execução das mesmas 20 consultas em uma cópia em memória com dados controlados;
- conferência de que nenhuma consulta falhou;
- conferência de que a validação não alterou o banco do projeto.

Como reproduzir a validação:
- abrir o banco `MiniErp.Api/Dados/mini-erp.db` em modo somente leitura;
- executar cada consulta deste documento na ordem apresentada;
- para consultas que dependem de produtos, fornecedores ou movimentações, usar uma cópia de teste com dados controlados para validar retornos não vazios;
- confirmar que todas as consultas retornam resultado válido ou zero linhas, sem erro de SQL.

Como o banco local possuía apenas uma categoria e nenhum produto, fornecedor ou movimentação, foi realizada uma segunda validação em uma cópia em memória. Essa cópia recebeu dados controlados somente durante a execução, sem modificar o banco do projeto.

### Banco local

- 20 de 20 consultas executadas sem erro.
- A consulta 11 retornou a categoria `Utensílios` com zero produtos.
- A consulta 13 retornou valor total de estoque igual a zero.
- A consulta 14 retornou a categoria `Utensílios` com valor igual a zero.
- As demais consultas retornaram zero linhas porque as tabelas correspondentes estavam vazias.

### Cópia em memória com dados controlados

| Consulta | Linhas | Evidência principal |
|---|---:|---|
| 1 | 5 | Todos os produtos foram retornados |
| 2 | 5 | Somente os campos selecionados foram exibidos |
| 3 | 5 | `Adaptador` apareceu como primeiro nome |
| 4 | 2 | `Mouse` apareceu com saldo 0 e mínimo 3 |
| 5 | 1 | Apenas `Mouse` estava sem estoque |
| 6 | 3 | `Cadeira`, com preço 500, apareceu primeiro |
| 7 | 4 | Foram encontrados nomes contendo `a` |
| 8 | 5 | Todos os produtos foram relacionados às categorias |
| 9 | 5 | Produtos sem vínculo exibiram `Sem fornecedor` |
| 10 | 2 | Dois produtos estavam sem fornecedor |
| 11 | 3 | `Periféricos` possuía três produtos |
| 12 | 5 | `Adaptador` apresentou valor total 250 |
| 13 | 1 | O valor total do estoque foi 10.800 |
| 14 | 3 | `Móveis` apresentou valor total 9.800 |
| 15 | 4 | O histórico foi retornado com produto e data |
| 16 | 4 | As movimentações foram limitadas e ordenadas |
| 17 | 2 | `Cadeira` apresentou dez unidades de entrada |
| 18 | 2 | `Teclado` apresentou três unidades de saída |
| 19 | 2 | Somente os fornecedores ativos foram retornados |
| 20 | 1 | `Fornecedor Sem Produto` foi identificado |

Resultado final: **20 de 20 consultas executadas com sucesso**, sem alterar o banco SQLite do projeto.

## Conceitos praticados

- `SELECT` e `FROM` para escolher campos e tabelas;
- `WHERE` e `LIKE` para filtrar registros;
- `ORDER BY` para ordenar resultados;
- `INNER JOIN` para relacionamentos obrigatoriamente encontrados;
- `LEFT JOIN` para preservar registros mesmo sem relacionamento;
- `COUNT`, `SUM` e `GROUP BY` para análises agrupadas;
- alias para deixar consultas com várias tabelas mais legíveis;
- `NULL` para representar um vínculo opcional ausente;
- `LIMIT` para restringir a quantidade de linhas retornadas;
- `CAST`, `ROUND` e `COALESCE` para tratar valores nas consultas do SQLite.
