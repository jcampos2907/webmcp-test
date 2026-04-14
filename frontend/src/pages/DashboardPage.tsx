import { useEffect, useState } from "react"
import { PageHeader } from "@/components/PageHeader"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { dashboardApi, type DailySalesRow } from "@/lib/api"
import { Skeleton } from "@/components/ui/skeleton"

export default function DashboardPage() {
  const [rows, setRows] = useState<DailySalesRow[] | null>(null)

  useEffect(() => {
    dashboardApi.dailySales().then(setRows).catch(() => setRows([]))
  }, [])

  const totalSales = rows?.reduce((s, r) => s + r.totalSales, 0) ?? 0
  const totalTickets = rows?.reduce((s, r) => s + r.ticketCount, 0) ?? 0
  const totalCharges = rows?.reduce((s, r) => s + r.chargeCount, 0) ?? 0

  return (
    <div className="p-8 max-w-6xl mx-auto">
      <PageHeader title="Dashboard" description="Last 7 days at a glance" />

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <StatCard label="Sales" value={formatMoney(totalSales)} loading={!rows} />
        <StatCard label="Tickets" value={String(totalTickets)} loading={!rows} />
        <StatCard label="Charges" value={String(totalCharges)} loading={!rows} />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Daily breakdown</CardTitle>
        </CardHeader>
        <CardContent>
          {!rows ? (
            <Skeleton className="h-40" />
          ) : rows.length === 0 ? (
            <p className="text-sm text-muted-foreground">No sales data yet.</p>
          ) : (
            <ul className="divide-y">
              {rows.map((r) => (
                <li key={r.date} className="flex justify-between py-2 text-sm">
                  <span>{new Date(r.date).toLocaleDateString()}</span>
                  <span className="tabular-nums">{formatMoney(r.totalSales)}</span>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function StatCard({ label, value, loading }: { label: string; value: string; loading: boolean }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{label}</CardTitle>
      </CardHeader>
      <CardContent>
        {loading ? <Skeleton className="h-7 w-24" /> : <p className="text-2xl font-semibold tabular-nums">{value}</p>}
      </CardContent>
    </Card>
  )
}

function formatMoney(v: number) {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(v)
}
