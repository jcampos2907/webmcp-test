import { useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import { Search, Shield, UserCog, UserPlus, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Skeleton } from "@/components/ui/skeleton"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { usersApi, type AdminUser, type AdminUserRole } from "@/lib/api"
import { useSession } from "@/lib/session"
import { cn } from "@/lib/utils"

const STORE_ROLES = ["None", "Cashier", "Mechanic", "Admin"] as const
type StoreRoleChoice = (typeof STORE_ROLES)[number]

function displayName(u: AdminUser) {
  return u.displayName || u.email || u.externalSubjectId.slice(0, 10)
}

export default function UsersSection() {
  const { currentStore, user: me } = useSession()
  const [users, setUsers] = useState<AdminUser[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState<string | null>(null)
  const [search, setSearch] = useState("")

  const conglomerateId = currentStore?.conglomerateId ?? null
  const conglomerateName = currentStore?.conglomerateName ?? "—"
  const storeId = currentStore?.id ?? null
  const storeName = currentStore?.name ?? "—"

  async function load() {
    setLoading(true)
    try { setUsers(await usersApi.list()) }
    finally { setLoading(false) }
  }
  useEffect(() => { load() }, [])

  const superAdmins = useMemo(() => users
    .map((u) => {
      const sa = u.assignments.find((a) => a.scope === "Conglomerate"
        && a.role === "SuperAdmin" && a.conglomerateId === conglomerateId)
      return sa ? { user: u, assignment: sa } : null
    })
    .filter((x): x is { user: AdminUser; assignment: AdminUserRole } => x !== null),
    [users, conglomerateId])

  const superAdminUserIds = useMemo(() => new Set(superAdmins.map((x) => x.user.id)), [superAdmins])

  const searchResults = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return []
    return users
      .filter((u) => !superAdminUserIds.has(u.id))
      .filter((u) => (u.displayName?.toLowerCase().includes(q) ?? false)
        || (u.email?.toLowerCase().includes(q) ?? false))
      .slice(0, 6)
  }, [search, users, superAdminUserIds])

  function currentStoreRole(u: AdminUser): AdminUserRole | null {
    return u.assignments.find((a) => a.scope === "Store" && a.storeId === storeId) ?? null
  }

  async function promoteToSuperAdmin(userId: string) {
    if (!conglomerateId) return
    setBusy(userId)
    try {
      await usersApi.upsertRole(userId, { scope: "Conglomerate", conglomerateId, role: "SuperAdmin" })
      toast.success("Granted SuperAdmin")
      setSearch("")
      await load()
    } catch (e) { toast.error(String(e)) }
    finally { setBusy(null) }
  }

  async function revokeSuperAdmin(a: AdminUserRole, userId: string) {
    if (userId === me?.id && !confirm("Remove your own SuperAdmin role? You will be locked out of this settings page.")) return
    setBusy(a.storeUserId)
    try {
      await usersApi.removeRole(a.storeUserId)
      toast.success("SuperAdmin revoked")
      await load()
    } catch (e) { toast.error(String(e)) }
    finally { setBusy(null) }
  }

  async function setStoreRole(u: AdminUser, next: StoreRoleChoice) {
    if (!storeId) return
    const existing = currentStoreRole(u)
    if ((existing?.role ?? "None") === next) return
    setBusy(u.id)
    try {
      if (next === "None") {
        if (existing) {
          await usersApi.removeRole(existing.storeUserId)
          toast.success(`Removed role at ${storeName}`)
        }
      } else {
        await usersApi.upsertRole(u.id, { scope: "Store", storeId, role: next })
        toast.success(`Set ${next} at ${storeName}`)
      }
      await load()
    } catch (e) { toast.error(String(e)) }
    finally { setBusy(null) }
  }

  if (!currentStore) {
    return (
      <Card>
        <CardContent className="pt-6">
          <p className="text-sm text-muted-foreground">Pick an active store to manage users.</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-6">
      {/* Staff matrix — scoped to active store */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <UserCog className="h-4 w-4" /> Staff at {storeName}
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {loading ? (
            <div className="p-6 space-y-2">
              {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-10" />)}
            </div>
          ) : users.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-10 text-muted-foreground">
              <UserCog className="h-8 w-8 mb-2 opacity-40" />
              <p className="text-sm">No users yet</p>
              <p className="text-xs mt-1">Users are provisioned on first OIDC sign-in.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="pl-6">User</TableHead>
                  {STORE_ROLES.map((r) => (
                    <TableHead key={r} className="text-center w-24 font-medium">{r}</TableHead>
                  ))}
                  <TableHead className="text-muted-foreground">Other assignments</TableHead>
                  <TableHead className="text-right pr-6">Last login</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.map((u) => {
                  const isSuper = superAdminUserIds.has(u.id)
                  const current: StoreRoleChoice = (currentStoreRole(u)?.role as StoreRoleChoice | undefined) ?? "None"
                  const other = u.assignments.filter((a) => {
                    if (a.scope === "Store" && a.storeId === storeId) return false
                    if (a.scope === "Conglomerate" && a.conglomerateId === conglomerateId && a.role === "SuperAdmin") return false
                    return true
                  })
                  return (
                    <TableRow key={u.id} className={cn(busy === u.id && "opacity-60")}>
                      <TableCell className="pl-6 font-medium">
                        <div className="flex items-center gap-2">
                          <span>{u.displayName ?? "—"}</span>
                          {u.email && <span className="text-xs text-muted-foreground">· {u.email}</span>}
                        </div>
                      </TableCell>
                      {isSuper ? (
                        <TableCell colSpan={STORE_ROLES.length} className="text-center">
                          <Badge variant="default" className="gap-1.5">
                            <Shield className="h-3 w-3" /> SuperAdmin
                          </Badge>
                        </TableCell>
                      ) : STORE_ROLES.map((r) => {
                        const selected = current === r
                        return (
                          <TableCell key={r} className="text-center p-0">
                            <button
                              type="button"
                              onClick={() => setStoreRole(u, r)}
                              disabled={busy === u.id}
                              aria-label={`Set ${r}`}
                              aria-pressed={selected}
                              className={cn(
                                "mx-auto my-2 flex h-6 w-6 items-center justify-center rounded-full border transition-colors",
                                selected
                                  ? "border-primary bg-primary text-primary-foreground"
                                  : "border-muted-foreground/30 hover:border-primary/60 hover:bg-muted",
                                busy === u.id && "cursor-not-allowed"
                              )}
                            >
                              {selected && <span className="h-2 w-2 rounded-full bg-primary-foreground" />}
                            </button>
                          </TableCell>
                        )
                      })}
                      <TableCell className="text-xs text-muted-foreground">
                        {other.length === 0 ? "—" : other.map((a) => (
                          <div key={a.storeUserId}>
                            {a.scope === "Store" && <span>{a.role} @ {a.storeName}</span>}
                            {a.scope === "Company" && <span>{a.role} @ {a.companyName} (company)</span>}
                            {a.scope === "Conglomerate" && <span>{a.role} @ {a.conglomerateName} (conglomerate)</span>}
                          </div>
                        ))}
                      </TableCell>
                      <TableCell className="text-right pr-6 text-xs text-muted-foreground">
                        {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString() : "never"}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* SuperAdmins — conglomerate-wide */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Shield className="h-4 w-4" /> SuperAdmins
          </CardTitle>
          <CardDescription>Conglomerate-wide access — {conglomerateName}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {loading ? (
            <Skeleton className="h-10" />
          ) : (
            <div className="rounded-md border divide-y">
              {superAdmins.length === 0 ? (
                <p className="px-4 py-3 text-sm text-muted-foreground italic">No SuperAdmins configured.</p>
              ) : superAdmins.map(({ user, assignment }) => (
                <div key={assignment.storeUserId} className="flex items-center justify-between px-4 py-2">
                  <div className="flex flex-col">
                    <span className="text-sm font-medium">{displayName(user)}</span>
                    {user.email && <span className="text-xs text-muted-foreground">{user.email}</span>}
                  </div>
                  <Button
                    size="sm" variant="ghost"
                    disabled={busy === assignment.storeUserId}
                    onClick={() => revokeSuperAdmin(assignment, user.id)}
                  >
                    <X className="h-3.5 w-3.5" /> Revoke
                  </Button>
                </div>
              ))}
            </div>
          )}

          {/* Grant via search */}
          <div className="relative">
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Grant SuperAdmin — search by name or email…"
                className="pl-8"
              />
            </div>
            {search.trim() && (
              <div className="absolute z-10 mt-1 w-full rounded-md border bg-popover shadow-md">
                {searchResults.length === 0 ? (
                  <p className="px-3 py-2 text-sm text-muted-foreground">No matching users.</p>
                ) : searchResults.map((u) => (
                  <button
                    key={u.id}
                    type="button"
                    disabled={busy === u.id}
                    onClick={() => promoteToSuperAdmin(u.id)}
                    className="flex w-full items-center justify-between px-3 py-2 text-left hover:bg-muted text-sm"
                  >
                    <div className="flex flex-col">
                      <span className="font-medium">{displayName(u)}</span>
                      {u.email && <span className="text-xs text-muted-foreground">{u.email}</span>}
                    </div>
                    <UserPlus className="h-4 w-4 text-muted-foreground" />
                  </button>
                ))}
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
