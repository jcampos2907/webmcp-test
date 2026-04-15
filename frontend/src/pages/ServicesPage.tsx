import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Cog, Pencil, Plus } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { servicesApi, type Service } from "@/lib/api"

export default function ServicesPage() {
  const [items, setItems] = useState<Service[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    servicesApi.list().then(setItems).finally(() => setLoading(false))
  }, [])

  return (
    <div className="p-6 lg:p-8 max-w-6xl mx-auto space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Services</h1>
          <p className="text-sm text-muted-foreground">{items.length} total</p>
        </div>
        <Button nativeButton={false} render={<Link to="/services/new" />}>
          <Plus className="h-4 w-4" /> New service
        </Button>
      </div>

      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Description</TableHead>
              <TableHead className="text-right">Price</TableHead>
              <TableHead className="text-right">Est. minutes</TableHead>
              <TableHead className="w-16 text-right">Edit</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && Array.from({ length: 4 }).map((_, i) => (
              <TableRow key={i}><TableCell colSpan={5}><Skeleton className="h-5" /></TableCell></TableRow>
            ))}
            {!loading && items.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>
                  <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
                    <Cog className="h-8 w-8 mb-2 opacity-40" />
                    <p className="text-sm">No services configured</p>
                  </div>
                </TableCell>
              </TableRow>
            )}
            {!loading && items.map((s) => (
              <TableRow key={s.id}>
                <TableCell className="font-medium">{s.name}</TableCell>
                <TableCell className="text-muted-foreground text-sm max-w-md truncate">{s.description ?? "—"}</TableCell>
                <TableCell className="text-right tabular-nums">${s.defaultPrice.toFixed(2)}</TableCell>
                <TableCell className="text-right tabular-nums text-muted-foreground">{s.estimatedMinutes ?? "—"}</TableCell>
                <TableCell className="text-right">
                  <Button
                    size="icon-sm"
                    variant="ghost"
                    nativeButton={false}
                    render={<Link to={`/services/${s.id}`} aria-label="Edit" />}
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Card>
    </div>
  )
}
