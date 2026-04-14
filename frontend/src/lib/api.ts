async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    headers: init?.body ? { "Content-Type": "application/json" } : undefined,
    ...init,
  })
  if (!res.ok) {
    const text = await res.text().catch(() => "")
    throw new Error(`API ${res.status}${text ? `: ${text}` : ""}`)
  }
  if (res.status === 204) return undefined as T
  return res.json()
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
}

// Dashboard
export type DailySalesRow = {
  date: string
  totalSales: number
  ticketCount: number
  chargeCount: number
}

export const dashboardApi = {
  dailySales: (from?: string, to?: string) => {
    const params = new URLSearchParams()
    if (from) params.set("from", from)
    if (to) params.set("to", to)
    const qs = params.toString()
    return request<DailySalesRow[]>(`/api/dashboard/daily-sales${qs ? `?${qs}` : ""}`)
  },
}
