namespace LimpMaxAgent.Domain.Interfaces;

// ================================================================
// MODELOS INTERNOS — formato próprio da aplicação
// Cada LlmClient traduz esses tipos pro formato do seu provedor
// ================================================================

/// <summary>
/// Uma mensagem no histórico da conversa.
/// </summary>
public class LlmMensagem
{
    public string Role { get; set; } = string.Empty;    // "user" ou "assistant"
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Uma ferramenta que o LLM pode chamar.
/// </summary>
public class LlmFerramenta
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    // Parâmetros no formato JSON Schema
    // ex: { "type": "object", "properties": { "nome_produto": { "type": "string" } } }
    public Dictionary<string, object> Parametros { get; set; } = new();
    public List<string> Obrigatorios { get; set; } = new();
}

/// <summary>
/// Resposta normalizada do LLM — sempre nesse formato,
/// independente de qual provedor foi usado.
/// </summary>
public class LlmResponse
{
    // "text"     → LLM respondeu em texto (fim da conversa)
    // "tool_use" → LLM quer chamar uma ferramenta
    public string TipoResposta { get; set; } = string.Empty;

    // Preenchido quando TipoResposta == "text"
    public string? TextoResposta { get; set; }

    // Preenchidos quando TipoResposta == "tool_use"
    public string? NomeFerramenta { get; set; }
    public string? IdChamada { get; set; }          // id da chamada (para devolver o resultado)
    public Dictionary<string, object>? Argumentos { get; set; }
}

// ================================================================
// CONTRATO — a abstração que o ChatService conhece
// ================================================================

/// <summary>
/// Contrato comum para todos os provedores de LLM.
/// ChatService depende APENAS desta interface.
/// </summary>
public interface ILlmClient
{
    Task<LlmResponse> EnviarAsync(
        string systemPrompt,
        List<LlmMensagem> mensagens,
        List<LlmFerramenta> ferramentas
    );

    /// <summary>
    /// Após receber um tool_use, devolve o resultado e pede a resposta final.
    /// </summary>
    Task<LlmResponse> EnviarResultadoToolAsync(
        string systemPrompt,
        List<LlmMensagem> mensagens,
        List<LlmFerramenta> ferramentas,
        string idChamada,
        string nomeFerramenta,
        string resultadoJson
    );
}