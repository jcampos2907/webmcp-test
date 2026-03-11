# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BikePOS is a bike shop point-of-sale system built with Blazor (.NET 10). It manages bike inventory, service tickets, and customer charges. WebMCP integration exposes shop functionality to AI agents via the Model Context Protocol.

The end goal is a full POS where bike service tickets can be created, edited, and charged.

## Common Commands

```bash
# Run the application
dotnet run

# Build
dotnet build

# Run with hot reload
dotnet watch

# EF Core migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

No test project exists currently.

## Architecture

**Runtime:** .NET 10, Blazor Interactive Server, EF Core with SQLite

**Key layers:**
- `Components/Pages/` — Blazor routable pages
  - `BikePages/` — Inventory CRUD (Index with QuickGrid, Create, Edit, Details, Delete)
  - `PosTerminal.razor` — Point-of-sale terminal with JS interop (charge, cashier, notifications, connectivity)
- `Models/Bike.cs` — Domain model with data annotation validation
- `Data/BikePosContext.cs` — EF Core DbContext
- `Data/SeedData.cs` — Database seeding on startup
- `Program.cs` — Entry point, DI setup, and minimal API endpoints (`/api/bikes`, `/api/bikes/{id}`, `/api/bikes/search`)

**JS Interop pattern:** `PosTerminal.razor` + co-located `PosTerminal.razor.js` use ES module import via `IJSObjectReference`. The JS module is imported in `OnAfterRenderAsync`, a `DotNetObjectReference` is passed for callbacks (e.g. `[JSInvokable] OnConnectionStatusChanged`), and `IAsyncDisposable` cleans up both references.

**WebMCP integration:**
- `wwwroot/js/webmcp.js` — MCP protocol widget library
- `wwwroot/js/webmcp-tools.js` — App-specific MCP tool/resource/prompt registrations (list-bikes, get-bike, search-bikes, navigate-to-bike, get-page-content)

**Rendering:** Interactive Server mode with antiforgery protection. Components use `IDbContextFactory<BikePosContext>` for data access.

**Dev URLs:** HTTP localhost:5141, HTTPS localhost:7245
