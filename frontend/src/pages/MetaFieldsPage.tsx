import { useEffect, useState } from "react"
import { toast } from "sonner"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Plus, Trash2, Pencil } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Checkbox } from "@/components/ui/checkbox"
import { Badge } from "@/components/ui/badge"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog"
import {
  Form, FormControl, FormField, FormItem, FormLabel, FormMessage,
} from "@/components/ui/form"
import { PageHeader } from "@/components/PageHeader"
import { metaFieldsApi, type MetaField, type MetaFieldInput } from "@/lib/api"

const ENTITY_TYPES = ["Customer", "Company", "Store"]
const FIELD_TYPES = ["text", "textarea", "number", "email", "phone", "date", "select", "checkbox"]

const schema = z.object({
  entityType: z.string().min(1),
  fieldType: z.string().min(1),
  key: z.string().min(1, "Key is required"),
  label: z.string().min(1, "Label is required"),
  sortOrder: z.number().int(),
  isRequired: z.boolean(),
  isActive: z.boolean(),
  defaultValue: z.string().nullable(),
  options: z.string().nullable(),
  regexPattern: z.string().nullable(),
})

type FormValues = z.infer<typeof schema>

const emptyField: FormValues = {
  entityType: "Customer", key: "", label: "", fieldType: "text",
  isRequired: false, sortOrder: 0, isActive: true,
  options: "", defaultValue: "", regexPattern: "",
}

export default function MetaFieldsPage() {
  const [entityType, setEntityType] = useState("Customer")
  const [fields, setFields] = useState<MetaField[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<MetaField | null>(null)
  const [open, setOpen] = useState(false)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: emptyField,
  })

  async function load() {
    setLoading(true)
    try {
      setFields(await metaFieldsApi.list(entityType))
    } finally { setLoading(false) }
  }

  useEffect(() => { load() }, [entityType])

  function openNew() {
    setEditing(null)
    form.reset({ ...emptyField, entityType, sortOrder: fields.length })
    setOpen(true)
  }

  function openEdit(f: MetaField) {
    setEditing(f)
    form.reset({
      entityType: f.entityType,
      fieldType: f.fieldType,
      key: f.key,
      label: f.label,
      sortOrder: f.sortOrder,
      isRequired: f.isRequired,
      isActive: f.isActive,
      defaultValue: f.defaultValue ?? "",
      options: f.options ?? "",
      regexPattern: f.regexPattern ?? "",
    })
    setOpen(true)
  }

  async function onSubmit(values: FormValues) {
    const payload: MetaFieldInput = {
      entityType: values.entityType,
      fieldType: values.fieldType,
      key: values.key,
      label: values.label,
      sortOrder: values.sortOrder,
      isRequired: values.isRequired,
      isActive: values.isActive,
      defaultValue: values.defaultValue || null,
      options: values.options || null,
      regexPattern: values.regexPattern || null,
    }
    try {
      if (editing) await metaFieldsApi.update(editing.id, payload)
      else await metaFieldsApi.create(payload)
      toast.success("Saved")
      setOpen(false)
      await load()
    } catch (err) { toast.error(String(err)) }
  }

  async function remove(f: MetaField) {
    if (!confirm(`Delete field "${f.label}"?`)) return
    try {
      await metaFieldsApi.remove(f.id)
      toast.success("Deleted")
      await load()
    } catch (err) { toast.error(String(err)) }
  }

  const fieldType = form.watch("fieldType")

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <PageHeader
        title="Meta fields"
        description="Custom extensible fields by entity type"
        actions={<Button onClick={openNew}><Plus className="h-4 w-4" />New field</Button>}
      />

      <div className="mb-4 flex gap-2 items-center">
        <Label className="text-sm">Entity type:</Label>
        <Select value={entityType} onValueChange={(v) => setEntityType(v ?? "Customer")}>
          <SelectTrigger className="w-[200px]"><SelectValue /></SelectTrigger>
          <SelectContent>
            {ENTITY_TYPES.map((t) => <SelectItem key={t} value={t}>{t}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>

      <Card>
        <CardContent className="pt-6">
          <Table>
            <TableHeader><TableRow>
              <TableHead className="w-16">Order</TableHead>
              <TableHead>Key</TableHead>
              <TableHead>Label</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Required</TableHead>
              <TableHead>Active</TableHead>
              <TableHead className="w-24"></TableHead>
            </TableRow></TableHeader>
            <TableBody>
              {loading && <TableRow><TableCell colSpan={7} className="text-center py-8 text-muted-foreground">Loading...</TableCell></TableRow>}
              {!loading && fields.length === 0 && <TableRow><TableCell colSpan={7} className="text-center py-8 text-muted-foreground">No fields defined</TableCell></TableRow>}
              {!loading && fields.map((f) => (
                <TableRow key={f.id}>
                  <TableCell className="tabular-nums">{f.sortOrder}</TableCell>
                  <TableCell className="font-mono text-xs">{f.key}</TableCell>
                  <TableCell>{f.label}</TableCell>
                  <TableCell><Badge variant="outline">{f.fieldType}</Badge></TableCell>
                  <TableCell>{f.isRequired ? "Yes" : "—"}</TableCell>
                  <TableCell>{f.isActive ? <Badge>Active</Badge> : <Badge variant="secondary">Inactive</Badge>}</TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button variant="ghost" size="icon-sm" onClick={() => openEdit(f)}><Pencil className="h-4 w-4" /></Button>
                      <Button variant="ghost" size="icon-sm" onClick={() => remove(f)}><Trash2 className="h-4 w-4" /></Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>{editing ? "Edit field" : "New field"}</DialogTitle></DialogHeader>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <FormField
                  control={form.control}
                  name="entityType"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Entity type</FormLabel>
                      <Select value={field.value} onValueChange={(v) => field.onChange(v ?? "Customer")}>
                        <FormControl>
                          <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {ENTITY_TYPES.map((t) => <SelectItem key={t} value={t}>{t}</SelectItem>)}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="fieldType"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Field type</FormLabel>
                      <Select value={field.value} onValueChange={(v) => field.onChange(v ?? "text")}>
                        <FormControl>
                          <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {FIELD_TYPES.map((t) => <SelectItem key={t} value={t}>{t}</SelectItem>)}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <FormField
                  control={form.control}
                  name="key"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Key (unique)</FormLabel>
                      <FormControl><Input placeholder="tax_id" {...field} /></FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="label"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Label</FormLabel>
                      <FormControl><Input placeholder="Tax ID" {...field} /></FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
              <div className="grid grid-cols-3 gap-3">
                <FormField
                  control={form.control}
                  name="sortOrder"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Sort order</FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          value={field.value ?? 0}
                          onChange={(e) => field.onChange(e.target.value === "" ? 0 : Number(e.target.value))}
                          onBlur={field.onBlur}
                          name={field.name}
                          ref={field.ref}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="isRequired"
                  render={({ field }) => (
                    <FormItem className="flex flex-row items-center gap-2 space-y-0 pb-2 self-end">
                      <FormControl>
                        <Checkbox checked={field.value} onCheckedChange={(c) => field.onChange(Boolean(c))} />
                      </FormControl>
                      <FormLabel className="mb-0">Required</FormLabel>
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="isActive"
                  render={({ field }) => (
                    <FormItem className="flex flex-row items-center gap-2 space-y-0 pb-2 self-end">
                      <FormControl>
                        <Checkbox checked={field.value} onCheckedChange={(c) => field.onChange(Boolean(c))} />
                      </FormControl>
                      <FormLabel className="mb-0">Active</FormLabel>
                    </FormItem>
                  )}
                />
              </div>
              <FormField
                control={form.control}
                name="defaultValue"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Default value</FormLabel>
                    <FormControl><Input {...field} value={field.value ?? ""} /></FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {fieldType === "select" && (
                <FormField
                  control={form.control}
                  name="options"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Options (comma-separated)</FormLabel>
                      <FormControl><Input placeholder="Option A,Option B" {...field} value={field.value ?? ""} /></FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              )}
              <FormField
                control={form.control}
                name="regexPattern"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Regex pattern (optional)</FormLabel>
                    <FormControl><Input {...field} value={field.value ?? ""} /></FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
                <Button type="submit" disabled={form.formState.isSubmitting}>Save</Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
