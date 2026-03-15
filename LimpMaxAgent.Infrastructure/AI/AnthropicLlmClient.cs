using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using LimpMaxAgent.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LimpMaxAgent.Infrastructure.AI;

/// <summary>
/// Adaptador para a API da Anthropic (Claude).
/// Traduz LlmMensagem/LlmFerramenta → tipos do SDK da Anthropic
/// e a resposta de volta para LlmResponse.
/// </summary>
public class AnthropicLlmClient : ILlmClient
{
    private readonly AnthropicClient _client;

    public AnthropicLlmClient(string apiKey)
    {
        _client = new AnthropicClient(apiKey);
    }

    public async Task<LlmResponse> EnviarAsync(
        string systemPrompt,
        List<LlmMensagem> mensagens,
        List<LlmFerramenta> ferramentas)
    {
        var response = await _client.Messages.GetClaudeMessageAsync(new MessageParameters
        {
            Model = "claude-sonnet-4-20250514",
            MaxTokens = 1024,
            SystemMessage = systemPrompt,
            Messages = TraduzirMensagens(mensagens),
            Tools = TraduzirFerramentas(ferramentas)
        });

        return TraduzirResposta(response);
    }

    public async Task<LlmResponse> EnviarResultadoToolAsync(
        string systemPrompt,
        List<LlmMensagem> mensagens,
        List<LlmFerramenta> ferramentas,
        string idChamada,
        string nomeFerramenta,
        string resultadoJson)
    {
        // Anthropic exige que o resultado da tool seja adicionado ao histórico
        // no formato específico deles antes de chamar novamente
        var mensagensAnthropic = TraduzirMensagens(mensagens);

        mensagensAnthropic.Add(new Message
        {
            Role = RoleType.User,
            Content = new List<ContentBase>
            {
                new ToolResultContent
                {
                    ToolUseId = idChamada,
                    Content = resultadoJson
                }
            }
        });

        var response = await _client.Messages.GetClaudeMessageAsync(new MessageParameters
        {
            Model = "claude-sonnet-4-20250514",
            MaxTokens = 1024,
            SystemMessage= systemPrompt,
            Messages = mensagensAnthropic,
            Tools = TraduzirFerramentas(ferramentas)
        });

        return TraduzirResposta(response);
    }

    // ---------------------------------------------------------------
    // Métodos de tradução — aqui fica o "dialeto" da Anthropic
    // ---------------------------------------------------------------

    private List<Message> TraduzirMensagens(List<LlmMensagem> mensagens)
    {
        return mensagens.Select(m => new Message
        {
            Role = m.Role == "user" ? RoleType.User: RoleType.Assistant,      // Anthropic usa "user" e "assistant" — igual ao nosso
            Content = new List<ContentBase>
            {
                new TextContent { Text = m.Content }
            }
        }).ToList();
    }

    private List<Function> TraduzirFerramentas(List<LlmFerramenta> ferramentas)
    {
        return ferramentas.Select(f => new Function(
            f.Nome,
            f.Descricao,
            JsonNode.Parse(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = f.Parametros,
                required = f.Obrigatorios
            }))
        )).ToList();
    }

    private LlmResponse TraduzirResposta(MessageResponse response)
    {
        // LLM quer usar ferramenta?
        if (response.StopReason == "tool_use")
        {
            var toolUse = response.Content.OfType<ToolUseContent>().First();

            return new LlmResponse
            {
                TipoResposta = "tool_use",
                NomeFerramenta = toolUse.Name,
                IdChamada = toolUse.Id,
                Argumentos = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    toolUse.Input.ToJsonString())
            };
        }

        // Resposta em texto
        return new LlmResponse
        {
            TipoResposta = "text",
            TextoResposta = response.Content.OfType<TextContent>().First().Text
        };
    }
}