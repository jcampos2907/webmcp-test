import { useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { CreditCard, Pencil, Plus, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog"
import { Form, FormField } from "@/components/ui/form"
import { Field, FieldGroup, FieldLabel, FieldError } from "@/components/ui/field"
import {
  terminalsApi, organizationApi,
  type AdminTerminal, type UpsertTerminal, type AdminStore,
} from "@/lib/api"

const PROVIDERS = ["Manual", "Ingenico", "Verifone", "PAX", "Nexgo"] as const

const schema = z.object({
  storeId: z.string().min(1, "Select a store"),
  name: z.string().min(1, "Name is required"),
  ipAddress: z.string().min(1, "IP is required"),
  port: z.number().int().min(1).max(65535),
  provider: z.string().min(1),
  isActive: z.boolean(),
})
type FormValues = z.infer<typeof schema>

export default function TerminalsSection() {
  const [items, setItems] = useState<AdminTerminal[]>([])
  const [stores, setStores] = useState<AdminStore[]>([])
  const [loading, setLoading] = useState(true)
  const [open, setOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { storeId: "", name: "", ipAddress: "", port: 8080, provider: "Manual", isActive: true },
  })

  async function load() {
    setLoading(true)
    try {
      const [terms, tree] = await Promise.all([terminalsApi.list(), organizationApi.tree()])
      setItems(terms)
      setStores(tree.flatMap((c) => c.companies.flatMap((co) => co.stores)))
    } finally { setLoading(false) }
  }
  useEffect(() => { load() }, [])

  const storeMap = useMemo(() => new Map(stores.map((s) => [s.id, s.name])), [stores])

  function openNew() {
    setEditingId(null)
    form.reset({ storeId: stores[0]?.id ?? "", name: "", ipAddress: "", port: 8080, provider: "Manual", isActive: true })
    setOpen(true)
  }

  function openEdit(t: AdminTerminal) {
    setEditingId(t.id)
    form.reset({
      storeId: t.storeId,
      name: t.name,
      ipAddress: t.ipAddress,
      port: t.port,
      provider: t.provider,
      isActive: t.isActive,
    })
    setOpen(true)
  }

  async function onSubmit(values: FormValues) {
    const payload: UpsertTerminal = { ...values }
    try {
      if (editingId) {
        await terminalsApi.update(editingId, payload)
        toast.success("Terminal updated")
      } else {
        await terminalsApi.create(payload)
        toast.success("Terminal created")
      }
      setOpen(false); setEditingId(null); load()
    } catch (e) { toast.error(String(e)) }
  }

  async function remove(t: AdminTerminal) {
    if (!confirm(`Remove terminal "${t.name}"?`)) return
    try { await terminalsApi.remove(t.id); toast.success("Removed"); load() }
    catch (e) { toast.error(String(e)) }
  }

  return (
    <Card>
      <div className="flex items-center justify-between p-4">
        <p className="text-sm text-muted-foreground">{items.length} terminal{items.length === 1 ? "" : "s"}</p>
        <Button size="sm" disabled={stores.length === 0} onClick={openNew}>
          <Plus className="h-3.5 w-3.5" /> New terminal
        </Button>
      </div>
      <CardContent className="pt-0">
        {loading ? (
          <div className="space-y-2">{Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-10" />)}</div>
        ) : items.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-10 text-muted-foreground">
            <CreditCard className="h-8 w-8 mb-2 opacity-40" />
            <p className="text-sm">No terminals configured</p>
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Store</TableHead>
                <TableHead>Provider</TableHead>
                <TableHead>Address</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-20 text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((t) => (
                <TableRow key={t.id}>
                  <TableCell className="font-medium">{t.name}</TableCell>
                  <TableCell className="text-muted-foreground">{t.storeName || storeMap.get(t.storeId) || "—"}</TableCell>
                  <TableCell><Badge variant="outline">{t.provider}</Badge></TableCell>
                  <TableCell className="text-muted-foreground font-mono text-xs">{t.ipAddress}:{t.port}</TableCell>
                  <TableCell><Badge variant={t.isActive ? "default" : "secondary"}>{t.isActive ? "Active" : "Inactive"}</Badge></TableCell>
                  <TableCell className="text-right">
                    <Button size="icon-sm" variant="ghost" onClick={() => openEdit(t)}>
                      <Pencil className="h-3.5 w-3.5" />
                    </Button>
                    <Button size="icon-sm" variant="ghost" onClick={() => remove(t)}>
                      <Trash2 className="h-3.5 w-3.5 text-destructive" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      <Dialog open={open} onOpenChange={(v) => { if (!v) { setOpen(false); setEditingId(null) } }}>
        <DialogContent>
          <DialogHeader><DialogTitle>{editingId ? "Edit terminal" : "New terminal"}</DialogTitle></DialogHeader>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              <FieldGroup>
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid || undefined}>
                      <FieldLabel htmlFor="terminal-name">Name</FieldLabel>
                      <Input id="terminal-name" placeholder="Front counter" {...field} aria-invalid={fieldState.invalid} />
                      <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                    </Field>
                  )}
                />
                <FormField
                  control={form.control}
                  name="storeId"
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid || undefined}>
                      <FieldLabel>Store</FieldLabel>
                      <Select value={field.value} onValueChange={(v) => field.onChange(v ?? "")}>
                        <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                        <SelectContent>
                          {stores.map((s) => <SelectItem key={s.id} value={s.id}>{s.name}</SelectItem>)}
                        </SelectContent>
                      </Select>
                      <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                    </Field>
                  )}
                />
                <div className="grid grid-cols-3 gap-4">
                  <FormField
                    control={form.control}
                    name="ipAddress"
                    render={({ field, fieldState }) => (
                      <Field className="col-span-2" data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="terminal-ipAddress">IP address</FieldLabel>
                        <Input id="terminal-ipAddress" placeholder="192.168.1.10" {...field} aria-invalid={fieldState.invalid} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="port"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="terminal-port">Port</FieldLabel>
                        <Input
                          id="terminal-port"
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
                <FormField
                  control={form.control}
                  name="provider"
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid || undefined}>
                      <FieldLabel>Provider</FieldLabel>
                      <Select value={field.value} onValueChange={(v) => field.onChange(v ?? "Manual")}>
                        <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                        <SelectContent>
                          {PROVIDERS.map((p) => <SelectItem key={p} value={p}>{p}</SelectItem>)}
                        </SelectContent>
                      </Select>
                      <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                    </Field>
                  )}
                />
                <FormField
                  control={form.control}
                  name="isActive"
                  render={({ field }) => (
                    <Field orientation="horizontal">
                      <Checkbox id="terminal-isActive" checked={field.value} onCheckedChange={(c) => field.onChange(Boolean(c))} />
                      <FieldLabel htmlFor="terminal-isActive" className="font-normal">Active</FieldLabel>
                    </Field>
                  )}
                />
              </FieldGroup>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => { setOpen(false); setEditingId(null) }}>Cancel</Button>
                <Button type="submit" disabled={form.formState.isSubmitting}>Save</Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </Card>
  )
}
