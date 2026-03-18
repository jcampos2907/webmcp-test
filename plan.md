# BikePOS — Comprehensive Implementation Plan

## Vision

A complete bike service shop POS system. Handles the full lifecycle: customer walks in → service ticket is created for their bike/rim/component → mechanic is assigned → work is done → products are added → customer pays at a physical terminal.

## What's Done (Phase 1)

### Core Models & Data
- [x] Customer with address fields and dynamic meta fields (MetaFieldDefinition + CustomerMetaValue)
- [x] Bike model (serviceable item — renamed to Component in Phase 1)
- [x] ServiceTicket with status workflow (Open → InProgress → WaitingForParts → Completed → Charged → Cancelled)
- [x] Mechanic model
- [x] Service model (base service types with default pricing)
- [x] Product model (parts/consumables with inventory tracking)
- [x] TicketProduct (products added to a ticket)
- [x] Charge model (payment records)
- [x] ShopSetting (key-value shop configuration)

### Pages & Flows
- [x] Home dashboard with recent charges and quick actions
- [x] Ticket creation wizard (4 steps: Customer → Component → Service → Summary)
- [x] Ticket edit/details (unified page, read-only when Charged)
- [x] Ticket list with QuickGrid
- [x] Customer CRUD with dynamic meta fields, regex validation, conditional fields
- [x] Customer form reusable component (used in customer pages + ticket create modal)
- [x] Mechanic, Service, Product CRUD pages
- [x] Settings page (vertical nav: Profile, Component Types, Client Fields, Shop Info, Billing placeholder, Notifications placeholder)
- [x] POS Terminal with JS interop

### Infrastructure
- [x] Blazorise UI framework (Tailwind provider + FontAwesome icons + Snackbar)
- [x] WebMCP integration for AI agent access
- [x] EF Core with SQLite, migrations, seed data
- [x] Minimal API endpoints for components

---

## Critical Bugs — FIXED

1. **~~ProcessCharge does not update ticket status to Charged~~** — FIXED: ProcessCharge now sets `ticket.Status = TicketStatus.Charged` after creating the Charge record.

2. **~~Inventory never decrements~~** — FIXED: Products decrement `QuantityInStock` on ticket save, restore on product removal and ticket cancellation.

3. **Customer dialog in Create ticket flow** — Verified working: modal renders correctly with inline styles (z-index 9999).

4. **~~BikePOS.db in git~~** — FIXED: Added `*.db`, `*.db-shm`, `*.db-wal` to `.gitignore`.

5. **~~No CustomerId on ServiceTicket~~** — FIXED: Added `CustomerId` FK to ServiceTicket (Phase 2 migration).

---

## Phase 1: Rename Bike → Component — DONE

**Goal**: Generalize the "Bike" model to handle all serviceable items (bikes, rims, pedals, frames, wheels, etc.)

- [x] Rename `Models/Bike.cs` → `Models/Component.cs`, class `Bike` → `Component`
- [x] Add `ComponentType` field (string: "Bicicleta", "Aro", "Pedal", "Marco", "Rueda", "Otro")
- [x] Update `ServiceTicket.BikeId` → `ServiceTicket.ComponentId` and nav property
- [x] Update `Customer.Bikes` → `Customer.Components`
- [x] Update `BikePosContext`: DbSet, OnModelCreating, seed data
- [x] Delete standalone `BikePages/` — components are only created within ticket flow or customer context
- [x] Remove "Inventory" nav link from sidebar
- [x] Update ticket Create flow (step 2) — labels, variables, methods, added ComponentType selector
- [x] Update ticket Edit/Details — all `ticket.Bike` → `ticket.Component`
- [x] Update ticket Index columns (shows ComponentType)
- [x] Update API endpoints (`/api/bikes` → `/api/components`)
- [x] Update `webmcp-tools.js` references
- [x] Create EF migration `RenameBikeToComponent` (uses RenameTable to preserve data)
- [x] Update CLAUDE.md
- [x] Configurable component types via Settings > "Tipos de Componente" (stored in ShopSetting)

---

## Phase 2: Ticket Workflow Hardening — DONE

**Goal**: Make the ticket lifecycle robust before adding payment integration.

- [x] Add `CustomerId` FK to ServiceTicket (direct customer reference, independent of component)
- [x] Implement inventory decrement on ticket save (and restore on product removal)
- [x] Fix ProcessCharge to set `ticket.Status = TicketStatus.Charged`
- [x] Add `UpdatedAt` auto-set in SaveChanges override
- [x] Add ticket cancellation flow (restore inventory, prevent charging cancelled tickets)
- [x] Add validation: prevent charging ticket with $0 total, prevent charging cancelled/already-charged tickets
- [x] Add discount support: `DiscountPercent` on ServiceTicket with UI in Create and Edit

---

## Phase 3: Translation & Internationalization (i18n) — DONE

**Goal**: Make all UI text translatable and support locale-specific formatting (currency, dates, numbers). Centralize all text into translation files so the entire UI can be served in one consistent language, and new languages can be added without code changes.

### Approach: Toolbelt.Blazor.I18nText
- [x] Add `I18nText/Text.es.json` and `I18nText/Text.en.json` with ~260 flattened translation keys
- [x] Register `AddI18nText()` in `Program.cs`
- [x] All pages inject `I18nText` and use `L.Key` pattern for all UI strings
- [x] Per-user language selector in Settings > Profile (persisted as `user_language` in ShopSetting)
- [x] Language switch via `I18nText.SetCurrentLanguageAsync()` — instant UI update

### Pages Translated
- [x] Home dashboard — all labels, buttons, status badges, cashier modal
- [x] Ticket Create wizard — step labels, placeholders, buttons, validation messages
- [x] Ticket Edit/Details — status labels, section headers, action buttons
- [x] Ticket Index — column headers, filter labels
- [x] Ticket Delete — confirmation text
- [x] Customer pages — form labels, table headers
- [x] Mechanic, Service, Product CRUD pages
- [x] Settings page — all section titles, field labels, messages
- [x] NavMenu — link labels

### Locale-Specific Formatting (Store Setting)
- [x] `ShopCultureService` loads `CultureInfo` from `ShopSetting` key `shop_locale`
- [x] Currency: all `.ToString("C", culture)` calls use store locale (₡, $, etc.)
- [x] Dates: all `.ToString("g", culture)` calls use store locale
- [x] Locale selector in Settings > Shop Info (es-CR, es-CL, es-MX, es-CO, es-AR, en-US)
- [x] Language (user preference) and locale/currency (store setting) are independent

### Design Decisions
- **Language vs Locale separation**: User language (UI text) is per-user in Profile. Store locale (currency, dates, number formats) is per-store in Shop Info. A user can read the UI in English while the store displays prices in ₡ (Costa Rican colones).
- **Flattened keys**: Toolbelt requires valid C# identifiers, so keys use underscore notation (e.g. `Common_Save`, `Tickets_Create_Step1`)
- **Keys grouped by page/feature**: `Common_*`, `Nav_*`, `Status_*`, `Tickets_*`, `Customers_*`, `Settings_*`, etc.

### Supported Languages
- `es` — Spanish (default)
- `en` — English

**Files**: `I18nText/Text.*.json`, `Services/ShopCultureService.cs`, `Program.cs`, all `.razor` pages

---

## Phase 4: Authentication & Authorization

**Goal**: ASP.NET Core Identity with roles: Admin, Mechanic, Cashier. Auth comes before payment terminal because payment endpoints must be secured.

### Setup
- Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Models/ApplicationUser.cs` — extends `IdentityUser`, adds `DisplayName`, optional `MechanicId` FK
- Change `BikePosContext` base to `IdentityDbContext<ApplicationUser>`
- Register Identity services in `Program.cs`, add middleware

### Pages
- `Components/Pages/Account/` — Login, Register (admin-only), Logout, AccessDenied

### Authorization Matrix

| Area | Admin | Mechanic | Cashier |
|------|-------|----------|---------|
| All CRUD | Yes | — | — |
| Tickets read/write | Yes | Own only | No |
| POS Terminal | Yes | No | Yes |
| Settings | Yes | No | No |
| Customer CRUD | Yes | Read only | Read only |

### Audit Trail
- Add `CreatedBy`/`UpdatedBy` (UserId) to ServiceTicket, Charge
- Log status changes with timestamp and user

### Seed Data
- Seed roles and default admin user (`admin@bikepos.local` / changeable password)

---

## Phase 5: Payment Terminal Integration

**Goal**: Integrate with physical payment terminals via an abstracted service interface.

### New Files
- `Services/IPaymentTerminalService.cs` — interface: CreateCheckout, GetStatus, Cancel, ListDevices
- `Services/SquareTerminalService.cs` — Square Terminal API via HttpClient
- `Services/ManualPaymentService.cs` — fallback for Cash/transfer

### Charge Model Updates
- Add `PaymentStatus` (Pending/Completed/Cancelled/Failed)
- Add `TerminalCheckoutId`, `CompletedAt`
- Support multiple payment methods: Cash, Card (terminal), Transfer

### POS Terminal Updates
- "Terminal" payment method shows "Waiting for customer..." with polling
- Device selector if multiple terminals
- Partial payments support (deposit now, rest on pickup)

### Configuration
```json
{ "Square": { "AccessToken": "", "LocationId": "", "Environment": "sandbox" } }
```

No PCI scope — Square Terminal handles card data on-device.

---

## Phase 6: Parametrizable Models Expansion

**Goal**: Extend the MetaFieldDefinition pattern (already working for Customer) to other entities.

- [ ] Add `EntityType` discriminator to `MetaFieldDefinition` (e.g. "Customer", "Component", "ServiceTicket")
- [ ] Create `ComponentMetaValue`, `TicketMetaValue` tables (same pattern as `CustomerMetaValue`)
- [ ] Settings UI: add tabs per entity type in "Campos" section
- [ ] Render dynamic fields in Component and Ticket forms
- [ ] Support per-country presets (CR, Chile, Mexico defaults for tax ID formats, currencies, required fields)

**Files**: `Models/MetaFieldDefinition.cs`, `Components/Pages/SettingsPages/Index.razor`, new MetaValue models

---

## Phase 7: ERP Integration

**Goal**: Bidirectional sync with external ERP systems, configurable through admin UI.

- [ ] Add `ExternalId` and `ExternalSource` fields to all syncable models (Customer, Component, Product, ServiceTicket, Charge)
- [ ] Create `SyncMapping` model: maps BikePOS fields ↔ ERP fields per entity
- [ ] Create `SyncLog` model: tracks sync events, errors, conflicts
- [ ] Webhook/event system: model save hooks trigger outbound sync
- [ ] Inbound sync endpoint: receives ERP updates
- [ ] Settings UI: ERP connection config, field mapping editor, sync status dashboard
- [ ] Support multiple ERPs simultaneously

### Entities to Sync
- Customers ↔ ERP Contacts/Business Partners
- Products ↔ ERP Items/Inventory
- Components ↔ ERP Assets
- ServiceTickets ↔ ERP Orders/Work Orders
- Charges ↔ ERP Payments/Invoices

---

## Phase 8: Enhancements

- [ ] Customer component history (all components + service history per component)
- [ ] Ticket timeline/activity log (status changes, notes, who did what)
- [ ] Mechanic workload view (assigned tickets dashboard)
- [ ] Inventory alerts (stock below threshold)
- [ ] Reports: daily sales, revenue by service type, mechanic productivity (Chart.js)
- [ ] Notification system (WhatsApp/email when service is ready)
- [ ] Billing/invoicing (electronic invoicing for CR tax compliance)
- [ ] Print/email receipt after charge
- [ ] Refund flow (reverse charge, reopen ticket)
- [ ] CSV export for reports

---

## Phase 9: WebMCP Expansion

**Goal**: Expose full POS workflow to AI agents.

### New Tools
- list-tickets, get-ticket, search-tickets, create-ticket, update-ticket-status
- list-mechanics, list-services, list-products
- get-pos-summary, process-charge
- list-customers, get-customer

### New Prompts
- ticket-summary, daily-report, inventory-status

---

## Architecture Principles

### Parametrizable Models
All core models support dynamic fields via `MetaFieldDefinition` + entity-specific MetaValue tables. MetaFieldDefinitions are configurable in Settings: label, key, type, regex validation, format mask, select options, conditional visibility, sort order. When auth is integrated, admins manage these through the UI.

### What Should Be Parametrizable Per Country/Deployment
- Currency and currency format
- Tax ID formats and labels (Cédula in CR, RUT in Chile, RFC in Mexico)
- Required fields per entity
- Custom model fields
- Invoice/receipt formats
- Payment methods available
- UI language (via i18n, Phase 3)

### Tech Stack Rules
- Blazorise UI with Tailwind provider only (no raw Bootstrap, no other UI libs)
- FontAwesome icons via Blazorise.Icons.FontAwesome
- Blazorise.Snackbar for toast notifications
- EF Core with SQLite (IDbContextFactory pattern)
- Interactive Server render mode
- Toolbelt.Blazor.I18nText for i18n (typed C# classes generated from JSON at build time)

---

## Phase Dependencies

```
Critical Bugs (immediate) ✅
  → Phase 1 (Bike→Component rename) ✅
    → Phase 2 (Ticket hardening) ✅
      → Phase 3 (i18n / Translation) ✅
        → Phase 4 (Auth)
          → Phase 5 (Payment terminal)
            → Phase 6 (Parametrizable expansion)
            → Phase 7 (ERP integration)
            → Phase 8 (Enhancements)
            → Phase 9 (WebMCP expansion)
```

Phases 6-9 can run in parallel once Auth and Payment are done.

---

## Verification

1. **Bugs**: ProcessCharge updates status, inventory decrements, customer dialog works, DB not in git ✅
2. **Phase 1**: All references to "Bike" replaced, migration applies cleanly, ticket flow works with component types ✅
3. **Phase 2**: Inventory tracks correctly, cancellation restores stock, discounts apply to total ✅
4. **Phase 3**: Switch locale in Settings → all UI text changes language, currency/date formats match locale
5. **Phase 4**: Login/logout works, role restrictions enforced, audit trail records user actions
6. **Phase 5**: Square sandbox checkout completes, polling updates status, Charge record persisted
7. **Phase 6**: Dynamic fields render on Component and Ticket forms, validation works
8. **Phase 7**: Create customer in BikePOS → appears in ERP, edit in ERP → syncs back
9. **Phase 8**: Dashboard shows metrics, reports export CSV, notifications send
10. **Phase 9**: WebMCP tools return correct data via Claude Desktop
