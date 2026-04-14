import { useEffect, useState, type FormEvent } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Card, CardContent } from "@/components/ui/card"
import { PageHeader } from "@/components/PageHeader"
import { servicesApi, type ServiceInput } from "@/lib/api"

const empty: ServiceInput = { name: "", description: null, defaultPrice: 0, estimatedMinutes: null }

export default function ServiceFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isEdit = Boolean(id)
  const [form, setForm] = useState<ServiceInput>(empty)
  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    servicesApi.get(id).then((s) => setForm({ name: s.name, description: s.description, defaultPrice: s.defaultPrice, estimatedMinutes: s.estimatedMinutes })).finally(() => setLoading(false))
  }, [id])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    try {
      if (isEdit && id) { await servicesApi.update(id, form); toast.success("Service updated") }
      else { await servicesApi.create(form); toast.success("Service created") }
      navigate("/services")
    } catch (err) { toast.error(String(err)) }
    finally { setSaving(false) }
  }

  async function onDelete() {
    if (!id || !confirm("Delete this service?")) return
    try { await servicesApi.remove(id); toast.success("Deleted"); navigate("/services") }
    catch (err) { toast.error(String(err)) }
  }

  if (loading) return <div className="p-8">Loading...</div>

  return (
    <div className="p-8 max-w-xl mx-auto">
      <PageHeader title={isEdit ? "Edit service" : "New service"} />
      <Card>
        <CardContent className="pt-6">
          <form onSubmit={onSubmit} className="space-y-4">
            <div>
              <Label className="mb-2 block">Name *</Label>
              <Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            </div>
            <div>
              <Label className="mb-2 block">Description</Label>
              <Textarea value={form.description ?? ""} onChange={(e) => setForm({ ...form, description: e.target.value || null })} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label className="mb-2 block">Default price *</Label>
                <Input type="number" step="0.01" value={form.defaultPrice} onChange={(e) => setForm({ ...form, defaultPrice: Number(e.target.value) })} required />
              </div>
              <div>
                <Label className="mb-2 block">Estimated minutes</Label>
                <Input type="number" value={form.estimatedMinutes ?? ""} onChange={(e) => setForm({ ...form, estimatedMinutes: e.target.value ? Number(e.target.value) : null })} />
              </div>
            </div>
            <div className="flex justify-between pt-2">
              <div>{isEdit && <Button type="button" variant="destructive" onClick={onDelete}>Delete</Button>}</div>
              <div className="flex gap-2">
                <Button type="button" variant="outline" onClick={() => navigate("/services")}>Cancel</Button>
                <Button type="submit" disabled={saving}>{saving ? "Saving..." : "Save"}</Button>
              </div>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
