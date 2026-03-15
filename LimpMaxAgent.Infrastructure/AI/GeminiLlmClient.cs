using LimpMaxAgent.Domain.Interfaces;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace LimpMaxAgent.Infrastructure.AI;

/// <summary>
/// Adaptador para a API do Google Gemini.
/// O Gemini tem um "dialeto" diferente — role "assistant" vira "model",
/// ferramentas ficam em "tools[].functionDeclarations", etc.
/// Toda essa tradução fica aqui, isolada.
/// </summary>
public class GeminiLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;   // gratuito
    private readonly string _url;

    public GeminiLlmClient(string apiKey,string modelo,string url)
    {
        _apiKey = apiKey;
        _model = modelo;
        _url = url;
        _http = new HttpClient
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };
    }

    public async Task<LlmResponse> EnviarAsync(
        string systemPrompt,
        List<LlmMensagem> mensagens,
        List<LlmFerramenta> ferramentas)
    {
        var payload = MontarPayload(systemPrompt, mensagens, ferramentas);
        return await ChamarApiAsync(payload);
    }

    public async Task<LlmResponse> EnviarResultadoToolAsync(
    string systemPrompt,
    List<LlmMensagem> mensagens,
    List<LlmFerramenta> ferramentas,
    string idChamada,
    string nomeFerramenta,
    string resultadoJson)
    {
        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = MontarConteudoComToolResult(mensagens, nomeFerramenta, resultadoJson),
            tools = new[]
            {
            new
            {
                function_declarations = ferramentas.Select(f => new
                {
                    name = f.Nome,
                    description = f.Descricao,
                    parameters = new
                    {
                        type = "object",
                        properties = f.Parametros,
                        required = f.Obrigatorios
                    }
                })
            }
        }
        };

        return await ChamarApiAsync(payload);
    }

    private object[] MontarConteudoComToolResult(
        List<LlmMensagem> mensagens,
        string nomeFerramenta,
        string resultadoJson)
    {
        var contents = mensagens.Select(m => new
        {
            role = m.Role == "assistant" ? "model" : "user",
            parts = new[] { new { text = m.Content } }
        }).Cast<object>().ToList();

        // resultado da tool vai como role "user" com formato function_response
        contents.Add(new
        {
            role = "user",
            parts = new[]
            {
            new
            {
                function_response = new
                {
                    name = nomeFerramenta,
                    response = new { result = resultadoJson }
                }
            }
        }
        });

        return contents.ToArray();
    }

    // ---------------------------------------------------------------
    // Métodos de tradução — aqui fica o "dialeto" do Gemini
    // ---------------------------------------------------------------

    private object MontarPayload(string systemPrompt, List<LlmMensagem> mensagens, List<LlmFerramenta> ferramentas)
    {
        return new
        {
            system_instruction = new             // Gemini usa esse formato pro system prompt
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = mensagens.Select(m => new
            {
                role = m.Role == "assistant" ? "model" : m.Role,  // ← tradução! "assistant" vira "model"
                parts = new[] { new { text = m.Content } }
            }),
            tools = new[]
            {
                new
                {
                    function_declarations = ferramentas.Select(f => new  // ← "tools" tem estrutura diferente
                    {
                        name = f.Nome,
                        description = f.Descricao,
                        parameters = new
                        {
                            type = "object",
                            properties = f.Parametros,
                            required = f.Obrigatorios
                        }
                    })
                }
            }
        };
    }

    private async Task<LlmResponse> ChamarApiAsync(object payload)
    {
        var url = $"{_url}/models/{_model}:generateContent?key={_apiKey}";
        var httpResponse = await _http.PostAsJsonAsync(url, payload);

        // ← adiciona isso temporariamente
        if (!httpResponse.IsSuccessStatusCode)
        {
            var erro = await httpResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"ERRO GEMINI: {erro}");
            throw new Exception($"Gemini retornou {httpResponse.StatusCode}: {erro}");
        }

        httpResponse.EnsureSuccessStatusCode();

        var json = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();
        return TraduzirResposta(json);
    }

    private LlmResponse TraduzirResposta(JsonElement json)
    {
        var candidate = json
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0];

        // Gemini retorna "functionCall" quando quer usar uma ferramenta
        if (candidate.TryGetProperty("functionCall", out var functionCall))
        {
            return new LlmResponse
            {
                TipoResposta = "tool_use",
                NomeFerramenta = functionCall.GetProperty("name").GetString(),
                IdChamada = Guid.NewGuid().ToString(),   // Gemini não retorna id — geramos um
                Argumentos = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    functionCall.GetProperty("args").GetRawText())
            };
        }

        // Resposta em texto
        return new LlmResponse
        {
            TipoResposta = "text",
            TextoResposta = candidate.GetProperty("text").GetString()
        };
    }
}