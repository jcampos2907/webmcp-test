import { useEffect, useMemo, useState } from "react"
import { Link } from "react-router-dom"
import { toast } from "sonner"
import { LayoutGrid, List, Plus, Search } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Skeleton } from "@/components/ui/skeleton"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { ticketsApi, TICKET_STATUS, type TicketListItem } from "@/lib/api"

const statusVariants: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  Open: "outline",
  InProgress: "default",
  WaitingForParts: "secondary",
  Completed: "secondary",
  Cancelled: "destructive",
  Delivered: "outline",
}

const BOARD_COLUMNS: Array<{ key: string; label: string; accent: string }> = [
  { key: "Open", label: "Open", accent: "bg-slate-400" },
  { key: "InProgress", label: "In Progress", accent: "bg-blue-500" },
  { key: "WaitingForParts", label: "Waiting for Parts", accent: "bg-amber-500" },
  { key: "Completed", label: "Ready", accent: "bg-emerald-500" },
  { key: "Delivered", label: "Delivered", accent: "bg-violet-500" },
]

type View = "board" | "list"

export default function TicketsPage() {
  const [items, setItems] = useState<TicketListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [view, setView] = useState<View>("board")
  const [q, setQ] = useState("")
  const [draggingId, setDraggingId] = useState<string | null>(null)
  const [dragOverCol, setDragOverCol] = useState<string | null>(null)

  function currentStatusLabel(t: TicketListItem) {
    return typeof t.status === "number" ? TICKET_STATUS[t.status] : t.status
  }

  async function moveTicket(ticketId: string, newStatus: string) {
    const ticket = items.find((x) => x.id === ticketId)
    if (!ticket) return
    const oldLabel = currentStatusLabel(ticket)
    if (oldLabel === newStatus) return
    const statusIndex = Object.entries(TICKET_STATUS).find(([, v]) => v === newStatus)?.[0]
    const nextStatus = statusIndex !== undefined ? Number(statusIndex) : newStatus
    setItems((prev) => prev.map((t) => (t.id === ticketId ? { ...t, status: nextStatus as TicketListItem["status"] } : t)))
    try {
      await ticketsApi.updateStatus(ticketId, newStatus)
      toast.success(`Moved to ${newStatus}`)
    } catch (err) {
      setItems((prev) => prev.map((t) => (t.id === ticketId ? { ...t, status: ticket.status } : t)))
      toast.error(`Failed to move: ${err}`)
    }
  }

  useEffect(() => {
    setLoading(true)
    ticketsApi.list().then(setItems).finally(() => setLoading(false))
  }, [])

  const filtered = useMemo(() => {
    const term = q.trim().toLowerCase()
    if (!term) return items
    return items.filter((t) =>
      [t.ticketDisplay, t.customerName, t.componentName, t.componentType, t.mechanicName]
        .filter(Boolean)
        .some((s) => s!.toLowerCase().includes(term))
    )
  }, [items, q])

  const byStatus = useMemo(() => {
    const map: Record<string, TicketListItem[]> = {}
    for (const col of BOARD_COLUMNS) map[col.key] = []
    for (const t of filtered) {
      const label = typeof t.status === "number" ? TICKET_STATUS[t.status] : t.status
      if (map[label]) map[label].push(t)
    }
    return map
  }, [filtered])

  return (
    <div className="p-6 lg:p-8 max-w-7xl mx-auto space-y-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Service Tickets</h1>
          <p className="text-sm text-muted-foreground">{filtered.length} ticket{filtered.length === 1 ? "" : "s"}</p>
        </div>
        <Button nativeButton={false} render={<Link to="/tickets/new" />}>
          <Plus /> New ticket
        </Button>
      </div>

      <div className="flex flex-col sm:flex-row gap-3 sm:items-center">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search tickets..." className="pl-8" />
        </div>
        <Tabs value={view} onValueChange={(v) => setView(v as View)}>
          <TabsList>
            <TabsTrigger value="board"><LayoutGrid className="h-4 w-4" /> Board</TabsTrigger>
            <TabsTrigger value="list"><List className="h-4 w-4" /> List</TabsTrigger>
          </TabsList>
        </Tabs>
      </div>

      {loading ? (
        <div className="grid gap-4 md:grid-cols-3 lg:grid-cols-5">
          {BOARD_COLUMNS.map((c) => <Skeleton key={c.key} className="h-64" />)}
        </div>
      ) : view === "board" ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
          {BOARD_COLUMNS.map((col) => (
            <BoardColumn
              key={col.key}
              statusKey={col.key}
              label={col.label}
              accent={col.accent}
              tickets={byStatus[col.key] ?? []}
              isDragOver={dragOverCol === col.key}
              onDragOver={() => setDragOverCol(col.key)}
              onDragLeave={() => setDragOverCol((v) => (v === col.key ? null : v))}
              onDrop={() => {
                if (draggingId) moveTicket(draggingId, col.key)
                setDraggingId(null)
                setDragOverCol(null)
              }}
              onCardDragStart={(id) => setDraggingId(id)}
              onCardDragEnd={() => { setDraggingId(null); setDragOverCol(null) }}
              draggingId={draggingId}
            />
          ))}
        </div>
      ) : (
        <Card>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Ticket</TableHead>
                <TableHead>Component</TableHead>
                <TableHead>Customer</TableHead>
                <TableHead>Mechanic</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Price</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.length === 0 && (
                <TableRow><TableCell colSpan={7} className="text-center py-8 text-muted-foreground">No tickets</TableCell></TableRow>
              )}
              {filtered.map((t) => {
                const statusLabel = typeof t.status === "number" ? TICKET_STATUS[t.status] : t.status
                return (
                  <TableRow key={t.id} className="cursor-pointer hover:bg-muted/50">
                    <TableCell className="font-medium">
                      <Link to={`/tickets/${t.id}`} className="hover:underline">{t.ticketDisplay}</Link>
                    </TableCell>
                    <TableCell>{t.componentName ?? t.componentType ?? "—"}</TableCell>
                    <TableCell>{t.customerName ?? "—"}</TableCell>
                    <TableCell>{t.mechanicName ?? "—"}</TableCell>
                    <TableCell>
                      <Badge variant={statusVariants[statusLabel] ?? "default"}>{statusLabel}</Badge>
                    </TableCell>
                    <TableCell className="text-right tabular-nums">${t.price.toFixed(2)}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{new Date(t.createdAt).toLocaleDateString()}</TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </Card>
      )}
    </div>
  )
}

type BoardColumnProps = {
  statusKey: string
  label: string
  accent: string
  tickets: TicketListItem[]
  isDragOver: boolean
  draggingId: string | null
  onDragOver: () => void
  onDragLeave: () => void
  onDrop: () => void
  onCardDragStart: (id: string) => void
  onCardDragEnd: () => void
}

function BoardColumn({ statusKey, label, accent, tickets, isDragOver, draggingId, onDragOver, onDragLeave, onDrop, onCardDragStart, onCardDragEnd }: BoardColumnProps) {
  return (
    <div
      className={`flex flex-col rounded-lg bg-muted/40 border transition-colors ${isDragOver ? "border-primary bg-primary/5" : ""}`}
      onDragOver={(e) => { e.preventDefault(); onDragOver() }}
      onDragLeave={onDragLeave}
      onDrop={(e) => { e.preventDefault(); onDrop() }}
    >
      <div className="flex items-center justify-between px-3 py-2.5 border-b">
        <div className="flex items-center gap-2">
          <span className={`h-2 w-2 rounded-full ${accent}`} />
          <span className="text-sm font-medium">{label}</span>
        </div>
        <Badge variant="secondary" className="tabular-nums">{tickets.length}</Badge>
      </div>
      <div className="p-2 space-y-2 max-h-[calc(100vh-280px)] overflow-y-auto min-h-[120px]">
        {tickets.length === 0 ? (
          <p className="text-xs text-muted-foreground text-center py-6">{isDragOver ? `Drop here → ${label}` : "No tickets"}</p>
        ) : (
          tickets.map((t) => (
            <TicketCard
              key={t.id}
              ticket={t}
              isDragging={draggingId === t.id}
              onDragStart={() => onCardDragStart(t.id)}
              onDragEnd={onCardDragEnd}
            />
          ))
        )}
      </div>
      <span className="sr-only">{statusKey}</span>
    </div>
  )
}

function TicketCard({ ticket, isDragging, onDragStart, onDragEnd }: { ticket: TicketListItem; isDragging: boolean; onDragStart: () => void; onDragEnd: () => void }) {
  const initials = ticket.mechanicName
    ? ticket.mechanicName.split(" ").map((w) => w[0]).slice(0, 2).join("").toUpperCase()
    : null
  return (
    <Link
      to={`/tickets/${ticket.id}`}
      className="block"
      draggable
      onDragStart={(e) => { e.dataTransfer.effectAllowed = "move"; onDragStart() }}
      onDragEnd={onDragEnd}
    >
      <div className={`rounded-md bg-card border p-3 space-y-2 hover:border-primary hover:shadow-sm transition-all cursor-grab active:cursor-grabbing ${isDragging ? "opacity-40" : ""}`}>
        <div className="flex items-start justify-between gap-2">
          <span className="font-medium text-sm">{ticket.ticketDisplay}</span>
          <span className="text-xs tabular-nums font-medium">${ticket.price.toFixed(2)}</span>
        </div>
        <div className="text-xs text-muted-foreground line-clamp-1">
          {ticket.componentName ?? ticket.componentType ?? "—"}
        </div>
        {ticket.customerName && (
          <div className="text-xs line-clamp-1">{ticket.customerName}</div>
        )}
        <div className="flex items-center justify-between pt-1 border-t">
          {initials ? (
            <div className="flex items-center gap-1.5">
              <div className="flex h-5 w-5 items-center justify-center rounded-full bg-primary/10 text-[9px] font-semibold text-primary">
                {initials}
              </div>
              <span className="text-[11px] text-muted-foreground truncate">{ticket.mechanicName}</span>
            </div>
          ) : (
            <span className="text-[11px] text-muted-foreground italic">Unassigned</span>
          )}
          <span className="text-[10px] text-muted-foreground tabular-nums">
            {new Date(ticket.createdAt).toLocaleDateString(undefined, { month: "short", day: "numeric" })}
          </span>
        </div>
      </div>
    </Link>
  )
}
