import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Pencil, Plus, Wrench } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { mechanicsApi, type Mechanic } from "@/lib/api"
import { useSession } from "@/lib/session"

export default function MechanicsPage() {
  const { can } = useSession()
  const canManage = can("mechanics.manage")
  const [items, setItems] = useState<Mechanic[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    mechanicsApi.list().then(setItems).finally(() => setLoading(false))
  }, [])

  const active = items.filter((m) => m.isActive).length

  return (
    <div className="p-6 lg:p-8 max-w-6xl mx-auto space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Mechanics</h1>
          <p className="text-sm text-muted-foreground">{items.length} total · {active} active</p>
        </div>
        {canManage && (
          <Button asChild>
            <Link to="/mechanics/new">
              <Plus className="h-4 w-4" /> New mechanic
            </Link>
          </Button>
        )}
      </div>

      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Phone</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Status</TableHead>
              {canManage && <TableHead className="w-16 text-right">Edit</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && Array.from({ length: 4 }).map((_, i) => (
              <TableRow key={i}><TableCell colSpan={canManage ? 5 : 4}><Skeleton className="h-5" /></TableCell></TableRow>
            ))}
            {!loading && items.length === 0 && (
              <TableRow>
                <TableCell colSpan={canManage ? 5 : 4}>
                  <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
                    <Wrench className="h-8 w-8 mb-2 opacity-40" />
                    <p className="text-sm">No mechanics</p>
                  </div>
                </TableCell>
              </TableRow>
            )}
            {!loading && items.map((m) => (
              <TableRow key={m.id}>
                <TableCell className="font-medium">{m.name}</TableCell>
                <TableCell className="text-muted-foreground">{m.phone ?? "—"}</TableCell>
                <TableCell className="text-muted-foreground">{m.email ?? "—"}</TableCell>
                <TableCell>
                  <Badge variant={m.isActive ? "default" : "secondary"}>
                    {m.isActive ? "Active" : "Inactive"}
                  </Badge>
                </TableCell>
                {canManage && (
                  <TableCell className="text-right">
                    <Button asChild size="icon-sm" variant="ghost">
                      <Link to={`/mechanics/${m.id}`} aria-label="Edit">
                        <Pencil className="h-3.5 w-3.5" />
                      </Link>
                    </Button>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Card>
    </div>
  )
}
