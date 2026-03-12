# BikePOS — Product Plan

## Vision
A complete bike service shop POS system. Handles the full lifecycle: customer walks in → service ticket is created for their bike/rim/component → mechanic is assigned → work is done → products are added → customer pays at a physical terminal.

## What's Done

### Core Models & Data
- [x] Customer with address fields and dynamic meta fields (MetaFieldDefinition + CustomerMetaValue)
- [x] Bike model (serviceable item — needs rename to Component, see Phase 2)
- [x] ServiceTicket with status workflow (Open → InProgress → WaitingForParts → Completed → Charged → Cancelled)
- [x] Mechanic model
- [x] Service model (base service types with default pricing)
- [x] Product model (parts/consumables with inventory tracking)
- [x] TicketProduct (products added to a ticket)
- [x] Charge model (payment records)
- [x] ShopSetting (key-value shop configuration)

### Pages & Flows
- [x] Home dashboard with recent charges and quick actions
- [x] Ticket creation wizard (4 steps: Customer → Bike → Service → Summary)
- [x] Ticket edit/details (unified page, read-only when Charged)
- [x] Ticket list with QuickGrid
- [x] Customer CRUD with dynamic meta fields, regex validation, conditional fields
- [x] Customer form reusable component (used in customer pages + ticket create modal)
- [x] Mechanic, Service, Product CRUD pages
- [x] Settings page (vertical nav: Profile, Client Fields, Shop Info, Billing placeholder, Notifications placeholder)
- [x] POS Terminal with JS interop

### Infrastructure
- [x] Blazorise UI framework (Tailwind provider + FontAwesome icons + Snackbar)
- [x] WebMCP integration for AI agent access
- [x] EF Core with SQLite, migrations, seed data
- [x] Minimal API endpoints for bikes

## Phase 2 — Rename Bike → Component

The "Bike" model should become "Component" to accommodate all serviceable items (bikes, rims, pedals, frames, wheels, etc.).

- [ ] Rename `Models/Bike.cs` → `Models/Component.cs`, class `Bike` → `Component`
- [ ] Add `ComponentType` field (e.g. "Bicicleta", "Aro", "Pedal", "Marco", "Rueda", "Otro")
- [ ] Update `ServiceTicket.BikeId` → `ServiceTicket.ComponentId` and nav property
- [ ] Update `Customer.Bikes` → `Customer.Components`
- [ ] Update `DbContext`: DbSet, OnModelCreating, seed data
- [ ] Delete standalone `BikePages/` — components are only created within ticket flow or customer context
- [ ] Remove "Inventory" nav link from sidebar
- [ ] Update ticket Create flow (step 2) — labels, variables, methods
- [ ] Update ticket Edit/Details — all `ticket.Bike` → `ticket.Component`
- [ ] Update ticket Index columns
- [ ] Update API endpoints (`/api/bikes` → `/api/components`)
- [ ] Update webmcp-tools.js references
- [ ] Create EF migration `RenameBikeToComponent`
- [ ] Update CLAUDE.md to reflect changes

## Phase 3 — Payment Terminal Integration

Integrate with a physical payment terminal so the cashier can charge directly from the POS.

- [ ] Research terminal options (SumUp, Square, or local CR provider)
- [ ] Define payment flow: Ticket → "Cobrar" → select payment method → terminal processes → Charge record created → ticket status → Charged
- [ ] Support multiple payment methods: cash, card (terminal), transfer
- [ ] Partial payments (e.g. deposit now, rest on pickup)
- [ ] Print/email receipt after charge
- [ ] Refund flow (reverse a charge, reopen ticket)

## Architecture Principle — Parametrizable Models

All core models should be designed so that administrators can extend them at runtime without code changes. This is critical because the app may run in different countries, each with its own requirements (currencies, tax ID formats, custom fields, legal documents, etc.).

**How it works:**
- Each major entity (Customer, Component, ServiceTicket, etc.) supports dynamic fields via `MetaFieldDefinition` + entity-specific MetaValue tables (e.g. `CustomerMetaValue`).
- MetaFieldDefinitions are configurable in Settings: label, key, type, regex validation, format mask, select options, conditional visibility, sort order.
- When auth is integrated, admins will be able to add/edit/disable these fields through the Settings UI — no developer intervention needed.

**What should be parametrizable per country/deployment:**
- Currency and currency format
- Tax ID formats and labels (Cédula in CR, RUT in Chile, RFC in Mexico, etc.)
- Required fields per entity (e.g. some countries require a tax ID on every customer)
- Custom model fields (e.g. "Giro" in Chile, "Actividad Económica" in CR)
- Invoice/receipt formats and legal requirements
- Payment methods available

**Pattern to follow:**
- Already implemented for Customer (MetaFieldDefinition + CustomerMetaValue with regex, conditionals, select fields)
- Extend to Component, ServiceTicket, and other entities as needed using the same pattern

## Phase 4 — ERP Integration

Bidirectional sync with external ERP systems (e.g. SAP Business One, Odoo, QuickBooks, custom ERPs). Configurable through the admin UI — no code changes needed per ERP.

**Principles:**
- All core models (Customer, Component, Product, ServiceTicket/Order, Charge/Payment) must have hook points for external sync
- Two-way street: changes in BikePOS push to ERP, changes in ERP pull into BikePOS
- Conflict resolution strategy (last-write-wins, manual review, or configurable per entity)
- Admin UI to configure: which ERP, field mappings, sync frequency, which entities to sync

**Implementation plan:**
- [ ] Add `ExternalId` and `ExternalSource` fields to all syncable models (Customer, Component, Product, ServiceTicket, Charge)
- [ ] Create `SyncMapping` model: maps BikePOS fields ↔ ERP fields per entity, configurable in Settings
- [ ] Create `SyncLog` model: tracks sync events, errors, conflicts
- [ ] Webhook/event system: model save hooks that trigger outbound sync
- [ ] Inbound sync endpoint: receives ERP updates and applies them
- [ ] Settings UI: ERP connection config, field mapping editor, sync status dashboard
- [ ] Support multiple ERPs simultaneously (e.g. accounting in one, inventory in another)

**Entities to sync:**
- Customers ↔ ERP Contacts/Business Partners
- Products ↔ ERP Items/Inventory
- Components ↔ ERP Assets (if supported)
- ServiceTickets ↔ ERP Orders/Work Orders
- Charges ↔ ERP Payments/Invoices

## Phase 5 — Enhancements

- [ ] Customer component history (show all components belonging to a customer, with service history per component)
- [ ] Ticket timeline/activity log (status changes, notes, who did what)
- [ ] Mechanic workload view (which tickets are assigned to whom)
- [ ] Inventory alerts (product stock below threshold)
- [ ] Reports: daily sales, revenue by service type, mechanic productivity
- [ ] Multi-user support with authentication and role-based permissions (admin, cashier, mechanic)
- [ ] Notification system (WhatsApp/email when service is ready)
- [ ] Billing/invoicing (electronic invoicing for CR tax compliance)
