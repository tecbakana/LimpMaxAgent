using LimpMaxAgent.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LimpMaxAgent.Infrastructure.AI;

/// <summary>
/// Lê "Llm:Provider" do appsettings.json e instancia o cliente correto.
/// O resto da aplicação nunca chama essa factory diretamente —
/// ela é usada apenas na configuração de DI.
/// </summary>
public class LlmFactory
{
    private readonly IConfiguration _config;

    public LlmFactory(IConfiguration config)
    {
        _config = config;
    }

    public ILlmClient Criar()
    {
        var provider = _config["Llm:Provider"]
            ?? throw new InvalidOperationException("'Llm:Provider' não configurado no appsettings.");

        // Switch expression — cada case instancia um cliente concreto
        // passando a API key do provedor correspondente
        return provider.ToLower() switch
        {
            "anthropic" => new AnthropicLlmClient(
                ObterChave("Llm:Anthropic:ApiKey")),

            "gemini" => new GeminiLlmClient(
                ObterChave("Llm:Gemini:ApiKey"),
                ObterModelo("Llm:Gemini:Model"),
                ObterChave("Llm:Gemini:Url")),

            // Para adicionar um novo provedor: 
            // 1. Crie GroqLlmClient : ILlmClient
            // 2. Adicione o case aqui
            // 3. Adicione a key no appsettings
            // — o resto da aplicação não muda nada —

            _ => throw new InvalidOperationException(
                $"Provider '{provider}' não suportado. Use: anthropic, gemini")
        };
    }

    private string ObterChave(string caminho)
    {
        return _config[caminho]
            ?? throw new InvalidOperationException($"'{caminho}' não encontrado no appsettings.");
    }
    private string ObterModelo(string caminho)
    {
        return _config[caminho]
            ?? throw new InvalidOperationException($"'{caminho}' não encontrado no appsettings.");
    }
}