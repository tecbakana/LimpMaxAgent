namespace LimpMaxAgent.Domain.Entities;

public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string UnidadeMedida { get; set; } = string.Empty; // "un", "cx", "fardo"
}

public class Estoque
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public DateTime UltimaAtualizacao { get; set; }
    
    // Navigation
    public Produto? Produto { get; set; }
}

public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CNPJ { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
}

public class Pedido
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime DataPedido { get; set; }
    public string Status { get; set; } = "Pendente"; // Pendente, Confirmado, Entregue, Cancelado
    public decimal ValorTotal { get; set; }
    
    // Navigation
    public Cliente? Cliente { get; set; }
    public List<ItemPedido> Itens { get; set; } = new();
}

public class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    
    // Navigation
    public Produto? Produto { get; set; }
}
