import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { PageHeader } from "@/components/PageHeader"
import { workloadApi, TICKET_STATUS, type MechanicWorkload } from "@/lib/api"

export default function MechanicWorkloadPage() {
  const [data, setData] = useState<MechanicWorkload | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    workloadApi.get().then(setData).finally(() => setLoading(false))
  }, [])

  if (loading) return <div className="p-8">Loading...</div>
  if (!data) return <div className="p-8">No data</div>

  const unassignedTickets = data.ticketsByMechanic["unassigned"] ?? []

  return (
    <div className="p-8 max-w-7xl mx-auto">
      <PageHeader title="Mechanic workload" description="Active tickets grouped by mechanic" />

      <div className="grid grid-cols-4 gap-4 mb-6">
        <Stat label="Open" value={data.totalOpen} />
        <Stat label="In progress" value={data.totalInProgress} />
        <Stat label="Waiting parts" value={data.totalWaiting} />
        <Stat label="Unassigned" value={data.unassigned} />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {unassignedTickets.length > 0 && (
          <Column title="Unassigned" count={unassignedTickets.length} tickets={unassignedTickets} />
        )}
        {data.mechanics.map((m) => {
          const tickets = data.ticketsByMechanic[m.id] ?? []
          return <Column key={m.id} title={m.name} count={tickets.length} tickets={tickets} />
        })}
      </div>
    </div>
  )
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <Card>
      <CardHeader className="pb-2"><CardTitle className="text-sm text-muted-foreground font-medium">{label}</CardTitle></CardHeader>
      <CardContent><p className="text-2xl font-semibold tabular-nums">{value}</p></CardContent>
    </Card>
  )
}

function Column({ title, count, tickets }: { title: string; count: number; tickets: Array<{ id: string; ticketDisplay: string; componentName: string | null; componentType: string | null; status: number; createdAt: string }> }) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-sm flex items-center justify-between">{title}<Badge variant="outline">{count}</Badge></CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        {tickets.length === 0 && <p className="text-xs text-muted-foreground">No active tickets</p>}
        {tickets.map((t) => (
          <Link key={t.id} to={`/tickets/${t.id}`} className="block border rounded-md p-3 text-sm hover:bg-muted/50">
            <div className="flex items-center justify-between mb-1">
              <span className="font-medium">{t.ticketDisplay}</span>
              <Badge variant="secondary" className="text-xs">{TICKET_STATUS[t.status]}</Badge>
            </div>
            <div className="text-muted-foreground text-xs">{t.componentName ?? t.componentType ?? "—"}</div>
          </Link>
        ))}
      </CardContent>
    </Card>
  )
}
