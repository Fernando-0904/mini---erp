# Fixação da Fase 7 - Semana 1

## 1. O que é uma tabela?
Uma tabela é como se fosse uma "planilha" dentro do banco de dados. Ela guarda informações de um mesmo assunto, por exemplo produtos, categorias ou fornecedores.

## 2. O que é uma coluna?
A coluna é cada tipo de dado que a tabela precisa guardar. Exemplo: na tabela de produtos, temos coluna de código, nome, preço e quantidade em estoque.

## 3. O que é uma linha?
A linha é um registro completo dentro da tabela. Por exemplo, um produto específico cadastrado, com todos os valores dele preenchidos.

## 4. O que é uma chave primária?
A chave primária é o campo que identifica cada registro de forma única. Não pode repetir e nem ficar vazio. No nosso caso, por exemplo, o produto usa o campo Código como chave primária.

## 5. O que é uma chave estrangeira?
A chave estrangeira é um campo que liga uma tabela com outra. Ela aponta para a chave primária de outra tabela. Isso ajuda a manter os dados conectados e organizados.

## 6. Qual é a relação entre produto e categoria?
A relação é de 1 para N (um para muitos): uma categoria pode ter vários produtos, e cada produto pertence a uma categoria.

## 7. Qual é a relação entre produto e fornecedor?
Também é uma relação de 1 para N: um fornecedor pode estar ligado a vários produtos. No nosso projeto, o fornecedor no produto é opcional, então o produto pode existir sem fornecedor.

## 8. Qual é a relação entre produto e movimentação de estoque?
É 1 para N: um produto pode ter várias movimentações de estoque (entrada e saída), porque cada movimentação vira um registro no histórico.

## 9. Por que não posso ter um produto apontando para uma categoria inexistente?
Porque isso quebra a integridade dos dados. Se a categoria não existe, o produto ficaria "solto" e o sistema começaria a ter inconsistência em filtros, relatórios e regras.

## 10. O que é integridade referencial?
Integridade referencial é a regra que garante que os relacionamentos entre tabelas sejam válidos. Ou seja, uma FK só pode apontar para um registro que realmente existe na tabela relacionada.

## 11. O que é uma migration?
Migration é um histórico de mudanças do banco de dados gerado a partir do código. Ela serve para criar e atualizar tabelas, colunas, índices e relacionamentos de forma controlada.

## 12. O que acontece quando eu altero uma entidade C# e gero uma migration?
Quando eu altero a entidade, o EF Core detecta diferença do modelo atual para o anterior. A migration gera os comandos para atualizar o banco com essa mudança.

## 13. Qual é a diferença entre a entidade C# e a tabela no banco?
A entidade C# é a representação em código (objeto). A tabela é a representação física no banco de dados. O Entity Framework faz o mapeamento entre as duas.

## 14. Qual é a função do DbContext?
O DbContext é a classe principal de acesso ao banco. Ele configura as entidades, relacionamentos e controla operações como consulta, inserção, edição e remoção.

## 15. Qual é a função do DbSet?
O DbSet representa cada tabela dentro do DbContext. É por ele que a aplicação consulta e manipula os dados de uma entidade específica.

---

# Fixação da Fase 7 - Semana 2

## 1. Para que serve o SELECT?
O `SELECT` serve para escolher os dados que eu quero consultar no banco. Posso selecionar todas as colunas usando `*`, mas estou aprendendo que é melhor informar somente as colunas necessárias quando já sei o que preciso mostrar.

## 2. Para que serve o FROM?
O `FROM` informa de qual tabela os dados serão consultados. Por exemplo, em `SELECT Nome FROM Produtos`, o `FROM Produtos` indica que o nome será buscado na tabela de produtos.

## 3. Para que serve o WHERE?
O `WHERE` serve para filtrar os registros. Ele permite definir uma condição, como buscar apenas produtos sem estoque ou fornecedores que estão ativos.

## 4. Para que serve o ORDER BY?
O `ORDER BY` organiza o resultado da consulta por uma ou mais colunas. A ordem padrão é crescente, mas posso usar `DESC` quando preciso mostrar primeiro os maiores valores ou as movimentações mais recentes.

## 5. Para que serve o LIKE?
O `LIKE` serve para procurar textos que seguem um padrão. Por exemplo, `LIKE '%teclado%'` encontra um nome que contém a palavra "teclado", mesmo que existam outros caracteres antes ou depois.

## 6. Qual é a diferença entre INNER JOIN e LEFT JOIN?
Pelo que aprendi, o `INNER JOIN` retorna somente os registros que possuem correspondência nas duas tabelas. O `LEFT JOIN` mantém todos os registros da tabela da esquerda, mesmo quando não existe um registro relacionado na tabela da direita.

## 7. Para que serve o GROUP BY?
O `GROUP BY` junta registros que possuem um valor em comum para que eu possa fazer cálculos por grupo. No MiniERP, posso agrupar produtos por categoria para contar quantos existem ou calcular o valor do estoque de cada categoria.

## 8. Para que serve o COUNT?
O `COUNT` serve para contar registros ou valores não nulos. Na consulta de produtos por categoria, usei `COUNT(p.Codigo)` para saber quantos produtos estavam ligados a cada categoria.

## 9. Para que serve o SUM?
O `SUM` soma os valores de uma coluna ou de um cálculo. Ele pode ser usado para descobrir o total de entradas, o total de saídas ou o valor total armazenado no estoque.

## 10. O que significa NULL?
`NULL` significa ausência de valor. Ele não é a mesma coisa que zero ou texto vazio. No projeto, um `FornecedorId` nulo indica que o produto ainda não possui fornecedor vinculado.

## 11. Por que usamos alias como p, c e f nas consultas?
Os alias são nomes menores dados às tabelas durante a consulta. Eles deixam os comandos com `JOIN` mais fáceis de ler e evitam dúvida quando tabelas diferentes possuem colunas com o mesmo nome, como `Nome`.

## 12. O que acontece se uma consulta com INNER JOIN não encontrar relacionamento?
Se o `INNER JOIN` não encontrar correspondência, aquele registro não aparece no resultado. Por isso, preciso avaliar se desejo somente relações existentes ou se devo usar `LEFT JOIN` para também mostrar os registros sem vínculo.

## 13. Em qual cenário de ERP eu usaria LEFT JOIN?
Eu usaria `LEFT JOIN`, por exemplo, para listar todos os fornecedores e descobrir quais não possuem produtos. Também posso usá-lo para mostrar todos os produtos, inclusive os que ainda estão sem fornecedor.

## 14. Como identifico produtos sem fornecedor?
Posso filtrar diretamente os produtos com `WHERE FornecedorId IS NULL`. Outra possibilidade é usar `LEFT JOIN` com fornecedores e verificar onde a chave do fornecedor relacionado ficou nula.

## 15. Como calculo o valor total do estoque?
Primeiro calculo o valor de cada produto multiplicando o preço unitário pela quantidade em estoque. Depois uso `SUM` para somar todos esses valores. Como o SQLite armazena o preço do projeto como `TEXT`, usei `CAST(PrecoUnitario AS REAL)` antes da multiplicação.

## Aprendizado da semana
Nesta semana, percebi que uma consulta SQL não serve somente para mostrar dados. Ela também ajuda a conferir regras, encontrar cadastros incompletos e transformar os dados do sistema em informações úteis. Ainda estou praticando principalmente os relacionamentos e agrupamentos, mas executar as consultas no banco do próprio projeto ajudou a entender melhor o que o Entity Framework faz por trás da aplicação.
