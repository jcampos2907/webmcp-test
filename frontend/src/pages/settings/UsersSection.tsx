import { useEffect, useMemo, useState } from "react"
import { toast } from "sonner"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Plus, UserCog, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
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
import {
  Form, FormControl, FormField, FormItem, FormLabel, FormMessage,
} from "@/components/ui/form"
import {
  usersApi, organizationApi,
  type AdminUser, type AdminStore, type AdminUserRole,
} from "@/lib/api"

const ROLES = ["Cashier", "Mechanic", "Admin", "SuperAdmin", "Developer"] as const

const schema = z.object({
  storeId: z.string().min(1, "Pick a store"),
  role: z.string().min(1),
})
type FormValues = z.infer<typeof schema>

export default function UsersSection() {
  const [users, setUsers] = useState<AdminUser[]>([])
  const [stores, setStores] = useState<AdminStore[]>([])
  const [loading, setLoading] = useState(true)
  const [activeUser, setActiveUser] = useState<AdminUser | null>(null)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { storeId: "", role: "Cashier" },
  })

  async function load() {
    setLoading(true)
    try {
      const [us, tree] = await Promise.all([usersApi.list(), organizationApi.tree()])
      setUsers(us)
      setStores(tree.flatMap((c) => c.companies.flatMap((co) => co.stores)))
    } finally { setLoading(false) }
  }
  useEffect(() => { load() }, [])

  const storeMap = useMemo(() => new Map(stores.map((s) => [s.id, s.name])), [stores])

  function openAssign(user: AdminUser) {
    setActiveUser(user)
    form.reset({ storeId: stores[0]?.id ?? "", role: "Cashier" })
  }

  async function onSubmit(values: FormValues) {
    if (!activeUser) return
    try {
      await usersApi.upsertRole(activeUser.id, values.storeId, values.role)
      toast.success("Role assigned")
      setActiveUser(null)
      load()
    } catch (e) { toast.error(String(e)) }
  }

  async function removeRole(r: AdminUserRole) {
    if (!confirm(`Remove ${r.role} role at ${r.storeName}?`)) return
    try { await usersApi.removeRole(r.storeUserId); toast.success("Role removed"); load() }
    catch (e) { toast.error(String(e)) }
  }

  return (
    <Card>
      <CardContent className="pt-6">
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
                <TableHead>Roles</TableHead>
                <TableHead>Last login</TableHead>
                <TableHead className="w-20 text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {users.map((u) => (
                <TableRow key={u.id}>
                  <TableCell className="font-medium">
                    <div>{u.displayName ?? "—"}</div>
                    <div className="text-[11px] text-muted-foreground font-mono truncate max-w-[200px]">{u.externalSubjectId}</div>
                  </TableCell>
                  <TableCell className="text-muted-foreground">{u.email ?? "—"}</TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1.5">
                      {u.assignments.length === 0 && <span className="text-xs text-muted-foreground italic">none</span>}
                      {u.assignments.map((a) => (
                        <Badge key={a.storeUserId} variant="secondary" className="gap-1 pr-1">
                          <span className="text-[10px]">{a.storeName || storeMap.get(a.storeId) || "?"}</span>
                          <span className="text-[10px] font-normal opacity-70">· {a.role}</span>
                          <button onClick={() => removeRole(a)} className="rounded hover:bg-muted ml-0.5 p-0.5">
                            <X className="h-3 w-3" />
                          </button>
                        </Badge>
                      ))}
                    </div>
                  </TableCell>
                  <TableCell className="text-muted-foreground text-xs">
                    {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString() : "never"}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button size="sm" variant="outline" onClick={() => openAssign(u)}>
                      <Plus className="h-3.5 w-3.5" /> Role
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      <Dialog open={!!activeUser} onOpenChange={(v) => !v && setActiveUser(null)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Assign role — {activeUser?.displayName ?? activeUser?.email}</DialogTitle></DialogHeader>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-3">
              <FormField
                control={form.control}
                name="storeId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Store</FormLabel>
                    <Select value={field.value} onValueChange={(v) => field.onChange(v ?? "")}>
                      <FormControl>
                        <SelectTrigger className="w-full"><SelectValue placeholder="Pick a store" /></SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {stores.map((s) => <SelectItem key={s.id} value={s.id}>{s.name}</SelectItem>)}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="role"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Role</FormLabel>
                    <Select value={field.value} onValueChange={(v) => field.onChange(v ?? "Cashier")}>
                      <FormControl>
                        <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {ROLES.map((r) => <SelectItem key={r} value={r}>{r}</SelectItem>)}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <p className="text-xs text-muted-foreground">Reassigning the same store updates the existing role.</p>
              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setActiveUser(null)}>Cancel</Button>
                <Button type="submit" disabled={form.formState.isSubmitting}>Save</Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </Card>
  )
}
