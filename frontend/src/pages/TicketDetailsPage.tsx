import { useEffect, useMemo, useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { toast } from "sonner"
import {
  ArrowLeft, Ban, Check, CheckCircle2, Circle, Coins, CreditCard,
  FileText, Loader2, Minus, Package, Plus, ReceiptText, RotateCcw, Search, Send, ShoppingCart, Trash2, Wallet, XCircle,
} from "lucide-react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import {
  Sheet, SheetContent, SheetHeader, SheetTitle,
} from "@/components/ui/sheet"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import {
  Dialog, DialogContent, DialogHeader, DialogTitle,
} from "@/components/ui/dialog"
import { cn } from "@/lib/utils"
import { PageHeader } from "@/components/PageHeader"
import {
  ticketsApi, createTicketApi, mechanicsApi, servicesApi, productsApi,
  publicTerminalsApi, paymentSessionsApi,
  type TicketDetails, type Mechanic, type Service, type Product, type PublicTerminal,
} from "@/lib/api"

const STATUS_FLOW = ["Open", "InProgress", "WaitingForParts", "Completed", "Charged"] as const
type Status = (typeof STATUS_FLOW)[number] | "Cancelled"

const METHODS = [
  { value: "Cash", label: "Cash", icon: Wallet },
  { value: "Card", label: "Card", icon: CreditCard },
  { value: "Transfer", label: "Transfer", icon: Send },
  { value: "Other", label: "Other", icon: Coins },
] as const

export default function TicketDetailsPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const [ticket, setTicket] = useState<TicketDetails | null>(null)
  const [loading, setLoading] = useState(true)
  const [productsOpen, setProductsOpen] = useState(false)

  async function refresh() {
    if (!id) return
    const t = await ticketsApi.get(id)
    setTicket(t)
  }

  useEffect(() => {
    if (!id) return
    ticketsApi.get(id).then(setTicket).finally(() => setLoading(false))
  }, [id])

  async function onCancel() {
    if (!id || !confirm("Cancel this ticket?")) return
    try {
      await ticketsApi.cancel(id)
      toast.success("Ticket cancelled")
      await refresh()
    } catch (err) { toast.error(String(err)) }
  }

  if (loading) return <div className="p-8">Loading...</div>
  if (!ticket) return <div className="p-8">Ticket not found.</div>

  const status = ticket.status as Status
  const readOnly = status === "Charged" || status === "Cancelled"

  return (
    <div className="p-6 md:p-8 max-w-[1400px] mx-auto">
      <PageHeader
        title={ticket.ticketDisplay}
        description={`${ticket.componentName ?? "—"} • ${ticket.customerName ?? "—"}`}
        actions={
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => navigate("/tickets")}>
              <ArrowLeft className="h-4 w-4" /> Back
            </Button>
            {status !== "Cancelled" && status !== "Completed" && (
              <Button variant="outline" onClick={onCancel}>
                <Ban className="h-4 w-4" /> Cancel
              </Button>
            )}
          </div>
        }
      />

      <div className="grid gap-6 lg:grid-cols-[1fr_340px]">
        {/* LEFT: details */}
        <div className="space-y-4">
          <div className="grid grid-cols-3 gap-3">
            <MetricCard label="Status">
              <Badge variant={status === "Cancelled" ? "destructive" : status === "Completed" || status === "Delivered" ? "default" : "secondary"}>
                {status}
              </Badge>
            </MetricCard>
            <MetricCard label="Total">
              <div className="text-xl font-semibold tabular-nums">${ticket.total.toFixed(2)}</div>
            </MetricCard>
            <MetricCard label="Balance">
              <div className={cn("text-xl font-semibold tabular-nums", ticket.isFullyPaid && "text-emerald-600")}>
                ${ticket.remainingBalance.toFixed(2)}
              </div>
              {ticket.isFullyPaid && <div className="text-[10px] uppercase tracking-wide text-emerald-600 font-semibold">Fully paid</div>}
            </MetricCard>
          </div>

          <Tabs defaultValue="overview">
            <TabsList>
              <TabsTrigger value="overview"><FileText className="h-3.5 w-3.5" /> Overview</TabsTrigger>
              <TabsTrigger value="products"><Package className="h-3.5 w-3.5" /> Products ({ticket.products.length})</TabsTrigger>
              <TabsTrigger value="charges"><ReceiptText className="h-3.5 w-3.5" /> Charges ({ticket.charges.length})</TabsTrigger>
            </TabsList>

            <TabsContent value="overview">
              <OverviewEditor ticket={ticket} readOnly={readOnly} onSaved={refresh} />
            </TabsContent>

            <TabsContent value="products">
              <Card>
                <CardHeader className="flex flex-row items-center justify-between pb-3">
                  <CardTitle className="text-sm">Products</CardTitle>
                  {!readOnly && (
                    <Button size="sm" variant="outline" onClick={() => setProductsOpen(true)}>
                      <Plus className="h-3.5 w-3.5" /> Manage
                    </Button>
                  )}
                </CardHeader>
                <CardContent>
                  {ticket.products.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No products on this ticket.</p>
                  ) : (
                    <Table>
                      <TableHeader><TableRow><TableHead>Product</TableHead><TableHead className="text-right">Qty</TableHead><TableHead className="text-right">Unit price</TableHead><TableHead className="text-right">Line total</TableHead>{!readOnly && <TableHead className="w-10" />}</TableRow></TableHeader>
                      <TableBody>
                        {ticket.products.map((p, i) => (
                          <TableRow key={i}>
                            <TableCell>{p.productName}</TableCell>
                            <TableCell className="text-right tabular-nums">{p.quantity}</TableCell>
                            <TableCell className="text-right tabular-nums">${p.unitPrice.toFixed(2)}</TableCell>
                            <TableCell className="text-right tabular-nums">${p.lineTotal.toFixed(2)}</TableCell>
                            {!readOnly && (
                              <TableCell className="text-right">
                                <Button
                                  size="icon-sm"
                                  variant="ghost"
                                  onClick={async () => {
                                    if (!confirm(`Remove ${p.productName}?`)) return
                                    try {
                                      await ticketsApi.removeProduct(ticket.id, p.productId)
                                      toast.success("Product removed")
                                      await refresh()
                                    } catch (err) { toast.error(String(err)) }
                                  }}
                                >
                                  <Trash2 className="h-3.5 w-3.5 text-destructive" />
                                </Button>
                              </TableCell>
                            )}
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
                            <TableCell>
                              <Badge variant={c.paymentStatus === "Completed" ? "default" : c.paymentStatus === "Refunded" ? "destructive" : "secondary"}>
                                {c.paymentStatus}
                              </Badge>
                            </TableCell>
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
          </Tabs>
        </div>

        {/* RIGHT: sticky sidebar — payment then timeline */}
        <aside className="space-y-4 lg:sticky lg:top-16 self-start max-h-[calc(100vh-5rem)] overflow-y-auto pr-1">
          <PaymentCard ticket={ticket} onDone={refresh} />
          <TimelineCard ticket={ticket} />
        </aside>
      </div>

      {productsOpen && (
        <ProductsPosSheet
          ticket={ticket}
          onClose={() => setProductsOpen(false)}
          onChanged={refresh}
        />
      )}
    </div>
  )
}

function OverviewEditor({
  ticket, readOnly, onSaved,
}: { ticket: TicketDetails; readOnly: boolean; onSaved: () => Promise<void> }) {
  const [mechanics, setMechanics] = useState<Mechanic[]>([])
  const [services, setServices] = useState<Service[]>([])
  const [price, setPrice] = useState<string>(ticket.servicePrice.toFixed(2))
  const [description, setDescription] = useState<string>(ticket.description ?? "")
  const [discount, setDiscount] = useState<string>(String(ticket.discountPercent))
  const [saving, setSaving] = useState(false)

  const mechanicId = useMemo(
    () => mechanics.find((m) => m.name === ticket.mechanicName)?.id ?? "__none",
    [mechanics, ticket.mechanicName],
  )
  const serviceId = useMemo(
    () => services.find((s) => s.name === ticket.serviceName)?.id ?? "__none",
    [services, ticket.serviceName],
  )

  useEffect(() => { setPrice(ticket.servicePrice.toFixed(2)) }, [ticket.servicePrice])
  useEffect(() => { setDescription(ticket.description ?? "") }, [ticket.description])
  useEffect(() => { setDiscount(String(ticket.discountPercent)) }, [ticket.discountPercent])

  useEffect(() => {
    if (readOnly) return
    mechanicsApi.list().then(setMechanics).catch(() => {})
    servicesApi.list().then(setServices).catch(() => {})
  }, [readOnly])

  async function savePatch(patch: Partial<{
    mechanicId: string | null
    baseServiceId: string | null
    baseServicePrice: number
    description: string | null
    discountPercent: number
  }>) {
    setSaving(true)
    try {
      await ticketsApi.update(ticket.id, {
        mechanicId: patch.mechanicId !== undefined ? patch.mechanicId : (mechanicId === "__none" ? null : mechanicId),
        baseServiceId: patch.baseServiceId !== undefined ? patch.baseServiceId : (serviceId === "__none" ? null : serviceId),
        baseServicePrice: patch.baseServicePrice ?? Number(price),
        description: patch.description !== undefined ? patch.description : (description || null),
        discountPercent: patch.discountPercent ?? Number(discount),
      })
      await onSaved()
    } catch (err) {
      toast.error(String(err))
    } finally {
      setSaving(false)
    }
  }

  function onServiceChange(v: string) {
    const id = v === "__none" ? null : v
    const svc = id ? services.find((s) => s.id === id) : null
    const newPrice = svc ? svc.defaultPrice : Number(price)
    if (svc) setPrice(svc.defaultPrice.toFixed(2))
    savePatch({ baseServiceId: id, baseServicePrice: newPrice })
  }

  function onPriceBlur() {
    const p = Number(price)
    if (isNaN(p) || p < 0) { toast.error("Price must be ≥ 0"); setPrice(ticket.servicePrice.toFixed(2)); return }
    if (p === ticket.servicePrice) return
    savePatch({ baseServicePrice: p })
  }

  function onDiscountBlur() {
    const d = Number(discount)
    if (isNaN(d) || d < 0 || d > 100) { toast.error("Discount must be 0–100"); setDiscount(String(ticket.discountPercent)); return }
    if (d === ticket.discountPercent) return
    savePatch({ discountPercent: d })
  }

  function onDescriptionBlur() {
    if ((description || null) === (ticket.description ?? null)) return
    savePatch({ description: description || null })
  }

  if (readOnly) {
    return (
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
    )
  }

  return (
    <Card>
      <CardHeader className="pb-3 flex flex-row items-center justify-between">
        <CardTitle className="text-sm">Details</CardTitle>
        <span className="text-[10px] text-muted-foreground h-4">
          {saving ? "Saving…" : ""}
        </span>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <div className="grid gap-1.5">
            <Label className="text-xs text-muted-foreground">Service</Label>
            <Select value={serviceId} onValueChange={(v) => onServiceChange(v ?? "__none")}>
              <SelectTrigger className="w-full">
                <SelectValue placeholder="None" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__none">None</SelectItem>
                {services.map((s) => <SelectItem key={s.id} value={s.id}>{s.name}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
          <div className="grid gap-1.5">
            <Label className="text-xs text-muted-foreground">Mechanic</Label>
            <Select
              value={mechanicId}
              onValueChange={(v) => savePatch({ mechanicId: (v ?? "__none") === "__none" ? null : (v as string) })}
            >
              <SelectTrigger className="w-full">
                <SelectValue placeholder="Unassigned" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__none">Unassigned</SelectItem>
                {mechanics.map((m) => <SelectItem key={m.id} value={m.id}>{m.name}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
          <div className="grid gap-1.5">
            <Label className="text-xs text-muted-foreground">Service price</Label>
            <Input
              type="number" step="0.01" min="0"
              value={price}
              onChange={(e) => setPrice(e.target.value)}
              onBlur={onPriceBlur}
              className="tabular-nums"
            />
          </div>
          <div className="grid gap-1.5">
            <Label className="text-xs text-muted-foreground">Discount %</Label>
            <Input
              type="number" step="1" min="0" max="100"
              value={discount}
              onChange={(e) => setDiscount(e.target.value)}
              onBlur={onDiscountBlur}
              className="tabular-nums"
            />
          </div>
        </div>

        <div className="grid gap-1.5">
          <Label className="text-xs text-muted-foreground">Description</Label>
          <Textarea
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            onBlur={onDescriptionBlur}
            placeholder="Notes, symptoms, requested work…"
          />
        </div>

        <Separator />

        <dl className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
          <Row label="Subtotal">${ticket.subtotal.toFixed(2)}</Row>
          <Row label="Total charged">${ticket.totalCharged.toFixed(2)}</Row>
          <Row label="Created">{new Date(ticket.createdAt).toLocaleString()}</Row>
          <Row label="Updated">{new Date(ticket.updatedAt).toLocaleString()}</Row>
        </dl>
      </CardContent>
    </Card>
  )
}

function ProductsPosSheet({
  ticket, onClose, onChanged,
}: { ticket: TicketDetails; onClose: () => void; onChanged: () => Promise<void> }) {
  const [products, setProducts] = useState<Product[]>([])
  const [search, setSearch] = useState("")
  const [busyId, setBusyId] = useState<string | null>(null)

  useEffect(() => { productsApi.list().then(setProducts).catch(() => {}) }, [])

  const filtered = useMemo(() => {
    if (!search) return products.slice(0, 16)
    const q = search.toLowerCase()
    return products.filter((p) =>
      p.name.toLowerCase().includes(q) || (p.sku?.toLowerCase().includes(q) ?? false),
    ).slice(0, 24)
  }, [search, products])

  async function addOne(productId: string) {
    setBusyId(productId)
    try {
      await ticketsApi.addProduct(ticket.id, productId, 1)
      await onChanged()
    } catch (err) { toast.error(String(err)) }
    finally { setBusyId(null) }
  }

  async function changeQty(productId: string, current: number, delta: number) {
    const next = current + delta
    setBusyId(productId)
    try {
      if (next <= 0) {
        await ticketsApi.removeProduct(ticket.id, productId)
      } else if (delta > 0) {
        await ticketsApi.addProduct(ticket.id, productId, 1)
      } else {
        // decrement: remove and re-add with reduced qty
        await ticketsApi.removeProduct(ticket.id, productId)
        await ticketsApi.addProduct(ticket.id, productId, next)
      }
      await onChanged()
    } catch (err) { toast.error(String(err)) }
    finally { setBusyId(null) }
  }

  async function removeLine(productId: string) {
    setBusyId(productId)
    try {
      await ticketsApi.removeProduct(ticket.id, productId)
      await onChanged()
    } catch (err) { toast.error(String(err)) }
    finally { setBusyId(null) }
  }

  const cartSubtotal = ticket.products.reduce((s, l) => s + l.lineTotal, 0)
  const itemCount = ticket.products.reduce((s, l) => s + l.quantity, 0)

  return (
    <Sheet open onOpenChange={(v) => !v && onClose()}>
      <SheetContent className="flex flex-col gap-0 p-0 !w-[min(88rem,98vw)] !max-w-none">
        <SheetHeader className="p-4 border-b">
          <SheetTitle>Products — {ticket.ticketDisplay}</SheetTitle>
        </SheetHeader>

        <div className="flex-1 min-h-0 grid grid-cols-1 lg:grid-cols-[1fr_380px] gap-0">
          {/* LEFT: product grid */}
          <div className="flex flex-col min-h-0 border-r">
            <div className="p-4 border-b">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  className="pl-9 h-10"
                  placeholder="Search products by name or SKU…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  autoFocus
                />
              </div>
            </div>
            <div className="flex-1 overflow-y-auto p-4">
              <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-3">
                {filtered.map((p) => {
                  const out = p.quantityInStock <= 0
                  return (
                    <button
                      key={p.id}
                      onClick={() => addOne(p.id)}
                      disabled={out || busyId === p.id}
                      className="group border rounded-lg p-3 text-left hover:border-primary hover:shadow-sm disabled:opacity-50 disabled:cursor-not-allowed transition-all bg-card"
                    >
                      <div className="font-medium text-sm line-clamp-2 min-h-[2.5rem]">{p.name}</div>
                      {p.sku && <div className="text-[10px] text-muted-foreground font-mono mt-1 truncate">{p.sku}</div>}
                      <div className="flex items-center justify-between mt-3">
                        <span className="font-semibold tabular-nums">${p.price.toFixed(2)}</span>
                        <Badge variant={p.quantityInStock > 5 ? "outline" : p.quantityInStock > 0 ? "secondary" : "destructive"}>
                          {p.quantityInStock > 0 ? `${p.quantityInStock} in stock` : "Out"}
                        </Badge>
                      </div>
                    </button>
                  )
                })}
                {filtered.length === 0 && (
                  <p className="col-span-full text-center py-8 text-sm text-muted-foreground">No products found</p>
                )}
              </div>
            </div>
          </div>

          {/* RIGHT: ticket tab */}
          <Card className="rounded-none border-0 border-l-0 flex flex-col min-h-0">
            <CardHeader className="border-b pb-3">
              <CardTitle className="flex items-center justify-between">
                <span className="flex items-center gap-2 text-sm"><ShoppingCart className="h-4 w-4" /> Ticket</span>
                <Badge variant="secondary" className="tabular-nums">{itemCount} item{itemCount === 1 ? "" : "s"}</Badge>
              </CardTitle>
            </CardHeader>
            <CardContent className="flex-1 overflow-y-auto pt-4">
              {ticket.products.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-10 text-muted-foreground">
                  <ShoppingCart className="h-8 w-8 mb-2 opacity-40" />
                  <p className="text-sm">No products yet</p>
                  <p className="text-xs mt-1">Tap a product to add</p>
                </div>
              ) : (
                <div className="space-y-2">
                  {ticket.products.map((l) => (
                    <div key={l.productId} className="flex items-start gap-2 text-sm rounded-md p-2 hover:bg-muted/50">
                      <div className="flex-1 min-w-0">
                        <div className="font-medium truncate">{l.productName}</div>
                        <div className="text-xs text-muted-foreground tabular-nums">
                          ${l.unitPrice.toFixed(2)} × {l.quantity} = <span className="font-medium text-foreground">${l.lineTotal.toFixed(2)}</span>
                        </div>
                      </div>
                      <div className="flex items-center gap-1 shrink-0">
                        <Button variant="outline" size="icon-sm" disabled={busyId === l.productId} onClick={() => changeQty(l.productId, l.quantity, -1)}>
                          <Minus className="h-3 w-3" />
                        </Button>
                        <span className="w-5 text-center tabular-nums text-sm">{l.quantity}</span>
                        <Button variant="outline" size="icon-sm" disabled={busyId === l.productId} onClick={() => changeQty(l.productId, l.quantity, +1)}>
                          <Plus className="h-3 w-3" />
                        </Button>
                        <Button variant="ghost" size="icon-sm" disabled={busyId === l.productId} onClick={() => removeLine(l.productId)}>
                          <Trash2 className="h-3 w-3" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
            <div className="border-t p-4 space-y-3 bg-muted/20">
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">Products subtotal</span>
                <span className="text-sm tabular-nums">${cartSubtotal.toFixed(2)}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="font-semibold">Ticket total</span>
                <span className="text-2xl font-semibold tabular-nums">${ticket.total.toFixed(2)}</span>
              </div>
              <Button className="w-full" size="lg" variant="outline" onClick={onClose}>
                Done
              </Button>
            </div>
          </Card>
        </div>
      </SheetContent>
    </Sheet>
  )
}

function PaymentCard({ ticket, onDone }: { ticket: TicketDetails; onDone: () => Promise<void> }) {
  const [mode, setMode] = useState<"charge" | "refund">("charge")
  const [amount, setAmount] = useState<string>(ticket.remainingBalance.toFixed(2))
  const [method, setMethod] = useState<string>("Card")
  const [cashier, setCashier] = useState("")
  const [busy, setBusy] = useState(false)
  const [terminals, setTerminals] = useState<PublicTerminal[]>([])
  const [terminalId, setTerminalId] = useState<string>("")
  const [terminalModal, setTerminalModal] = useState<{
    sessionId: string; status: "Processing" | "Success" | "Failed"; remaining: number; error?: string
  } | null>(null)
  const cancelled = ticket.status === "Cancelled"
  const pendingCardCharges = ticket.charges.filter((c) => c.paymentStatus === "Pending" && c.paymentMethod === "Card")
  const pendingAmount = pendingCardCharges.reduce((sum, c) => sum + c.amount, 0)

  useEffect(() => {
    setAmount((mode === "charge" ? ticket.remainingBalance : ticket.totalCharged).toFixed(2))
  }, [mode, ticket.remainingBalance, ticket.totalCharged])

  useEffect(() => {
    publicTerminalsApi.list().then((ts) => {
      setTerminals(ts)
      if (ts.length && !terminalId) setTerminalId(ts[0].id)
    }).catch(() => {})
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function pollTerminalUntilDone(sessionId: string) {
    const deadline = Date.now() + 15000
    while (Date.now() < deadline) {
      await new Promise((r) => setTimeout(r, 1000))
      const remaining = Math.max(0, Math.ceil((deadline - Date.now()) / 1000))
      let status: string
      try {
        const r = await paymentSessionsApi.status(sessionId)
        status = r.status
      } catch { continue }
      setTerminalModal((m) => m && m.status === "Processing" ? { ...m, remaining } : m)
      if (status === "Completed") {
        try {
          const c = await paymentSessionsApi.confirm(sessionId)
          if (c.errorMessage) {
            setTerminalModal({ sessionId, status: "Failed", remaining: 0, error: c.errorMessage })
            return
          }
          setTerminalModal({ sessionId, status: "Success", remaining: 0 })
          await new Promise((r) => setTimeout(r, 1500))
          setTerminalModal(null)
          await onDone()
          return
        } catch (e) {
          setTerminalModal({ sessionId, status: "Failed", remaining: 0, error: String(e) })
          return
        }
      }
      if (status === "Failed" || status === "Cancelled") {
        setTerminalModal({ sessionId, status: "Failed", remaining: 0, error: status })
        return
      }
    }
    try { await paymentSessionsApi.cancel(sessionId) } catch {}
    setTerminalModal({ sessionId, status: "Failed", remaining: 0, error: "Timeout — terminal did not respond" })
  }

  async function cancelTerminalSession() {
    if (!terminalModal) return
    try { await paymentSessionsApi.cancel(terminalModal.sessionId) } catch {}
    setTerminalModal(null)
    await onDone()
  }

  async function submit() {
    const value = Number(amount)
    if (!value || value <= 0) { toast.error("Amount must be > 0"); return }
    if (mode === "charge" && method === "Card" && !terminalId) {
      toast.error("Select a terminal"); return
    }
    setBusy(true)
    try {
      if (mode === "charge") {
        const r = await createTicketApi.charge(ticket.id, value, method, cashier || undefined, method === "Card" ? terminalId : null)
        if (r.paymentSessionId) {
          setTerminalModal({ sessionId: r.paymentSessionId, status: "Processing", remaining: 15 })
          pollTerminalUntilDone(r.paymentSessionId)
        } else {
          toast.success("Charge processed")
          await onDone()
        }
      } else {
        await createTicketApi.refund(ticket.id, value, method, cashier || undefined)
        toast.success("Refund processed")
        await onDone()
      }
    } catch (err) {
      toast.error(String(err))
    } finally {
      setBusy(false)
    }
  }

  if (ticket.isFullyPaid && mode === "charge") {
    return (
      <Card className="border-emerald-200 bg-emerald-50/40">
        <CardHeader className="pb-3">
          <CardTitle className="text-sm flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4 text-emerald-600" /> Fully paid
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <p className="text-xs text-muted-foreground">No outstanding balance on this ticket.</p>
          {ticket.totalCharged > 0 && (
            <Button variant="outline" size="sm" className="w-full" onClick={() => setMode("refund")}>
              <RotateCcw className="h-3.5 w-3.5" /> Issue refund
            </Button>
          )}
        </CardContent>
      </Card>
    )
  }

  return (
    <>
    <TerminalPaymentDialog state={terminalModal} onCancel={cancelTerminalSession} onClose={() => setTerminalModal(null)} />
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm flex items-center gap-2">
            {mode === "charge" ? <CreditCard className="h-4 w-4" /> : <RotateCcw className="h-4 w-4" />}
            {mode === "charge" ? "Collect payment" : "Refund"}
          </CardTitle>
          {ticket.totalCharged > 0 && (
            <button
              type="button"
              onClick={() => setMode((m) => (m === "charge" ? "refund" : "charge"))}
              className="text-[11px] text-muted-foreground hover:text-foreground underline"
            >
              {mode === "charge" ? "Refund instead" : "Back to charge"}
            </button>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        {mode === "charge" && pendingCardCharges.length > 0 && (
          <div className="rounded-md border border-amber-200 bg-amber-50 dark:bg-amber-950/20 dark:border-amber-900/40 p-2.5 text-xs flex items-start gap-2">
            <CreditCard className="h-3.5 w-3.5 mt-0.5 shrink-0 text-amber-600" />
            <div>
              <div className="font-medium text-amber-900 dark:text-amber-100">Card charge pending</div>
              <div className="text-amber-800/80 dark:text-amber-200/70">
                ${pendingAmount.toFixed(2)} awaiting terminal confirmation
              </div>
            </div>
          </div>
        )}
        <div className="rounded-md bg-muted/40 p-2.5 text-sm flex items-center justify-between">
          <span className="text-muted-foreground text-xs">
            {mode === "charge" ? "Outstanding" : "Already charged"}
          </span>
          <span className="font-semibold tabular-nums">
            ${(mode === "charge" ? ticket.remainingBalance : ticket.totalCharged).toFixed(2)}
          </span>
        </div>

        <div>
          <Label className="text-xs">Method</Label>
          <div className="grid grid-cols-4 gap-1.5 mt-1">
            {METHODS.map((m) => {
              const active = method === m.value
              return (
                <button
                  key={m.value}
                  type="button"
                  disabled={cancelled || busy}
                  onClick={() => setMethod(m.value)}
                  className={cn(
                    "flex flex-col items-center gap-1 rounded-md border py-2 text-[10px] transition-colors",
                    "hover:bg-muted/60 disabled:opacity-50",
                    active && "border-primary bg-primary/5 text-primary",
                  )}
                >
                  <m.icon className="h-3.5 w-3.5" />
                  <span>{m.label}</span>
                </button>
              )
            })}
          </div>
        </div>

        {mode === "charge" && method === "Card" && (
          <div>
            <Label className="text-xs">Terminal</Label>
            <Select value={terminalId} onValueChange={setTerminalId} disabled={cancelled || busy}>
              <SelectTrigger className="w-full">
                <SelectValue placeholder={terminals.length ? "Select terminal" : "No terminals configured"} />
              </SelectTrigger>
              <SelectContent>
                {terminals.map((t) => (
                  <SelectItem key={t.id} value={t.id}>{t.name} <span className="text-muted-foreground">· {t.provider}</span></SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        <div>
          <Label className="text-xs">Amount</Label>
          <Input
            type="number" step="0.01" min="0"
            value={amount}
            disabled={cancelled || busy}
            onChange={(e) => setAmount(e.target.value)}
            className="tabular-nums font-semibold"
          />
        </div>

        <div>
          <Label className="text-xs">Cashier (optional)</Label>
          <Input value={cashier} disabled={cancelled || busy} onChange={(e) => setCashier(e.target.value)} placeholder="—" />
        </div>

        <Button className="w-full" disabled={busy || cancelled || Number(amount) <= 0} onClick={submit}>
          {busy ? "Processing…" : mode === "charge"
            ? method === "Card" ? "Charge on terminal" : `Charge $${Number(amount).toFixed(2)}`
            : `Refund $${Number(amount).toFixed(2)}`}
        </Button>

        {cancelled && (
          <p className="text-[11px] text-muted-foreground text-center">Ticket is cancelled.</p>
        )}
      </CardContent>
    </Card>
    </>
  )
}

function TerminalPaymentDialog({
  state, onCancel, onClose,
}: {
  state: { sessionId: string; status: "Processing" | "Success" | "Failed"; remaining: number; error?: string } | null
  onCancel: () => void
  onClose: () => void
}) {
  const open = state !== null
  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v && state?.status !== "Processing") onClose() }}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>
            {state?.status === "Processing" && "Waiting for terminal"}
            {state?.status === "Success" && "Payment approved"}
            {state?.status === "Failed" && "Payment failed"}
          </DialogTitle>
        </DialogHeader>
        <div className="flex flex-col items-center justify-center py-6 gap-3">
          {state?.status === "Processing" && (
            <>
              <Loader2 className="h-10 w-10 animate-spin text-primary" />
              <p className="text-sm text-muted-foreground">Tap or insert card on the terminal…</p>
              <p className="text-2xl font-semibold tabular-nums">{state.remaining}s</p>
              <Button variant="outline" size="sm" onClick={onCancel}>Cancel</Button>
            </>
          )}
          {state?.status === "Success" && (
            <>
              <CheckCircle2 className="h-12 w-12 text-emerald-600" />
              <p className="text-sm text-muted-foreground">Charge confirmed</p>
            </>
          )}
          {state?.status === "Failed" && (
            <>
              <XCircle className="h-12 w-12 text-destructive" />
              <p className="text-sm text-muted-foreground text-center">{state.error ?? "Terminal did not complete the payment."}</p>
              <Button size="sm" onClick={onClose}>Close</Button>
            </>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}

function TimelineCard({ ticket }: { ticket: TicketDetails }) {
  const status = ticket.status as Status
  const steps = useMemo(() => {
    if (status === "Cancelled") {
      return [
        { label: "Created", state: "completed" as const, ts: ticket.createdAt },
        { label: "Cancelled", state: "current" as const, ts: ticket.updatedAt, danger: true },
      ]
    }
    const currentIdx = STATUS_FLOW.indexOf(status as Exclude<Status, "Cancelled">)
    return STATUS_FLOW.map((s, i) => ({
      label: s === "InProgress" ? "In progress" : s === "WaitingForParts" ? "Waiting for parts" : s === "Charged" ? "Paid" : s,
      state: (i < currentIdx ? "completed" : i === currentIdx ? "current" : "upcoming") as "completed" | "current" | "upcoming",
      ts: undefined as string | undefined,
      danger: false,
    }))
  }, [status, ticket.createdAt, ticket.updatedAt])

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-sm">Progress</CardTitle>
      </CardHeader>
      <CardContent className="space-y-0">
        <ol className="space-y-0">
          {steps.map((s, i) => {
            const isLast = i === steps.length - 1
            return (
              <li key={i} className="relative flex gap-3 pb-4 last:pb-0">
                {!isLast && (
                  <span
                    aria-hidden
                    className={cn(
                      "absolute left-[11px] top-6 bottom-0 w-px",
                      s.state === "completed" ? "bg-emerald-300" : "bg-border",
                    )}
                  />
                )}
                <span
                  className={cn(
                    "relative z-10 mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full",
                    s.state === "completed" && "bg-emerald-500 text-white",
                    s.state === "current" && (s.danger ? "bg-destructive text-destructive-foreground ring-4 ring-destructive/20" : "bg-primary text-primary-foreground ring-4 ring-primary/20"),
                    s.state === "upcoming" && "bg-muted text-muted-foreground border",
                  )}
                >
                  {s.state === "completed" ? <Check className="h-3 w-3" /> : s.state === "current" ? <Circle className="h-2 w-2 fill-current" /> : <Circle className="h-2 w-2" />}
                </span>
                <div className="flex-1 min-w-0 pt-0.5">
                  <div className={cn(
                    "text-sm leading-tight",
                    s.state === "current" ? "font-semibold" : s.state === "completed" ? "font-medium" : "text-muted-foreground",
                  )}>
                    {s.label}
                  </div>
                  {s.ts && <div className="text-[10px] text-muted-foreground mt-0.5">{new Date(s.ts).toLocaleString()}</div>}
                </div>
              </li>
            )
          })}
        </ol>

        {ticket.events.length > 0 && (
          <>
            <Separator className="my-4" />
            <div className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground mb-3">Activity</div>
            <ol className="space-y-3">
              {ticket.events.slice().reverse().map((e, i) => (
                <li key={i} className="flex gap-2.5 text-xs">
                  <span className="mt-1 h-1.5 w-1.5 shrink-0 rounded-full bg-muted-foreground/40" />
                  <div className="min-w-0 flex-1">
                    <div className="font-medium leading-tight">{formatEventType(e.eventType)}</div>
                    {e.description && <div className="text-muted-foreground truncate">{e.description}</div>}
                    <div className="text-[10px] text-muted-foreground/80 mt-0.5">{new Date(e.createdAt).toLocaleString()}</div>
                  </div>
                </li>
              ))}
            </ol>
          </>
        )}
      </CardContent>
    </Card>
  )
}

function formatEventType(t: string): string {
  return t.replace(/([A-Z])/g, " $1").trim().replace(/^./, (c) => c.toUpperCase())
}

function MetricCard({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <Card>
      <CardContent className="py-3 px-4">
        <div className="text-[10px] uppercase tracking-wide text-muted-foreground font-semibold mb-1">{label}</div>
        {children}
      </CardContent>
    </Card>
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
