# SQL vs LINQ - Fase 7 Semana 3

## Objetivo
Comparar consultas SQL da Semana 2 com suas versões equivalentes em LINQ/EF Core usadas no Mini ERP.

## Consulta 1 - Produtos abaixo do estoque mínimo

### SQL
```sql
SELECT Codigo, Nome, QuantidadeEstoque, EstoqueMinimo
FROM Produtos
WHERE QuantidadeEstoque <= EstoqueMinimo
ORDER BY QuantidadeEstoque, Nome;
```

### LINQ
```csharp
var itens = await contexto.Produtos
    .AsNoTracking()
    .Where(produto => produto.QuantidadeEstoque <= produto.EstoqueMinimo)
    .OrderBy(produto => produto.QuantidadeEstoque)
    .ThenBy(produto => produto.Nome)
    .Select(produto => new ProdutoEstoqueBaixoResponse
    {
        Codigo = produto.Codigo,
        Nome = produto.Nome,
        QuantidadeEstoque = produto.QuantidadeEstoque,
        EstoqueMinimo = produto.EstoqueMinimo,
        Categoria = produto.Categoria != null ? produto.Categoria.Nome : "Sem categoria"
    })
    .ToListAsync();
```

### Equivalência
- `WHERE` no SQL equivale a `Where` no LINQ.
- `ORDER BY` no SQL equivale a `OrderBy` e `ThenBy` no LINQ.
- `SELECT` no SQL equivale ao `Select` que projeta DTO.

## Consulta 2 - Produtos sem estoque

### SQL
```sql
SELECT Codigo, Nome, QuantidadeEstoque
FROM Produtos
WHERE QuantidadeEstoque = 0
ORDER BY Nome;
```

### LINQ
```csharp
var itens = await contexto.Produtos
    .AsNoTracking()
    .Where(produto => produto.QuantidadeEstoque == 0)
    .OrderBy(produto => produto.Nome)
    .Select(produto => new ProdutoSemEstoqueResponse
    {
        Codigo = produto.Codigo,
        Nome = produto.Nome,
        QuantidadeEstoque = produto.QuantidadeEstoque,
        Categoria = produto.Categoria != null ? produto.Categoria.Nome : "Sem categoria"
    })
    .ToListAsync();
```

### Equivalência
- `= 0` no SQL equivale a `== 0` no LINQ.
- A ordenação e a projeção seguem o mesmo raciocínio da consulta anterior.

## Consulta 3 - Valor total em estoque por categoria

### SQL
```sql
SELECT
    c.Nome AS Categoria,
    COALESCE(SUM(p.PrecoUnitario * p.QuantidadeEstoque), 0) AS ValorTotal
FROM Categorias c
LEFT JOIN Produtos p ON p.CategoriaId = c.Id
GROUP BY c.Id, c.Nome
ORDER BY ValorTotal DESC, c.Nome;
```

### LINQ
```csharp
var itens = await contexto.Categorias
    .AsNoTracking()
    .Select(categoria => new ValorEstoquePorCategoriaResponse
    {
        Categoria = categoria.Nome,
        ValorTotal = contexto.Produtos
            .AsNoTracking()
            .Where(produto => produto.CategoriaId == categoria.Id)
            .Select(produto => (decimal?)produto.PrecoUnitario * produto.QuantidadeEstoque)
            .Sum() ?? 0m
    })
    .OrderByDescending(item => item.ValorTotal)
    .ThenBy(item => item.Categoria)
    .ToListAsync();
```

### Equivalência
- `LEFT JOIN` + `GROUP BY` no SQL aparecem como projeção por categoria e subconsulta agregada no LINQ.
- `SUM` + `COALESCE` no SQL equivalem a `Sum() ?? 0m` no LINQ.

## Consulta 4 - Produtos sem fornecedor

### SQL
```sql
SELECT p.Codigo, p.Nome
FROM Produtos p
WHERE p.FornecedorId IS NULL
ORDER BY p.Nome;
```

### LINQ
```csharp
var itens = await contexto.Produtos
    .AsNoTracking()
    .Where(produto => produto.FornecedorId == null)
    .OrderBy(produto => produto.Nome)
    .Select(produto => new ProdutoSemFornecedorResponse
    {
        Codigo = produto.Codigo,
        Nome = produto.Nome,
        Categoria = produto.Categoria != null ? produto.Categoria.Nome : "Sem categoria"
    })
    .ToListAsync();
```

### Equivalência
- `IS NULL` no SQL equivale a `== null` no LINQ.
- A consulta permanece de leitura e retorna somente projeção para DTO.

## Consulta 5 - Últimas movimentações com limite

### SQL
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

### LINQ
```csharp
int limiteAjustado = limite <= 0 ? 10 : Math.Min(limite, 100);

var itens = await contexto.MovimentacoesEstoque
    .AsNoTracking()
    .OrderByDescending(movimentacao => movimentacao.DataMovimentacaoUtc)
    .Take(limiteAjustado)
    .Select(movimentacao => new UltimaMovimentacaoResponse
    {
        Produto = movimentacao.Produto != null ? movimentacao.Produto.Nome : string.Empty,
        Tipo = movimentacao.Tipo.ToString(),
        Quantidade = movimentacao.Quantidade,
        SaldoAnterior = movimentacao.SaldoAnterior,
        SaldoNovo = movimentacao.SaldoNovo,
        DataMovimentacaoUtc = movimentacao.DataMovimentacaoUtc
    })
    .ToListAsync();
```

### Equivalência
- `ORDER BY ... DESC` no SQL equivale a `OrderByDescending` no LINQ.
- `LIMIT` no SQL equivale a `Take` no LINQ.
- O limite no LINQ recebe regra adicional de proteção (`1..100`) antes da consulta.

## Observações práticas
- `AsNoTracking` é recomendado em consultas analíticas de leitura para reduzir overhead de rastreamento.
- `IQueryable` permite montar a expressão e deixar o EF Core traduzir para SQL no banco.
- A consulta só é executada quando materializada, por exemplo com `ToListAsync`.
- O SQL final pode ser inspecionado pelos logs do EF Core em ambiente de desenvolvimento.
