using Dapper;
using Microsoft.Data.SqlClient;
using LimpMaxAgent.Domain.Entities;
using LimpMaxAgent.Domain.Interfaces;

namespace LimpMaxAgent.Infrastructure.Repositories;

/// <summary>
/// Repositório de produtos usando Dapper para queries SQL Server.
/// Dapper é leve e perfeito para queries simples — sem o overhead do EF Core.
/// </summary>
public class ProdutoRepository : IProdutoRepository
{
    private readonly string _connectionString;

    public ProdutoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<Produto>> BuscarPorNomeAsync(string nomeParcial)
    {
        using var conn = new SqlConnection(_connectionString);

        const string sql = """
            SELECT p.Id, p.Nome, p.Descricao, p.Preco, p.UnidadeMedida
            FROM Produtos p
            INNER JOIN Estoque e ON e.ProdutoId = p.Id
            WHERE (p.Nome LIKE @Nome OR p.Descricao LIKE @Nome)
              AND p.Ativo = 1
              AND e.Quantidade > 0
            ORDER BY p.Nome
            """;

        return await conn.QueryAsync<Produto>(sql, new { Nome = $"%{nomeParcial}%" });
    }

    public async Task<Produto?> BuscarPorIdAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);

        const string sql = "SELECT Id, Nome, Descricao, Preco, UnidadeMedida FROM Produtos WHERE Id = @Id AND Ativo = 1";

        return await conn.QueryFirstOrDefaultAsync<Produto>(sql, new { Id = id });
    }

    public async Task<int> ObterEstoqueAsync(int produtoId)
    {
        using var conn = new SqlConnection(_connectionString);

        const string sql = "SELECT ISNULL(Quantidade, 0) FROM Estoque WHERE ProdutoId = @ProdutoId";

        return await conn.QueryFirstOrDefaultAsync<int>(sql, new { ProdutoId = produtoId });
    }
}

/// <summary>
/// Repositório de pedidos.
/// </summary>
public class PedidoRepository : IPedidoRepository
{
    private readonly string _connectionString;

    public PedidoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> CriarPedidoAsync(Pedido pedido)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            // Insere o pedido
            const string sqlPedido = """
                INSERT INTO Pedidos (ClienteId, DataPedido, Status, ValorTotal)
                VALUES (@ClienteId, @DataPedido, @Status, @ValorTotal);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            var pedidoId = await conn.QueryFirstAsync<int>(sqlPedido, pedido, transaction);

            // Insere os itens
            const string sqlItem = """
                INSERT INTO ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
                VALUES (@PedidoId, @ProdutoId, @Quantidade, @PrecoUnitario);
                """;

            foreach (var item in pedido.Itens)
            {
                item.PedidoId = pedidoId;
                await conn.ExecuteAsync(sqlItem, item, transaction);
            }

            // Baixa estoque
            const string sqlEstoque = """
                UPDATE Estoque SET Quantidade = Quantidade - @Quantidade
                WHERE ProdutoId = @ProdutoId;
                """;

            foreach (var item in pedido.Itens)
                await conn.ExecuteAsync(sqlEstoque, item, transaction);

            transaction.Commit();
            return pedidoId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Pedido?> BuscarPorIdAsync(int pedidoId)
    {
        using var conn = new SqlConnection(_connectionString);

        const string sql = """
            SELECT p.Id, p.ClienteId, p.DataPedido, p.Status, p.ValorTotal,
                   i.Id, i.PedidoId, i.ProdutoId, i.Quantidade, i.PrecoUnitario
            FROM Pedidos p
            LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
            WHERE p.Id = @PedidoId
            """;

        var pedidoDict = new Dictionary<int, Pedido>();

        await conn.QueryAsync<Pedido, ItemPedido, Pedido>(sql,
            (pedido, item) =>
            {
                if (!pedidoDict.TryGetValue(pedido.Id, out var pedidoExistente))
                {
                    pedidoExistente = pedido;
                    pedidoDict.Add(pedido.Id, pedidoExistente);
                }
                if (item is not null)
                    pedidoExistente.Itens.Add(item);
                return pedidoExistente;
            },
            new { PedidoId = pedidoId },
            splitOn: "Id");

        return pedidoDict.Values.FirstOrDefault();
    }

    public async Task<IEnumerable<Pedido>> BuscarPorClienteAsync(int clienteId)
    {
        using var conn = new SqlConnection(_connectionString);

        const string sql = """
            SELECT Id, ClienteId, DataPedido, Status, ValorTotal
            FROM Pedidos
            WHERE ClienteId = @ClienteId
            ORDER BY DataPedido DESC
            """;

        return await conn.QueryAsync<Pedido>(sql, new { ClienteId = clienteId });
    }

    public async Task AtualizarStatusAsync(int pedidoId, string novoStatus)
    {
        using var conn = new SqlConnection(_connectionString);

        const string sql = "UPDATE Pedidos SET Status = @Status WHERE Id = @Id";
        await conn.ExecuteAsync(sql, new { Status = novoStatus, Id = pedidoId });
    }
}

/// <summary>
/// Repositório de clientes.
/// </summary>
public class ClienteRepository : IClienteRepository
{
    private readonly string _connectionString;

    public ClienteRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Cliente?> BuscarPorIdAsync(int clienteId)
    {
        using var conn = new SqlConnection(_connectionString);
        const string sql = "SELECT Id, Nome, CNPJ, Email, Telefone FROM Clientes WHERE Id = @Id";
        return await conn.QueryFirstOrDefaultAsync<Cliente>(sql, new { Id = clienteId });
    }

    public async Task<Cliente?> BuscarPorCNPJAsync(string cnpj)
    {
        using var conn = new SqlConnection(_connectionString);
        const string sql = "SELECT Id, Nome, CNPJ, Email, Telefone FROM Clientes WHERE CNPJ = @CNPJ";
        return await conn.QueryFirstOrDefaultAsync<Cliente>(sql, new { CNPJ = cnpj });
    }
}
