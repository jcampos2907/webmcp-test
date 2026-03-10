# WebMCP

A .NET Blazor application showcasing WebMCP (Model Context Protocol) integration.

## Overview

This project demonstrates how to build a Blazor web application with [WebMCP](https://webmcp.dev/) integration, using Entity Framework Core for data access.

## What is WebMCP?

[WebMCP](https://webmcp.dev/) is an open-source JavaScript library that integrates websites with the Model Context Protocol (MCP), enabling users to interact with webpages through language models and AI agents. It provides:

- **Tools** - Register custom functions that AI agents can invoke on your website
- **Prompts** - Predefined templates for standardizing common LLM queries
- **Resources** - Expose webpage data and content via URIs for LLM context
- **Sampling** - Allow servers to request LLM completions with human oversight

A small widget appears on your site as the connection point. Users connect via an MCP client (like Claude Desktop), generate a token, and can then interact with your site's registered tools and resources through their AI assistant.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A supported database (configured via `appsettings.json`)

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/webmcp.git
   cd webmcp
   ```

2. Apply database migrations:
   ```bash
   dotnet ef database update
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

## Project Structure

- `Components/` - Blazor components and pages
- `Models/` - Data models (e.g., `Bike`)
- `Migrations/` - EF Core database migrations

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
