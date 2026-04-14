import { useEffect, useState, type FormEvent } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent } from "@/components/ui/card"
import { PageHeader } from "@/components/PageHeader"
import { mechanicsApi, type MechanicInput } from "@/lib/api"

const empty: MechanicInput = { name: "", phone: null, email: null, isActive: true }

export default function MechanicFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isEdit = Boolean(id)
  const [form, setForm] = useState<MechanicInput>(empty)
  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    mechanicsApi.get(id).then((m) => setForm({ name: m.name, phone: m.phone, email: m.email, isActive: m.isActive })).finally(() => setLoading(false))
  }, [id])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    try {
      if (isEdit && id) { await mechanicsApi.update(id, form); toast.success("Mechanic updated") }
      else { await mechanicsApi.create(form); toast.success("Mechanic created") }
      navigate("/mechanics")
    } catch (err) { toast.error(String(err)) }
    finally { setSaving(false) }
  }

  async function onDelete() {
    if (!id || !confirm("Delete this mechanic?")) return
    try { await mechanicsApi.remove(id); toast.success("Deleted"); navigate("/mechanics") }
    catch (err) { toast.error(String(err)) }
  }

  if (loading) return <div className="p-8">Loading...</div>

  return (
    <div className="p-8 max-w-xl mx-auto">
      <PageHeader title={isEdit ? "Edit mechanic" : "New mechanic"} />
      <Card>
        <CardContent className="pt-6">
          <form onSubmit={onSubmit} className="space-y-4">
            <div>
              <Label className="mb-2 block">Name *</Label>
              <Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            </div>
            <div>
              <Label className="mb-2 block">Phone</Label>
              <Input value={form.phone ?? ""} onChange={(e) => setForm({ ...form, phone: e.target.value || null })} />
            </div>
            <div>
              <Label className="mb-2 block">Email</Label>
              <Input type="email" value={form.email ?? ""} onChange={(e) => setForm({ ...form, email: e.target.value || null })} />
            </div>
            <label className="flex items-center gap-2">
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
              <span className="text-sm">Active</span>
            </label>
            <div className="flex justify-between pt-2">
              <div>{isEdit && <Button type="button" variant="destructive" onClick={onDelete}>Delete</Button>}</div>
              <div className="flex gap-2">
                <Button type="button" variant="outline" onClick={() => navigate("/mechanics")}>Cancel</Button>
                <Button type="submit" disabled={saving}>{saving ? "Saving..." : "Save"}</Button>
              </div>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
