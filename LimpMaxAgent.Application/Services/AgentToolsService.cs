using System.Text.Json;
using LimpMaxAgent.Application.DTOs;
using LimpMaxAgent.Application.Interfaces;
using LimpMaxAgent.Domain.Entities;
using LimpMaxAgent.Domain.Interfaces;

namespace LimpMaxAgent.Application.Services;

/// <summary>
/// Ferramentas que o agente de IA pode chamar.
/// Cada método aqui é uma "Tool" exposta ao LLM.
/// O LLM decide QUANDO chamar — o código decide COMO executar.
/// </summary>
public class AgentToolsService : IAgentToolsService
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IClienteRepository _clienteRepository;

    public AgentToolsService(
        IProdutoRepository produtoRepository,
        IPedidoRepository pedidoRepository,
        IClienteRepository clienteRepository)
    {
        _produtoRepository = produtoRepository;
        _pedidoRepository = pedidoRepository;
        _clienteRepository = clienteRepository;
    }

    /// <summary>
    /// Tool: consultar_estoque
    /// LLM chama quando o cliente pergunta sobre disponibilidade ou preço
    /// </summary>
    public async Task<string> ConsultarEstoqueAsync(string nomeProduto)
    {
        var produtos = await _produtoRepository.BuscarPorNomeAsync(nomeProduto);

        if (!produtos.Any())
            return JsonSerializer.Serialize(new { encontrado = false, mensagem = "Nenhum produto encontrado." });

        var resultados = new List<EstoqueResultDto>();
        foreach (var produto in produtos)
        {
            var qtd = await _produtoRepository.ObterEstoqueAsync(produto.Id);
            resultados.Add(new EstoqueResultDto(
                produto.Id,
                produto.Nome,
                produto.Preco,
                qtd,
                produto.UnidadeMedida
            ));
        }

        return JsonSerializer.Serialize(resultados);
    }

    /// <summary>
    /// Tool: registrar_pedido
    /// LLM chama quando o cliente confirma que quer comprar
    /// </summary>
    public async Task<string> RegistrarPedidoAsync(int clienteId, int produtoId, int quantidade)
    {
        // Valida cliente
        var cliente = await _clienteRepository.BuscarPorIdAsync(clienteId);
        if (cliente is null)
            return JsonSerializer.Serialize(new PedidoResultDto(false, null, null, "Cliente não encontrado."));

        // Valida produto e estoque
        var produto = await _produtoRepository.BuscarPorIdAsync(produtoId);
        if (produto is null)
            return JsonSerializer.Serialize(new PedidoResultDto(false, null, null, "Produto não encontrado."));

        var estoqueAtual = await _produtoRepository.ObterEstoqueAsync(produtoId);
        if (estoqueAtual < quantidade)
            return JsonSerializer.Serialize(new PedidoResultDto(false, null, null,
                $"Estoque insuficiente. Disponível: {estoqueAtual} {produto.UnidadeMedida}."));

        // Cria pedido
        var pedido = new Pedido
        {
            ClienteId = clienteId,
            DataPedido = DateTime.Now,
            Status = "Pendente",
            ValorTotal = produto.Preco * quantidade,
            Itens = new List<ItemPedido>
            {
                new ItemPedido
                {
                    ProdutoId = produtoId,
                    Quantidade = quantidade,
                    PrecoUnitario = produto.Preco
                }
            }
        };

        var pedidoId = await _pedidoRepository.CriarPedidoAsync(pedido);

        return JsonSerializer.Serialize(new PedidoResultDto(
            true,
            pedidoId,
            pedido.ValorTotal,
            $"Pedido criado com sucesso para {cliente.Nome}."
        ));
    }

    /// <summary>
    /// Tool: consultar_pedidos_cliente
    /// LLM chama quando o cliente pergunta sobre seus pedidos anteriores
    /// </summary>
    public async Task<string> ConsultarPedidosClienteAsync(int clienteId)
    {
        var pedidos = await _pedidoRepository.BuscarPorClienteAsync(clienteId);

        if (!pedidos.Any())
            return JsonSerializer.Serialize(new { mensagem = "Nenhum pedido encontrado para este cliente." });

        var resumo = pedidos.Select(p => new
        {
            p.Id,
            p.DataPedido,
            p.Status,
            p.ValorTotal,
            Itens = p.Itens.Count
        });

        return JsonSerializer.Serialize(resumo);
    }

    /// <summary>
    /// Tool: cancelar_pedido
    /// LLM chama quando o cliente pede cancelamento
    /// </summary>
    public async Task<string> CancelarPedidoAsync(int pedidoId, int clienteId)
    {
        var pedido = await _pedidoRepository.BuscarPorIdAsync(pedidoId);

        if (pedido is null)
            return JsonSerializer.Serialize(new { sucesso = false, mensagem = "Pedido não encontrado." });

        if (pedido.ClienteId != clienteId)
            return JsonSerializer.Serialize(new { sucesso = false, mensagem = "Este pedido não pertence ao cliente informado." });

        if (pedido.Status == "Entregue")
            return JsonSerializer.Serialize(new { sucesso = false, mensagem = "Não é possível cancelar um pedido já entregue." });

        await _pedidoRepository.AtualizarStatusAsync(pedidoId, "Cancelado");

        return JsonSerializer.Serialize(new { sucesso = true, mensagem = $"Pedido #{pedidoId} cancelado com sucesso." });
    }
}
