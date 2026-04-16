import { useEffect, useMemo, useState } from "react"
import { useNavigate } from "react-router-dom"
import { toast } from "sonner"
import { Plus, Trash2, Search, Sparkles, Loader2 } from "lucide-react"
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
import { InputGroup, InputGroupAddon, InputGroupInput } from "@/components/ui/input-group"
import { Field, FieldGroup, FieldLabel, FieldDescription } from "@/components/ui/field"
import { PageHeader } from "@/components/PageHeader"
import { EntityPicker } from "@/components/EntityPicker"
import { SegmentedToggle } from "@/components/Field"
import {
  customersApi, mechanicsApi, servicesApi, productsApi,
  componentsApi, createTicketApi, cadenceApi,
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

  const [componentMode, setComponentMode] = useState<"existing" | "new">("existing")
  const [componentId, setComponentId] = useState<string>("")
  const [newComponent, setNewComponent] = useState({
    name: "", componentType: "Bike", brand: "", color: "", sku: "",
  })

  const [lines, setLines] = useState<LineItem[]>([])
  const [productSearch, setProductSearch] = useState("")
  const [saving, setSaving] = useState(false)
  const [suggesting, setSuggesting] = useState(false)

  const [customerMode, setCustomerMode] = useState<"existing" | "new">("existing")
  const [newCustomer, setNewCustomer] = useState({
    firstName: "", lastName: "", phone: "", email: "", city: "",
  })

  async function suggestDescription() {
    const bike = componentMode === "new"
      ? [newComponent.brand, newComponent.name].filter(Boolean).join(" ").trim()
      : components.find((c) => c.id === componentId)?.name ?? ""
    const service = services.find((s) => s.id === serviceId)?.name ?? ""
    if (!bike && !service) {
      toast.error("Pick a service or name the component first")
      return
    }
    setSuggesting(true)
    try {
      const { suggestion } = await cadenceApi.suggestTicketDescription(bike, service)
      setDescription(suggestion)
    } catch (err) {
      toast.error(String(err))
    } finally {
      setSuggesting(false)
    }
  }

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
    if (customerMode === "existing" && !customerId) {
      toast.error("Select a customer")
      return
    }
    if (customerMode === "new" && (!newCustomer.firstName.trim() || !newCustomer.lastName.trim())) {
      toast.error("First and last name are required")
      return
    }
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
      let finalCustomerId = customerId
      if (customerMode === "new") {
        const created = await customersApi.create({
          firstName: newCustomer.firstName,
          lastName: newCustomer.lastName,
          phone: newCustomer.phone || null,
          email: newCustomer.email || null,
          street: null,
          city: newCustomer.city || null,
          state: null,
          zipCode: null,
          country: null,
        })
        finalCustomerId = created.id
      }
      let finalComponentId = componentId
      if (componentMode === "new") {
        const created = await componentsApi.create({
          name: newComponent.name,
          componentType: newComponent.componentType,
          customerId: finalCustomerId || null,
          brand: newComponent.brand || null,
          color: newComponent.color || null,
          sku: newComponent.sku || null,
          price: 0,
        })
        finalComponentId = created.id
      }
      const result = await createTicketApi.create({
        componentId: finalComponentId,
        customerId: finalCustomerId || null,
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
            <CardContent>
              <FieldGroup>
              <Field>
                <div className="flex items-end justify-between gap-2">
                  <FieldLabel>Customer <span className="text-destructive">*</span></FieldLabel>
                  <SegmentedToggle
                    value={customerMode}
                    onChange={setCustomerMode}
                    options={[{ value: "existing", label: "Existing" }, { value: "new", label: "New" }]}
                  />
                </div>
                {customerMode === "existing" ? (
                  <EntityPicker
                    value={customerId}
                    onChange={setCustomerId}
                    options={customers.map((c) => ({
                      id: c.id,
                      label: c.fullName,
                      sublabel: [c.phone, c.email].filter(Boolean).join(" · ") || undefined,
                      keywords: [c.phone, c.email].filter(Boolean) as string[],
                    }))}
                    placeholder="Select a customer"
                    searchPlaceholder="Search customers by name, phone, email…"
                    emptyText="No customers match."
                    allowClear={false}
                  />
                ) : (
                  <FieldGroup>
                    <div className="grid grid-cols-2 gap-4">
                      <Field>
                        <FieldLabel>First name <span className="text-destructive">*</span></FieldLabel>
                        <Input
                          value={newCustomer.firstName}
                          onChange={(e) => setNewCustomer({ ...newCustomer, firstName: e.target.value })}
                        />
                      </Field>
                      <Field>
                        <FieldLabel>Last name <span className="text-destructive">*</span></FieldLabel>
                        <Input
                          value={newCustomer.lastName}
                          onChange={(e) => setNewCustomer({ ...newCustomer, lastName: e.target.value })}
                        />
                      </Field>
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                      <Field>
                        <FieldLabel>Phone</FieldLabel>
                        <Input
                          type="tel"
                          value={newCustomer.phone}
                          onChange={(e) => setNewCustomer({ ...newCustomer, phone: e.target.value })}
                        />
                      </Field>
                      <Field>
                        <FieldLabel>Email</FieldLabel>
                        <Input
                          type="email"
                          value={newCustomer.email}
                          onChange={(e) => setNewCustomer({ ...newCustomer, email: e.target.value })}
                        />
                      </Field>
                    </div>
                    <Field>
                      <FieldLabel>City</FieldLabel>
                      <Input
                        value={newCustomer.city}
                        onChange={(e) => setNewCustomer({ ...newCustomer, city: e.target.value })}
                      />
                    </Field>
                  </FieldGroup>
                )}
              </Field>

              <Field>
                <FieldLabel>Mechanic</FieldLabel>
                <EntityPicker
                  value={mechanicId}
                  onChange={setMechanicId}
                  options={mechanics.map((m) => ({ id: m.id, label: m.name }))}
                  placeholder="(unassigned)"
                  searchPlaceholder="Search mechanics…"
                  emptyText="No mechanics match."
                  clearLabel="Unassigned"
                />
                <FieldDescription>Can be assigned later</FieldDescription>
              </Field>
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Component</CardTitle></CardHeader>
            <CardContent>
              <FieldGroup>
              <SegmentedToggle
                value={componentMode}
                onChange={setComponentMode}
                options={[{ value: "existing", label: "Existing" }, { value: "new", label: "New" }]}
              />

              {componentMode === "existing" ? (
                <Field>
                  <FieldLabel>Select component</FieldLabel>
                  <EntityPicker
                    value={componentId}
                    onChange={setComponentId}
                    disabled={!customerId}
                    options={components.map((c) => ({
                      id: c.id,
                      label: c.name ?? c.componentType,
                      sublabel: [c.brand, c.sku].filter(Boolean).join(" · ") || undefined,
                      keywords: [c.brand, c.sku, c.componentType].filter(Boolean) as string[],
                    }))}
                    placeholder={customerId ? "Pick a component" : "Select a customer first"}
                    searchPlaceholder="Search by name, brand, SKU…"
                    emptyText="This customer has no components on file."
                    allowClear={false}
                  />
                  {!customerId && <FieldDescription>Pick a customer first, or switch to New.</FieldDescription>}
                </Field>
              ) : (
                <FieldGroup>
                  <div className="grid grid-cols-3 gap-4">
                    <Field className="col-span-2">
                      <FieldLabel>Name <span className="text-destructive">*</span></FieldLabel>
                      <Input
                        placeholder="e.g. Trek FX 3 Disc"
                        value={newComponent.name}
                        onChange={(e) => setNewComponent({ ...newComponent, name: e.target.value })}
                      />
                    </Field>
                    <Field>
                      <FieldLabel>Type</FieldLabel>
                      <Select
                        value={newComponent.componentType}
                        onValueChange={(v) => setNewComponent({ ...newComponent, componentType: v ?? "Bike" })}
                      >
                        <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Bike">Bike</SelectItem>
                          <SelectItem value="Rim">Rim</SelectItem>
                          <SelectItem value="Other">Other</SelectItem>
                        </SelectContent>
                      </Select>
                    </Field>
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <Field>
                      <FieldLabel>Brand</FieldLabel>
                      <Input
                        placeholder="Trek, Specialized…"
                        value={newComponent.brand}
                        onChange={(e) => setNewComponent({ ...newComponent, brand: e.target.value })}
                      />
                    </Field>
                    <Field>
                      <FieldLabel>Color</FieldLabel>
                      <Input
                        value={newComponent.color}
                        onChange={(e) => setNewComponent({ ...newComponent, color: e.target.value })}
                      />
                    </Field>
                  </div>
                  <Field>
                    <FieldLabel>SKU</FieldLabel>
                    <Input
                      placeholder="auto-generated"
                      value={newComponent.sku}
                      onChange={(e) => setNewComponent({ ...newComponent, sku: e.target.value })}
                    />
                    <FieldDescription>Auto-generated if blank</FieldDescription>
                  </Field>
                </FieldGroup>
              )}
              </FieldGroup>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>Service</CardTitle></CardHeader>
            <CardContent>
              <FieldGroup>
              <Field>
                <FieldLabel>Service</FieldLabel>
                <Select value={serviceId} onValueChange={(v) => setServiceId(v ?? "")}>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="(none)" />
                  </SelectTrigger>
                  <SelectContent>
                    {services.map((s) => (
                      <SelectItem key={s.id} value={s.id}>
                        <span className="flex-1">{s.name}</span>
                        <span className="text-muted-foreground tabular-nums">${s.defaultPrice.toFixed(2)}</span>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {!serviceId && <FieldDescription>Pick the base service — price comes from the catalog</FieldDescription>}
              </Field>
              <Field>
                <div className="flex items-center justify-between gap-2">
                  <FieldLabel>Description / notes</FieldLabel>
                  <Button
                    type="button" variant="ghost" size="sm"
                    onClick={suggestDescription} disabled={suggesting}
                    className="cadence-suggest-btn h-7 gap-1.5 text-xs"
                  >
                    {suggesting
                      ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
                      : <Sparkles className="cadence-icon h-3.5 w-3.5" />}
                    Suggest with Cadence
                  </Button>
                </div>
                <Textarea
                  rows={4}
                  placeholder="What should the mechanic know? Describe the work, parts to check, customer requests…"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                />
              </Field>
              </FieldGroup>
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
                        <TableCell className="font-medium">{l.productName}</TableCell>
                        <TableCell>
                          <Input
                            type="number" min={1}
                            className="h-8 w-20"
                            value={l.quantity}
                            onChange={(e) => updateLine(i, { quantity: Math.max(1, Number(e.target.value)) })}
                          />
                        </TableCell>
                        <TableCell>
                          <InputGroup className="h-8">
                            <InputGroupAddon>$</InputGroupAddon>
                            <InputGroupInput
                              type="number" step="0.01" min={0}
                              value={l.unitPrice}
                              onChange={(e) => updateLine(i, { unitPrice: Number(e.target.value) })}
                            />
                          </InputGroup>
                        </TableCell>
                        <TableCell className="text-right font-medium tabular-nums">${(l.unitPrice * l.quantity).toFixed(2)}</TableCell>
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
          <Card className="sticky top-6">
            <CardHeader><CardTitle>Summary</CardTitle></CardHeader>
            <CardContent className="space-y-3 text-sm">
              <Row label="Service"><span className="tabular-nums">${servicePrice.toFixed(2)}</span></Row>
              <Row label="Products"><span className="tabular-nums">${productsSubtotal.toFixed(2)}</span></Row>
              <Row label="Subtotal"><span className="tabular-nums">${subtotal.toFixed(2)}</span></Row>
              <div className="flex items-center justify-between gap-3">
                <Label className="text-muted-foreground m-0">Discount</Label>
                <InputGroup className="h-8 w-24">
                  <InputGroupInput
                    type="number" min={0} max={100}
                    value={discountPercent}
                    onChange={(e) => setDiscountPercent(Number(e.target.value))}
                  />
                  <InputGroupAddon align="inline-end">%</InputGroupAddon>
                </InputGroup>
              </div>
              {discount > 0 && (
                <Row label={<span className="text-muted-foreground">Discount applied</span>}>
                  <span className="tabular-nums text-emerald-600">-${discount.toFixed(2)}</span>
                </Row>
              )}
              <Separator />
              <Row label={<span className="font-semibold">Total</span>}>
                <span className="font-semibold text-lg tabular-nums">${total.toFixed(2)}</span>
              </Row>
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

