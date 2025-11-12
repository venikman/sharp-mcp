# sharp-mcp

This project hosts a local AG-UI chat experience backed by an LM Studio model and can also run as an MCP STDIO server for @modelcontextprotocol/inspector.

## Prerequisites

- .NET 10 SDK (Preview)
- LM Studio with your model served over HTTP (defaults to `http://localhost:1234/v1`) and API key `lm-studio` unless overridden.
- Node.js + bun (for MCP Inspector).

Set these environment variables as needed:

```bash
export LMSTUDIO_BASE_URL="http://localhost:1234/v1"
export LMSTUDIO_MODEL="kat-dev-mlx"
export LMSTUDIO_API_KEY="lm-studio"
```

## AG-UI Web Chat

1. Start LM Studio so it listens on `LMSTUDIO_BASE_URL`.
2. From the repo root run:
   ```bash
   dotnet run
   ```
3. Open http://localhost:5000/. The static "AG-UI Playground" UI posts to `POST /agui`, streams responses, and displays tool events.
4. If you need verbose logs, either set `DOTNET_ENVIRONMENT=Development` or add the logging filters in `Program.cs` as discussed.

## MCP Inspector Mode

1. Run the inspector proxy and point it at the MCP mode:
   ```bash
   bunx @modelcontextprotocol/inspector --command "dotnet run -- --mcp"
   ```
2. The proxy prints a URL like `http://localhost:6274/?MCP_PROXY_AUTH_TOKEN=...`. Open it in your browser (it usually opens automatically).
3. In the Inspector UI, create a new STDIO connection with:
   - Command: `dotnet`
   - Args: `run -- --mcp`
4. Once connected, use the Tools tab to invoke `AgentTools.ask` or any other tools you add. LM Studio handles the actual completions.

## Troubleshooting

- **No logging output**: ensure `DOTNET_ENVIRONMENT=Development` or add logging config in `Program.cs`/`appsettings.Development.json`.
- **Inspector errors about `--command`**: the UI's command field must be `dotnet`, not `--command`. Only the CLI flag uses `--command`.
- **400 from `/agui`**: confirm the request body uses `context: []` (the playground already does). Also confirm LM Studio is reachable at the configured endpoint.

## Build

```bash
dotnet build
```

Use `dotnet run` for AG-UI, or `dotnet run -- --mcp` when embedding inside other tooling.
