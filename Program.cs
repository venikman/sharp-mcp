// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatRole = Microsoft.Extensions.AI.ChatRole;
using TextContent = Microsoft.Extensions.AI.TextContent;

if (args.Contains("--mcp"))
{
    await RunMcpServerAsync(args);
    return;
}

await RunAguiServerAsync(args);

static async Task RunAguiServerAsync(string[] args)
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.Services.AddHttpClient().AddLogging();
    builder.Services.AddAGUI();

    WebApplication app = builder.Build();

    app.UseDefaultFiles();
    app.UseStaticFiles();

    ChatClient chatClient = CreateChatClient(builder.Configuration);

    AIAgent agent = chatClient.AsIChatClient().CreateAIAgent(
        name: "AGUIAssistant",
        instructions: "You are a helpful assistant.");

    // Map the AG-UI agent endpoint
    app.MapAGUI("/agui", agent);

    await app.RunAsync();
}

static async Task RunMcpServerAsync(string[] args)
{
    string[] filteredArgs = args.Where(static arg => arg != "--mcp").ToArray();
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(filteredArgs);

    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    builder.Services.AddSingleton<IChatClient>(sp => CreateChatClient(sp.GetRequiredService<IConfiguration>()).AsIChatClient());

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    await builder.Build().RunAsync();
}

static ChatClient CreateChatClient(IConfiguration configuration)
{
    string baseUrl = configuration["LMSTUDIO_BASE_URL"] ?? "http://localhost:1234/v1";
    string apiKey = configuration["LMSTUDIO_API_KEY"] ?? "lm-studio";
    string model = configuration["LMSTUDIO_MODEL"] ?? "kat-dev-mlx";

    OpenAIClientOptions clientOptions = new()
    {
        Endpoint = new Uri(baseUrl),
    };

    return new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions)
        .GetChatClient(model);
}

[McpServerToolType]
public sealed class AgentTools(IChatClient chatClient)
{
    [McpServerTool, Description("Send a prompt to the local LM Studio model and return the response text.")]
    public async Task<string> AskAsync(
        [Description("User message to send to the agent")] string prompt,
        CancellationToken cancellationToken)
    {
        ChatResponse response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            new ChatOptions(),
            cancellationToken);

        return string.Join(
            Environment.NewLine,
            response.Messages
                .SelectMany(static m => m.Contents.OfType<TextContent>())
                .Select(static c => c.Text));
    }
}
