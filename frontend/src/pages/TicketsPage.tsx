import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { PageHeader } from "@/components/PageHeader"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { ticketsApi, TICKET_STATUS, type TicketListItem } from "@/lib/api"

const statusVariants: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  Open: "outline",
  InProgress: "default",
  WaitingForParts: "secondary",
  Completed: "secondary",
  Cancelled: "destructive",
  Delivered: "outline",
}

export default function TicketsPage() {
  const [items, setItems] = useState<TicketListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [status, setStatus] = useState<string>("all")

  useEffect(() => {
    setLoading(true)
    ticketsApi.list(status === "all" ? undefined : status).then(setItems).finally(() => setLoading(false))
  }, [status])

  return (
    <div className="p-8 max-w-6xl mx-auto">
      <PageHeader
        title="Service tickets"
        actions={<Button disabled>New ticket (in Blazor)</Button>}
      />

      <div className="mb-4">
        <Select value={status} onValueChange={(v) => setStatus(v ?? "all")}>
          <SelectTrigger className="max-w-[200px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            <SelectItem value="Open">Open</SelectItem>
            <SelectItem value="InProgress">In progress</SelectItem>
            <SelectItem value="WaitingForParts">Waiting for parts</SelectItem>
            <SelectItem value="Completed">Completed</SelectItem>
            <SelectItem value="Cancelled">Cancelled</SelectItem>
            <SelectItem value="Delivered">Delivered</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="rounded-md border bg-card">
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
            {loading && <TableRow><TableCell colSpan={7} className="text-center py-8 text-muted-foreground">Loading...</TableCell></TableRow>}
            {!loading && items.length === 0 && <TableRow><TableCell colSpan={7} className="text-center py-8 text-muted-foreground">No tickets</TableCell></TableRow>}
            {!loading && items.map((t) => {
              const statusLabel = typeof t.status === "number" ? TICKET_STATUS[t.status] : t.status
              return (
                <TableRow key={t.id} className="cursor-pointer hover:bg-muted/50">
                  <TableCell className="font-medium">
                    <Link to={`/tickets/${t.id}`} className="hover:underline">{t.ticketDisplay}</Link>
                  </TableCell>
                  <TableCell>{t.componentName ?? "—"}</TableCell>
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
      </div>
    </div>
  )
}
