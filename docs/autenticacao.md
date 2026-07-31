# Autenticacao, sessao e recuperacao de senha

## Objetivo

Este documento resume como funcionam cadastro, login, sessao, CSRF, confirmacao de e-mail e recuperacao de senha no MiniERP.

O objetivo da implementacao atual e proteger as rotas do ERP em ambiente local de estudo, evitando credenciais no `localStorage` e mantendo as contas persistidas no SQLite.

## Visao geral do fluxo

```text
Frontend -> /auth/csrf -> token CSRF em memoria
Frontend -> /auth/cadastro -> cria usuario pendente de confirmacao
API -> EmailSimuladoService -> registra link de confirmacao
Frontend -> /auth/confirmar-email -> confirma token
Frontend -> /auth/login -> cria cookie MiniErp.Auth
Frontend -> rotas do ERP -> envia cookie automaticamente
Frontend -> /auth/logout -> encerra sessao
```

## Cadastro de usuario

Endpoint:

```text
POST /auth/cadastro
```

Entrada esperada:

```json
{
  "nome": "Fernando",
  "email": "fernando@example.com",
  "senha": "12345678"
}
```

Regras aplicadas:

- nome obrigatorio com pelo menos 3 caracteres;
- e-mail obrigatorio, valido e unico;
- senha obrigatoria com pelo menos 8 caracteres;
- e-mail e normalizado antes de gravar;
- conta nasce com `EmailConfirmado = false`;
- senha nunca e gravada em texto puro.

A senha e protegida com salt aleatorio de 16 bytes e hash PBKDF2 com SHA-256 e 100.000 iteracoes.

## Confirmacao de e-mail

Quando a conta e criada, a API gera um token de confirmacao com validade de 24 horas.

Endpoint:

```text
POST /auth/confirmar-email
```

Entrada esperada:

```json
{
  "token": "token-recebido-no-link"
}
```

O token real nao e salvo no banco. A tabela `TokensUsuario` armazena apenas o hash do token, o tipo, a data de criacao, a data de expiracao e a data de uso.

Depois de usado, o token recebe `UsadoEmUtc` e nao pode ser reutilizado.

## Reenvio de confirmacao

Endpoint:

```text
POST /auth/reenviar-confirmacao
```

Entrada esperada:

```json
{
  "email": "fernando@example.com"
}
```

Se a conta existir e ainda nao estiver confirmada, a API gera um novo token. Tokens pendentes anteriores do mesmo tipo sao marcados como usados.

## Login e sessao

Endpoint:

```text
POST /auth/login
```

Entrada esperada:

```json
{
  "email": "fernando@example.com",
  "senha": "12345678"
}
```

O login so e liberado para contas com e-mail confirmado.

Quando as credenciais sao validas, a API cria o cookie de autenticacao:

```text
MiniErp.Auth
```

Caracteristicas do cookie:

- `HttpOnly`, para impedir acesso via JavaScript;
- `SameSite=Strict` em desenvolvimento local;
- `SameSite=None` e `Secure` fora de desenvolvimento;
- validade de 8 horas;
- renovacao deslizante enquanto a sessao continua ativa.

O frontend envia o cookie automaticamente usando `credentials: "include"` nas chamadas `fetch`.

## Perfil da sessao

Endpoint:

```text
GET /auth/me
```

Esse endpoint retorna os dados basicos do usuario autenticado. O frontend usa essa rota ao carregar a aplicacao para decidir se deve mostrar o ERP ou a tela de acesso restrito.

Resposta esperada:

```json
{
  "id": 1,
  "nome": "Administrador",
  "email": "admin@mini-erp.com",
  "perfil": "Administrador"
}
```

## Logout

Endpoint:

```text
POST /auth/logout
```

O logout remove a sessao atual. Depois disso, chamadas protegidas voltam a responder `401 Unauthorized`.

## CSRF

As requisicoes de escrita usam protecao antiforgery.

Endpoint para obter token:

```text
GET /auth/csrf
```

O frontend guarda o token apenas em memoria e envia o valor neste cabecalho:

```text
X-CSRF-TOKEN
```

Rotas que alteram dados exigem esse token em chamadas `POST`, `PUT`, `PATCH` e `DELETE`.

## Recuperacao de senha

Solicitacao de recuperacao:

```text
POST /auth/esqueci-senha
```

Entrada esperada:

```json
{
  "email": "fernando@example.com"
}
```

Se o e-mail existir e estiver confirmado, a API gera um token de redefinicao com validade de 2 horas.

Redefinicao de senha:

```text
POST /auth/redefinir-senha
```

Entrada esperada:

```json
{
  "token": "token-recebido-no-link",
  "novaSenha": "novaSenha123"
}
```

Depois de redefinir a senha, o token e marcado como usado e nao pode ser reutilizado.

## E-mail simulado

No ambiente `Development`, os e-mails nao sao enviados para um provedor real. Eles sao registrados pelo `EmailSimuladoService`.

Endpoint disponivel apenas em desenvolvimento:

```text
GET /dev/emails
```

Esse endpoint ajuda a consultar os links gerados para confirmacao de e-mail e recuperacao de senha durante testes locais.

## Rotas publicas e protegidas

Rotas publicas:

- `GET /auth/csrf`
- `POST /auth/cadastro`
- `POST /auth/login`
- `POST /auth/confirmar-email`
- `POST /auth/reenviar-confirmacao`
- `POST /auth/esqueci-senha`
- `POST /auth/redefinir-senha`
- `GET /dev/emails`, apenas em desenvolvimento

Rotas protegidas:

- `GET /auth/me`
- `POST /auth/logout`
- rotas de produtos;
- rotas de categorias;
- rotas de fornecedores;
- rotas de movimentacoes de estoque.

## Perfis e permissoes

O MiniERP usa o campo `Perfil` do usuario como role da sessao. O valor e gravado na claim `ClaimTypes.Role` e usado pelas politicas de autorizacao da API.

Perfis atuais:

| Perfil | Permissoes |
|---|---|
| `Administrador` ou `Admin` | Pode consultar, cadastrar, editar, movimentar, inativar e remover registros |
| `Operador` ou `Usuário` | Pode consultar, cadastrar, editar, movimentar e inativar, mas nao pode remover registros |
| `Consulta` | Pode apenas consultar dados |

Observacoes:

- `Admin` e aceito como perfil legado da conta administrativa criada por migration;
- `Usuário` e aceito como perfil legado de contas criadas antes da padronizacao para `Operador`;
- novos cadastros recebem o perfil `Operador` por padrao;
- tentativas sem permissao retornam `403 Forbidden`.

## Conta administrativa local

A migration cria uma conta administrativa para facilitar o uso local:

```text
E-mail: admin@mini-erp.com
Senha: 123456
```

Essa conta e adequada apenas para desenvolvimento e deve ser removida ou substituida antes de qualquer ambiente real.

## Limitacoes atuais

- O envio de e-mail e simulado.
- Ainda nao existe bloqueio progressivo por tentativas de login.
- Ainda nao existe auditoria de acesso.
- Ainda nao existe segundo fator de autenticacao.
- Todos os usuarios autenticados acessam as mesmas funcionalidades.
- O projeto usa SQLite local, adequado para estudo e desenvolvimento.
