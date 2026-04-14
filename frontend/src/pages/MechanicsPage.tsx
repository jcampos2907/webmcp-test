import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Plus } from "lucide-react"
import { buttonVariants } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { PageHeader } from "@/components/PageHeader"
import { mechanicsApi, type Mechanic } from "@/lib/api"

export default function MechanicsPage() {
  const [items, setItems] = useState<Mechanic[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    mechanicsApi.list().then(setItems).finally(() => setLoading(false))
  }, [])

  return (
    <div className="p-8 max-w-6xl mx-auto">
      <PageHeader
        title="Mechanics"
        actions={
          <Link to="/mechanics/new" className={buttonVariants()}><Plus className="h-4 w-4" />New mechanic</Link>
        }
      />

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Phone</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="w-20"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && <TableRow><TableCell colSpan={5} className="text-center py-8 text-muted-foreground">Loading...</TableCell></TableRow>}
            {!loading && items.length === 0 && <TableRow><TableCell colSpan={5} className="text-center py-8 text-muted-foreground">No mechanics</TableCell></TableRow>}
            {!loading && items.map((m) => (
              <TableRow key={m.id}>
                <TableCell className="font-medium">{m.name}</TableCell>
                <TableCell>{m.phone ?? "—"}</TableCell>
                <TableCell>{m.email ?? "—"}</TableCell>
                <TableCell>
                  <Badge variant={m.isActive ? "default" : "secondary"}>
                    {m.isActive ? "Active" : "Inactive"}
                  </Badge>
                </TableCell>
                <TableCell>
                  <Link to={`/mechanics/${m.id}`} className={buttonVariants({ size: "sm", variant: "ghost" })}>Edit</Link>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
