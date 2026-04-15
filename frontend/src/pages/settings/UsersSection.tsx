import { useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import { Shield, UserCog, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { usersApi, type AdminUser, type AdminUserRole } from "@/lib/api"
import { useSession } from "@/lib/session"

const STORE_ROLES = ["None", "Cashier", "Mechanic", "Admin"] as const
type StoreRoleChoice = (typeof STORE_ROLES)[number]

export default function UsersSection() {
  const { currentStore, user: me } = useSession()
  const [users, setUsers] = useState<AdminUser[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState<string | null>(null)

  const conglomerateId = currentStore?.conglomerateId ?? null
  const conglomerateName = currentStore?.conglomerateName ?? "—"
  const storeId = currentStore?.id ?? null
  const storeName = currentStore?.name ?? "—"

  async function load() {
    setLoading(true)
    try {
      setUsers(await usersApi.list())
    } finally { setLoading(false) }
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
  const nonSuperAdmins = useMemo(() => users.filter((u) => !superAdminUserIds.has(u.id)), [users, superAdminUserIds])

  function currentStoreRole(u: AdminUser): AdminUserRole | null {
    return u.assignments.find((a) => a.scope === "Store" && a.storeId === storeId) ?? null
  }

  async function promoteToSuperAdmin(userId: string) {
    if (!conglomerateId) return
    setBusy(userId)
    try {
      await usersApi.upsertRole(userId, { scope: "Conglomerate", conglomerateId, role: "SuperAdmin" })
      toast.success("Granted SuperAdmin")
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
      {/* SuperAdmins — conglomerate-wide */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Shield className="h-4 w-4" /> SuperAdmins
          </CardTitle>
          <CardDescription>Conglomerate-wide access — {conglomerateName}</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <Skeleton className="h-10" />
          ) : superAdmins.length === 0 ? (
            <p className="text-sm text-muted-foreground italic">No SuperAdmins configured.</p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {superAdmins.map(({ user, assignment }) => (
                <Badge key={assignment.storeUserId} variant="secondary" className="gap-1.5 pr-1 py-1">
                  <span className="text-xs">{user.displayName || user.email || user.externalSubjectId.slice(0, 10)}</span>
                  {user.email && <span className="text-[10px] opacity-70">· {user.email}</span>}
                  <button
                    onClick={() => revokeSuperAdmin(assignment, user.id)}
                    disabled={busy === assignment.storeUserId}
                    className="rounded hover:bg-muted p-0.5 ml-0.5"
                    aria-label="Revoke SuperAdmin"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </Badge>
              ))}
            </div>
          )}
          {!loading && nonSuperAdmins.length > 0 && (
            <div className="mt-4 flex flex-wrap items-center gap-2">
              <span className="text-xs text-muted-foreground">Promote:</span>
              {nonSuperAdmins.map((u) => (
                <Button
                  key={u.id}
                  size="sm"
                  variant="outline"
                  disabled={busy === u.id}
                  onClick={() => promoteToSuperAdmin(u.id)}
                >
                  {u.displayName || u.email || u.externalSubjectId.slice(0, 10)}
                </Button>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Staff — scoped to active store */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <UserCog className="h-4 w-4" /> Staff at {storeName}
          </CardTitle>
          <CardDescription>
            Store roles apply only here. SuperAdmins override everything.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="space-y-2">{Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-12" />)}</div>
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
                  <TableHead>User</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Role at {storeName}</TableHead>
                  <TableHead className="text-muted-foreground">Other assignments</TableHead>
                  <TableHead className="text-right">Last login</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.map((u) => {
                  const isSuper = superAdminUserIds.has(u.id)
                  const current = currentStoreRole(u)?.role ?? "None"
                  const other = u.assignments.filter((a) => {
                    if (a.scope === "Store" && a.storeId === storeId) return false
                    if (a.scope === "Conglomerate" && a.conglomerateId === conglomerateId && a.role === "SuperAdmin") return false
                    return true
                  })
                  return (
                    <TableRow key={u.id}>
                      <TableCell className="font-medium">
                        <div>{u.displayName ?? "—"}</div>
                        <div className="text-[11px] text-muted-foreground font-mono truncate max-w-[180px]">{u.externalSubjectId}</div>
                      </TableCell>
                      <TableCell className="text-muted-foreground">{u.email ?? "—"}</TableCell>
                      <TableCell>
                        {isSuper ? (
                          <Badge variant="default">SuperAdmin (conglomerate)</Badge>
                        ) : (
                          <Select
                            value={current}
                            onValueChange={(v) => setStoreRole(u, v as StoreRoleChoice)}
                            disabled={busy === u.id}
                          >
                            <SelectTrigger className="w-40"><SelectValue /></SelectTrigger>
                            <SelectContent>
                              {STORE_ROLES.map((r) => <SelectItem key={r} value={r}>{r}</SelectItem>)}
                            </SelectContent>
                          </Select>
                        )}
                      </TableCell>
                      <TableCell className="text-xs text-muted-foreground">
                        {other.length === 0 ? "—" : other.map((a) => (
                          <div key={a.storeUserId}>
                            {a.scope === "Store" && <span>{a.role} @ {a.storeName}</span>}
                            {a.scope === "Company" && <span>{a.role} @ {a.companyName} (company)</span>}
                            {a.scope === "Conglomerate" && <span>{a.role} @ {a.conglomerateName} (conglomerate)</span>}
                          </div>
                        ))}
                      </TableCell>
                      <TableCell className="text-right text-xs text-muted-foreground">
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
    </div>
  )
}
