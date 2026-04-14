import { useEffect, useState, type FormEvent } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent } from "@/components/ui/card"
import { PageHeader } from "@/components/PageHeader"
import { productsApi, type ProductInput } from "@/lib/api"

const empty: ProductInput = { name: "", sku: null, price: 0, quantityInStock: 0, category: null }

export default function ProductFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isEdit = Boolean(id)
  const [form, setForm] = useState<ProductInput>(empty)
  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    productsApi.get(id).then((p) => setForm({ name: p.name, sku: p.sku, price: p.price, quantityInStock: p.quantityInStock, category: p.category })).finally(() => setLoading(false))
  }, [id])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    try {
      if (isEdit && id) { await productsApi.update(id, form); toast.success("Product updated") }
      else { await productsApi.create(form); toast.success("Product created") }
      navigate("/products")
    } catch (err) { toast.error(String(err)) }
    finally { setSaving(false) }
  }

  async function onDelete() {
    if (!id || !confirm("Delete this product?")) return
    try { await productsApi.remove(id); toast.success("Deleted"); navigate("/products") }
    catch (err) { toast.error(String(err)) }
  }

  if (loading) return <div className="p-8">Loading...</div>

  return (
    <div className="p-8 max-w-xl mx-auto">
      <PageHeader title={isEdit ? "Edit product" : "New product"} />
      <Card>
        <CardContent className="pt-6">
          <form onSubmit={onSubmit} className="space-y-4">
            <div>
              <Label className="mb-2 block">Name *</Label>
              <Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label className="mb-2 block">SKU</Label>
                <Input value={form.sku ?? ""} onChange={(e) => setForm({ ...form, sku: e.target.value || null })} />
              </div>
              <div>
                <Label className="mb-2 block">Category</Label>
                <Input value={form.category ?? ""} onChange={(e) => setForm({ ...form, category: e.target.value || null })} />
              </div>
              <div>
                <Label className="mb-2 block">Price *</Label>
                <Input type="number" step="0.01" value={form.price} onChange={(e) => setForm({ ...form, price: Number(e.target.value) })} required />
              </div>
              <div>
                <Label className="mb-2 block">Stock *</Label>
                <Input type="number" value={form.quantityInStock} onChange={(e) => setForm({ ...form, quantityInStock: Number(e.target.value) })} required />
              </div>
            </div>
            <div className="flex justify-between pt-2">
              <div>{isEdit && <Button type="button" variant="destructive" onClick={onDelete}>Delete</Button>}</div>
              <div className="flex gap-2">
                <Button type="button" variant="outline" onClick={() => navigate("/products")}>Cancel</Button>
                <Button type="submit" disabled={saving}>{saving ? "Saving..." : "Save"}</Button>
              </div>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
