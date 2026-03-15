using System.Text.Json;
using LimpMaxAgent.Application.DTOs;
using LimpMaxAgent.Application.Interfaces;
using LimpMaxAgent.Domain.Interfaces;

namespace LimpMaxAgent.Application.Services;

/// <summary>
/// ChatService refatorado.
/// Compare com a versão anterior — a única diferença é que
/// agora depende de ILlmClient em vez de AnthropicClient diretamente.
/// Todo o resto é idêntico — é exatamente esse o valor da abstração.
/// </summary>
public class ChatService : IChatService
{
    private readonly ILlmClient _llm;           // ← abstração, não Anthropic/Gemini
    private readonly IAgentToolsService _tools;

    private const string SystemPrompt = """
    Você é a assistente virtual da Distribuidora LimpMax, especializada em produtos de limpeza.
    
    Seu papel é:
    - Atender clientes de forma cordial e profissional
    - Consultar disponibilidade e preços de produtos
    - Registrar pedidos quando o cliente confirmar
    - Informar sobre status de pedidos anteriores
    - Processar cancelamentos quando solicitado
    
    Regras importantes:
    - SEMPRE confirme os detalhes do pedido (produto, quantidade, valor total) antes de registrar
    - Se o produto não tiver estoque suficiente, informe e sugira alternativas
    - Pedidos acima de R$200,00 têm frete grátis — mencione isso quando relevante
    - Seja objetivo mas simpático
    
    Regras para busca de produtos:
    - Quando o cliente mencionar um produto composto, quebre em termos principais
    - Exemplos: "luva latex" → busque "luva" e depois "latex" separadamente
    - "desinfetante pinho 5L" → busque "desinfetante" e "pinho" separadamente
    - Se a primeira busca não retornar resultado, tente com termos alternativos
    - Combine os resultados e apresente ao cliente de forma clara
    - Se você entender que cabe listar opções de preço, liste
    
    Use as ferramentas disponíveis para buscar informações atualizadas do sistema.
    Nunca invente dados de estoque, preços ou pedidos.
    """;

    // ↓ recebe ILlmClient — não sabe se é Anthropic, Gemini ou Groq
    public ChatService(ILlmClient llm, IAgentToolsService tools)
    {
        _llm = llm;
        _tools = tools;
    }

    public async Task<ChatResponseDto> ProcessarMensagemAsync(ChatRequestDto request)
    {
        var mensagens = MontarHistorico(request);
        var ferramentas = DefinirFerramentas();
        string? nomeToolUsada = null;

        // Primeira chamada ao LLM
        var response = await _llm.EnviarAsync(SystemPrompt, mensagens, ferramentas);

        // Loop: continua enquanto o LLM quiser usar ferramentas
        while (response.TipoResposta == "tool_use")
        {
            nomeToolUsada = response.NomeFerramenta!;

            // Executa a ferramenta no nosso código
            var resultado = await ExecutarFerramenta(
                response.NomeFerramenta!,
                response.Argumentos!,
                request.ClienteId);

            // Devolve resultado ao LLM e pede próxima resposta
            response = await _llm.EnviarResultadoToolAsync(
                SystemPrompt,
                mensagens,
                ferramentas,
                response.IdChamada!,
                response.NomeFerramenta!,
                resultado);
        }

        // Aqui TipoResposta == "text" — resposta final
        mensagens.Add(new LlmMensagem { Role = "assistant", Content = response.TextoResposta! });

        return new ChatResponseDto(
            Resposta: response.TextoResposta!,
            Historico: mensagens.Select(m => new MensagemHistoricoDto(m.Role, m.Content)).ToList(),
            ToolUsada: nomeToolUsada is not null,
            NomeTool: nomeToolUsada
        );
    }

    private List<LlmFerramenta> DefinirFerramentas()
    {
        return new List<LlmFerramenta>
        {
            new LlmFerramenta
            {
                Nome = "consultar_estoque",
                Descricao = "Consulta disponibilidade e preço de produtos pelo nome.",
                Parametros = new Dictionary<string, object>
                {
                    ["nome_produto"] = new { type = "string", description = "Nome ou parte do nome do produto" }
                },
                Obrigatorios = new List<string> { "nome_produto" }
            },
            new LlmFerramenta
            {
                Nome = "registrar_pedido",
                Descricao = "Registra um pedido. Use APENAS após o cliente confirmar.",
                Parametros = new Dictionary<string, object>
                {
                    ["produto_id"] = new { type = "integer", description = "ID do produto" },
                    ["quantidade"] = new { type = "integer", description = "Quantidade desejada" }
                },
                Obrigatorios = new List<string> { "produto_id", "quantidade" }
            },
            new LlmFerramenta
            {
                Nome = "consultar_pedidos",
                Descricao = "Lista os pedidos anteriores do cliente.",
                Parametros = new Dictionary<string, object>(),
                Obrigatorios = new List<string>()
            },
            new LlmFerramenta
            {
                Nome = "cancelar_pedido",
                Descricao = "Cancela um pedido existente do cliente.",
                Parametros = new Dictionary<string, object>
                {
                    ["pedido_id"] = new { type = "integer", description = "ID do pedido a cancelar" }
                },
                Obrigatorios = new List<string> { "pedido_id" }
            }
        };
    }

    private List<LlmMensagem> MontarHistorico(ChatRequestDto request)
    {
        var mensagens = request.Historico?
            .Select(h => new LlmMensagem { Role = h.Role, Content = h.Content })
            .ToList() ?? new List<LlmMensagem>();

        mensagens.Add(new LlmMensagem { Role = "user", Content = request.Mensagem });
        return mensagens;
    }

    private async Task<string> ExecutarFerramenta(
        string nome,
        Dictionary<string, object> args,
        int clienteId)
    {
        return nome switch
        {
            "consultar_estoque" => await _tools.ConsultarEstoqueAsync(
                args["nome_produto"].ToString()!),

            "registrar_pedido" => await _tools.RegistrarPedidoAsync(
                clienteId,
                Convert.ToInt32(args["produto_id"]),
                Convert.ToInt32(args["quantidade"])),

            "consultar_pedidos" => await _tools.ConsultarPedidosClienteAsync(clienteId),

            "cancelar_pedido" => await _tools.CancelarPedidoAsync(
                Convert.ToInt32(args["pedido_id"]),
                clienteId),

            _ => JsonSerializer.Serialize(new { erro = $"Ferramenta '{nome}' não reconhecida." })
        };
    }
}