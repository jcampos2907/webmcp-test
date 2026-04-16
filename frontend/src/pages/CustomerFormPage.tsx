import { useEffect, useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent } from "@/components/ui/card"
import {
  Field, FieldGroup, FieldLabel, FieldError,
} from "@/components/ui/field"
import { Form, FormField } from "@/components/ui/form"
import { PageHeader } from "@/components/PageHeader"
import { customersApi, type CustomerInput } from "@/lib/api"

const schema = z.object({
  firstName: z.string().min(1, "First name is required"),
  lastName: z.string().min(1, "Last name is required"),
  phone: z.string().nullable(),
  email: z.string().nullable().refine(
    (v) => !v || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v),
    { message: "Invalid email" }
  ),
  street: z.string().nullable(),
  city: z.string().nullable(),
  state: z.string().nullable(),
  zipCode: z.string().nullable(),
  country: z.string().nullable(),
})

type FormValues = z.infer<typeof schema>

const empty: FormValues = {
  firstName: "",
  lastName: "",
  phone: "",
  email: "",
  street: "",
  city: "",
  state: "",
  zipCode: "",
  country: "",
}

export default function CustomerFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isEdit = Boolean(id)
  const [loading, setLoading] = useState(isEdit)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: empty,
  })

  useEffect(() => {
    if (!id) return
    customersApi
      .get(id)
      .then((c) =>
        form.reset({
          firstName: c.firstName,
          lastName: c.lastName,
          phone: c.phone ?? "",
          email: c.email ?? "",
          street: c.street ?? "",
          city: c.city ?? "",
          state: c.state ?? "",
          zipCode: c.zipCode ?? "",
          country: c.country ?? "",
        })
      )
      .finally(() => setLoading(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  async function onSubmit(values: FormValues) {
    const payload: CustomerInput = {
      firstName: values.firstName,
      lastName: values.lastName,
      phone: values.phone || null,
      email: values.email || null,
      street: values.street || null,
      city: values.city || null,
      state: values.state || null,
      zipCode: values.zipCode || null,
      country: values.country || null,
    }
    try {
      if (isEdit && id) {
        await customersApi.update(id, payload)
        toast.success("Customer updated")
      } else {
        await customersApi.create(payload)
        toast.success("Customer created")
      }
      navigate("/customers")
    } catch (err) {
      toast.error(String(err))
    }
  }

  async function onDelete() {
    if (!id || !confirm("Delete this customer?")) return
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
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              <FieldGroup>
                <div className="grid grid-cols-2 gap-4">
                  <TextField control={form.control} name="firstName" label="First name" required />
                  <TextField control={form.control} name="lastName" label="Last name" required />
                  <TextField control={form.control} name="phone" label="Phone" />
                  <TextField control={form.control} name="email" label="Email" type="email" />
                </div>
                <TextField control={form.control} name="street" label="Street" />
                <div className="grid grid-cols-2 gap-4">
                  <TextField control={form.control} name="city" label="City" />
                  <TextField control={form.control} name="state" label="State" />
                  <TextField control={form.control} name="zipCode" label="Zip code" />
                  <TextField control={form.control} name="country" label="Country" />
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
                  <Button type="button" variant="outline" onClick={() => navigate("/customers")}>
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

function TextField({
  control,
  name,
  label,
  type = "text",
  required,
  className,
}: {
  control: ReturnType<typeof useForm<FormValues>>["control"]
  name: keyof FormValues
  label: string
  type?: string
  required?: boolean
  className?: string
}) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <Field className={className} data-invalid={fieldState.invalid || undefined}>
          <FieldLabel htmlFor={name}>
            {label}
            {required && <span className="text-destructive">*</span>}
          </FieldLabel>
          <Input id={name} type={type} {...field} value={field.value ?? ""} aria-invalid={fieldState.invalid} />
          <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
        </Field>
      )}
    />
  )
}
