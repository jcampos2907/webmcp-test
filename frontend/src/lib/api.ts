export const STORE_ID_KEY = "bikepos.storeId"

function buildHeaders(init?: RequestInit): HeadersInit | undefined {
  const headers: Record<string, string> = {}
  if (init?.body) headers["Content-Type"] = "application/json"
  const storeId = typeof window !== "undefined" ? localStorage.getItem(STORE_ID_KEY) : null
  if (storeId) headers["X-Store-Id"] = storeId
  if (init?.headers) Object.assign(headers, init.headers as Record<string, string>)
  return Object.keys(headers).length ? headers : undefined
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, { ...init, headers: buildHeaders(init) })
  if (!res.ok) {
    const text = await res.text().catch(() => "")
    throw new Error(`API ${res.status}${text ? `: ${text}` : ""}`)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

// ============ Session (current store / store switcher) ============
export type SessionStore = {
  id: string; name: string; isActive: boolean;
  companyId: string; companyName: string; countryCode: string | null;
  conglomerateId: string; conglomerateName: string;
}
export type SessionInfo = { currentStore: SessionStore | null; availableStores: SessionStore[] }

export const sessionApi = {
  get: () => request<SessionInfo>("/api/session"),
}

// Customers
export type Customer = {
  id: string
  firstName: string
  lastName: string
  fullName: string
  phone: string | null
  email: string | null
  city: string | null
}

export type CustomerDetail = {
  id: string
  firstName: string
  lastName: string
  phone: string | null
  email: string | null
  street: string | null
  city: string | null
  state: string | null
  zipCode: string | null
  country: string | null
}

export type CustomerInput = Omit<CustomerDetail, "id"> & { storeId?: string | null }

export const customersApi = {
  list: (search?: string) =>
    request<Customer[]>(`/api/customers${search ? `?search=${encodeURIComponent(search)}` : ""}`),
  get: (id: string) => request<CustomerDetail>(`/api/customers/${id}`),
  create: (input: CustomerInput) =>
    request<{ id: string }>("/api/customers", { method: "POST", body: JSON.stringify(input) }),
  update: (id: string, input: CustomerInput) =>
    request<void>(`/api/customers/${id}`, { method: "PUT", body: JSON.stringify({ ...input, id }) }),
  remove: (id: string) => request<void>(`/api/customers/${id}`, { method: "DELETE" }),
}

// Mechanics
export type Mechanic = {
  id: string
  name: string
  phone: string | null
  email: string | null
  isActive: boolean
}

export type MechanicInput = Omit<Mechanic, "id"> & { storeId?: string | null }

export const mechanicsApi = {
  list: () => request<Mechanic[]>("/api/mechanics"),
  get: (id: string) => request<Mechanic>(`/api/mechanics/${id}`),
  create: (input: MechanicInput) =>
    request<{ id: string }>("/api/mechanics", { method: "POST", body: JSON.stringify(input) }),
  update: (id: string, input: MechanicInput) =>
    request<void>(`/api/mechanics/${id}`, { method: "PUT", body: JSON.stringify({ ...input, id }) }),
  remove: (id: string) => request<void>(`/api/mechanics/${id}`, { method: "DELETE" }),
}

// Services
export type Service = {
  id: string
  name: string
  description: string | null
  defaultPrice: number
  estimatedMinutes: number | null
}

export type ServiceInput = Omit<Service, "id"> & { storeId?: string | null }

export const servicesApi = {
  list: () => request<Service[]>("/api/services"),
  get: (id: string) => request<Service>(`/api/services/${id}`),
  create: (input: ServiceInput) =>
    request<{ id: string }>("/api/services", { method: "POST", body: JSON.stringify(input) }),
  update: (id: string, input: ServiceInput) =>
    request<void>(`/api/services/${id}`, { method: "PUT", body: JSON.stringify({ ...input, id }) }),
  remove: (id: string) => request<void>(`/api/services/${id}`, { method: "DELETE" }),
}

// Products
export type Product = {
  id: string
  name: string
  sku: string | null
  price: number
  quantityInStock: number
  category: string | null
}

export type ProductInput = Omit<Product, "id"> & { storeId?: string | null }

export const productsApi = {
  list: (search?: string) =>
    request<Product[]>(`/api/products${search ? `?search=${encodeURIComponent(search)}` : ""}`),
  get: (id: string) => request<Product>(`/api/products/${id}`),
  create: (input: ProductInput) =>
    request<{ id: string }>("/api/products", { method: "POST", body: JSON.stringify(input) }),
  update: (id: string, input: ProductInput) =>
    request<void>(`/api/products/${id}`, { method: "PUT", body: JSON.stringify({ ...input, id }) }),
  remove: (id: string) => request<void>(`/api/products/${id}`, { method: "DELETE" }),
}

// Tickets
export type TicketListItem = {
  id: string
  ticketDisplay: string
  ticketNumber: number
  status: number | string
  componentName: string | null
  componentType: string | null
  customerName: string | null
  mechanicName: string | null
  price: number
  createdAt: string
}

export type TicketDetails = {
  id: string
  ticketNumber: number
  ticketDisplay: string
  status: string
  componentName: string | null
  componentType: string | null
  customerName: string | null
  mechanicName: string | null
  serviceName: string | null
  servicePrice: number
  description: string | null
  discountPercent: number
  subtotal: number
  total: number
  totalCharged: number
  remainingBalance: number
  isFullyPaid: boolean
  createdAt: string
  updatedAt: string
  createdBy: string | null
  products: Array<{
    productId: string
    productName: string
    quantity: number
    unitPrice: number
    lineTotal: number
  }>
  charges: Array<{
    id: string
    amount: number
    paymentMethod: string
    paymentStatus: string
    cashierName: string | null
    chargedAt: string
    notes: string | null
  }>
  events: Array<{
    eventType: string
    description: string | null
    createdAt: string
  }>
}

export const TICKET_STATUS: Record<number, string> = {
  0: "Open",
  1: "InProgress",
  2: "WaitingForParts",
  3: "Completed",
  4: "Cancelled",
  5: "Delivered",
}

export const ticketsApi = {
  list: (status?: string) =>
    request<TicketListItem[]>(`/api/tickets${status ? `?status=${status}` : ""}`),
  get: (id: string) => request<TicketDetails>(`/api/tickets/${id}`),
  cancel: (id: string) => request<void>(`/api/tickets/${id}/cancel`, { method: "POST" }),
  updateStatus: (id: string, status: string) =>
    request<void>(`/api/tickets/${id}/status`, {
      method: "POST",
      body: JSON.stringify({ status, changedBy: null }),
    }),
  search: (q: string) => request<TicketListItem[]>(`/api/tickets/search?q=${encodeURIComponent(q)}`),
  update: (id: string, input: {
    mechanicId: string | null
    baseServiceId: string | null
    baseServicePrice: number
    description: string | null
    discountPercent: number
    updatedBy?: string | null
  }) => request<void>(`/api/tickets/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  addProduct: (id: string, productId: string, quantity: number) =>
    request<void>(`/api/tickets/${id}/products`, { method: "POST", body: JSON.stringify({ productId, quantity }) }),
  removeProduct: (id: string, productId: string) =>
    request<void>(`/api/tickets/${id}/products/${productId}`, { method: "DELETE" }),
}

// Components
export type ComponentItem = {
  id: string
  name: string | null
  componentType: string
  brand: string | null
  color: string | null
  sku: string
  price: number
  customerId: string | null
}

export type ComponentInput = {
  name: string
  componentType: string
  customerId: string | null
  brand: string | null
  color: string | null
  sku: string | null
  price: number
  storeId?: string | null
}

export const componentsApi = {
  list: (customerId?: string) =>
    request<ComponentItem[]>(`/api/components${customerId ? `?customerId=${customerId}` : ""}`),
  create: (input: ComponentInput) =>
    request<ComponentItem>("/api/components", { method: "POST", body: JSON.stringify(input) }),
}

// Create ticket
export type CreateTicketInput = {
  componentId: string
  customerId: string | null
  mechanicId: string | null
  baseServiceId: string | null
  baseServicePrice: number
  description: string | null
  discountPercent: number
  storeId?: string | null
  createdBy?: string | null
  products: Array<{ productId: string; productName: string; unitPrice: number; quantity: number }>
}

export const createTicketApi = {
  create: (input: CreateTicketInput) =>
    request<{ ticketId: string; ticketNumber: number; ticketDisplay: string }>("/api/tickets", {
      method: "POST",
      body: JSON.stringify(input),
    }),
  charge: (id: string, amount: number, paymentMethod: string, cashierName?: string, terminalId?: string | null) =>
    request<{ chargeId: string | null; chargedAmount: number; isFullyPaid: boolean; paymentSessionId: string | null; errorMessage: string | null }>(`/api/tickets/${id}/charges`, {
      method: "POST",
      body: JSON.stringify({ amount, paymentMethod, cashierName, terminalId }),
    }),
  refund: (id: string, amount: number, paymentMethod: string, cashierName?: string) =>
    request<{ chargeId: string; refundedAmount: number }>(`/api/tickets/${id}/refunds`, {
      method: "POST",
      body: JSON.stringify({ amount, paymentMethod, cashierName }),
    }),
}

// Terminals (public) + payment sessions
export type PublicTerminal = { id: string; name: string; provider: string; isActive: boolean }
export const publicTerminalsApi = {
  list: () => request<PublicTerminal[]>("/api/terminals"),
}
export const paymentSessionsApi = {
  status: (id: string) => request<{ status: string }>(`/api/payment-sessions/${id}/status`),
  confirm: (id: string) =>
    request<{ chargeId: string | null; chargedAmount: number; isFullyPaid: boolean; errorMessage: string | null }>(
      `/api/payment-sessions/${id}/confirm`,
      { method: "POST" }
    ),
  cancel: (id: string) => request<{ cancelled: boolean }>(`/api/payment-sessions/${id}/cancel`, { method: "POST" }),
}

// Mechanic workload
export type MechanicWorkload = {
  mechanics: Mechanic[]
  ticketsByMechanic: Record<string, Array<{ id: string; ticketDisplay: string; componentName: string | null; componentType: string | null; status: number; createdAt: string }>>
  totalOpen: number
  totalInProgress: number
  totalWaiting: number
  unassigned: number
}

export const workloadApi = {
  get: () => request<MechanicWorkload>("/api/mechanics/workload"),
}

// Dashboard
export type DailySalesRow = {
  date: string
  totalSales: number
  ticketCount: number
  chargeCount: number
}

// Meta fields
export type MetaField = {
  id: string
  entityType: string
  key: string
  label: string
  fieldType: string
  isRequired: boolean
  sortOrder: number
  isActive: boolean
  options: string | null
  defaultValue: string | null
  regexPattern: string | null
}

export type MetaFieldInput = Omit<MetaField, "id">

export const metaFieldsApi = {
  list: (entityType = "Customer") =>
    request<MetaField[]>(`/api/meta-fields?entityType=${encodeURIComponent(entityType)}`),
  create: (input: MetaFieldInput) =>
    request<MetaField>("/api/meta-fields", { method: "POST", body: JSON.stringify(input) }),
  update: (id: string, input: MetaFieldInput) =>
    request<MetaField>(`/api/meta-fields/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  remove: (id: string) => request<void>(`/api/meta-fields/${id}`, { method: "DELETE" }),
}

// Shop settings
export type ShopSettings = {
  shopName: string | null
  shopAddress: string | null
  shopPhone: string | null
  shopEmail: string | null
  shopTaxId: string | null
  receiptFooter: string | null
}

export const settingsApi = {
  getShop: () => request<ShopSettings>("/api/settings/shop"),
  saveShop: (input: ShopSettings) =>
    request<void>("/api/settings/shop", { method: "PUT", body: JSON.stringify(input) }),
}

// Reports
export type DailySalesReportRow = { date: string; revenue: number; transactions: number; cash: number; card: number; transfer: number }
export type ServiceRevenueRow = { serviceName: string; revenue: number; ticketCount: number }
export type MechanicProductivityRow = { mechanicName: string; ticketCount: number; avgHoursToComplete: number }

export const reportsApi = {
  dailySales: (from: string, to: string) =>
    request<DailySalesReportRow[]>(`/api/reports/daily-sales?from=${from}&to=${to}`),
  serviceRevenue: (from: string, to: string) =>
    request<ServiceRevenueRow[]>(`/api/reports/service-revenue?from=${from}&to=${to}`),
  mechanicProductivity: (from: string, to: string) =>
    request<MechanicProductivityRow[]>(`/api/reports/mechanic-productivity?from=${from}&to=${to}`),
}

// Dashboard KPIs & activity
export type DashboardKpis = {
  todayRevenue: number
  todayTransactions: number
  openTickets: number
  readyToCharge: number
}

export type RecentCharge = {
  id: string
  amount: number
  paymentMethod: string
  ticketDisplay: string | null
  ticketId: string | null
  chargedAt: string
}

export const dashboardApi = {
  kpis: () => request<DashboardKpis>("/api/dashboard/kpis"),
  recentCharges: (take = 10) => request<RecentCharge[]>(`/api/dashboard/recent-charges?take=${take}`),
  dailySales: (from?: string, to?: string) => {
    const params = new URLSearchParams()
    if (from) params.set("from", from)
    if (to) params.set("to", to)
    const qs = params.toString()
    return request<DailySalesRow[]>(`/api/dashboard/daily-sales${qs ? `?${qs}` : ""}`)
  },
}

// ============ Admin: Organization ============
export type AdminStore = { id: string; companyId: string; name: string; address: string | null; phone: string | null; email: string | null; isActive: boolean }
export type AdminCompany = { id: string; conglomerateId: string; name: string; locale: string; currency: string; taxId: string | null; countryCode: string | null; stores: AdminStore[] }
export type AdminConglomerate = { id: string; name: string; companies: AdminCompany[] }

export type UpsertCompany = { conglomerateId: string; name: string; locale: string; currency: string; taxId: string | null; countryCode: string | null }
export type UpsertStore = { companyId: string; name: string; address: string | null; phone: string | null; email: string | null; isActive: boolean }

export const organizationApi = {
  tree: () => request<AdminConglomerate[]>("/api/admin/organization"),
  renameConglomerate: (id: string, name: string) =>
    request<void>(`/api/admin/organization/conglomerates/${id}`, { method: "PUT", body: JSON.stringify({ name }) }),
  createCompany: (input: UpsertCompany) =>
    request<{ id: string }>("/api/admin/organization/companies", { method: "POST", body: JSON.stringify(input) }),
  updateCompany: (id: string, input: UpsertCompany) =>
    request<void>(`/api/admin/organization/companies/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  deleteCompany: (id: string) =>
    request<void>(`/api/admin/organization/companies/${id}`, { method: "DELETE" }),
  createStore: (input: UpsertStore) =>
    request<{ id: string }>("/api/admin/organization/stores", { method: "POST", body: JSON.stringify(input) }),
  updateStore: (id: string, input: UpsertStore) =>
    request<void>(`/api/admin/organization/stores/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  deleteStore: (id: string) =>
    request<void>(`/api/admin/organization/stores/${id}`, { method: "DELETE" }),
}

// ============ Admin: Terminals ============
export type AdminTerminal = {
  id: string; storeId: string; storeName: string; name: string;
  ipAddress: string; port: number; provider: string; isActive: boolean; lastSeenAt: string | null;
}
export type UpsertTerminal = { storeId: string; name: string; ipAddress: string; port: number; provider: string; isActive: boolean }

export const terminalsApi = {
  list: (storeId?: string) =>
    request<AdminTerminal[]>(`/api/admin/terminals${storeId ? `?storeId=${storeId}` : ""}`),
  create: (input: UpsertTerminal) =>
    request<{ id: string }>("/api/admin/terminals", { method: "POST", body: JSON.stringify(input) }),
  update: (id: string, input: UpsertTerminal) =>
    request<void>(`/api/admin/terminals/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  remove: (id: string) => request<void>(`/api/admin/terminals/${id}`, { method: "DELETE" }),
}

// ============ Admin: Users ============
export type AdminUserRole = { storeUserId: string; storeId: string; storeName: string; role: string }
export type AdminUser = {
  id: string; displayName: string | null; email: string | null; externalSubjectId: string;
  lastLoginAt: string | null; createdAt: string; assignments: AdminUserRole[];
}

export const usersApi = {
  list: () => request<AdminUser[]>("/api/admin/users"),
  upsertRole: (userId: string, storeId: string, role: string) =>
    request<void>(`/api/admin/users/${userId}/roles`, { method: "POST", body: JSON.stringify({ storeId, role }) }),
  removeRole: (storeUserId: string) =>
    request<void>(`/api/admin/users/roles/${storeUserId}`, { method: "DELETE" }),
}

// ============ Admin: OIDC ============
export type AdminOidcConfig = {
  id: string; conglomerateId: string; authority: string; clientId: string;
  hasClientSecret: boolean; responseType: string; scopes: string;
  mapInboundClaims: boolean; saveTokens: boolean; getClaimsFromUserInfoEndpoint: boolean;
  providerName: string | null; isActive: boolean; updatedAt: string;
}
export type UpsertOidcConfig = {
  conglomerateId: string; authority: string; clientId: string; clientSecret: string | null;
  responseType: string; scopes: string; mapInboundClaims: boolean; saveTokens: boolean;
  getClaimsFromUserInfoEndpoint: boolean; providerName: string | null; isActive: boolean;
}

export const oidcApi = {
  list: () => request<AdminOidcConfig[]>("/api/admin/oidc"),
  create: (input: UpsertOidcConfig) =>
    request<{ id: string }>("/api/admin/oidc", { method: "POST", body: JSON.stringify(input) }),
  update: (id: string, input: UpsertOidcConfig) =>
    request<void>(`/api/admin/oidc/${id}`, { method: "PUT", body: JSON.stringify(input) }),
  remove: (id: string) => request<void>(`/api/admin/oidc/${id}`, { method: "DELETE" }),
}
