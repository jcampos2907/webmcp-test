import { useEffect, useState, type FormEvent } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent } from "@/components/ui/card"
import { PageHeader } from "@/components/PageHeader"
import { customersApi, type CustomerInput } from "@/lib/api"

const empty: CustomerInput = {
  firstName: "",
  lastName: "",
  phone: null,
  email: null,
  street: null,
  city: null,
  state: null,
  zipCode: null,
  country: null,
}

export default function CustomerFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isEdit = Boolean(id)
  const [form, setForm] = useState<CustomerInput>(empty)
  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    customersApi
      .get(id)
      .then((c) =>
        setForm({
          firstName: c.firstName,
          lastName: c.lastName,
          phone: c.phone,
          email: c.email,
          street: c.street,
          city: c.city,
          state: c.state,
          zipCode: c.zipCode,
          country: c.country,
        })
      )
      .finally(() => setLoading(false))
  }, [id])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    try {
      if (isEdit && id) {
        await customersApi.update(id, form)
        toast.success("Customer updated")
      } else {
        await customersApi.create(form)
        toast.success("Customer created")
      }
      navigate("/customers")
    } catch (err) {
      toast.error(String(err))
    } finally {
      setSaving(false)
    }
  }

  async function onDelete() {
    if (!id) return
    if (!confirm("Delete this customer?")) return
    try {
      await customersApi.remove(id)
      toast.success("Customer deleted")
      navigate("/customers")
    } catch (err) {
      toast.error(String(err))
    }
  }

  if (loading) return <div className="p-8">Loading...</div>

  return (
    <div className="p-8 max-w-2xl mx-auto">
      <PageHeader title={isEdit ? "Edit customer" : "New customer"} />
      <Card>
        <CardContent className="pt-6">
          <form onSubmit={onSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Field label="First name" required value={form.firstName} onChange={(v) => setForm({ ...form, firstName: v })} />
              <Field label="Last name" required value={form.lastName} onChange={(v) => setForm({ ...form, lastName: v })} />
              <Field label="Phone" value={form.phone ?? ""} onChange={(v) => setForm({ ...form, phone: v || null })} />
              <Field label="Email" type="email" value={form.email ?? ""} onChange={(v) => setForm({ ...form, email: v || null })} />
              <Field label="Street" value={form.street ?? ""} onChange={(v) => setForm({ ...form, street: v || null })} className="col-span-2" />
              <Field label="City" value={form.city ?? ""} onChange={(v) => setForm({ ...form, city: v || null })} />
              <Field label="State" value={form.state ?? ""} onChange={(v) => setForm({ ...form, state: v || null })} />
              <Field label="Zip code" value={form.zipCode ?? ""} onChange={(v) => setForm({ ...form, zipCode: v || null })} />
              <Field label="Country" value={form.country ?? ""} onChange={(v) => setForm({ ...form, country: v || null })} />
            </div>
            <div className="flex justify-between pt-2">
              <div>
                {isEdit && (
                  <Button type="button" variant="destructive" onClick={onDelete}>
                    Delete
                  </Button>
                )}
              </div>
              <div className="flex gap-2">
                <Button type="button" variant="outline" onClick={() => navigate("/customers")}>
                  Cancel
                </Button>
                <Button type="submit" disabled={saving}>
                  {saving ? "Saving..." : "Save"}
                </Button>
              </div>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}

function Field({
  label,
  value,
  onChange,
  type = "text",
  required,
  className,
}: {
  label: string
  value: string
  onChange: (v: string) => void
  type?: string
  required?: boolean
  className?: string
}) {
  return (
    <div className={className}>
      <Label className="mb-2 block">{label}{required && " *"}</Label>
      <Input type={type} value={value} onChange={(e) => onChange(e.target.value)} required={required} />
    </div>
  )
}
