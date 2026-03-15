using System.Net;
using System.Text.Json;

namespace LimpMaxAgent.API.Middlewares;

/// <summary>
/// Captura exceções não tratadas e retorna um JSON padronizado.
/// Evita vazar stack traces para o cliente em produção.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            erro = "Ocorreu um erro interno. Tente novamente.",
            detalhe = _env.IsDevelopment() ? ex.Message : null  // só mostra detalhe em dev
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
