# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BikePOS is a bike service shop POS system built with Blazor (.NET 10). It handles the full service lifecycle: customers, service tickets for bikes/rims/components, mechanic assignment, product tracking, and payment processing. WebMCP integration exposes shop functionality to AI agents via the Model Context Protocol.

**Always read `plan.md` before starting significant work.** It contains the product roadmap, what's done, and what's next. Keep it updated as work progresses.

## Development Guidelines

- Write clean, readable, maintainable code. Favor clarity over cleverness.
- Reuse patterns wherever possible — if a pattern exists in the codebase, follow it.
- **Parametrizable models:** Whenever possible, design models to be extensible via MetaFieldDefinitions (dynamic key-value fields configurable through Settings). This allows admins to add custom fields without code changes.
- **UI framework:** Use only Blazorise with the Tailwind provider. Do not use raw Bootstrap or other UI libraries. Tailwind utility classes are fine for layout/spacing.
- Keep components small and reusable (e.g. CustomerForm is used in both CustomerPages and the ticket create modal).

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
  - `ServiceTicketPages/` — Ticket CRUD (Create wizard, Edit/Details unified, Index, Delete)
  - `CustomerPages/` — Customer CRUD + reusable CustomerForm component
  - `SettingsPages/` — Settings with vertical nav (Profile, Client Meta Fields, Shop Info)
  - `MechanicPages/`, `ServicePages/`, `ProductPages/` — Entity CRUD
  - `PosTerminal.razor` — Point-of-sale terminal with JS interop
- `Models/` — Domain models (Customer, Component, ServiceTicket, Mechanic, Service, Product, TicketProduct, Charge, MetaFieldDefinition, CustomerMetaValue, ShopSetting)
- `Data/BikePosContext.cs` — EF Core DbContext
- `Data/SeedData.cs` — Database seeding on startup
- `Program.cs` — Entry point, DI setup, and minimal API endpoints

**JS Interop pattern:** `PosTerminal.razor` + co-located `PosTerminal.razor.js` use ES module import via `IJSObjectReference`. The JS module is imported in `OnAfterRenderAsync`, a `DotNetObjectReference` is passed for callbacks (e.g. `[JSInvokable] OnConnectionStatusChanged`), and `IAsyncDisposable` cleans up both references.

**WebMCP integration:**
- `wwwroot/js/webmcp.js` — MCP protocol widget library
- `wwwroot/js/webmcp-tools.js` — App-specific MCP tool/resource/prompt registrations (list-components, get-component, search-components, get-page-content)

**Rendering:** Interactive Server mode with antiforgery protection. Components use `IDbContextFactory<BikePosContext>` for data access.

**Dev URLs:** HTTP localhost:5141, HTTPS localhost:7245
