using LimpMaxAgent.Application.DTOs;

namespace LimpMaxAgent.Application.Interfaces;

// Contrato principal do serviço de chat com IA
public interface IChatService
{
    Task<ChatResponseDto> ProcessarMensagemAsync(ChatRequestDto request);
}

// Contrato das ferramentas disponíveis para o agente
public interface IAgentToolsService
{
    Task<string> ConsultarEstoqueAsync(string nomeProduto);
    Task<string> RegistrarPedidoAsync(int clienteId, int produtoId, int quantidade);
    Task<string> ConsultarPedidosClienteAsync(int clienteId);
    Task<string> CancelarPedidoAsync(int pedidoId, int clienteId);
}
