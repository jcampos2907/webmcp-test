import { useEffect, useState } from "react"
import { toast } from "sonner"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Building2, MapPin, Network, Pencil, Plus, Store, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Checkbox } from "@/components/ui/checkbox"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from "@/components/ui/dialog"
import { Form, FormField } from "@/components/ui/form"
import { Field, FieldGroup, FieldLabel, FieldError } from "@/components/ui/field"
import {
  organizationApi,
  type AdminCompany, type AdminConglomerate, type AdminStore,
  type UpsertCompany, type UpsertStore,
} from "@/lib/api"
import { CountryFlag, CountrySelect } from "@/components/ui/country-select"
import { useCountries } from "@/lib/countries"

const companySchema = z.object({
  conglomerateId: z.string().min(1),
  name: z.string().min(1, "Name is required"),
  locale: z.string().min(1),
  currency: z.string().min(1).max(10),
  taxId: z.string().nullable(),
  countryCode: z.string().nullable(),
})
type CompanyFormValues = z.infer<typeof companySchema>

const storeSchema = z.object({
  companyId: z.string().min(1),
  name: z.string().min(1, "Name is required"),
  address: z.string().nullable(),
  phone: z.string().nullable(),
  email: z.string().nullable(),
  isActive: z.boolean(),
})
type StoreFormValues = z.infer<typeof storeSchema>

export default function OrganizationSection() {
  const [tree, setTree] = useState<AdminConglomerate[]>([])
  const [loading, setLoading] = useState(true)
  const [conglomerateName, setConglomerateName] = useState<string | null>(null)

  const [companyOpen, setCompanyOpen] = useState(false)
  const [companyEditingId, setCompanyEditingId] = useState<string | null>(null)
  const [storeOpen, setStoreOpen] = useState(false)
  const [storeEditingId, setStoreEditingId] = useState<string | null>(null)

  const companyForm = useForm<CompanyFormValues>({
    resolver: zodResolver(companySchema),
    defaultValues: { conglomerateId: "", name: "", locale: "es-CR", currency: "CRC", taxId: "", countryCode: "CR" },
  })
  const storeForm = useForm<StoreFormValues>({
    resolver: zodResolver(storeSchema),
    defaultValues: { companyId: "", name: "", address: "", phone: "", email: "", isActive: true },
  })

  async function load() {
    setLoading(true)
    try { setTree(await organizationApi.tree()) }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [])

  const conglomerate = tree[0]
  const companies = conglomerate?.companies ?? []
  const totalStores = companies.reduce((s, c) => s + c.stores.length, 0)

  function openNewCompany() {
    if (!conglomerate) return
    setCompanyEditingId(null)
    companyForm.reset({ conglomerateId: conglomerate.id, name: "", locale: "es-CR", currency: "CRC", taxId: "", countryCode: "CR" })
    setCompanyOpen(true)
  }
  function openEditCompany(c: AdminCompany) {
    setCompanyEditingId(c.id)
    companyForm.reset({
      conglomerateId: c.conglomerateId,
      name: c.name,
      locale: c.locale,
      currency: c.currency,
      taxId: c.taxId ?? "",
      countryCode: c.countryCode ?? "",
    })
    setCompanyOpen(true)
  }
  async function saveCompany(values: CompanyFormValues) {
    const payload: UpsertCompany = {
      conglomerateId: values.conglomerateId,
      name: values.name,
      locale: values.locale,
      currency: values.currency,
      taxId: values.taxId?.trim() ? values.taxId : null,
      countryCode: values.countryCode?.trim() ? values.countryCode : null,
    }
    try {
      if (companyEditingId) await organizationApi.updateCompany(companyEditingId, payload)
      else await organizationApi.createCompany(payload)
      toast.success(companyEditingId ? "Company updated" : "Company created")
      setCompanyOpen(false); setCompanyEditingId(null); load()
    } catch (e) { toast.error(String(e)) }
  }

  async function deleteCompany(c: AdminCompany) {
    if (!confirm(`Delete company "${c.name}"?`)) return
    try { await organizationApi.deleteCompany(c.id); toast.success("Deleted"); load() }
    catch (e) { toast.error(String(e)) }
  }

  function openNewStore(companyId: string) {
    setStoreEditingId(null)
    storeForm.reset({ companyId, name: "", address: "", phone: "", email: "", isActive: true })
    setStoreOpen(true)
  }
  function openEditStore(s: AdminStore) {
    setStoreEditingId(s.id)
    storeForm.reset({
      companyId: s.companyId,
      name: s.name,
      address: s.address ?? "",
      phone: s.phone ?? "",
      email: s.email ?? "",
      isActive: s.isActive,
    })
    setStoreOpen(true)
  }
  async function saveStore(values: StoreFormValues) {
    const payload: UpsertStore = {
      companyId: values.companyId,
      name: values.name,
      address: values.address?.trim() ? values.address : null,
      phone: values.phone?.trim() ? values.phone : null,
      email: values.email?.trim() ? values.email : null,
      isActive: values.isActive,
    }
    try {
      if (storeEditingId) await organizationApi.updateStore(storeEditingId, payload)
      else await organizationApi.createStore(payload)
      toast.success(storeEditingId ? "Store updated" : "Store created")
      setStoreOpen(false); setStoreEditingId(null); load()
    } catch (e) { toast.error(String(e)) }
  }

  async function deleteStore(s: AdminStore) {
    if (!confirm(`Delete store "${s.name}"?`)) return
    try { await organizationApi.deleteStore(s.id); toast.success("Deleted"); load() }
    catch (e) { toast.error(String(e)) }
  }

  async function renameConglomerate() {
    if (!conglomerate || !conglomerateName?.trim()) { setConglomerateName(null); return }
    try {
      await organizationApi.renameConglomerate(conglomerate.id, conglomerateName)
      toast.success("Renamed"); setConglomerateName(null); load()
    } catch (e) { toast.error(String(e)) }
  }

  if (loading) {
    return <Card><CardContent className="pt-6 space-y-2">{Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-12" />)}</CardContent></Card>
  }

  if (!conglomerate) {
    return (
      <Card><CardContent className="pt-6">
        <p className="text-sm text-muted-foreground">No conglomerate found. Seed one in the Blazor app first.</p>
      </CardContent></Card>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-end">
        <Button size="sm" onClick={openNewCompany}>
          <Plus className="h-3.5 w-3.5" /> New company
        </Button>
      </div>

      <div className="rounded-xl border bg-gradient-to-b from-muted/30 to-transparent p-6 overflow-x-auto">
        <div className="flex justify-center">
          <div className="group relative rounded-lg bg-card border-2 border-primary/30 shadow-sm px-5 py-3 min-w-[280px]">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-md bg-primary text-primary-foreground">
                <Network className="h-5 w-5" />
              </div>
              <div className="flex-1">
                <div className="text-[10px] uppercase tracking-wide text-muted-foreground font-semibold">Conglomerate</div>
                {conglomerateName !== null ? (
                  <Input
                    autoFocus
                    value={conglomerateName}
                    onChange={(e) => setConglomerateName(e.target.value)}
                    onBlur={renameConglomerate}
                    onKeyDown={(e) => { if (e.key === "Enter") renameConglomerate(); if (e.key === "Escape") setConglomerateName(null) }}
                    className="h-7 text-sm font-semibold"
                  />
                ) : (
                  <button
                    className="font-semibold text-sm text-left hover:underline"
                    onClick={() => setConglomerateName(conglomerate.name)}
                  >
                    {conglomerate.name}
                  </button>
                )}
                <div className="text-[11px] text-muted-foreground">{companies.length} companies · {totalStores} stores</div>
              </div>
            </div>
          </div>
        </div>

        {companies.length === 0 ? (
          <div className="mt-8 flex flex-col items-center text-muted-foreground">
            <div className="h-8 w-px bg-border" />
            <div className="mt-3 rounded-md border-2 border-dashed px-6 py-4 text-sm">No companies yet</div>
          </div>
        ) : (
          <>
            <div className="mx-auto h-8 w-px bg-border" />
            <div className="relative">
              <div
                className="absolute left-0 right-0 top-0 h-px bg-border"
                style={{ marginLeft: `calc(100% / ${companies.length * 2})`, marginRight: `calc(100% / ${companies.length * 2})` }}
              />
              <div className="grid gap-6" style={{ gridTemplateColumns: `repeat(${companies.length}, minmax(260px, 1fr))` }}>
                {companies.map((co) => (
                  <CompanyBranch
                    key={co.id}
                    company={co}
                    onEditCompany={() => openEditCompany(co)}
                    onDeleteCompany={() => deleteCompany(co)}
                    onAddStore={() => openNewStore(co.id)}
                    onEditStore={(s) => openEditStore(s)}
                    onDeleteStore={(s) => deleteStore(s)}
                  />
                ))}
              </div>
            </div>
          </>
        )}
      </div>

      <Dialog open={companyOpen} onOpenChange={(v) => { if (!v) { setCompanyOpen(false); setCompanyEditingId(null) } }}>
        <DialogContent>
          <DialogHeader><DialogTitle>{companyEditingId ? "Edit company" : "New company"}</DialogTitle></DialogHeader>
          <Form {...companyForm}>
            <form onSubmit={companyForm.handleSubmit(saveCompany)} className="space-y-6">
              <FieldGroup>
                <FormField
                  control={companyForm.control}
                  name="name"
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid || undefined}>
                      <FieldLabel htmlFor="company-name">Name</FieldLabel>
                      <Input id="company-name" {...field} aria-invalid={fieldState.invalid} />
                      <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                    </Field>
                  )}
                />
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={companyForm.control}
                    name="currency"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="company-currency">Currency</FieldLabel>
                        <Input
                          id="company-currency"
                          maxLength={10}
                          {...field}
                          onChange={(e) => field.onChange(e.target.value.toUpperCase())}
                          aria-invalid={fieldState.invalid}
                        />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                  <FormField
                    control={companyForm.control}
                    name="locale"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="company-locale">Locale</FieldLabel>
                        <Input id="company-locale" placeholder="es-CR" {...field} aria-invalid={fieldState.invalid} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                  <FormField
                    control={companyForm.control}
                    name="taxId"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="company-taxId">Tax ID</FieldLabel>
                        <Input id="company-taxId" {...field} value={field.value ?? ""} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                  <FormField
                    control={companyForm.control}
                    name="countryCode"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel>Country</FieldLabel>
                        <CountrySelect value={field.value ?? ""} onChange={(code) => field.onChange(code)} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                </div>
              </FieldGroup>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => { setCompanyOpen(false); setCompanyEditingId(null) }}>Cancel</Button>
                <Button type="submit" disabled={companyForm.formState.isSubmitting}>Save</Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>

      <Dialog open={storeOpen} onOpenChange={(v) => { if (!v) { setStoreOpen(false); setStoreEditingId(null) } }}>
        <DialogContent>
          <DialogHeader><DialogTitle>{storeEditingId ? "Edit store" : "New store"}</DialogTitle></DialogHeader>
          <Form {...storeForm}>
            <form onSubmit={storeForm.handleSubmit(saveStore)} className="space-y-6">
              <FieldGroup>
                <FormField
                  control={storeForm.control}
                  name="name"
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid || undefined}>
                      <FieldLabel htmlFor="store-name">Name</FieldLabel>
                      <Input id="store-name" {...field} aria-invalid={fieldState.invalid} />
                      <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                    </Field>
                  )}
                />
                <FormField
                  control={storeForm.control}
                  name="address"
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid || undefined}>
                      <FieldLabel htmlFor="store-address">Address</FieldLabel>
                      <Input id="store-address" {...field} value={field.value ?? ""} />
                      <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                    </Field>
                  )}
                />
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={storeForm.control}
                    name="phone"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="store-phone">Phone</FieldLabel>
                        <Input id="store-phone" {...field} value={field.value ?? ""} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                  <FormField
                    control={storeForm.control}
                    name="email"
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid || undefined}>
                        <FieldLabel htmlFor="store-email">Email</FieldLabel>
                        <Input id="store-email" type="email" {...field} value={field.value ?? ""} />
                        <FieldError errors={fieldState.error ? [fieldState.error] : undefined} />
                      </Field>
                    )}
                  />
                </div>
                <FormField
                  control={storeForm.control}
                  name="isActive"
                  render={({ field }) => (
                    <Field orientation="horizontal">
                      <Checkbox id="store-isActive" checked={field.value} onCheckedChange={(c) => field.onChange(Boolean(c))} />
                      <FieldLabel htmlFor="store-isActive" className="font-normal">Active</FieldLabel>
                    </Field>
                  )}
                />
              </FieldGroup>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => { setStoreOpen(false); setStoreEditingId(null) }}>Cancel</Button>
                <Button type="submit" disabled={storeForm.formState.isSubmitting}>Save</Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </div>
  )
}

function CompanyBranch({
  company, onEditCompany, onDeleteCompany, onAddStore, onEditStore, onDeleteStore,
}: {
  company: AdminCompany
  onEditCompany: () => void
  onDeleteCompany: () => void
  onAddStore: () => void
  onEditStore: (s: AdminStore) => void
  onDeleteStore: (s: AdminStore) => void
}) {
  const { countries } = useCountries()
  const countryName = company.countryCode
    ? countries.find((c) => c.code === company.countryCode)?.name ?? company.countryCode
    : null
  return (
    <div className="flex flex-col items-center">
      <div className="h-6 w-px bg-border" />

      <div className="w-full rounded-lg border bg-card shadow-sm">
        <div className="flex items-start gap-3 p-3 border-b">
          <CountryFlag code={company.countryCode} size={40} />
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-1.5">
              <Building2 className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
              <span className="font-semibold text-sm truncate">{company.name}</span>
            </div>
            <div className="flex flex-wrap gap-1 mt-1">
              {countryName && (
                <Badge variant="outline" className="text-[10px]">{countryName}</Badge>
              )}
              <Badge variant="outline" className="text-[10px] font-mono">{company.currency}</Badge>
              <Badge variant="secondary" className="text-[10px]">{company.locale}</Badge>
              {company.taxId && <Badge variant="outline" className="text-[10px]">{company.taxId}</Badge>}
            </div>
          </div>
          <div className="flex gap-0.5 shrink-0">
            <Button size="icon-sm" variant="ghost" onClick={onEditCompany}>
              <Pencil className="h-3 w-3" />
            </Button>
            <Button size="icon-sm" variant="ghost" onClick={onDeleteCompany}>
              <Trash2 className="h-3 w-3 text-destructive" />
            </Button>
          </div>
        </div>

        <div className="p-2 space-y-1.5">
          {company.stores.length === 0 ? (
            <p className="text-xs text-muted-foreground text-center py-3 italic">No stores</p>
          ) : (
            company.stores.map((s) => (
              <div key={s.id} className="group rounded-md border px-2.5 py-1.5 hover:bg-muted/40 transition-colors">
                <div className="flex items-center gap-2">
                  <Store className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-1.5">
                      <span className="text-sm font-medium truncate">{s.name}</span>
                      {!s.isActive && <Badge variant="secondary" className="text-[9px]">Off</Badge>}
                    </div>
                    {s.address && (
                      <div className="flex items-center gap-1 text-[10px] text-muted-foreground truncate">
                        <MapPin className="h-2.5 w-2.5 shrink-0" />
                        <span className="truncate">{s.address}</span>
                      </div>
                    )}
                  </div>
                  <div className="flex gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity">
                    <Button size="icon-sm" variant="ghost" onClick={() => onEditStore(s)}>
                      <Pencil className="h-3 w-3" />
                    </Button>
                    <Button size="icon-sm" variant="ghost" onClick={() => onDeleteStore(s)}>
                      <Trash2 className="h-3 w-3 text-destructive" />
                    </Button>
                  </div>
                </div>
              </div>
            ))
          )}
          <Button size="sm" variant="outline" className="w-full h-7 text-xs" onClick={onAddStore}>
            <Plus className="h-3 w-3" /> Add store
          </Button>
        </div>
      </div>
    </div>
  )
}
