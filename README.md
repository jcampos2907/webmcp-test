# BikePOS

A bike shop point-of-sale and service ticket management system built with .NET Blazor and WebMCP integration.

## Overview

BikePOS is a Blazor web application for managing bike shop operations: inventory, service tickets, and point-of-sale charging. It integrates [WebMCP](https://webmcp.dev/) to expose shop functionality to AI agents via the Model Context Protocol.

## What is WebMCP?

[WebMCP](https://webmcp.dev/) is an open-source JavaScript library that integrates websites with the Model Context Protocol (MCP), enabling users to interact with webpages through language models and AI agents. It provides:

- **Tools** - Register custom functions that AI agents can invoke on your website
- **Prompts** - Predefined templates for standardizing common LLM queries
- **Resources** - Expose webpage data and content via URIs for LLM context
- **Sampling** - Allow servers to request LLM completions with human oversight

A small widget appears on your site as the connection point. Users connect via an MCP client (like Claude Desktop), generate a token, and can then interact with your site's registered tools and resources through their AI assistant.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/bikepos.git
   cd bikepos
   ```

2. Apply database migrations:
   ```bash
   dotnet ef database update
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

The app runs at http://localhost:5141 (or https://localhost:7245).

## Features

- **Inventory Management** - CRUD for bikes (name, SKU, color, brand, price)
- **POS Terminal** - Charge customers, set cashier, track terminal connectivity (JS interop demo)
- **Service Tickets** - (Planned) Create, edit, and charge service tickets for bike repairs
- **WebMCP Integration** - AI agents can list, search, and navigate bike inventory

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
