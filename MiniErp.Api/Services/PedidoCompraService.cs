using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;
using MiniErp.Api.DTOs;
using MiniErp.Api.Models;

namespace MiniErp.Api.Services;

public class PedidoCompraService
{
    private readonly AppDbContext contexto;
    private readonly MovimentacaoEstoqueService movimentacaoEstoqueService;

    public PedidoCompraService(AppDbContext contexto, MovimentacaoEstoqueService movimentacaoEstoqueService)
    {
        this.contexto = contexto;
        this.movimentacaoEstoqueService = movimentacaoEstoqueService;
    }

    public async Task<(PedidoCompraResponse? Pedido, List<string> Erros)> CriarPedidoAsync(PedidoCompraRequest request)
    {
        List<string> erros = ValidarRequest(request);

        Fornecedor? fornecedor = await contexto.Fornecedores
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.FornecedorId);

        if (fornecedor is null)
        {
            erros.Add("Fornecedor não encontrado.");
        }
        else if (!fornecedor.Ativo)
        {
            erros.Add("Fornecedor informado está inativo.");
        }

        Dictionary<int, Produto> produtosMapeados = [];

        if (request.Itens.Count > 0)
        {
            int[] codigosProdutos = request.Itens.Select(item => item.ProdutoCodigo).Distinct().ToArray();
            List<Produto> produtos = await contexto.Produtos
                .Where(produto => codigosProdutos.Contains(produto.Codigo))
                .ToListAsync();

            produtosMapeados = produtos.ToDictionary(produto => produto.Codigo, produto => produto);

            foreach (PedidoCompraItemRequest item in request.Itens)
            {
                if (!produtosMapeados.ContainsKey(item.ProdutoCodigo))
                {
                    erros.Add($"Produto {item.ProdutoCodigo} não encontrado.");
                }
            }
        }

        if (erros.Count > 0)
        {
            return (null, erros);
        }

        PedidoCompra pedido = new()
        {
            FornecedorId = request.FornecedorId,
            Status = PedidoCompraStatus.Aberto,
            CriadoEmUtc = DateTime.UtcNow,
            Itens = request.Itens.Select(item =>
            {
                Produto produto = produtosMapeados[item.ProdutoCodigo];
                return new PedidoCompraItem
                {
                    ProdutoCodigo = item.ProdutoCodigo,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = produto.PrecoUnitario
                };
            }).ToList()
        };

        contexto.PedidosCompra.Add(pedido);
        await contexto.SaveChangesAsync();

        PedidoCompra? pedidoCompleto = await BuscarPedidoCompletoAsync(pedido.Id);
        return pedidoCompleto is null
            ? (null, ["Não foi possível carregar o pedido criado."])
            : (MapearResponse(pedidoCompleto), []);
    }

    public async Task<List<PedidoCompraResponse>> ListarPedidosAsync()
    {
        List<PedidoCompra> pedidos = await contexto.PedidosCompra
            .AsNoTracking()
            .Include(pedido => pedido.Fornecedor)
            .Include(pedido => pedido.Itens)
                .ThenInclude(item => item.Produto)
            .OrderByDescending(pedido => pedido.CriadoEmUtc)
            .ToListAsync();

        return pedidos.Select(MapearResponse).ToList();
    }

    public async Task<(PedidoCompraResponse? Pedido, string Erro)> ReceberPedidoAsync(int pedidoId)
    {
        PedidoCompra? pedido = await contexto.PedidosCompra
            .Include(item => item.Itens)
            .ThenInclude(item => item.Produto)
            .Include(item => item.Fornecedor)
            .FirstOrDefaultAsync(item => item.Id == pedidoId);

        if (pedido is null)
        {
            return (null, "Pedido de compra não encontrado.");
        }

        if (pedido.Status != PedidoCompraStatus.Aberto)
        {
            return (null, "Apenas pedidos abertos podem ser recebidos.");
        }

        foreach (PedidoCompraItem item in pedido.Itens)
        {
            bool movimentado = movimentacaoEstoqueService.RegistrarEntrada(
                item.ProdutoCodigo,
                item.Quantidade,
                out _,
                out string erroMovimentacao);

            if (!movimentado)
            {
                return (null, $"Não foi possível receber o pedido: {erroMovimentacao}");
            }
        }

        pedido.Status = PedidoCompraStatus.Recebido;
        pedido.RecebidoEmUtc = DateTime.UtcNow;
        await contexto.SaveChangesAsync();

        PedidoCompra? pedidoAtualizado = await BuscarPedidoCompletoAsync(pedido.Id);
        return pedidoAtualizado is null
            ? (null, "Pedido recebido, mas não foi possível recarregar os dados.")
            : (MapearResponse(pedidoAtualizado), string.Empty);
    }

    private static List<string> ValidarRequest(PedidoCompraRequest request)
    {
        List<string> erros = [];

        if (request.FornecedorId <= 0)
        {
            erros.Add("Fornecedor é obrigatório.");
        }

        if (request.Itens is null || request.Itens.Count == 0)
        {
            erros.Add("Informe ao menos um item no pedido de compra.");
            return erros;
        }

        foreach (PedidoCompraItemRequest item in request.Itens)
        {
            if (item.ProdutoCodigo <= 0)
            {
                erros.Add("Produto do pedido precisa ter código válido.");
            }

            if (item.Quantidade <= 0)
            {
                erros.Add("Quantidade do pedido precisa ser maior que zero.");
            }
        }

        return erros;
    }

    private async Task<PedidoCompra?> BuscarPedidoCompletoAsync(int pedidoId)
    {
        return await contexto.PedidosCompra
            .AsNoTracking()
            .Include(pedido => pedido.Fornecedor)
            .Include(pedido => pedido.Itens)
                .ThenInclude(item => item.Produto)
            .FirstOrDefaultAsync(pedido => pedido.Id == pedidoId);
    }

    private static PedidoCompraResponse MapearResponse(PedidoCompra pedido)
    {
        return new PedidoCompraResponse
        {
            Id = pedido.Id,
            FornecedorId = pedido.FornecedorId,
            FornecedorNome = pedido.Fornecedor?.Nome ?? string.Empty,
            Status = pedido.Status.ToString(),
            CriadoEmUtc = pedido.CriadoEmUtc,
            RecebidoEmUtc = pedido.RecebidoEmUtc,
            ValorTotal = pedido.Itens.Sum(item => item.PrecoUnitario * item.Quantidade),
            Itens = pedido.Itens.Select(item => new PedidoCompraItemResponse
            {
                ProdutoCodigo = item.ProdutoCodigo,
                ProdutoNome = item.Produto?.Nome ?? string.Empty,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                ValorTotal = item.PrecoUnitario * item.Quantidade
            }).ToList()
        };
    }
}
