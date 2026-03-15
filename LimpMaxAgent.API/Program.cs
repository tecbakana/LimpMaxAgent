using LimpMaxAgent.API.Middlewares;
using LimpMaxAgent.CrossCutting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// -----------------------------------------------
// Serviços
// -----------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LimpMax Agent API", Version = "v1" });
});

// Registra todos os serviços da aplicação (ver CrossCutting/DependencyInjection.cs)
builder.Services.AddLimpMaxServices(builder.Configuration);

// CORS — libera acesso do frontend local durante desenvolvimento
builder.Services.AddCors(options =>
{
    options.AddPolicy("DesenvolvimentoLocal", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// -----------------------------------------------
// Pipeline HTTP
// -----------------------------------------------
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>(); // tratamento global de erros

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DesenvolvimentoLocal");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"ERRO NA INICIALIZAÇÃO: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ReadLine(); // segura a janela aberta
}
