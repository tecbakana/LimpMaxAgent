namespace LimpMaxAgent.Application.DTOs;

// DTO de entrada do chat
public record ChatRequestDto(
    string Mensagem,
    int ClienteId,
    List<MensagemHistoricoDto>? Historico = null
);

// DTO de saída do chat
public record ChatResponseDto(
    string Resposta,
    List<MensagemHistoricoDto> Historico,
    bool ToolUsada,
    string? NomeTool
);

// Representa uma mensagem no histórico da conversa
public record MensagemHistoricoDto(
    string Role,   // "user" ou "assistant"
    string Content
);

// DTO de resultado da consulta de estoque (retornado ao LLM)
public record EstoqueResultDto(
    int ProdutoId,
    string Nome,
    decimal Preco,
    int QuantidadeDisponivel,
    string UnidadeMedida
);

// DTO de resultado da criação de pedido (retornado ao LLM)
public record PedidoResultDto(
    bool Sucesso,
    int? PedidoId,
    decimal? ValorTotal,
    string? Mensagem
);
