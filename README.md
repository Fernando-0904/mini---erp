# Mini ERP - Controle de Produtos e Estoque

## Sobre o projeto

Este projeto é um Mini ERP simples para controle de produtos e estoque. Ele foi construído em três partes: primeiro uma versão em console com C#, depois uma versão web usando HTML, CSS e JavaScript e, por fim, uma API com ASP.NET Core.

A ideia principal foi criar um sistema pequeno, mas completo o suficiente para praticar cadastro, listagem, busca, validações, cálculos de estoque e manipulação de dados na tela.

## Versão em C#

A primeira parte do projeto foi feita em C# com .NET. Essa versão roda pelo terminal e concentra a lógica principal do sistema.

Nela é possível:

- cadastrar produtos;
- listar produtos cadastrados;
- buscar produto por código;
- calcular o valor total do estoque;
- editar produto;
- remover produto;
- visualizar um resumo do estoque;
- listar produtos com estoque baixo;
- buscar produtos por nome.

Essa etapa ajudou a praticar estruturas básicas de programação, como `if`, `switch`, `while`, `foreach`, listas, objetos e validações de entrada.

## Versão web

Depois da versão em console, foi criada uma interface web para representar o sistema de forma visual.

A página possui formulário de cadastro, indicadores, busca por código, tabela de produtos, mensagens para o usuário e botões de ação.

Com JavaScript, a tela passou a funcionar diretamente no navegador. A versão web permite:

- cadastrar produtos pela tela;
- validar os campos informados;
- impedir código duplicado;
- atualizar a tabela automaticamente;
- calcular o valor total de cada produto;
- atualizar os indicadores do estoque;
- buscar produtos por código;
- limpar a busca;
- remover produtos com confirmação;
- mostrar a situação do estoque;
- salvar os produtos no navegador usando `localStorage`.

### Atualizações recentes das funções da tela

Além das funções já existentes, a versão web recebeu melhorias no fluxo de ações da tabela e do formulário:

- editar produto diretamente pela tabela;
- remover produto com confirmação;
- alterar o botão de cadastro para "Salvar alteração" durante a edição;
- bloquear temporariamente o campo de código durante a edição para evitar inconsistências;
- limpar o modo de edição ao salvar ou ao clicar em limpar formulário;
- buscar produtos por código ou nome;
- executar busca pressionando Enter no campo de busca.

Fluxo da edição de produto:

1. Ao clicar em Editar, os dados do item voltam para o formulário.
2. O sistema entra em modo de edição e altera o texto do botão principal.
3. Após salvar, os dados são atualizados na lista, a tabela é renderizada novamente e os indicadores são recalculados.
4. O modo de edição é encerrado e o formulário retorna ao estado normal.

## Estrutura visual da versão web

Na parte de HTML, a página foi organizada em seções para deixar o sistema mais fácil de entender e usar. A estrutura possui cabeçalho, área de indicadores, formulário de cadastro, campo de busca, tabela de produtos e rodapé.

O formulário foi montado com campos para código, nome, preço e quantidade. Cada campo possui `label`, `id` e `name`, o que ajuda tanto na organização da tela quanto na integração com o JavaScript.

A tabela foi criada para exibir os produtos cadastrados e também mostrar informações calculadas, como valor total e situação do estoque. O corpo da tabela recebeu um `id` para que o JavaScript consiga limpar e montar as linhas conforme os produtos forem cadastrados, buscados ou removidos.

No CSS, a interface recebeu uma aparência simples e organizada, com cores neutras, seções em formato de blocos, bordas leves, espaçamentos internos e botões padronizados. A ideia foi deixar a tela parecida com um módulo básico de sistema, sem depender de bibliotecas externas.

Também foi adicionada responsividade. Os indicadores usam grid para ficarem lado a lado em telas maiores e se ajustarem em telas menores. A tabela possui rolagem horizontal quando necessário, evitando que o conteúdo quebre em telas pequenas.

As mensagens e os status de estoque também receberam classes CSS próprias. Com isso, o sistema consegue diferenciar visualmente mensagens de sucesso, mensagens de erro, produtos sem estoque, produtos com estoque baixo e produtos disponíveis.

## Organização do código JavaScript

No começo, todo o JavaScript da versão web ficava em um único arquivo. Conforme o projeto cresceu, o código foi separado em arquivos menores, cada um com uma responsabilidade clara. A ideia foi deixar o projeto mais fácil de ler, manter e evoluir, seguindo a mesma linha de separação de responsabilidades que foi aplicada na versão em C#.

Hoje a parte web está organizada assim:

- `dom-elements.js`: centraliza a captura dos elementos da tela, deixando os demais arquivos livres de chamadas repetidas de seleção de elementos;
- `ui.js`: concentra as funções que desenham e atualizam a interface, como montar a tabela, atualizar os indicadores e exibir mensagens;
- `storage.js`: cuida da persistência, ou seja, salvar e carregar os produtos no `localStorage`;
- `api.js`: centraliza as chamadas HTTP para a API de produtos;
- `produto-controller.js`: reúne o estado, as regras e os eventos das ações de cadastrar, editar, remover e buscar;
- `app.js`: serve apenas como ponto de entrada, iniciando a aplicação.

Além da separação, a montagem da tabela também foi melhorada. No lugar de gerar HTML em texto com `innerHTML`, as linhas passaram a ser criadas com `document.createElement`, `textContent` e `appendChild`. Os botões de ação deixaram de usar `onclick` direto no HTML e passaram a ser ligados com `addEventListener`, deixando o comportamento controlado pelo JavaScript.

O carregamento dos dados salvos também ficou mais seguro. A leitura do `localStorage` passou a usar `try/catch`, de forma que, se os dados estiverem inválidos ou corrompidos, a aplicação limpa o conteúdo inválido e inicia com uma lista vazia, em vez de quebrar.

## Regras do sistema

O sistema aplica algumas regras para evitar cadastros inválidos:

- o código precisa ser maior que zero;
- não pode haver dois produtos com o mesmo código;
- o nome do produto não pode ficar vazio;
- o preço precisa ser maior que zero;
- a quantidade não pode ficar vazia;
- a quantidade não pode ser negativa.

A quantidade igual a zero é permitida, pois representa um produto cadastrado, mas sem unidades em estoque.

## API de produtos

Além da versão em console e da versão web, o projeto também possui uma API criada com ASP.NET Core Minimal API.

Por enquanto, a API trabalha com os produtos em memória. Isso significa que os dados existem enquanto a aplicação está rodando, mas são apagados quando a API é encerrada. A versão web já consome a API e mantém o `localStorage` como fallback quando a API está indisponível.

A API possui os seguintes endpoints:

| Método | Rota | Descrição |
|---|---|---|
| GET | `/produtos` | Lista todos os produtos cadastrados |
| GET | `/produtos/{codigo}` | Busca um produto pelo código |
| POST | `/produtos` | Cadastra um novo produto |
| PUT | `/produtos/{codigo}` | Edita um produto existente |
| DELETE | `/produtos/{codigo}` | Remove um produto existente |

Exemplo de JSON usado no cadastro e na edição:

```json
{
  "codigo": 101,
  "nome": "Teclado",
  "precoUnitario": 120,
  "quantidadeEstoque": 5
}
```

A API também possui validações para evitar dados inválidos:

- o código precisa ser maior que zero;
- o nome do produto não pode ficar vazio;
- o preço unitário precisa ser maior que zero;
- a quantidade em estoque não pode ser negativa;
- não pode haver dois produtos com o mesmo código;
- na edição, o código da URL precisa ser igual ao código enviado no corpo da requisição.

Algumas respostas esperadas da API:

| Situação | Resposta |
|---|---|
| Produto cadastrado com sucesso | `201 Created` |
| Produto editado com sucesso | `200 OK` |
| Produto removido com sucesso | `204 No Content` |
| Produto não encontrado | `404 Not Found` |
| Código duplicado no cadastro | `409 Conflict` |
| Dados inválidos | `400 Bad Request` |

## Tecnologias utilizadas

- C#
- .NET 10
- ASP.NET Core Minimal API
- HTML
- CSS
- JavaScript
- Git

## Estrutura do projeto

```text
projeto erp/
├── Program.cs
├── Produto.cs
├── ProjetoErp.csproj
├── index.html
├── README.md
├── MiniErp.Api/
│   ├── Models/
│   │   └── Produto.cs
│   ├── Services/
│   │   └── ProdutoService.cs
│   ├── Program.cs
│   ├── MiniErp.Api.csproj
│   └── MiniErp.Api.http
├── miniErpWeb/
│   ├── index.html
│   ├── css/
│   │   └── style.css
│   ├── js/
│   │   ├── app.js
│   │   ├── api.js
│   │   ├── dom-elements.js
│   │   ├── produto-controller.js
│   │   ├── storage.js
│   │   └── ui.js
│   └── assets/
└── .gitignore
```

## Como executar a versão em C#

No terminal, dentro da pasta do projeto, execute:

```bash
dotnet build
dotnet run
```

## Como executar a API

No terminal, dentro da pasta do projeto, execute:

```bash
dotnet run --project MiniErp.Api
```

Por padrão, a API pode ser acessada localmente em:

```text
http://localhost:5208
```

O arquivo `MiniErp.Api/MiniErp.Api.http` possui exemplos de requisições para listar, buscar, cadastrar, editar e remover produtos.

## Como abrir a versão web

Depois que o GitHub Pages estiver ativado, a versão web poderá ser acessada por este link:

[Acessar versão web publicada](https://fernando-0904.github.io/mini---erp/)

O arquivo `index.html` da raiz serve apenas para redirecionar o GitHub Pages para a pasta da aplicação web.

Para abrir localmente, use o arquivo abaixo diretamente no navegador:

[miniErpWeb/index.html](miniErpWeb/index.html)

No GitHub, o link local acima abre o arquivo HTML dentro do repositório. O link do GitHub Pages abre a aplicação funcionando como página web.

Para abrir somente a versão web localmente, não é necessário instalar pacotes, rodar servidor, usar banco de dados ou configurar a API.

## Testes manuais realizados

| Cenário | Entrada | Resultado esperado | Status |
|---|---|---|---|
| Cadastro válido | Código 101, nome Teclado, preço 120, quantidade 5 | Produto cadastrado, exibido na tabela e indicadores atualizados | OK |
| Código duplicado | Cadastrar outro produto com código 101 | Sistema exibe erro e não cadastra o produto | OK |
| Código inválido | Código vazio, zero, negativo ou decimal | Sistema exibe erro de código inválido | OK |
| Nome vazio | Nome em branco | Sistema exibe erro e não cadastra o produto | OK |
| Preço inválido | Preço vazio, zero, negativo ou texto inválido | Sistema exibe erro de preço inválido | OK |
| Quantidade inválida | Quantidade vazia, negativa, decimal ou texto inválido | Sistema exibe erro de quantidade inválida | OK |
| Quantidade zero | Código válido, nome válido, preço válido, quantidade 0 | Produto cadastrado como sem estoque | OK |
| Busca por código existente | Buscar código de produto cadastrado | Sistema exibe o produto encontrado | OK |
| Busca por nome existente | Buscar parte do nome de produto cadastrado | Sistema exibe os produtos compatíveis | OK |
| Busca sem resultado | Buscar código ou nome inexistente | Sistema exibe mensagem de nenhum produto encontrado | OK |
| Edição válida | Editar nome, preço e quantidade com valores válidos | Produto atualizado na tabela e indicadores recalculados | OK |
| Edição com campo inválido | Informar campo inválido durante edição | Sistema exibe erro e não salva a alteração | OK |
| Remoção confirmada | Clicar em Remover e confirmar | Produto removido da tabela e indicadores atualizados | OK |
| Remoção cancelada | Clicar em Remover e cancelar | Produto permanece cadastrado | OK |
| Persistência após atualizar página | Cadastrar produto e pressionar F5 | Produto continua listado após recarregar a página | OK |
| localStorage inválido | Alterar manualmente `produtos` no localStorage para valor inválido | Aplicação não quebra, limpa os dados inválidos e inicia lista vazia | OK |

## Testes manuais da integração com API

Depois da criação da API, a versão web passou a tentar usar os endpoints do back-end para listar, cadastrar, editar, remover e buscar produtos por código. O `localStorage` foi mantido como fallback para quando a API estiver indisponível.

Testes com a API ligada:

| Cenário | Entrada | Resultado esperado | Status |
|---|---|---|---|
| Listagem pela API | Abrir a tela com a API rodando | Sistema carrega os produtos usando `GET /produtos` | OK |
| Cadastro pela API | Código 101, nome Teclado, preço 120, quantidade 5 | Produto cadastrado usando `POST /produtos` e exibido na tabela | OK |
| Código duplicado pela API | Cadastrar outro produto com código 101 | API retorna conflito e o produto não é cadastrado novamente | OK |
| Cadastro inválido pela API | Nome vazio, preço zero ou quantidade negativa | API retorna erro de validação e o produto não é cadastrado | OK |
| Edição pela API | Editar nome, preço e quantidade de produto existente | Produto atualizado usando `PUT /produtos/{codigo}` | OK |
| Remoção pela API | Remover produto existente e confirmar | Produto removido usando `DELETE /produtos/{codigo}` | OK |
| Busca por código pela API | Buscar código 101 | Produto encontrado usando `GET /produtos/{codigo}` | OK |
| Busca por código inexistente | Buscar código 999 | Sistema informa que nenhum produto foi encontrado | OK |
| Busca por nome | Buscar parte do nome do produto | Sistema mantém a busca local por nome usando os dados carregados | OK |
| CORS da API | Frontend chamar `http://localhost:5208` | API permite a chamada do navegador | OK |

Testes com a API desligada:

| Cenário | Entrada | Resultado esperado | Status |
|---|---|---|---|
| Carregamento sem API | Abrir a tela com a API desligada | Sistema carrega os produtos salvos no `localStorage` | OK |
| Cadastro local | Cadastrar produto com a API desligada | Produto é salvo no navegador e exibido na tabela | OK |
| Edição local | Editar produto com a API desligada | Produto é editado no navegador e salvo no `localStorage` | OK |
| Remoção local | Remover produto com a API desligada | Produto é removido no navegador e o `localStorage` é atualizado | OK |
| Busca por código local | Buscar código de produto salvo localmente | Sistema encontra o produto usando os dados locais | OK |
| Busca por nome local | Buscar parte do nome com a API desligada | Sistema encontra produtos usando os dados locais | OK |
| Persistência local | Recarregar a página depois de alterações locais | Dados continuam disponíveis pelo `localStorage` | OK |

Observações da integração:

- a API ainda usa dados em memória, então os produtos cadastrados nela são perdidos quando a API é encerrada;
- o `localStorage` funciona como fallback quando a API está indisponível;
- ainda não existe sincronização automática entre dados locais e dados da API;
- a busca por nome continua local porque a API atual possui busca apenas por código.

## Maiores dificuldades

A maior dificuldade foi entender a integração do JavaScript com a página.

Foi nessa parte que ficaram os pontos mais importantes do projeto web: capturar os dados do formulário, validar as informações, montar a tabela dinamicamente, atualizar os indicadores, controlar os botões de busca e remoção, mostrar mensagens na tela e manter os dados salvos com `localStorage`.

Essa etapa exigiu entender melhor como o JavaScript conversa com o HTML e como cada ação do usuário precisa alterar alguma parte da página.

## O que foi praticado

Durante o desenvolvimento, foram praticados:

- lógica de programação;
- criação de menus no console;
- leitura e validação de dados;
- uso de classes e objetos em C#;
- manipulação de listas;
- criação de estrutura HTML;
- estilização com CSS;
- organização visual com seções, formulários, indicadores e tabela;
- responsividade básica para telas menores;
- eventos no JavaScript;
- manipulação do DOM;
- criação de elementos da tabela com `createElement`, `textContent` e `appendChild`;
- separação do JavaScript em arquivos por responsabilidade;
- tratamento de erro no carregamento de dados com `try/catch`;
- armazenamento local com `localStorage`;
- criação de API com ASP.NET Core Minimal API;
- criação de endpoints HTTP para produtos;
- validação de dados recebidos pela API;
- versionamento com Git e envio para o GitHub.

## Próximos passos possíveis

- melhorar alguns detalhes visuais da interface;
- melhorar a indicação visual de quando o sistema está usando a API ou o modo local;
- adicionar banco de dados futuramente;
- criar testes automatizados para a API.

---

Projeto desenvolvido como prática de aprendizado em desenvolvimento de software.
