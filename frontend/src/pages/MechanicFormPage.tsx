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
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
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
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Name *</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="phone"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Phone</FormLabel>
                    <FormControl>
                      <Input {...field} value={field.value ?? ""} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Email</FormLabel>
                    <FormControl>
                      <Input type="email" {...field} value={field.value ?? ""} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="isActive"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center gap-2 space-y-0">
                    <FormControl>
                      <Checkbox
                        checked={field.value}
                        onCheckedChange={(c) => field.onChange(Boolean(c))}
                      />
                    </FormControl>
                    <FormLabel className="mb-0">Active</FormLabel>
                  </FormItem>
                )}
              />
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
