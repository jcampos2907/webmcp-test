import { useEffect, useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Card, CardContent } from "@/components/ui/card"
import {
  Field, FieldGroup, FieldLabel, FieldError,
} from "@/components/ui/field"
import { Form, FormField } from "@/components/ui/form"
import { PageHeader } from "@/components/PageHeader"
import { mechanicsApi, type MechanicInput } from "@/lib/api"

const schema = z.object({
  name: z.string().min(1, "Name is required"),
  phone: z.string().nullable(),
  email: z.string().nullable().refine(
    (v) => !v || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v),
    { message: "Invalid email" }
  ),
  isActive: z.boolean(),
})

type FormValues = z.infer<typeof schema>

export default function MechanicFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isEdit = Boolean(id)
  const [loading, setLoading] = useState(isEdit)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", phone: "", email: "", isActive: true },
  })

  useEffect(() => {
    if (!id) return
    mechanicsApi
      .get(id)
      .then((m) =>
        form.reset({
          name: m.name,
          phone: m.phone ?? "",
          email: m.email ?? "",
          isActive: m.isActive,
        })
      )
      .finally(() => setLoading(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  async function onSubmit(values: FormValues) {
    const payload: MechanicInput = {
      name: values.name,
      phone: values.phone || null,
      email: values.email || null,
      isActive: values.isActive,
    }
    try {
      if (isEdit && id) {
        await mechanicsApi.update(id, payload)
        toast.success("Mechanic updated")
      } else {
        await mechanicsApi.create(payload)
        toast.success("Mechanic created")
      }
      navigate("/mechanics")
    } catch (err) {
      toast.error(String(err))
    }
  }

  async function onDelete() {
    if (!id || !confirm("Delete this mechanic?")) return
    try {
      await mechanicsApi.remove(id)
      toast.success("Deleted")
      navigate("/mechanics")
    } catch (err) {
      toast.error(String(err))
    }
  }

  if (loading) return <div className="p-8">Loading...</div>

  return (
    <div className="p-8 max-w-xl mx-auto">
      <PageHeader title={isEdit ? "Edit mechanic" : "New mechanic"} />
      <Card>
        <CardContent className="pt-6">
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              <FieldGroup>
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid || undefined}>
                      <FieldLabel htmlFor="name">Name <span className="text-destructive">*</span></FieldLabel>
                      <Input id="name" {...field} aria-invalid={fieldState.invalid} />
                      <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                    </Field>
                  )}
                />
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="phone"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="phone">Phone</FieldLabel>
                        <Input id="phone" {...field} value={field.value ?? ""} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="email"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="email">Email</FieldLabel>
                        <Input id="email" type="email" {...field} value={field.value ?? ""} aria-invalid={fieldState.invalid} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                </div>
                <FormField
                  control={form.control}
                  name="isActive"
                  render={({ field }) => (
                    <Field orientation="horizontal">
                      <Checkbox
                        id="isActive"
                        checked={field.value}
                        onCheckedChange={(c) => field.onChange(Boolean(c))}
                      />
                      <FieldLabel htmlFor="isActive" className="font-normal">Active</FieldLabel>
                    </Field>
                  )}
                />
              </FieldGroup>
              <div className="flex justify-between pt-2">
                <div>
                  {isEdit && (
                    <Button type="button" variant="destructive" onClick={onDelete}>
                      Delete
                    </Button>
                  )}
                </div>
                <div className="flex gap-2">
                  <Button type="button" variant="outline" onClick={() => navigate("/mechanics")}>
                    Cancel
                  </Button>
                  <Button type="submit" disabled={form.formState.isSubmitting}>
                    {form.formState.isSubmitting ? "Saving..." : "Save"}
                  </Button>
                </div>
              </div>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  )
}
