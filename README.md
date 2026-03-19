# LimpMaxAgent — Agente de IA com Function Calling
![.NET](https://img.shields.io/badge/.NET-6-blue)
![AI](https://img.shields.io/badge/AI-LLM-green)

Agente de IA capaz de interagir com dados estruturados (SQL Server) utilizando linguagem natural por meio de function calling controlado.

Este projeto demonstra como LLMs podem executar operações reais com segurança, sem acesso direto ao banco de dados.

---

## 🚀 Ideia principal

O modelo de IA **não acessa o banco diretamente**.  
Todas as operações são executadas por funções controladas no backend.

---

## 💡 Problema que resolve

Permite que usuários interajam com sistemas (estoque, pedidos, etc.) usando linguagem natural, sem acesso direto ao banco ou à lógica de negócio.

---

## ⚙️ Como funciona

1. Usuário envia uma mensagem (ex: “Tem desinfetante 5L?”)  
2. O LLM interpreta a intenção  
3. Caso necessário, solicita uma função (tool)  
4. O backend executa a operação no SQL Server  
5. O resultado é retornado ao modelo  
6. O modelo responde em linguagem natural  

👉 O modelo apenas **solicita ações**, nunca executa diretamente.

---

## 📸 Exemplo (Swagger)

![Swagger Example](./swaggerLimpaMax1.png)
![Swagger Example](./swaggerLimpaMax2.png)

---

## 🧠 Exemplo de fluxo

**Usuário**
"quais detergentes voce tem?"

**Sistema:**
- Identifica intenção de consulta  
- Executa `consultar_estoque`  
- Consulta o banco via Dapper  
- Retorna os dados ao modelo  

**Resposta:**
"Temos duas opções de detergente neutro:\n\n*   **Detergente Neutro 500ml**: R$2,50 a unidade, com 1200 unidades disponíveis.\n*   **Detergente Neutro 5L**: R$18,90 a unidade, com 350 unidades disponíveis.\n\nQual você gostaria de pedir ou tem interesse em saber mais?"


## 🧱 Arquitetura

- **API** → Entrada HTTP (Controllers, Middlewares)  
- **Application** → Orquestração do agente e regras  
- **Domain** → Entidades e contratos  
- **Infrastructure** → Acesso a dados (SQL Server + Dapper)  
- **CrossCutting** → Injeção de dependência 

---

## 🔧 Tecnologias

- .NET / C#  
- SQL Server  
- Dapper  
- Anthropic (Claude)  
- Function Calling / Tool Use  

---

## 🛠️ Funcionalidades do agente

- Consultar estoque  
- Registrar pedidos  
- Consultar pedidos  
- Cancelar pedidos  

---

## ▶️ Como executar

1. Criar banco via script SQL  
2. Configurar API Key  
3. Instalar dependências  
4. Executar via Visual Studio (Swagger disponível)  

---

## 📌 Próximos passos

- Autenticação JWT  
- Persistência de histórico  
- Cache de consultas  
- Suporte a múltiplas tools  
- Testes automatizados  

---

## 📍 Por que isso importa

Essa abordagem permite integrar IA com sistemas de negócio de forma segura, evitando acesso direto ao banco enquanto mantém capacidade de automação real.

# Guia de Instalação e Uso

## Estrutura da Solution

```
LimpMaxAgent/
│
├── LimpMaxAgent.API/               ← Entrada HTTP (Controllers, Middlewares, Program.cs)
│   ├── Controllers/
│   │   └── ChatController.cs       ← Endpoint POST /api/chat/mensagem
│   ├── Middlewares/
│   │   └── ExceptionMiddleware.cs  ← Captura erros globais
│   └── Program.cs                  ← Startup, DI, pipeline
│
├── LimpMaxAgent.Application/       ← Regras de negócio, orquestração
│   ├── DTOs/
│   │   └── DTOs.cs                 ← Objetos de entrada/saída
│   ├── Interfaces/
│   │   └── IServices.cs            ← Contratos dos serviços
│   └── Services/
│       ├── ChatService.cs          ← Loop principal: LLM + tool calling
│       └── AgentToolsService.cs    ← Funções que o LLM pode chamar
│
├── LimpMaxAgent.Domain/            ← Entidades e contratos do domínio
│   ├── Entities/
│   │   └── Entities.cs             ← Produto, Estoque, Cliente, Pedido, ItemPedido
│   └── Interfaces/
│       └── IRepositories.cs        ← Contratos dos repositórios
│
├── LimpMaxAgent.Infrastructure/    ← Acesso a dados (SQL Server + Dapper)
│   ├── Repositories/
│   │   └── Repositories.cs        ← ProdutoRepository, PedidoRepository, ClienteRepository
│   └── Data/
│       └── CreateDatabase.sql     ← Script para criar o banco
│
└── LimpMaxAgent.CrossCutting/      ← Configuração de DI
    └── DependencyInjection.cs      ← Registra tudo no container
```

---

## Como o Function Calling funciona (o ponto central)

```
1. Usuário envia mensagem via POST /api/chat/mensagem
         ↓
2. ChatService monta o histórico + define as tools disponíveis
         ↓
3. LLM recebe mensagem + lista de ferramentas
         ↓
4. LLM decide: preciso de dados → retorna tool_use (ex: consultar_estoque)
         ↓
5. Nosso código executa AgentToolsService.ConsultarEstoqueAsync()
         ↓
6. AgentToolsService consulta o SQL Server via Dapper
         ↓
7. Resultado (JSON) é devolvido ao LLM
         ↓
8. LLM formula a resposta em linguagem natural
         ↓
9. Resposta chega ao usuário
```

O LLM **nunca acessa o banco diretamente**. Ele apenas "pede" que o código faça isso.

---

## Ferramentas disponíveis para o agente

| Tool | Quando o LLM chama |
|---|---|
| `consultar_estoque` | Cliente pergunta sobre produto/preço |
| `registrar_pedido` | Cliente confirma que quer comprar |
| `consultar_pedidos` | Cliente pergunta sobre pedidos anteriores |
| `cancelar_pedido` | Cliente solicita cancelamento |

---

## Como rodar

### 1. Criar o banco
Abra o SQL Server Management Studio e execute:
```
LimpMaxAgent.Infrastructure/Data/CreateDatabase.sql
```

### 2. Configurar a API key
Em `LimpMaxAgent.API/appsettings.json`:
```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-..."
  }
}
```

### 3. Instalar pacotes NuGet
No Package Manager Console:
```
Install-Package Anthropic.SDK
Install-Package Dapper
Install-Package Microsoft.Data.SqlClient
```

### 4. Rodar
`F5` no Visual Studio — o Swagger abre em `https://localhost:{porta}/swagger`

---

## Testando pelo Swagger

**POST** `/api/chat/mensagem`

```json
{
  "mensagem": "Tem desinfetante 5L?",
  "clienteId": 1,
  "historico": []
}
```

Resposta esperada:
```json
{
  "resposta": "Sim! Temos o Desinfetante Pinho 5L por R$22,00, com 280 unidades disponíveis. Deseja fazer um pedido?",
  "historico": [...],
  "toolUsada": true,
  "nomeTool": "consultar_estoque"
}
```

---

## Próximos passos para evoluir

- [ ] Autenticação JWT no Controller
- [ ] Cache de produtos (evitar queries repetidas ao banco)
- [ ] Histórico persistido no banco (não depender do frontend guardar)
- [ ] Múltiplas tools numa mesma resposta (LLM pode chamar várias)
- [ ] Testes unitários dos Services (mockar repositórios com Moq)
- [ ] Frontend simples em HTML/JS para testar o chat
