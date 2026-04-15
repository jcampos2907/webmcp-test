import { useEffect, useMemo, useState } from "react"
import { Link, useNavigate } from "react-router-dom"
import {
  ArrowRight,
  CreditCard,
  DollarSign,
  Package,
  Plus,
  Search,
  Ticket as TicketIcon,
  Users,
  Wrench,
} from "lucide-react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { Input } from "@/components/ui/input"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  customersApi,
  dashboardApi,
  ticketsApi,
  type Customer,
  type DashboardKpis,
  type DailySalesRow,
  type RecentCharge,
  type TicketListItem,
} from "@/lib/api"

type SearchMode = "tickets" | "customers"

export default function DashboardPage() {
  const [kpis, setKpis] = useState<DashboardKpis | null>(null)
  const [charges, setCharges] = useState<RecentCharge[] | null>(null)
  const [sales, setSales] = useState<DailySalesRow[] | null>(null)

  useEffect(() => {
    dashboardApi.kpis().then(setKpis).catch(() => setKpis({ todayRevenue: 0, todayTransactions: 0, openTickets: 0, readyToCharge: 0 }))
    dashboardApi.recentCharges(8).then(setCharges).catch(() => setCharges([]))
    dashboardApi.dailySales().then(setSales).catch(() => setSales([]))
  }, [])

  return (
    <div className="p-6 lg:p-8 max-w-7xl mx-auto space-y-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Dashboard</h1>
          <p className="text-sm text-muted-foreground">Today's activity and quick actions</p>
        </div>
        <div className="flex flex-wrap gap-3">
          <Button variant="outline" size="lg" className="gap-2" nativeButton={false} render={<Link to="/pos" />}>
            <Package className="h-4 w-4" /> Open POS
          </Button>
          <Button size="lg" className="gap-2" nativeButton={false} render={<Link to="/tickets/new" />}>
            <Plus className="h-4 w-4" /> New Ticket
          </Button>
        </div>
      </div>

      <div className="grid gap-4 grid-cols-2 lg:grid-cols-4">
        <KpiCard label="Today's Revenue" value={kpis ? formatMoney(kpis.todayRevenue) : null} icon={DollarSign} />
        <KpiCard label="Transactions" value={kpis ? String(kpis.todayTransactions) : null} icon={CreditCard} />
        <KpiCard label="Open Tickets" value={kpis ? String(kpis.openTickets) : null} icon={TicketIcon} />
        <KpiCard label="Ready to Charge" value={kpis ? String(kpis.readyToCharge) : null} icon={Wrench} accent />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Quick search</CardTitle>
            <CardDescription>Find tickets or customers fast</CardDescription>
          </CardHeader>
          <CardContent>
            <QuickSearch />
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Recent charges</CardTitle>
              <CardDescription>Latest 8 payments</CardDescription>
            </div>
            <Button size="sm" variant="ghost" nativeButton={false} render={<Link to="/reports" />}>
              View <ArrowRight />
            </Button>
          </CardHeader>
          <CardContent>
            {!charges ? (
              <div className="space-y-2">
                {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-10" />)}
              </div>
            ) : charges.length === 0 ? (
              <p className="text-sm text-muted-foreground">No charges yet.</p>
            ) : (
              <ul className="divide-y">
                {charges.map((c) => (
                  <li key={c.id} className="py-2 flex items-center justify-between text-sm">
                    <div className="min-w-0">
                      {c.ticketId ? (
                        <Link to={`/tickets/${c.ticketId}`} className="font-medium hover:underline">
                          {c.ticketDisplay ?? "Ticket"}
                        </Link>
                      ) : (
                        <span className="font-medium">Walk-in</span>
                      )}
                      <div className="text-xs text-muted-foreground flex items-center gap-2">
                        <Badge variant="secondary" className="text-[10px]">{c.paymentMethod}</Badge>
                        {new Date(c.chargedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                      </div>
                    </div>
                    <span className="tabular-nums font-medium">{formatMoney(c.amount)}</span>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Last 7 days</CardTitle>
          <CardDescription>Daily revenue breakdown</CardDescription>
        </CardHeader>
        <CardContent>
          <DailySalesChart rows={sales} />
        </CardContent>
      </Card>

      <div className="grid gap-4 md:grid-cols-3">
        <QuickLink to="/customers" icon={Users} title="Customers" description="Manage customer profiles" />
        <QuickLink to="/mechanics/workload" icon={Wrench} title="Workload" description="See mechanic assignments" />
        <QuickLink to="/reports" icon={DollarSign} title="Reports" description="Sales, services, productivity" />
      </div>
    </div>
  )
}

function KpiCard({
  label,
  value,
  icon: Icon,
  accent,
}: {
  label: string
  value: string | null
  icon: React.ComponentType<{ className?: string }>
  accent?: boolean
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-xs font-medium text-muted-foreground">{label}</CardTitle>
        <Icon className={`h-4 w-4 ${accent ? "text-primary" : "text-muted-foreground"}`} />
      </CardHeader>
      <CardContent>
        {value === null ? <Skeleton className="h-8 w-20" /> : (
          <div className="text-2xl font-semibold tabular-nums">{value}</div>
        )}
      </CardContent>
    </Card>
  )
}

function QuickLink({
  to,
  icon: Icon,
  title,
  description,
}: {
  to: string
  icon: React.ComponentType<{ className?: string }>
  title: string
  description: string
}) {
  return (
    <Link to={to}>
      <Card className="hover:border-primary transition-colors h-full">
        <CardHeader className="flex-row items-center gap-3 space-y-0">
          <div className="p-2 rounded-md bg-muted">
            <Icon className="h-5 w-5" />
          </div>
          <div>
            <CardTitle className="text-base">{title}</CardTitle>
            <CardDescription>{description}</CardDescription>
          </div>
        </CardHeader>
      </Card>
    </Link>
  )
}

function QuickSearch() {
  const [mode, setMode] = useState<SearchMode>("tickets")
  const [q, setQ] = useState("")
  const [tickets, setTickets] = useState<TicketListItem[]>([])
  const [customers, setCustomers] = useState<Customer[]>([])
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  useEffect(() => {
    const term = q.trim()
    if (!term) {
      setTickets([])
      setCustomers([])
      return
    }
    setLoading(true)
    const t = setTimeout(() => {
      if (mode === "tickets") {
        ticketsApi.search(term).then(setTickets).catch(() => setTickets([])).finally(() => setLoading(false))
      } else {
        customersApi.list(term).then(setCustomers).catch(() => setCustomers([])).finally(() => setLoading(false))
      }
    }, 200)
    return () => clearTimeout(t)
  }, [q, mode])

  return (
    <div className="space-y-3">
      <div className="flex flex-col sm:flex-row gap-2">
        <Tabs value={mode} onValueChange={(v) => setMode(v as SearchMode)}>
          <TabsList>
            <TabsTrigger value="tickets">Tickets</TabsTrigger>
            <TabsTrigger value="customers">Customers</TabsTrigger>
          </TabsList>
        </Tabs>
        <div className="relative flex-1">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder={mode === "tickets" ? "Ticket #, customer, component..." : "Name, phone, email..."}
            className="pl-8"
          />
        </div>
      </div>

      {q.trim() && (
        <div className="border rounded-md max-h-64 overflow-y-auto">
          {loading ? (
            <div className="p-3 text-sm text-muted-foreground">Searching...</div>
          ) : mode === "tickets" ? (
            tickets.length === 0 ? (
              <div className="p-3 text-sm text-muted-foreground">No tickets found</div>
            ) : (
              <ul className="divide-y">
                {tickets.slice(0, 8).map((t) => (
                  <li key={t.id}>
                    <button
                      className="w-full text-left p-3 hover:bg-muted text-sm flex items-center justify-between"
                      onClick={() => navigate(`/tickets/${t.id}`)}
                    >
                      <div>
                        <div className="font-medium">{t.ticketDisplay}</div>
                        <div className="text-xs text-muted-foreground">
                          {t.customerName ?? "—"} · {t.componentName ?? t.componentType ?? "—"}
                        </div>
                      </div>
                      <span className="tabular-nums">{formatMoney(t.price)}</span>
                    </button>
                  </li>
                ))}
              </ul>
            )
          ) : customers.length === 0 ? (
            <div className="p-3 text-sm text-muted-foreground">No customers found</div>
          ) : (
            <ul className="divide-y">
              {customers.slice(0, 8).map((c) => (
                <li key={c.id}>
                  <button
                    className="w-full text-left p-3 hover:bg-muted text-sm"
                    onClick={() => navigate(`/customers/${c.id}`)}
                  >
                    <div className="font-medium">{c.fullName}</div>
                    <div className="text-xs text-muted-foreground">
                      {c.phone ?? c.email ?? "—"}
                    </div>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}

function DailySalesChart({ rows }: { rows: DailySalesRow[] | null }) {
  const max = useMemo(() => (rows ? Math.max(1, ...rows.map((r) => r.totalSales)) : 1), [rows])

  if (!rows) return <Skeleton className="h-40" />
  if (rows.length === 0) return <p className="text-sm text-muted-foreground">No sales data yet.</p>

  const hasData = rows.some((r) => r.totalSales > 0)

  return (
    <div className="space-y-3">
      <div className="flex items-end gap-3 h-40 border-b border-l pl-2 pb-1">
        {rows.map((r) => {
          const h = hasData ? Math.max(2, (r.totalSales / max) * 100) : 2
          return (
            <div key={r.date} className="flex-1 flex flex-col items-center gap-1 h-full justify-end">
              <span className="text-[10px] tabular-nums text-muted-foreground">
                {r.totalSales > 0 ? formatMoney(r.totalSales) : ""}
              </span>
              <div
                className="w-full max-w-12 rounded-t bg-primary/80 hover:bg-primary transition-colors"
                style={{ height: `${h}%` }}
                title={`${formatMoney(r.totalSales)} · ${r.ticketCount} tickets`}
              />
            </div>
          )
        })}
      </div>
      <div className="flex gap-3 pl-2">
        {rows.map((r) => (
          <div key={r.date} className="flex-1 text-center text-[11px] text-muted-foreground">
            {new Date(r.date).toLocaleDateString(undefined, { weekday: "short", day: "numeric" })}
          </div>
        ))}
      </div>
      {!hasData && <p className="text-xs text-center text-muted-foreground">No revenue recorded in this range.</p>}
    </div>
  )
}

function formatMoney(v: number) {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(v)
}
