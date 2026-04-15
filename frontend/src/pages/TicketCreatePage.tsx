import { useEffect, useMemo, useState } from "react"
import { useNavigate } from "react-router-dom"
import { toast } from "sonner"
import { Plus, Trash2, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import { PageHeader } from "@/components/PageHeader"
import {
  customersApi, mechanicsApi, servicesApi, productsApi,
  componentsApi, createTicketApi,
  type Customer, type Mechanic, type Service, type Product, type ComponentItem,
} from "@/lib/api"

type LineItem = { productId: string; productName: string; unitPrice: number; quantity: number }

export default function TicketCreatePage() {
  const navigate = useNavigate()

  const [customers, setCustomers] = useState<Customer[]>([])
  const [mechanics, setMechanics] = useState<Mechanic[]>([])
  const [services, setServices] = useState<Service[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [components, setComponents] = useState<ComponentItem[]>([])

  const [customerId, setCustomerId] = useState<string>("")
  const [mechanicId, setMechanicId] = useState<string>("")
  const [serviceId, setServiceId] = useState<string>("")
  const [servicePrice, setServicePrice] = useState(0)
  const [discountPercent, setDiscountPercent] = useState(0)
  const [description, setDescription] = useState("")

  const [componentMode, setComponentMode] = useState<"existing" | "new">("new")
  const [componentId, setComponentId] = useState<string>("")
  const [newComponent, setNewComponent] = useState({
    name: "", componentType: "Bike", brand: "", color: "", sku: "", price: 0,
  })

  const [lines, setLines] = useState<LineItem[]>([])
  const [productSearch, setProductSearch] = useState("")
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    customersApi.list().then(setCustomers)
    mechanicsApi.list().then(setMechanics)
    servicesApi.list().then(setServices)
    productsApi.list().then(setProducts)
  }, [])

  useEffect(() => {
    if (customerId) componentsApi.list(customerId).then(setComponents)
    else setComponents([])
    setComponentId("")
  }, [customerId])

  useEffect(() => {
    if (serviceId) {
      const s = services.find((x) => x.id === serviceId)
      if (s) setServicePrice(s.defaultPrice)
    }
  }, [serviceId, services])

  const filteredProducts = useMemo(() => {
    if (!productSearch) return products.slice(0, 8)
    const q = productSearch.toLowerCase()
    return products.filter((p) =>
      p.name.toLowerCase().includes(q) || (p.sku?.toLowerCase().includes(q) ?? false)
    ).slice(0, 8)
  }, [productSearch, products])

  function addLine(p: Product) {
    setLines((prev) => {
      const existing = prev.find((l) => l.productId === p.id)
      if (existing) return prev.map((l) => l.productId === p.id ? { ...l, quantity: l.quantity + 1 } : l)
      return [...prev, { productId: p.id, productName: p.name, unitPrice: p.price, quantity: 1 }]
    })
  }

  function updateLine(idx: number, patch: Partial<LineItem>) {
    setLines((prev) => prev.map((l, i) => i === idx ? { ...l, ...patch } : l))
  }

  function removeLine(idx: number) {
    setLines((prev) => prev.filter((_, i) => i !== idx))
  }

  const productsSubtotal = lines.reduce((s, l) => s + l.unitPrice * l.quantity, 0)
  const subtotal = servicePrice + productsSubtotal
  const discount = subtotal * (discountPercent / 100)
  const total = subtotal - discount

  async function save() {
    if (componentMode === "existing" && !componentId) {
      toast.error("Select a component")
      return
    }
    if (componentMode === "new" && !newComponent.name.trim()) {
      toast.error("Component name is required")
      return
    }
    setSaving(true)
    try {
      let finalComponentId = componentId
      if (componentMode === "new") {
        const created = await componentsApi.create({
          name: newComponent.name,
          componentType: newComponent.componentType,
          customerId: customerId || null,
          brand: newComponent.brand || null,
          color: newComponent.color || null,
          sku: newComponent.sku || null,
          price: newComponent.price,
        })
        finalComponentId = created.id
      }
      const result = await createTicketApi.create({
        componentId: finalComponentId,
        customerId: customerId || null,
        mechanicId: mechanicId || null,
        baseServiceId: serviceId || null,
        baseServicePrice: servicePrice,
        description: description || null,
        discountPercent,
        products: lines,
      })
      toast.success(`Created ${result.ticketDisplay}`)
      navigate(`/tickets/${result.ticketId}`)
    } catch (err) {
      toast.error(String(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <PageHeader
        title="New service ticket"
        actions={
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => navigate("/tickets")}>Cancel</Button>
            <Button onClick={save} disabled={saving}>{saving ? "Saving..." : "Create ticket"}</Button>
          </div>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <CardHeader><CardTitle>Customer & mechanic</CardTitle></CardHeader>
            <CardContent className="grid grid-cols-2 gap-4">
              <div>
                <Label>Customer</Label>
                <Select value={customerId} onValueChange={(v) => setCustomerId(v ?? "")}>
                  <SelectTrigger><SelectValue placeholder="(walk-in)" /></SelectTrigger>
                  <SelectContent>
                    {customers.map((c) => <SelectItem key={c.id} value={c.id}>{c.fullName}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Mechanic</Label>
                <Select value={mechanicId} onValueChange={(v) => setMechanicId(v ?? "")}>
                  <SelectTrigger><SelectValue placeholder="(unassigned)" /></SelectTrigger>
                  <SelectContent>
                    {mechanics.map((m) => <SelectItem key={m.id} value={m.id}>{m.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Component</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              <div className="flex gap-2">
                <Button variant={componentMode === "new" ? "default" : "outline"} size="sm" onClick={() => setComponentMode("new")}>New</Button>
                <Button variant={componentMode === "existing" ? "default" : "outline"} size="sm" onClick={() => setComponentMode("existing")} disabled={!customerId}>Existing</Button>
              </div>
              {componentMode === "existing" ? (
                <div>
                  <Label>Select component</Label>
                  <Select value={componentId} onValueChange={(v) => setComponentId(v ?? "")}>
                    <SelectTrigger><SelectValue placeholder="Pick one" /></SelectTrigger>
                    <SelectContent>
                      {components.map((c) => (
                        <SelectItem key={c.id} value={c.id}>
                          {c.name ?? c.componentType} · {c.brand ?? "—"} · {c.sku}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              ) : (
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <Label>Name</Label>
                    <Input value={newComponent.name} onChange={(e) => setNewComponent({ ...newComponent, name: e.target.value })} />
                  </div>
                  <div>
                    <Label>Type</Label>
                    <Select value={newComponent.componentType} onValueChange={(v) => setNewComponent({ ...newComponent, componentType: v ?? "Bike" })}>
                      <SelectTrigger><SelectValue /></SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Bike">Bike</SelectItem>
                        <SelectItem value="Rim">Rim</SelectItem>
                        <SelectItem value="Other">Other</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <Label>Brand</Label>
                    <Input value={newComponent.brand} onChange={(e) => setNewComponent({ ...newComponent, brand: e.target.value })} />
                  </div>
                  <div>
                    <Label>Color</Label>
                    <Input value={newComponent.color} onChange={(e) => setNewComponent({ ...newComponent, color: e.target.value })} />
                  </div>
                  <div>
                    <Label>SKU</Label>
                    <Input value={newComponent.sku} onChange={(e) => setNewComponent({ ...newComponent, sku: e.target.value })} placeholder="auto-generated" />
                  </div>
                  <div>
                    <Label>Price</Label>
                    <Input type="number" step="0.01" value={newComponent.price} onChange={(e) => setNewComponent({ ...newComponent, price: Number(e.target.value) })} />
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Service</CardTitle></CardHeader>
            <CardContent className="grid grid-cols-2 gap-4">
              <div>
                <Label>Service</Label>
                <Select value={serviceId} onValueChange={(v) => setServiceId(v ?? "")}>
                  <SelectTrigger><SelectValue placeholder="(none)" /></SelectTrigger>
                  <SelectContent>
                    {services.map((s) => <SelectItem key={s.id} value={s.id}>{s.name} — ${s.defaultPrice.toFixed(2)}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Price</Label>
                <Input type="number" step="0.01" value={servicePrice} onChange={(e) => setServicePrice(Number(e.target.value))} />
              </div>
              <div className="col-span-2">
                <Label>Description / notes</Label>
                <Textarea rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Products</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input className="pl-9" placeholder="Search products..." value={productSearch} onChange={(e) => setProductSearch(e.target.value)} />
              </div>
              {productSearch && (
                <div className="border rounded-md divide-y">
                  {filteredProducts.map((p) => (
                    <button key={p.id} type="button" onClick={() => { addLine(p); setProductSearch("") }} className="w-full flex items-center justify-between px-3 py-2 text-sm hover:bg-muted text-left">
                      <span>{p.name} <span className="text-muted-foreground text-xs">{p.sku ?? ""}</span></span>
                      <span className="tabular-nums">${p.price.toFixed(2)} <Badge variant="outline" className="ml-2">{p.quantityInStock}</Badge></span>
                    </button>
                  ))}
                  {filteredProducts.length === 0 && <div className="px-3 py-2 text-sm text-muted-foreground">No matches</div>}
                </div>
              )}
              {lines.length > 0 && (
                <Table>
                  <TableHeader><TableRow><TableHead>Product</TableHead><TableHead className="w-24">Qty</TableHead><TableHead className="w-28">Unit</TableHead><TableHead className="text-right">Total</TableHead><TableHead className="w-12"></TableHead></TableRow></TableHeader>
                  <TableBody>
                    {lines.map((l, i) => (
                      <TableRow key={l.productId}>
                        <TableCell>{l.productName}</TableCell>
                        <TableCell><Input type="number" min={1} value={l.quantity} onChange={(e) => updateLine(i, { quantity: Math.max(1, Number(e.target.value)) })} /></TableCell>
                        <TableCell><Input type="number" step="0.01" value={l.unitPrice} onChange={(e) => updateLine(i, { unitPrice: Number(e.target.value) })} /></TableCell>
                        <TableCell className="text-right tabular-nums">${(l.unitPrice * l.quantity).toFixed(2)}</TableCell>
                        <TableCell><Button variant="ghost" size="icon-sm" onClick={() => removeLine(i)}><Trash2 className="h-4 w-4" /></Button></TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
              {lines.length === 0 && !productSearch && (
                <p className="text-sm text-muted-foreground flex items-center gap-2"><Plus className="h-4 w-4" /> Search to add products</p>
              )}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-4">
          <Card>
            <CardHeader><CardTitle>Summary</CardTitle></CardHeader>
            <CardContent className="space-y-3 text-sm">
              <Row label="Service"><span className="tabular-nums">${servicePrice.toFixed(2)}</span></Row>
              <Row label="Products"><span className="tabular-nums">${productsSubtotal.toFixed(2)}</span></Row>
              <Row label="Subtotal"><span className="tabular-nums">${subtotal.toFixed(2)}</span></Row>
              <div className="flex items-center justify-between">
                <Label className="text-muted-foreground">Discount %</Label>
                <Input className="w-20 h-8" type="number" min={0} max={100} value={discountPercent} onChange={(e) => setDiscountPercent(Number(e.target.value))} />
              </div>
              <Separator />
              <Row label={<span className="font-semibold">Total</span>}><span className="font-semibold text-lg tabular-nums">${total.toFixed(2)}</span></Row>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

function Row({ label, children }: { label: React.ReactNode; children: React.ReactNode }) {
  return <div className="flex items-center justify-between"><span className="text-muted-foreground">{label}</span>{children}</div>
}
