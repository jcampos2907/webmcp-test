import { useEffect, useState } from "react"
import { toast } from "sonner"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { KeyRound, Pencil, Plus, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog"
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select"
import {
  Form, FormControl, FormField, FormItem, FormLabel, FormMessage,
} from "@/components/ui/form"
import {
  oidcApi, organizationApi,
  type AdminOidcConfig, type AdminConglomerate, type UpsertOidcConfig,
} from "@/lib/api"

const schema = z.object({
  conglomerateId: z.string().min(1, "Select a conglomerate"),
  authority: z.string().min(1, "Authority is required"),
  clientId: z.string().min(1, "Client ID is required"),
  clientSecret: z.string().nullable(),
  responseType: z.string().min(1),
  scopes: z.string().min(1),
  mapInboundClaims: z.boolean(),
  saveTokens: z.boolean(),
  getClaimsFromUserInfoEndpoint: z.boolean(),
  providerName: z.string().nullable(),
  isActive: z.boolean(),
})

type FormValues = z.infer<typeof schema>

const blank = (conglomerateId: string): FormValues => ({
  conglomerateId, authority: "", clientId: "", clientSecret: "",
  responseType: "code", scopes: "openid profile email",
  mapInboundClaims: false, saveTokens: true, getClaimsFromUserInfoEndpoint: true,
  providerName: "", isActive: true,
})

export default function OAuthSection() {
  const [items, setItems] = useState<AdminOidcConfig[]>([])
  const [conglomerates, setConglomerates] = useState<AdminConglomerate[]>([])
  const [loading, setLoading] = useState(true)
  const [open, setOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: blank(""),
  })

  async function load() {
    setLoading(true)
    try {
      const [list, tree] = await Promise.all([oidcApi.list(), organizationApi.tree()])
      setItems(list)
      setConglomerates(tree)
    } finally { setLoading(false) }
  }
  useEffect(() => { load() }, [])

  async function onSubmit(values: FormValues) {
    const payload: UpsertOidcConfig = {
      conglomerateId: values.conglomerateId,
      authority: values.authority,
      clientId: values.clientId,
      clientSecret: values.clientSecret?.trim() ? values.clientSecret : null,
      responseType: values.responseType,
      scopes: values.scopes,
      mapInboundClaims: values.mapInboundClaims,
      saveTokens: values.saveTokens,
      getClaimsFromUserInfoEndpoint: values.getClaimsFromUserInfoEndpoint,
      providerName: values.providerName?.trim() ? values.providerName : null,
      isActive: values.isActive,
    }
    try {
      if (editingId) {
        await oidcApi.update(editingId, payload)
        toast.success("OIDC config updated")
      } else {
        await oidcApi.create(payload)
        toast.success("OIDC config created")
      }
      setOpen(false)
      setEditingId(null)
      load()
    } catch (e) { toast.error(String(e)) }
  }

  async function remove(c: AdminOidcConfig) {
    if (!confirm(`Delete OIDC config "${c.providerName ?? c.clientId}"?`)) return
    try { await oidcApi.remove(c.id); toast.success("Deleted"); load() }
    catch (e) { toast.error(String(e)) }
  }

  function openNew() {
    setEditingId(null)
    form.reset(blank(conglomerates[0]?.id ?? ""))
    setOpen(true)
  }

  function openEdit(c: AdminOidcConfig) {
    setEditingId(c.id)
    form.reset({
      conglomerateId: c.conglomerateId,
      authority: c.authority,
      clientId: c.clientId,
      clientSecret: "",
      responseType: c.responseType,
      scopes: c.scopes,
      mapInboundClaims: c.mapInboundClaims,
      saveTokens: c.saveTokens,
      getClaimsFromUserInfoEndpoint: c.getClaimsFromUserInfoEndpoint,
      providerName: c.providerName ?? "",
      isActive: c.isActive,
    })
    setOpen(true)
  }

  return (
    <Card>
      <div className="flex items-center justify-between p-4">
        <p className="text-sm text-muted-foreground">{items.length} provider{items.length === 1 ? "" : "s"}</p>
        <Button size="sm" disabled={conglomerates.length === 0} onClick={openNew}>
          <Plus className="h-3.5 w-3.5" /> New provider
        </Button>
      </div>
      <CardContent className="pt-0 space-y-2">
        {loading ? (
          <>{Array.from({ length: 2 }).map((_, i) => <Skeleton key={i} className="h-20" />)}</>
        ) : items.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-10 text-muted-foreground">
            <KeyRound className="h-8 w-8 mb-2 opacity-40" />
            <p className="text-sm">No OIDC providers configured</p>
          </div>
        ) : items.map((c) => (
          <div key={c.id} className="rounded-md border p-3">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0 space-y-1">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-sm">{c.providerName ?? "OIDC Provider"}</span>
                  <Badge variant={c.isActive ? "default" : "secondary"} className="text-[10px]">{c.isActive ? "Active" : "Disabled"}</Badge>
                  {c.hasClientSecret && <Badge variant="outline" className="text-[10px]">secret set</Badge>}
                </div>
                <div className="text-xs text-muted-foreground font-mono truncate">{c.authority}</div>
                <div className="text-xs text-muted-foreground">client: <span className="font-mono">{c.clientId}</span> · scopes: {c.scopes}</div>
              </div>
              <div className="flex gap-1 shrink-0">
                <Button size="icon-sm" variant="ghost" onClick={() => openEdit(c)}>
                  <Pencil className="h-3.5 w-3.5" />
                </Button>
                <Button size="icon-sm" variant="ghost" onClick={() => remove(c)}>
                  <Trash2 className="h-3.5 w-3.5 text-destructive" />
                </Button>
              </div>
            </div>
          </div>
        ))}
      </CardContent>

      <Dialog open={open} onOpenChange={(v) => { if (!v) { setOpen(false); setEditingId(null) } }}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{editingId ? "Edit OIDC provider" : "New OIDC provider"}</DialogTitle>
            <DialogDescription>
              Client secrets are stored server-side and never returned. Leave blank to keep the current value.
            </DialogDescription>
          </DialogHeader>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
              <FormField
                control={form.control}
                name="conglomerateId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Conglomerate</FormLabel>
                    <Select value={field.value} onValueChange={(v) => field.onChange(v ?? "")}>
                      <FormControl>
                        <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {conglomerates.map((c) => <SelectItem key={c.id} value={c.id}>{c.name}</SelectItem>)}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="providerName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Provider name</FormLabel>
                    <FormControl><Input placeholder="e.g. Okta, Auth0" {...field} value={field.value ?? ""} /></FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="authority"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Authority</FormLabel>
                    <FormControl><Input placeholder="https://example.auth0.com/" {...field} /></FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <div className="grid grid-cols-2 gap-3">
                <FormField
                  control={form.control}
                  name="clientId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Client ID</FormLabel>
                      <FormControl><Input {...field} /></FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="clientSecret"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Client secret</FormLabel>
                      <FormControl>
                        <Input
                          type="password"
                          placeholder={editingId ? "leave blank to keep" : ""}
                          {...field}
                          value={field.value ?? ""}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <FormField
                  control={form.control}
                  name="responseType"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Response type</FormLabel>
                      <FormControl><Input {...field} /></FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="scopes"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Scopes</FormLabel>
                      <FormControl><Input {...field} /></FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
              <div className="grid grid-cols-2 gap-2 pt-1">
                <BoolField control={form.control} name="isActive" label="Active" />
                <BoolField control={form.control} name="saveTokens" label="Save tokens" />
                <BoolField control={form.control} name="mapInboundClaims" label="Map inbound claims" />
                <BoolField control={form.control} name="getClaimsFromUserInfoEndpoint" label="UserInfo claims" />
              </div>
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

function BoolField({
  control,
  name,
  label,
}: {
  control: ReturnType<typeof useForm<FormValues>>["control"]
  name: "isActive" | "saveTokens" | "mapInboundClaims" | "getClaimsFromUserInfoEndpoint"
  label: string
}) {
  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem className="flex flex-row items-center gap-2 space-y-0">
          <FormControl>
            <Checkbox checked={field.value} onCheckedChange={(c) => field.onChange(Boolean(c))} />
          </FormControl>
          <FormLabel className="mb-0">{label}</FormLabel>
        </FormItem>
      )}
    />
  )
}
