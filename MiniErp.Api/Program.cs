using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;
using MiniErp.Api.Security;
using MiniErp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

string[] allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[]
    {
        "http://localhost:5500",
        "http://127.0.0.1:5500",
        "https://fernando-0904.github.io"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("MiniErpCors", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MiniErp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = builder.Environment.IsDevelopment()
            ? SameSiteMode.Strict
            : SameSiteMode.None;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "MiniErp.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = builder.Environment.IsDevelopment()
        ? SameSiteMode.Strict
        : SameSiteMode.None;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
string databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=Dados/mini-erp.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(databaseConnectionString));
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<FornecedorService>();
builder.Services.AddScoped<MovimentacaoEstoqueService>();
builder.Services.AddScoped<UsuarioLocalService>();
builder.Services.AddSingleton<EmailSimuladoService>();
builder.Services.AddSingleton<IEmailService>(serviceProvider => serviceProvider.GetRequiredService<EmailSimuladoService>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("MiniErpCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/produtos", (ProdutoService produtoService) =>
{
    return Results.Ok(produtoService.ListarProdutos());
});

app.MapGet("/produtos/estoque-baixo", (int? categoriaId, ProdutoService produtoService) =>
{
    return Results.Ok(produtoService.ListarProdutosComEstoqueBaixo(categoriaId));
});

app.MapGet("/produtos/sem-estoque", (int? categoriaId, ProdutoService produtoService) =>
{
    return Results.Ok(produtoService.ListarProdutosSemEstoque(categoriaId));
});

app.MapGet("/produtos/{codigo:int}", (int codigo, ProdutoService produtoService) =>
{
    Produto? produto = produtoService.BuscarPorCodigo(codigo);

    if (produto == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(produto);
});

app.MapPost("/produtos", (ProdutoRequest request, ProdutoService produtoService, CategoriaService categoriaService, FornecedorService fornecedorService) =>
{
    Produto produto = MapearProdutoRequest(request);
    List<string> erros = produtoService.ValidarProduto(produto);
    erros.AddRange(categoriaService.ValidarCategoriaDoProduto(produto.CategoriaId));
    erros.AddRange(fornecedorService.ValidarFornecedorDoProduto(produto.FornecedorId));

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool cadastrado = produtoService.CadastrarProduto(produto);

    if (!cadastrado)
    {
        return Results.Conflict("Já existe um produto com esse código.");
    }

    return Results.Created($"/produtos/{produto.Codigo}", produto);
}).RequireAntiforgery();

app.MapPut("/produtos/{codigo:int}", (int codigo, ProdutoRequest request, ProdutoService produtoService, CategoriaService categoriaService, FornecedorService fornecedorService) =>
{
    Produto produtoAtualizado = MapearProdutoRequest(request);
    Produto? produtoExistente = produtoService.BuscarPorCodigo(codigo);

    if (produtoExistente == null)
    {
        return Results.NotFound();
    }

    List<string> erros = produtoService.ValidarProduto(produtoAtualizado);

    if (codigo != produtoAtualizado.Codigo)
    {
        erros.Add("O código da URL deve ser igual ao código do produto.");
    }

    if (produtoAtualizado.QuantidadeEstoque != produtoExistente.QuantidadeEstoque)
    {
        erros.Add("A quantidade em estoque deve ser alterada por uma movimentação de entrada ou saída.");
    }

    erros.AddRange(categoriaService.ValidarCategoriaDoProduto(produtoAtualizado.CategoriaId));
    erros.AddRange(fornecedorService.ValidarFornecedorDoProduto(produtoAtualizado.FornecedorId));

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool editado = produtoService.EditarProduto(codigo, produtoAtualizado);

    if (!editado)
    {
        return Results.NotFound();
    }

    return Results.Ok(produtoAtualizado);
}).RequireAntiforgery();

app.MapDelete("/produtos/{codigo:int}", (int codigo, ProdutoService produtoService) =>
{
    bool removido = produtoService.RemoverProduto(codigo);

    if (!removido)
    {
        return Results.NotFound();
    }

    return Results.NoContent();
}).RequireAntiforgery();

app.MapGet(
    "/produtos/{codigo:int}/movimentacoes",
    (int codigo, ProdutoService produtoService, MovimentacaoEstoqueService movimentacaoService) =>
    {
        Produto? produto = produtoService.BuscarPorCodigo(codigo);

        if (produto == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(movimentacaoService.ListarMovimentacoesPorProduto(codigo));
    });

app.MapPost(
    "/produtos/{codigo:int}/movimentacoes/entrada",
    (int codigo, MovimentacaoEstoqueRequest request, MovimentacaoEstoqueService movimentacaoService) =>
    {
        bool movimentado = movimentacaoService.RegistrarEntrada(codigo, request.Quantidade, out MovimentacaoEstoque? movimentacao, out string erro);

        if (!movimentado)
        {
            if (erro == "Produto não encontrado.")
            {
                return Results.NotFound(erro);
            }

            return Results.BadRequest(erro);
        }

        return Results.Created($"/produtos/{codigo}/movimentacoes/{movimentacao!.Id}", movimentacao);
    }).RequireAntiforgery();

app.MapPost(
    "/produtos/{codigo:int}/movimentacoes/saida",
    (int codigo, MovimentacaoEstoqueRequest request, MovimentacaoEstoqueService movimentacaoService) =>
    {
        bool movimentado = movimentacaoService.RegistrarSaida(codigo, request.Quantidade, out MovimentacaoEstoque? movimentacao, out string erro);

        if (!movimentado)
        {
            if (erro == "Produto não encontrado.")
            {
                return Results.NotFound(erro);
            }

            return Results.BadRequest(erro);
        }

        return Results.Created($"/produtos/{codigo}/movimentacoes/{movimentacao!.Id}", movimentacao);
    }).RequireAntiforgery();

app.MapGet("/categorias", (CategoriaService categoriaService) =>
{
    return Results.Ok(categoriaService.ListarCategorias());
});

app.MapGet("/categorias/{id:int}", (int id, CategoriaService categoriaService) =>
{
    Categoria? categoria = categoriaService.BuscarPorId(id);

    if (categoria == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(categoria);
});

app.MapPost("/categorias", (CategoriaRequest request, CategoriaService categoriaService) =>
{
    Categoria categoria = MapearCategoriaRequest(request);
    List<string> erros = categoriaService.ValidarCategoria(categoria);

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool cadastrada = categoriaService.CadastrarCategoria(categoria);

    if (!cadastrada)
    {
        return Results.Conflict("Já existe uma categoria com esse nome.");
    }

    return Results.Created($"/categorias/{categoria.Id}", categoria);
}).RequireAntiforgery();

app.MapPut("/categorias/{id:int}", (int id, CategoriaRequest request, CategoriaService categoriaService) =>
{
    Categoria categoriaAtualizada = MapearCategoriaRequest(request);
    List<string> erros = categoriaService.ValidarCategoria(categoriaAtualizada);

    if (id != categoriaAtualizada.Id)
    {
        erros.Add("O id da URL deve ser igual ao id da categoria.");
    }

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool editada = categoriaService.EditarCategoria(id, categoriaAtualizada);

    if (!editada)
    {
        return Results.NotFound("Categoria não encontrada ou nome já está em uso.");
    }

    return Results.Ok(categoriaAtualizada);
}).RequireAntiforgery();

app.MapDelete("/categorias/{id:int}", (int id, CategoriaService categoriaService) =>
{
    if (categoriaService.BuscarPorId(id) == null)
    {
        return Results.NotFound();
    }

    if (categoriaService.PossuiProdutosVinculados(id))
    {
        return Results.BadRequest("Não é possível remover uma categoria vinculada a produtos.");
    }

    categoriaService.RemoverCategoria(id);
    return Results.NoContent();
}).RequireAntiforgery();

app.MapGet("/fornecedores", (FornecedorService fornecedorService) =>
{
    return Results.Ok(fornecedorService.ListarFornecedores());
});

app.MapGet("/fornecedores/{id:int}", (int id, FornecedorService fornecedorService) =>
{
    Fornecedor? fornecedor = fornecedorService.BuscarPorId(id);

    if (fornecedor == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(fornecedor);
});

app.MapPost("/fornecedores", (FornecedorRequest request, FornecedorService fornecedorService) =>
{
    Fornecedor fornecedor = MapearFornecedorRequest(request);
    List<string> erros = fornecedorService.ValidarFornecedor(fornecedor);

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool cadastrado = fornecedorService.CadastrarFornecedor(fornecedor);

    if (!cadastrado)
    {
        return Results.Conflict("Já existe um fornecedor com esse código ou documento.");
    }

    return Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor);
}).RequireAntiforgery();

app.MapPut("/fornecedores/{id:int}", (int id, FornecedorRequest request, FornecedorService fornecedorService) =>
{
    if (fornecedorService.BuscarPorId(id) == null)
    {
        return Results.NotFound();
    }

    Fornecedor fornecedorAtualizado = MapearFornecedorRequest(request);
    List<string> erros = fornecedorService.ValidarFornecedor(fornecedorAtualizado);

    if (erros.Count > 0)
    {
        return Results.BadRequest(erros);
    }

    bool editado = fornecedorService.EditarFornecedor(id, fornecedorAtualizado);

    if (!editado)
    {
        return Results.Conflict("Já existe um fornecedor com esse código ou documento.");
    }

    return Results.Ok(fornecedorService.BuscarPorId(id));
}).RequireAntiforgery();

app.MapPatch("/fornecedores/{id:int}/inativar", (int id, FornecedorService fornecedorService) =>
{
    if (!fornecedorService.InativarFornecedor(id))
    {
        return Results.NotFound();
    }

    return Results.Ok(fornecedorService.BuscarPorId(id));
}).RequireAntiforgery();

app.MapDelete("/fornecedores/{id:int}", (int id, FornecedorService fornecedorService) =>
{
    if (fornecedorService.BuscarPorId(id) == null)
    {
        return Results.NotFound();
    }

    if (fornecedorService.PossuiProdutosVinculados(id))
    {
        return Results.BadRequest("Não é possível remover um fornecedor vinculado a produtos.");
    }

    fornecedorService.RemoverFornecedor(id);
    return Results.NoContent();
}).RequireAntiforgery();

app.MapGet("/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
{
    AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { token = tokens.RequestToken });
})
    .AllowAnonymous();

app.MapPost("/auth/cadastro", async (CadastroUsuarioRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    (UsuarioResponse? usuario, string erro) = await usuarioService.CadastrarAsync(
        request.Nome,
        request.Email,
        request.Senha);

    if (usuario is null)
    {
        return erro == "Já existe uma conta com este e-mail."
            ? Results.Conflict(erro)
            : Results.BadRequest(erro);
    }

    context.Response.Headers.CacheControl = "no-store";
    return Results.Json(new
    {
        usuario.Email,
        mensagem = "Conta criada. Confirme seu e-mail para entrar."
    }, statusCode: StatusCodes.Status201Created);
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/login", async (LoginRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
    {
        return Results.BadRequest("E-mail e senha são obrigatórios.");
    }

    ResultadoAutenticacao autenticacao = usuarioService.Autenticar(request.Email, request.Senha);

    if (autenticacao.EmailNaoConfirmado)
    {
        return Results.BadRequest("Confirme seu e-mail antes de entrar.");
    }

    if (autenticacao.Usuario is null)
    {
        return Results.Unauthorized();
    }

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        CriarPrincipal(autenticacao.Usuario),
        CriarPropriedadesAutenticacao());

    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(autenticacao.Usuario);
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/confirmar-email", (ConfirmarEmailRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    bool confirmado = usuarioService.ConfirmarEmail(request.Token);
    context.Response.Headers.CacheControl = "no-store";
    return confirmado
        ? Results.Ok(new { mensagem = "E-mail confirmado com sucesso. Você já pode entrar." })
        : Results.BadRequest("Token de confirmação inválido ou expirado.");
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/reenviar-confirmacao", async (ReenviarConfirmacaoEmailRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    await usuarioService.ReenviarConfirmacaoEmailAsync(request.Email);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { mensagem = "Se a conta existir e ainda não estiver confirmada, enviaremos uma nova confirmação." });
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/esqueci-senha", async (EsqueciSenhaRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    await usuarioService.SolicitarRedefinicaoSenhaAsync(request.Email);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { mensagem = "Se o e-mail estiver cadastrado, enviaremos instruções de recuperação." });
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapPost("/auth/redefinir-senha", (RedefinirSenhaRequest request, UsuarioLocalService usuarioService, HttpContext context) =>
{
    bool redefinida = usuarioService.RedefinirSenha(request.Token, request.NovaSenha);
    context.Response.Headers.CacheControl = "no-store";
    return redefinida
        ? Results.Ok(new { mensagem = "Senha redefinida com sucesso. Entre com sua nova senha." })
        : Results.BadRequest("Token inválido ou expirado, ou senha fora dos critérios.");
})
    .AllowAnonymous()
    .RequireAntiforgery();

app.MapGet("/auth/me", (ClaimsPrincipal principal, HttpContext context) =>
{
    UsuarioResponse? usuario = MapearUsuarioClaims(principal);
    context.Response.Headers.CacheControl = "no-store";
    return usuario is null ? Results.Unauthorized() : Results.Ok(usuario);
});

app.MapPost("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Headers.CacheControl = "no-store";
    return Results.NoContent();
})
    .RequireAntiforgery();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/emails", (EmailSimuladoService emailService) =>
    {
        return Results.Ok(emailService.Listar());
    })
        .AllowAnonymous();
}

app.Run();

static Produto MapearProdutoRequest(ProdutoRequest request)
{
    return new Produto
    {
        Codigo = request.Codigo,
        Nome = request.Nome,
        PrecoUnitario = request.PrecoUnitario,
        QuantidadeEstoque = request.QuantidadeEstoque,
        EstoqueMinimo = request.EstoqueMinimo,
        CategoriaId = request.CategoriaId,
        FornecedorId = request.FornecedorId
    };
}

static Categoria MapearCategoriaRequest(CategoriaRequest request)
{
    return new Categoria
    {
        Id = request.Id,
        Nome = request.Nome
    };
}

static Fornecedor MapearFornecedorRequest(FornecedorRequest request)
{
    return new Fornecedor
    {
        Codigo = request.Codigo,
        Nome = request.Nome,
        Documento = request.Documento,
        Email = request.Email,
        Telefone = request.Telefone,
        Ativo = request.Ativo
    };
}

static ClaimsPrincipal CriarPrincipal(UsuarioResponse usuario)
{
    Claim[] claims =
    [
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Name, usuario.Nome),
        new(ClaimTypes.Email, usuario.Email),
        new(ClaimTypes.Role, usuario.Perfil)
    ];

    ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    return new ClaimsPrincipal(identity);
}

static AuthenticationProperties CriarPropriedadesAutenticacao()
{
    return new AuthenticationProperties
    {
        AllowRefresh = true,
        IsPersistent = false
    };
}

static UsuarioResponse? MapearUsuarioClaims(ClaimsPrincipal principal)
{
    string? id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    string? nome = principal.FindFirstValue(ClaimTypes.Name);
    string? email = principal.FindFirstValue(ClaimTypes.Email);
    string? perfil = principal.FindFirstValue(ClaimTypes.Role);

    if (!int.TryParse(id, out int usuarioId) ||
        string.IsNullOrWhiteSpace(nome) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(perfil))
    {
        return null;
    }

    return new UsuarioResponse
    {
        Id = usuarioId,
        Nome = nome,
        Email = email,
        Perfil = perfil
    };
}

public partial class Program;
