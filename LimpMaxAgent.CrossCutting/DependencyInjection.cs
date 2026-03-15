using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LimpMaxAgent.Application.Interfaces;
using LimpMaxAgent.Application.Services;
using LimpMaxAgent.Domain.Interfaces;
using LimpMaxAgent.Infrastructure.AI;
using LimpMaxAgent.Infrastructure.Repositories;

namespace LimpMaxAgent.CrossCutting;

public static class DependencyInjection
{
    public static IServiceCollection AddLimpMaxServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LimpMaxDB")
            ?? throw new InvalidOperationException("Connection string 'LimpMaxDB' não encontrada.");

        // -------------------------------------------------------
        // LLM — a factory decide qual cliente instanciar
        // baseado em "Llm:Provider" no appsettings.json
        // -------------------------------------------------------
        services.AddSingleton<LlmFactory>();
        services.AddSingleton<ILlmClient>(sp =>
        {
            var factory = sp.GetRequiredService<LlmFactory>();
            return factory.Criar();     // ← aqui a mágica acontece
        });

        // Repositórios
        services.AddScoped<IProdutoRepository>(_ => new ProdutoRepository(connectionString));
        services.AddScoped<IPedidoRepository>(_ => new PedidoRepository(connectionString));
        services.AddScoped<IClienteRepository>(_ => new ClienteRepository(connectionString));

        // Serviços
        services.AddScoped<IAgentToolsService, AgentToolsService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}