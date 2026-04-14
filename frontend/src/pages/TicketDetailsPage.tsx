import { useEffect, useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { PageHeader } from "@/components/PageHeader"
import { ticketsApi, type TicketDetails } from "@/lib/api"

export default function TicketDetailsPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const [ticket, setTicket] = useState<TicketDetails | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    ticketsApi.get(id).then(setTicket).finally(() => setLoading(false))
  }, [id])

  async function onCancel() {
    if (!id || !confirm("Cancel this ticket?")) return
    try {
      await ticketsApi.cancel(id)
      toast.success("Ticket cancelled")
      const refreshed = await ticketsApi.get(id)
      setTicket(refreshed)
    } catch (err) { toast.error(String(err)) }
  }

  if (loading) return <div className="p-8">Loading...</div>
  if (!ticket) return <div className="p-8">Ticket not found.</div>

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <PageHeader
        title={ticket.ticketDisplay}
        description={`${ticket.componentName ?? "—"} • ${ticket.customerName ?? "—"}`}
        actions={
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => navigate("/tickets")}>Back</Button>
            {ticket.status !== "Cancelled" && ticket.status !== "Completed" && (
              <Button variant="destructive" onClick={onCancel}>Cancel ticket</Button>
            )}
          </div>
        }
      />

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm text-muted-foreground font-medium">Status</CardTitle></CardHeader>
          <CardContent><Badge className="text-sm">{ticket.status}</Badge></CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm text-muted-foreground font-medium">Total</CardTitle></CardHeader>
          <CardContent><p className="text-2xl font-semibold tabular-nums">${ticket.total.toFixed(2)}</p></CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm text-muted-foreground font-medium">Balance</CardTitle></CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold tabular-nums">${ticket.remainingBalance.toFixed(2)}</p>
            {ticket.isFullyPaid && <Badge variant="secondary" className="mt-1">Fully paid</Badge>}
          </CardContent>
        </Card>
      </div>

      <Tabs defaultValue="overview">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="products">Products ({ticket.products.length})</TabsTrigger>
          <TabsTrigger value="charges">Charges ({ticket.charges.length})</TabsTrigger>
          <TabsTrigger value="events">Timeline ({ticket.events.length})</TabsTrigger>
        </TabsList>

        <TabsContent value="overview">
          <Card>
            <CardContent className="pt-6">
              <dl className="grid grid-cols-2 gap-x-6 gap-y-3 text-sm">
                <Row label="Service">{ticket.serviceName ?? "—"}</Row>
                <Row label="Service price">${ticket.servicePrice.toFixed(2)}</Row>
                <Row label="Mechanic">{ticket.mechanicName ?? "—"}</Row>
                <Row label="Discount">{ticket.discountPercent}%</Row>
                <Row label="Subtotal">${ticket.subtotal.toFixed(2)}</Row>
                <Row label="Total charged">${ticket.totalCharged.toFixed(2)}</Row>
                <Row label="Created">{new Date(ticket.createdAt).toLocaleString()}</Row>
                <Row label="Updated">{new Date(ticket.updatedAt).toLocaleString()}</Row>
              </dl>
              {ticket.description && (
                <>
                  <Separator className="my-4" />
                  <div>
                    <dt className="text-muted-foreground text-xs uppercase tracking-wide mb-1">Description</dt>
                    <dd className="whitespace-pre-wrap text-sm">{ticket.description}</dd>
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="products">
          <Card>
            <CardContent className="pt-6">
              {ticket.products.length === 0 ? (
                <p className="text-sm text-muted-foreground">No products on this ticket.</p>
              ) : (
                <Table>
                  <TableHeader><TableRow><TableHead>Product</TableHead><TableHead className="text-right">Qty</TableHead><TableHead className="text-right">Unit price</TableHead><TableHead className="text-right">Line total</TableHead></TableRow></TableHeader>
                  <TableBody>
                    {ticket.products.map((p, i) => (
                      <TableRow key={i}>
                        <TableCell>{p.productName}</TableCell>
                        <TableCell className="text-right tabular-nums">{p.quantity}</TableCell>
                        <TableCell className="text-right tabular-nums">${p.unitPrice.toFixed(2)}</TableCell>
                        <TableCell className="text-right tabular-nums">${p.lineTotal.toFixed(2)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="charges">
          <Card>
            <CardContent className="pt-6">
              {ticket.charges.length === 0 ? (
                <p className="text-sm text-muted-foreground">No charges yet.</p>
              ) : (
                <Table>
                  <TableHeader><TableRow><TableHead>Date</TableHead><TableHead>Method</TableHead><TableHead>Status</TableHead><TableHead>Cashier</TableHead><TableHead className="text-right">Amount</TableHead></TableRow></TableHeader>
                  <TableBody>
                    {ticket.charges.map((c) => (
                      <TableRow key={c.id}>
                        <TableCell className="text-sm text-muted-foreground">{new Date(c.chargedAt).toLocaleString()}</TableCell>
                        <TableCell>{c.paymentMethod}</TableCell>
                        <TableCell><Badge variant={c.paymentStatus === "Completed" ? "default" : c.paymentStatus === "Refunded" ? "destructive" : "secondary"}>{c.paymentStatus}</Badge></TableCell>
                        <TableCell>{c.cashierName ?? "—"}</TableCell>
                        <TableCell className="text-right tabular-nums">${c.amount.toFixed(2)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="events">
          <Card>
            <CardContent className="pt-6">
              {ticket.events.length === 0 ? (
                <p className="text-sm text-muted-foreground">No events.</p>
              ) : (
                <ul className="space-y-3">
                  {ticket.events.map((e, i) => (
                    <li key={i} className="flex gap-3 text-sm">
                      <div className="w-40 shrink-0 text-muted-foreground">{new Date(e.createdAt).toLocaleString()}</div>
                      <div>
                        <div className="font-medium">{e.eventType}</div>
                        {e.description && <div className="text-muted-foreground">{e.description}</div>}
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <>
      <dt className="text-muted-foreground">{label}</dt>
      <dd>{children}</dd>
    </>
  )
}
