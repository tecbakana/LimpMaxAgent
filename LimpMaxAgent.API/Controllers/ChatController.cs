using Microsoft.AspNetCore.Mvc;
using LimpMaxAgent.Application.DTOs;
using LimpMaxAgent.Application.Interfaces;

namespace LimpMaxAgent.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;
    private readonly IConfiguration _configuration;  // ← adiciona

    public ChatController(IChatService chatService, ILogger<ChatController> logger, IConfiguration configuration)
    {
        _chatService = chatService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Envia uma mensagem ao agente e recebe a resposta.
    /// O histórico deve ser mantido pelo cliente (frontend) e enviado a cada requisição.
    /// </summary>
    /// <remarks>
    /// Exemplo de request:
    /// {
    ///   "mensagem": "Tem desinfetante 5L?",
    ///   "clienteId": 1,
    ///   "historico": []
    /// }
    /// </remarks>
    [HttpPost("mensagem")]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EnviarMensagem([FromBody] ChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Mensagem))
            return BadRequest("Mensagem não pode ser vazia.");

        if (request.ClienteId <= 0)
            return BadRequest("ClienteId inválido.");

        _logger.LogInformation("Chat - Cliente {ClienteId}: {Mensagem}", request.ClienteId, request.Mensagem);

        var response = await _chatService.ProcessarMensagemAsync(request);

        if (response.ToolUsada)
            _logger.LogInformation("Tool utilizada: {Tool}", response.NomeTool);

        return Ok(response);
    }

    [HttpGet("modelos")]
    public async Task<IActionResult> ListarModelos()
    {
        var apiKey = _configuration["Llm:Gemini:ApiKey"];
        var http = new HttpClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
        var response = await http.GetStringAsync(url);
        return Ok(response);
    }

}
