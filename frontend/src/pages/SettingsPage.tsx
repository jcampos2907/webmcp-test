import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { toast } from "sonner"
import {
  Building2,
  CreditCard,
  KeyRound,
  Store,
  Tags,
  UserCog,
  type LucideIcon,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Skeleton } from "@/components/ui/skeleton"
import { cn } from "@/lib/utils"
import { settingsApi, type ShopSettings } from "@/lib/api"
import OrganizationSection from "./settings/OrganizationSection"
import TerminalsSection from "./settings/TerminalsSection"
import UsersSection from "./settings/UsersSection"
import OAuthSection from "./settings/OAuthSection"

type Section = {
  id: string
  label: string
  icon: LucideIcon
  description: string
}

const sections: Section[] = [
  { id: "shop", label: "Shop info", icon: Store, description: "Receipt header, tax ID, address" },
  { id: "meta", label: "Meta fields", icon: Tags, description: "Custom fields for customers & tickets" },
  { id: "org", label: "Organization", icon: Building2, description: "Companies and stores" },
  { id: "terminals", label: "Terminals", icon: CreditCard, description: "Payment terminal devices" },
  { id: "users", label: "Users", icon: UserCog, description: "Staff accounts and roles" },
  { id: "oauth", label: "OAuth", icon: KeyRound, description: "Identity provider configuration" },
]

export default function SettingsPage() {
  const [active, setActive] = useState("shop")
  const current = sections.find((s) => s.id === active)!

  return (
    <div className="p-6 lg:p-8 max-w-6xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Settings</h1>
        <p className="text-sm text-muted-foreground">Shop configuration and integrations</p>
      </div>

      <div className="grid gap-6 md:grid-cols-[240px_1fr]">
        <nav className="flex md:flex-col gap-1 overflow-x-auto md:overflow-visible">
          {sections.map((s) => {
            const isActive = s.id === active
            return (
              <button
                key={s.id}
                onClick={() => setActive(s.id)}
                className={cn(
                  "flex items-center gap-3 px-3 py-2 rounded-md text-sm text-left whitespace-nowrap transition-colors flex-shrink-0",
                  isActive ? "bg-muted font-medium" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                )}
              >
                <s.icon className="h-4 w-4" />
                <span>{s.label}</span>
              </button>
            )
          })}
        </nav>

        <div>
          <div className="mb-4">
            <h2 className="text-lg font-semibold flex items-center gap-2">
              <current.icon className="h-5 w-5" /> {current.label}
            </h2>
            <p className="text-sm text-muted-foreground">{current.description}</p>
          </div>

          {active === "shop" && <ShopInfoSection />}
          {active === "meta" && <MetaFieldsSection />}
          {active === "org" && <OrganizationSection />}
          {active === "terminals" && <TerminalsSection />}
          {active === "users" && <UsersSection />}
          {active === "oauth" && <OAuthSection />}
        </div>
      </div>
    </div>
  )
}

function ShopInfoSection() {
  const [form, setForm] = useState<ShopSettings>({ shopName: "", shopAddress: "", shopPhone: "", shopEmail: "", shopTaxId: "", receiptFooter: "" })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    settingsApi.getShop().then((s) => {
      setForm({
        shopName: s.shopName ?? "",
        shopAddress: s.shopAddress ?? "",
        shopPhone: s.shopPhone ?? "",
        shopEmail: s.shopEmail ?? "",
        shopTaxId: s.shopTaxId ?? "",
        receiptFooter: s.receiptFooter ?? "",
      })
    }).finally(() => setLoading(false))
  }, [])

  async function save() {
    setSaving(true)
    try {
      await settingsApi.saveShop(form)
      toast.success("Shop settings saved")
    } catch (err) { toast.error(String(err)) }
    finally { setSaving(false) }
  }

  if (loading) {
    return (
      <Card>
        <CardContent className="pt-6 space-y-3">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-9" />)}
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Shop details</CardTitle>
        <CardDescription>Appears on receipts and invoices</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid sm:grid-cols-2 gap-4">
          <Field label="Shop name">
            <Input value={form.shopName ?? ""} onChange={(e) => setForm({ ...form, shopName: e.target.value })} placeholder="My Bike Shop" />
          </Field>
          <Field label="Tax ID">
            <Input value={form.shopTaxId ?? ""} onChange={(e) => setForm({ ...form, shopTaxId: e.target.value })} placeholder="RFC / RUT / EIN" />
          </Field>
          <Field label="Phone">
            <Input value={form.shopPhone ?? ""} onChange={(e) => setForm({ ...form, shopPhone: e.target.value })} placeholder="+1 555-0000" />
          </Field>
          <Field label="Email">
            <Input type="email" value={form.shopEmail ?? ""} onChange={(e) => setForm({ ...form, shopEmail: e.target.value })} placeholder="hello@shop.com" />
          </Field>
          <Field label="Address" className="sm:col-span-2">
            <Input value={form.shopAddress ?? ""} onChange={(e) => setForm({ ...form, shopAddress: e.target.value })} placeholder="Street, City, State" />
          </Field>
          <Field label="Receipt footer" hint="Shown at the bottom of printed receipts" className="sm:col-span-2">
            <Textarea rows={3} value={form.receiptFooter ?? ""} onChange={(e) => setForm({ ...form, receiptFooter: e.target.value })} placeholder="Thank you for your business!" />
          </Field>
        </div>
        <div className="flex justify-end">
          <Button onClick={save} disabled={saving}>{saving ? "Saving..." : "Save changes"}</Button>
        </div>
      </CardContent>
    </Card>
  )
}

function MetaFieldsSection() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Meta field definitions</CardTitle>
        <CardDescription>
          Add custom fields to customers, tickets, and other entities without code changes.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Button asChild>
          <Link to="/settings/meta-fields">
            <Tags className="h-4 w-4" /> Open meta fields editor
          </Link>
        </Button>
      </CardContent>
    </Card>
  )
}

function Field({ label, hint, children, className }: { label: string; hint?: string; children: React.ReactNode; className?: string }) {
  return (
    <div className={cn("space-y-1.5", className)}>
      <Label>{label}</Label>
      {children}
      {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
    </div>
  )
}
