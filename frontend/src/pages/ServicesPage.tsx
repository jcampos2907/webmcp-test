import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Plus } from "lucide-react"
import { buttonVariants } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { PageHeader } from "@/components/PageHeader"
import { servicesApi, type Service } from "@/lib/api"

export default function ServicesPage() {
  const [items, setItems] = useState<Service[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    servicesApi.list().then(setItems).finally(() => setLoading(false))
  }, [])

  return (
    <div className="p-8 max-w-6xl mx-auto">
      <PageHeader
        title="Services"
        actions={<Link to="/services/new" className={buttonVariants()}><Plus className="h-4 w-4" />New service</Link>}
      />

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Description</TableHead>
              <TableHead className="text-right">Price</TableHead>
              <TableHead className="text-right">Est. minutes</TableHead>
              <TableHead className="w-20"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && <TableRow><TableCell colSpan={5} className="text-center py-8 text-muted-foreground">Loading...</TableCell></TableRow>}
            {!loading && items.length === 0 && <TableRow><TableCell colSpan={5} className="text-center py-8 text-muted-foreground">No services</TableCell></TableRow>}
            {!loading && items.map((s) => (
              <TableRow key={s.id}>
                <TableCell className="font-medium">{s.name}</TableCell>
                <TableCell className="text-muted-foreground text-sm">{s.description ?? "—"}</TableCell>
                <TableCell className="text-right tabular-nums">${s.defaultPrice.toFixed(2)}</TableCell>
                <TableCell className="text-right tabular-nums">{s.estimatedMinutes ?? "—"}</TableCell>
                <TableCell>
                  <Link to={`/services/${s.id}`} className={buttonVariants({ size: "sm", variant: "ghost" })}>Edit</Link>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
