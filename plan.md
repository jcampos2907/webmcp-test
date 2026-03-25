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

## Phase 4: Authentication, Authorization & Multi-Tenancy

**Goal**: Secure the app with external OAuth/OIDC and support multi-tenant operations. A conglomerate can own multiple companies (each in a different country with its own currency), and each company can have multiple stores/locations. Users are assigned roles per store. All data is row-filtered by StoreId in a shared database.

### Design Decisions
- **OIDC for authentication** — IdP (Keycloak, Authentik, Azure AD, etc.) handles identity. No local passwords.
- **No ASP.NET Core Identity** — no IdentityUser, no password hashing, no Identity tables.
- **Row-level multi-tenancy** — all data tables get a `StoreId` FK. EF Core global query filters scope all queries to the current store. One database, one deployment.
- **Store assignment is local** — `StoreUser` table maps (UserId → StoreId → Role). The IdP authenticates; the app determines store access and roles. A user can be `admin` at Store A and `mechanic` at Store B.
- **Currency/locale is company-level** — a conglomerate may operate companies in different countries (e.g. Costa Rica uses ₡, Chile uses $). Each company has its own currency/locale settings.
- **Cashier = logged-in user** — POS terminal uses the authenticated user's name, no manual cashier input.

### Tenant Hierarchy
```
Conglomerate (optional top level — e.g. "FamCR Group")
  └─ Company (legal/financial entity — e.g. "BikePOS Costa Rica S.A.")
      ├─ Currency, locale, tax settings (company-wide)
      └─ Store / Location (physical site — e.g. "Sucursal Escazú")
          ├─ All operational data scoped here (tickets, customers, products, etc.)
          └─ StoreUser (userId + role per store)
```

### New Models
- **Conglomerate**: `Id`, `Name`, `CreatedAt`
- **Company**: `Id`, `ConglomerateId`, `Name`, `Locale` (e.g. "es-CR"), `Currency` (e.g. "CRC"), `TaxId`, `CreatedAt`
- **Store**: `Id`, `CompanyId`, `Name`, `Address`, `Phone`, `Email`, `IsActive`, `CreatedAt`
- **AppUser**: `Id`, `ExternalSubjectId` (from IdP `sub` claim), `DisplayName`, `Email`, `LastLoginAt`
- **StoreUser**: `Id`, `AppUserId`, `StoreId`, `Role` (enum: SuperAdmin, Admin, Mechanic, Cashier)

### Implementation Steps (one at a time, testable independently)

#### Step 1: OIDC plumbing + login/logout — DONE
- [x] OIDC config in `appsettings.json`, Authentication + Cookie + OpenIdConnect in `Program.cs`
- [x] Auth middleware, `CascadingAuthenticationState`, `AuthorizeRouteView`
- [x] Login/logout HTTP endpoints, user initials + logout in NavMenu
- [x] FallbackPolicy requires authenticated users on all pages

#### Step 2: Tenant models + migration — DONE
- [x] Create models: `Conglomerate`, `Company`, `Store`, `AppUser`, `StoreUser`
- [x] Create `StoreRole` enum: `SuperAdmin`, `Admin`, `Mechanic`, `Cashier`
- [x] Add `StoreId` FK (nullable) to all existing data models
- [x] Add `CreatedBy`/`UpdatedBy` to ServiceTicket and Charge
- [x] Register DbSets, create + apply `AddMultiTenancy` migration
- [x] Seed default conglomerate + company + store, all sample data gets StoreId=1

#### Step 3: Tenant resolution + scoped context — DONE
- [x] `TenantContext` scoped service populated from cookie claims
- [x] `OnTokenValidated` upserts `AppUser`, resolves `StoreUser`, adds tenant claims to cookie
- [x] Auto-assigns SuperAdmin if IdP `roles` claim contains `superadmin` or first user ever
- [x] `TenantInitializer` wrapper redirects users without store access to `/account/no-access`
- [x] `TenantDbContextFactory` wrapper auto-sets `CurrentStoreId` on created contexts
- [x] EF Core global query filters on all 9 tenant-scoped entities
- [x] `ShopCultureService` reads locale from `Company` model (falls back to `ShopSetting`)

#### Step 4: Role-based authorization — DONE
- [x] Role from `StoreUser` added as `ClaimTypes.Role` claim during `OnTokenValidated`
- [x] `[Authorize(Roles = "...")]` on all 25 pages per authorization matrix
- [x] NavMenu: show/hide links based on `TenantContext.Role`
- [x] `AccessDenied` page for authenticated users without required role
- [x] POS Terminal: cashier = logged-in user from `TenantContext.DisplayName` (removed manual cashier modal)

#### Step 5: Superadmin management UI — DONE
- [x] Settings > Companies: superadmin can create/edit/delete companies (name, locale, currency, tax ID)
- [x] Settings > Stores: superadmin can create/edit/activate/deactivate stores within a company
- [x] Settings > Users: view all users, expand to see/edit store assignments, add/remove roles per store
- [x] Admin section divider in Settings vertical nav (Companies, Stores, Users)
- [x] `ConglomerateId` added to `TenantContext` + populated from claims in `OnTokenValidated`
- [x] Locale dropdown removed from Shop Info — shows read-only note pointing to Companies section
- [x] Locale-to-currency auto-mapping in Companies section (es-CR→CRC, en-US→USD, etc.)
- [x] Safety: cannot delete company with stores, cannot remove last SuperAdmin assignment
- [x] ~40 new i18n keys in both `Text.en.json` and `Text.es.json`
- **Test**: Superadmin creates a new company + store → can assign users to it → locale/currency set at company level.

#### Step 6: Store switcher + audit trail — DONE
- [x] Store switcher "Switch to" button in Organization diagram (SuperAdmin only)
- [x] Cookie-based store override with JS interop + TenantInitializer dual-path (HttpContext + JS)
- [x] `TenantContext.SwitchContext` + `IsOverridden` flag prevents `PopulateFromClaims` overwrite
- [x] `TenantDbContextFactory` decorator auto-sets `CurrentStoreId` on all DbContext instances
- [x] Global interactive render mode (`Routes @rendermode="InteractiveServer"`) for shared DI scope
- [x] URL-routed settings tabs (`/settings/{section}`) — tab persists on refresh and store switch
- [x] `CreatedBy`/`UpdatedBy` populated via `Tenant.UserIdentifier` on all ticket/charge save operations
- [x] Audit trail displayed in ticket detail view (created by, updated by with timestamps)
- **Test**: SuperAdmin switches stores in Organization → sees different data, stays on org tab. Ticket shows creator/updater.

#### Step 7: OAuth GUI setup + Developer role — DONE
- [x] `Developer` role added to `StoreRole` enum (can access Settings + OAuth config)
- [x] `OidcConfig` model: stores OIDC provider settings per conglomerate (authority, client ID/secret, scopes, etc.)
- [x] Client ID and Client Secret encrypted at rest via ASP.NET Data Protection API (`SecretProtector` service)
- [x] OAuth settings section in Settings page with: provider name, authority URL, client ID/secret (masked), response type, scopes, advanced toggles
- [x] Read-only callback URLs panel for easy IdP configuration copy-paste
- [x] Connection status indicator (configured / not configured)
- [x] OAuth tab visible only to SuperAdmin and Developer roles
- [x] i18n keys for all OAuth UI strings (en + es)
- **Test**: Developer user sees OAuth tab, configures provider → secrets stored encrypted in DB. Regular admin cannot see the tab.

### Authorization Matrix

| Area | superadmin | developer | admin | mechanic | cashier |
|------|------------|-----------|-------|----------|---------|
| Companies & Stores (create/edit/delete) | Yes | — | — | — | — |
| User management (assign roles per store) | Yes | — | — | — | — |
| Settings (meta fields, component types) | Yes | — | — | — | — |
| Payment terminals (per store) | Yes | — | Yes | — | — |
| OAuth / OIDC configuration | Yes | Yes | — | — | — |
| Services CRUD | Yes | — | Read/Write | — | — |
| Products CRUD | Yes | — | Read/Write | — | — |
| Mechanics CRUD | Yes | — | Read/Write | — | — |
| Components CRUD | Yes | — | Read/Write | — | — |
| Tickets read/write | Yes | — | Read/Write | Own only | — |
| Customer CRUD | Yes | — | Read/Write | Read only | Read only |
| POS Terminal | Yes | — | Yes | — | Yes |
| Home dashboard | Yes | Yes | Yes | Yes | Yes |

### Role Descriptions
- **superadmin**: Conglomerate owner. Creates/manages companies and stores. Manages user access. Configures system-wide settings (meta fields, component types, OAuth). Full access to all operational data across all stores.
- **developer**: Technical integration role. Can configure OAuth/OIDC provider settings and view system configuration. No access to operational data (tickets, customers, etc.).
- **admin**: Store manager. Can read and write all operational data within their assigned store(s). Uses POS terminal. Cannot create companies/stores or change system configuration.
- **mechanic**: Workshop staff. Views and works on their own assigned tickets. Read-only access to customers. Scoped to their assigned store.
- **cashier**: Front desk / POS operator. Uses the POS terminal (cashier = their own identity). Can look up customers. Scoped to their assigned store.

### IdP Configuration (admin responsibility, not app code)
- Create OIDC client with Authorization Code flow
- Set redirect URI: `https://localhost:7245/signin-oidc`
- Set post-logout redirect: `https://localhost:7245/signout-callback-oidc`
- No role claims needed from IdP — roles are managed per-store in the app

### Config Shape
```json
{
  "Oidc": {
    "Authority": "https://keycloak.example/realms/master",
    "ClientId": "bikepos",
    "ClientSecret": "..."
  }
}
```

**Files**: `Program.cs`, `appsettings.json`, `Models/AppUser.cs`, `Components/Pages/Account/`, `Components/Layout/NavMenu.razor`, `Components/App.razor`

---

## Phase 5: Payment Terminal Integration

**Goal**: Integrate with network-connected payment terminals via a vendor-agnostic abstraction. Terminals are physical devices on the local network (IP-based) that handle card data on-device (no PCI scope for the app).

### Architecture
- `IPaymentTerminalProvider` — vendor-agnostic interface for terminal communication
- `ManualPaymentProvider` — built-in fallback for Cash/Transfer (no terminal needed)
- Future vendor adapters plug in via the same interface (e.g. Ingenico, Verifone, PAX, Nexgo)

### IPaymentTerminalProvider Interface
```csharp
Task<TerminalDevice[]> DiscoverDevicesAsync();           // Network discovery or configured devices
Task<PaymentSession> CreatePaymentAsync(PaymentRequest);  // Send amount to terminal
Task<PaymentSession> GetStatusAsync(string sessionId);    // Poll terminal for result
Task<bool> CancelAsync(string sessionId);                 // Cancel in-progress payment
Task<bool> PingAsync(string deviceId);                    // Health check
```

### New Models
- **PaymentTerminal**: `Id`, `StoreId`, `Name`, `IpAddress`, `Port`, `Provider` (enum), `IsActive`, `LastSeenAt`
- **PaymentSession**: `Id`, `ChargeId`, `TerminalId`, `Status` (Pending/Processing/Completed/Failed/Cancelled), `ExternalRef`, `CreatedAt`, `CompletedAt`

### Charge Model Updates
- Add `PaymentStatus` enum (Pending/Completed/Cancelled/Failed)
- Add `PaymentSessionId` FK, `CompletedAt`
- Support multiple payment methods: Cash, Card (terminal), Transfer, Mixed

### POS Terminal Updates
- Terminal selector dropdown (configured devices for this store)
- "Card" payment method → sends amount to selected terminal → "Waiting for customer..." with polling
- Partial payments support (deposit now, rest on pickup)
- Terminal management in Settings (add/edit/remove devices per store, test connection)

### Settings > Terminals (new section, Admin+ access)
- List configured terminals for current store
- Add terminal: name, IP address, port, provider type
- Test connection button (ping)
- Device status indicator (online/offline based on last ping)

### Implementation Steps
1. [x] Models + migration (PaymentTerminal, PaymentSession, Charge updates with PaymentStatus + CompletedAt)
2. [x] `IPaymentTerminalProvider` interface + `ManualPaymentProvider` + `PaymentTerminalService` resolver
3. [x] Terminal management UI in Settings (add/edit/delete/activate/deactivate/test connection, i18n)
4. [x] POS Terminal integration (terminal selector for Card payments, send-to-terminal flow, 2s polling loop, cancel button, PaymentSession persistence)
5. [x] Partial payments (previous payments display, remaining balance calc, partial toggle, ticket only marked Charged when fully paid)

No PCI scope — terminals handle card data on-device. The app only sends the amount and receives success/failure.

---

## Phase 6: Parametrizable Models Expansion

**Goal**: Extend the MetaFieldDefinition pattern (already working for Customer) to other entities.

- [x] Add `EntityType` discriminator to `MetaFieldDefinition` — already existed, extended to support "Component" and "ServiceTicket"
- [x] Use generic `EntityMetaValue` table for all non-customer entities (no separate tables needed)
- [x] Settings UI: entity type tabs in Custom Fields section now include Customer, Component, Service Ticket, Company, Group, Store
- [x] Render dynamic fields in Ticket forms (Create step 4 + Edit page) with validation, conditional visibility, upsert save
- [x] Render dynamic fields in Component creation (inline in ticket wizard step 2) with save on create
- [x] Support per-country presets — auto-applied from company's `CountryCode`, conditional field linking, no overwrite of custom fields
- [x] JS action scripts on meta fields — `ActionEvent` (blur/input/change) + `ActionScript` with sandboxed execution via `new Function` + global shadowing + blocklist
- [x] Input masks — `FormatMask` property with real-time masking via JS interop (`9`=digit, `A`=letter, `X`=alphanumeric)
- [x] Default values — `DefaultValue` property auto-populated on form load
- [x] Inline regex validation on blur — `RegexPattern` + `RegexMessage` enforced in all entity forms
- [x] Security hardening — client-side blocklist + server-side validation of action scripts before save
- [x] Delete custom field functionality in Settings
- [x] Form Editor — unified field layout editor with block-scoped drag-and-drop reordering (see below)

### Form Editor

**Goal**: Provide a visual editor in Settings where admins can see ALL fields for an entity (base model fields + custom meta fields) in a single, reorderable list. This controls the render order and layout of fields in the actual entity forms (CustomerForm, Ticket Create/Edit, Component section).

#### Concept
- **Unified field list**: Base entity fields (e.g. FirstName, LastName, Phone, Email for Customer) appear alongside custom meta fields in one sortable list.
- **Base fields are locked**: They cannot be edited, deleted, or hidden — only reordered. They display with a lock icon and muted styling.
- **Custom meta fields are fully editable**: Edit, delete, reorder — same capabilities as the current Custom Fields section, but now in context with base fields.
- **Drag-and-drop reordering**: All fields (base + custom) can be dragged to reorder. The sort order determines how fields render in the actual forms.
- **Per-entity-type tabs**: Same entity type tabs as current Custom Fields section (Customer, Component, Service Ticket, Company, Group, Store).

#### Data Model Changes
- New model **`BaseFieldLayout`**: `Id`, `EntityType`, `FieldKey` (e.g. "FirstName"), `Label`, `SortOrder`, `CompanyId`, `StoreId`
- On first load for an entity type, auto-seed `BaseFieldLayout` records from a static registry of known base fields per entity
- Meta fields already have `SortOrder` — unify the sort space so base fields and meta fields interleave correctly
- All sort orders are relative integers (0, 1, 2, ...) — renumbered on drag-drop save

#### Base Field Registry (static, per entity type)
| Entity | Base Fields |
|--------|-------------|
| Customer | FirstName, LastName, Phone, Email, Address |
| Component | Name, ComponentType, Brand, Model, Color, SerialNumber, Notes |
| ServiceTicket | Customer, Component, Mechanic, Services, Status, DiscountPercent, Notes |
| Company | Name, Locale, Currency, TaxId, CountryCode |
| Store | Name, Address, Phone, Email |

#### UI Design
- Replace (or enhance) current Custom Fields tab with Form Editor
- Each entity type tab shows a vertical sortable list of field cards:
  - **Base field card**: Lock icon, field label (localized), field key (muted), type badge ("text", "select", etc.) — non-interactive except drag handle
  - **Meta field card**: Drag handle, field label, field key, type badge, Edit button, Delete button, Active/Inactive toggle
- Drag handle on left side of each card for reordering
- "Add Custom Field" button at bottom (opens existing field editor form)
- Save button persists new sort orders for all fields

#### Drag-and-Drop Implementation
- Use HTML5 Drag and Drop API via JS interop (Blazor doesn't have native sortable lists)
- JS module `wwwroot/js/form-editor-dnd.js`:
  - `initSortable(containerSelector, dotNetRef)` — attaches drag listeners to `.field-card` elements
  - On drop: reorders DOM, calls `[JSInvokable] OnFieldReordered(string[] orderedIds)` back to Blazor
  - Visual feedback: drag ghost, drop indicator line between cards
- Blazor component receives new order, updates `SortOrder` on both `BaseFieldLayout` and `MetaFieldDefinition` records, saves to DB

#### Form Rendering Changes
- `CustomerForm.razor`, `Create.razor`, `Edit.razor` — instead of rendering base fields first then meta fields separately, load the unified sort order and render ALL fields in sort order
- Each form builds a merged list: `List<FormField>` where `FormField` is a union type (base field reference or meta field definition), sorted by `SortOrder`
- Base fields render their existing Blazorise inputs; meta fields render the dynamic field template (text/select/textarea with mask, validation, conditional visibility)
- Fallback: if no `BaseFieldLayout` records exist (first run), render in default order (base fields first, then meta fields by their `SortOrder`)

#### Implementation Steps
1. [x] Create `BaseFieldLayout` model + migration
2. [x] Create static base field registry (`BaseFieldLayout.GetBaseFields(entityType)`)
3. [x] Create `EditorField` union type + merge/sort logic
4. [x] Build `form-editor-dnd.js` — HTML5 sortable with Blazor interop
5. [x] Build Form Editor UI component in Settings (replaces Custom Fields section)
6. [x] Wire drag-drop save — persist reordered `SortOrder` values
7. [x] Update `CustomerForm.razor` to render fields in unified sort order
8. [x] Update `Create.razor` (ticket) to render fields in unified sort order
9. [x] Update `Edit.razor` (ticket) to render fields in unified sort order
10. [x] Auto-seed `BaseFieldLayout` on first load per entity type
11. [x] i18n keys for Form Editor UI strings

**Block/Zone System**: Fields are grouped into blocks (e.g. Customer has "info" + "address", ServiceTicket has "header"/"details"/"products"/"summary"/"totals"). Fixed blocks (header, products, totals) render structurally and cannot be reordered. Non-fixed blocks allow field reordering within them. `FormBlockDefinition.cs` defines the block registry per entity type. The Form Editor renders each block as a section with per-block DnD containers. `form-editor-dnd.js` supports multi-container block-scoped drag-and-drop.

**Files**: `Models/BaseFieldLayout.cs`, `Models/FormBlockDefinition.cs`, `wwwroot/js/form-editor-dnd.js`, `Components/Pages/SettingsPages/Index.razor`, `Components/Pages/CustomerPages/CustomerForm.razor`, `Components/Pages/ServiceTicketPages/Create.razor`, `Components/Pages/ServiceTicketPages/Edit.razor`, `I18nText/Text.*.json`

---

## Phase 7: Enhancements

- [x] Customer component history (all components + service history per component)
- [x] Ticket timeline/activity log (status changes, notes, who did what)
- [x] Mechanic workload view (assigned tickets dashboard)
- ~~Inventory alerts~~ — deferred to ERP integration phase
- [x] Reports: daily sales, revenue by service type, mechanic productivity (Chart.js)
- [x] Notification system (WhatsApp/email when service is ready)
- ~~Billing/invoicing~~ — deferred to ERP integration phase
- [x] Print/email receipt after charge
- [x] Refund flow (reverse charge, reopen ticket)
- [x] CSV export for reports
- [x] Zero-flash i18n — eliminate the English flash on page load (see below)

### Zero-Flash i18n

**Problem**: Every page starts with `Text L = new()` (English defaults) and only resolves the correct language after `OnInitializedAsync` completes. This causes a visible flash of English text before the correct language renders.

**Solution**: Resolve language once before any component renders, using a three-layer approach:

#### 1. Language cookie + middleware
- Create `I18nCookieMiddleware` that runs before the Blazor circuit starts
- Reads a `lang` cookie from the request
- If no cookie exists, falls back to the `Accept-Language` header (picks `es` or `en`, defaults to `es`)
- Calls `I18nText.SetCurrentLanguageAsync(lang)` so the i18n system is pre-configured
- Store resolved language in `HttpContext.Items["lang"]` for downstream use

#### 2. Cookie write on language change
- In Settings profile save (`SaveProfile`), write/update the `lang` cookie via JS interop (`document.cookie`)
- Also write the cookie on first login if the user has a saved preference in the DB

#### 3. MainLayout cascading text table
- `MainLayout.razor` loads the `Text` table once in `OnInitializedAsync`
- Cascades it as a `CascadingValue<Text>` to all child components
- Child components receive `[CascadingParameter] Text L` — no per-page language loading needed
- Remove per-page `Text L = new()` and `I18nText.GetTextTableAsync` calls

#### Implementation steps
1. [ ] Create `I18nCookieMiddleware` — read cookie / Accept-Language, set i18n language
2. [ ] Register middleware in `Program.cs` before `app.MapRazorComponents`
3. [ ] Update `MainLayout.razor` — load Text table, cascade as `CascadingValue<Text>`
4. [ ] Update Settings `SaveProfile` — write `lang` cookie via JS interop on language change
5. [ ] Remove per-page `Text L = new()` + `GetTextTableAsync` calls from all pages (use cascading parameter)
6. [ ] Test: first visit (no cookie) uses browser language, subsequent visits use cookie, language switch is instant

**Files**: `Middleware/I18nCookieMiddleware.cs`, `Program.cs`, `Components/Layout/MainLayout.razor`, `Components/Pages/SettingsPages/Index.razor`, all page components

---

## Phase 8: ERP Integration

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
            → Phase 7 (Enhancements) ← was Phase 8, moved up
            → Phase 8 (ERP integration) ← was Phase 7, moved down
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
6. **Phase 5**: Terminal configured in Settings → payment sent to device → polling updates status → Charge record persisted
7. **Phase 6**: Dynamic fields render on Component and Ticket forms, validation works
8. **Phase 7**: Dashboard shows metrics, reports export CSV, notifications send
9. **Phase 8**: Create customer in BikePOS → appears in ERP, edit in ERP → syncs back
10. **Phase 9**: WebMCP tools return correct data via Claude Desktop
