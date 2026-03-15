using LimpMaxAgent.Domain.Entities;

namespace LimpMaxAgent.Domain.Interfaces;

// Contrato para repositório de produtos/estoque
public interface IProdutoRepository
{
    Task<IEnumerable<Produto>> BuscarPorNomeAsync(string nomeParcial);
    Task<Produto?> BuscarPorIdAsync(int id);
    Task<int> ObterEstoqueAsync(int produtoId);
}

// Contrato para repositório de pedidos
public interface IPedidoRepository
{
    Task<int> CriarPedidoAsync(Pedido pedido);
    Task<Pedido?> BuscarPorIdAsync(int pedidoId);
    Task<IEnumerable<Pedido>> BuscarPorClienteAsync(int clienteId);
    Task AtualizarStatusAsync(int pedidoId, string novoStatus);
}

// Contrato para repositório de clientes
public interface IClienteRepository
{
    Task<Cliente?> BuscarPorIdAsync(int clienteId);
    Task<Cliente?> BuscarPorCNPJAsync(string cnpj);
}
