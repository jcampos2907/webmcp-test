import { useEffect, useState } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent } from "@/components/ui/card"
import { InputGroup, InputGroupAddon, InputGroupInput } from "@/components/ui/input-group"
import {
  Field, FieldGroup, FieldLabel, FieldError,
} from "@/components/ui/field"
import { Form, FormField } from "@/components/ui/form"
import { PageHeader } from "@/components/PageHeader"
import { productsApi, type ProductInput } from "@/lib/api"

const schema = z.object({
  name: z.string().min(1, "Name is required"),
  sku: z.string().nullable(),
  category: z.string().nullable(),
  price: z.number().min(0, "Must be >= 0"),
  quantityInStock: z.number().int().min(0, "Must be >= 0"),
})

type FormValues = z.infer<typeof schema>

export default function ProductFormPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isEdit = Boolean(id)
  const [loading, setLoading] = useState(isEdit)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", sku: "", category: "", price: 0, quantityInStock: 0 },
  })

  useEffect(() => {
    if (!id) return
    productsApi
      .get(id)
      .then((p) =>
        form.reset({
          name: p.name,
          sku: p.sku ?? "",
          category: p.category ?? "",
          price: p.price,
          quantityInStock: p.quantityInStock,
        })
      )
      .finally(() => setLoading(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  async function onSubmit(values: FormValues) {
    const payload: ProductInput = {
      name: values.name,
      sku: (values.sku as string) || null,
      category: (values.category as string) || null,
      price: Number(values.price),
      quantityInStock: Number(values.quantityInStock),
    }
    try {
      if (isEdit && id) {
        await productsApi.update(id, payload)
        toast.success("Product updated")
      } else {
        await productsApi.create(payload)
        toast.success("Product created")
      }
      navigate("/products")
    } catch (err) {
      toast.error(String(err))
    }
  }

  async function onDelete() {
    if (!id || !confirm("Delete this product?")) return
    try {
      await productsApi.remove(id)
      toast.success("Deleted")
      navigate("/products")
    } catch (err) {
      toast.error(String(err))
    }
  }

  if (loading) return <div className="p-8">Loading...</div>

  return (
    <div className="p-8 max-w-xl mx-auto">
      <PageHeader title={isEdit ? "Edit product" : "New product"} />
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
                    name="sku"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="sku">SKU</FieldLabel>
                        <Input id="sku" {...field} value={field.value ?? ""} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="category"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="category">Category</FieldLabel>
                        <Input id="category" {...field} value={field.value ?? ""} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="price"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="price">Price <span className="text-destructive">*</span></FieldLabel>
                        <InputGroup>
                          <InputGroupAddon>$</InputGroupAddon>
                          <InputGroupInput
                            id="price"
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
                    name="quantityInStock"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="stock">Stock <span className="text-destructive">*</span></FieldLabel>
                        <Input
                          id="stock"
                          type="number"
                          value={field.value ?? 0}
                          onChange={(e) => field.onChange(e.target.value === "" ? 0 : Number(e.target.value))}
                          onBlur={field.onBlur}
                          name={field.name}
                          ref={field.ref}
                          aria-invalid={fieldState.invalid}
                        />
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
                  <Button type="button" variant="outline" onClick={() => navigate("/products")}>
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
