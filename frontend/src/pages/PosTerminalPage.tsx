import { useEffect, useMemo, useState } from "react"
import { useNavigate } from "react-router-dom"
import { toast } from "sonner"
import { Search, Trash2, Plus, Minus, ShoppingCart } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog"
import { PageHeader } from "@/components/PageHeader"
import {
  productsApi, customersApi, componentsApi, createTicketApi,
  type Product, type Customer,
} from "@/lib/api"

type CartLine = { productId: string; productName: string; unitPrice: number; quantity: number; inStock: number }

const QUICK_SALE_COMPONENT_NAME = "Walk-in sale"

export default function PosTerminalPage() {
  const navigate = useNavigate()
  const [products, setProducts] = useState<Product[]>([])
  const [customers, setCustomers] = useState<Customer[]>([])
  const [customerId, setCustomerId] = useState("")
  const [search, setSearch] = useState("")
  const [cart, setCart] = useState<CartLine[]>([])
  const [checkoutOpen, setCheckoutOpen] = useState(false)
  const [method, setMethod] = useState("Cash")
  const [cashier, setCashier] = useState("")
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    productsApi.list().then(setProducts)
    customersApi.list().then(setCustomers)
  }, [])

  const filteredProducts = useMemo(() => {
    if (!search) return products.slice(0, 12)
    const q = search.toLowerCase()
    return products.filter((p) =>
      p.name.toLowerCase().includes(q) || (p.sku?.toLowerCase().includes(q) ?? false)
    ).slice(0, 20)
  }, [search, products])

  function addToCart(p: Product) {
    setCart((prev) => {
      const existing = prev.find((l) => l.productId === p.id)
      if (existing) return prev.map((l) => l.productId === p.id ? { ...l, quantity: l.quantity + 1 } : l)
      return [...prev, { productId: p.id, productName: p.name, unitPrice: p.price, quantity: 1, inStock: p.quantityInStock }]
    })
  }

  function updateQty(id: string, delta: number) {
    setCart((prev) => prev.flatMap((l) => {
      if (l.productId !== id) return [l]
      const q = l.quantity + delta
      return q <= 0 ? [] : [{ ...l, quantity: q }]
    }))
  }

  function removeLine(id: string) {
    setCart((prev) => prev.filter((l) => l.productId !== id))
  }

  const subtotal = cart.reduce((s, l) => s + l.unitPrice * l.quantity, 0)

  async function checkout() {
    if (cart.length === 0) return
    setBusy(true)
    try {
      const component = await componentsApi.create({
        name: QUICK_SALE_COMPONENT_NAME,
        componentType: "Other",
        customerId: customerId || null,
        brand: null,
        color: null,
        sku: null,
        price: 0,
      })
      const ticket = await createTicketApi.create({
        componentId: component.id,
        customerId: customerId || null,
        mechanicId: null,
        baseServiceId: null,
        baseServicePrice: 0,
        description: "POS quick sale",
        discountPercent: 0,
        products: cart.map((l) => ({ productId: l.productId, productName: l.productName, unitPrice: l.unitPrice, quantity: l.quantity })),
      })
      await createTicketApi.charge(ticket.ticketId, subtotal, method, cashier || undefined)
      toast.success(`Sale complete · ${ticket.ticketDisplay}`)
      setCart([])
      setCustomerId("")
      setCheckoutOpen(false)
      navigate(`/tickets/${ticket.ticketId}`)
    } catch (err) {
      toast.error(String(err))
    } finally {
      setBusy(false)
    }
  }

  const itemCount = cart.reduce((s, l) => s + l.quantity, 0)

  return (
    <div className="p-6 lg:p-8 max-w-7xl mx-auto space-y-6">
      <PageHeader title="POS terminal" description="Quick-sale cart for walk-in customers" />

      <div className="grid grid-cols-1 lg:grid-cols-[1fr_380px] gap-6">
        <div className="space-y-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input className="pl-9 h-10" placeholder="Search products by name or SKU..." value={search} onChange={(e) => setSearch(e.target.value)} autoFocus />
          </div>
          <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-3">
            {filteredProducts.map((p) => (
              <button
                key={p.id}
                onClick={() => addToCart(p)}
                disabled={p.quantityInStock <= 0}
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
            ))}
            {filteredProducts.length === 0 && <p className="col-span-full text-center py-8 text-sm text-muted-foreground">No products found</p>}
          </div>
        </div>

        <Card className="lg:sticky lg:top-4 self-start flex flex-col max-h-[calc(100vh-120px)]">
          <CardHeader className="border-b pb-3">
            <CardTitle className="flex items-center justify-between">
              <span className="flex items-center gap-2"><ShoppingCart className="h-5 w-5" /> Cart</span>
              <Badge variant="secondary" className="tabular-nums">{itemCount} item{itemCount === 1 ? "" : "s"}</Badge>
            </CardTitle>
          </CardHeader>
          <CardContent className="flex-1 overflow-y-auto space-y-4 pt-4">
            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground uppercase tracking-wide">Customer</Label>
              <Select value={customerId} onValueChange={(v) => setCustomerId(v ?? "")}>
                <SelectTrigger><SelectValue placeholder="Walk-in customer" /></SelectTrigger>
                <SelectContent>
                  {customers.map((c) => <SelectItem key={c.id} value={c.id}>{c.fullName}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>

            <Separator />

            {cart.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-10 text-muted-foreground">
                <ShoppingCart className="h-8 w-8 mb-2 opacity-40" />
                <p className="text-sm">Cart is empty</p>
                <p className="text-xs mt-1">Tap a product to add</p>
              </div>
            ) : (
              <div className="space-y-2">
                {cart.map((l) => (
                  <div key={l.productId} className="flex items-start gap-2 text-sm rounded-md p-2 hover:bg-muted/50">
                    <div className="flex-1 min-w-0">
                      <div className="font-medium truncate">{l.productName}</div>
                      <div className="text-xs text-muted-foreground tabular-nums">
                        ${l.unitPrice.toFixed(2)} × {l.quantity} = <span className="font-medium text-foreground">${(l.unitPrice * l.quantity).toFixed(2)}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      <Button variant="outline" size="icon-sm" onClick={() => updateQty(l.productId, -1)}><Minus className="h-3 w-3" /></Button>
                      <span className="w-5 text-center tabular-nums text-sm">{l.quantity}</span>
                      <Button variant="outline" size="icon-sm" onClick={() => updateQty(l.productId, 1)}><Plus className="h-3 w-3" /></Button>
                      <Button variant="ghost" size="icon-sm" onClick={() => removeLine(l.productId)}><Trash2 className="h-3 w-3" /></Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
          <div className="border-t p-4 space-y-3 bg-muted/20">
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">Subtotal</span>
              <span className="text-sm tabular-nums">${subtotal.toFixed(2)}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="font-semibold">Total</span>
              <span className="text-2xl font-semibold tabular-nums">${subtotal.toFixed(2)}</span>
            </div>
            <Button className="w-full" size="lg" disabled={cart.length === 0} onClick={() => setCheckoutOpen(true)}>
              Checkout
            </Button>
          </div>
        </Card>
      </div>

      <Dialog open={checkoutOpen} onOpenChange={setCheckoutOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Checkout — ${subtotal.toFixed(2)}</DialogTitle></DialogHeader>
          <div className="space-y-3">
            <div>
              <Label>Payment method</Label>
              <Select value={method} onValueChange={(v) => setMethod(v ?? "Cash")}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Cash">Cash</SelectItem>
                  <SelectItem value="Card">Card</SelectItem>
                  <SelectItem value="Transfer">Transfer</SelectItem>
                  <SelectItem value="Other">Other</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label>Cashier (optional)</Label>
              <Input value={cashier} onChange={(e) => setCashier(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCheckoutOpen(false)} disabled={busy}>Cancel</Button>
            <Button onClick={checkout} disabled={busy}>{busy ? "Processing..." : `Charge $${subtotal.toFixed(2)}`}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
