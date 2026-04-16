import { useEffect, useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Card, CardContent } from "@/components/ui/card"
import { InputGroup, InputGroupAddon, InputGroupInput } from "@/components/ui/input-group"
import {
  Field, FieldGroup, FieldLabel, FieldError,
} from "@/components/ui/field"
import { Form, FormField } from "@/components/ui/form"
import { PageHeader } from "@/components/PageHeader"
import { servicesApi, type ServiceInput } from "@/lib/api"

const schema = z.object({
  name: z.string().min(1, "Name is required"),
  description: z.string().nullable(),
  defaultPrice: z.number().min(0, "Must be >= 0"),
  estimatedMinutes: z.number().int().min(0).nullable(),
})

type FormValues = z.infer<typeof schema>

export default function ServiceFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isEdit = Boolean(id)
  const [loading, setLoading] = useState(isEdit)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", description: "", defaultPrice: 0, estimatedMinutes: null },
  })

  useEffect(() => {
    if (!id) return
    servicesApi
      .get(id)
      .then((s) =>
        form.reset({
          name: s.name,
          description: s.description ?? "",
          defaultPrice: s.defaultPrice,
          estimatedMinutes: s.estimatedMinutes ?? null,
        })
      )
      .finally(() => setLoading(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  async function onSubmit(values: FormValues) {
    const payload: ServiceInput = {
      name: values.name,
      description: (values.description as string) || null,
      defaultPrice: Number(values.defaultPrice),
      estimatedMinutes: values.estimatedMinutes ?? null,
    }
    try {
      if (isEdit && id) {
        await servicesApi.update(id, payload)
        toast.success("Service updated")
      } else {
        await servicesApi.create(payload)
        toast.success("Service created")
      }
      navigate("/services")
    } catch (err) {
      toast.error(String(err))
    }
  }

  async function onDelete() {
    if (!id || !confirm("Delete this service?")) return
    try {
      await servicesApi.remove(id)
      toast.success("Deleted")
      navigate("/services")
    } catch (err) {
      toast.error(String(err))
    }
  }

  if (loading) return <div className="p-8">Loading...</div>

  return (
    <div className="p-8 max-w-xl mx-auto">
      <PageHeader title={isEdit ? "Edit service" : "New service"} />
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
                <FormField
                  control={form.control}
                  name="description"
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid || undefined}>
                      <FieldLabel htmlFor="description">Description</FieldLabel>
                      <Textarea id="description" {...field} value={field.value ?? ""} />
                      <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                    </Field>
                  )}
                />
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="defaultPrice"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="defaultPrice">Default price <span className="text-destructive">*</span></FieldLabel>
                        <InputGroup>
                          <InputGroupAddon>$</InputGroupAddon>
                          <InputGroupInput
                            id="defaultPrice"
                            type="number"
                            step="0.01"
                            value={field.value ?? 0}
                            onChange={(e) => field.onChange(e.target.value === "" ? 0 : Number(e.target.value))}
                            onBlur={field.onBlur}
                            name={field.name}
                            ref={field.ref}
                            aria-invalid={fieldState.invalid}
                          />
                        </InputGroup>
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="estimatedMinutes"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="estimatedMinutes">Estimated minutes</FieldLabel>
                        <InputGroup>
                          <InputGroupInput
                            id="estimatedMinutes"
                            type="number"
                            value={field.value ?? ""}
                            onChange={(e) => field.onChange(e.target.value === "" ? null : Number(e.target.value))}
                            onBlur={field.onBlur}
                            name={field.name}
                            ref={field.ref}
                          />
                          <InputGroupAddon align="inline-end">min</InputGroupAddon>
                        </InputGroup>
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                </div>
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
                  <Button type="button" variant="outline" onClick={() => navigate("/services")}>
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
