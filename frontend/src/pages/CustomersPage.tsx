import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Pencil, Plus, Search, Users } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { customersApi, type Customer } from "@/lib/api"

export default function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [search, setSearch] = useState("")
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const t = setTimeout(() => {
      setLoading(true)
      customersApi
        .list(search)
        .then(setCustomers)
        .finally(() => setLoading(false))
    }, 200)
    return () => clearTimeout(t)
  }, [search])

  return (
    <div className="p-6 lg:p-8 max-w-6xl mx-auto space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Customers</h1>
          <p className="text-sm text-muted-foreground">{customers.length} total</p>
        </div>
        <Button nativeButton={false} render={<Link to="/customers/new" />}>
          <Plus className="h-4 w-4" /> New customer
        </Button>
      </div>

      <div className="relative max-w-sm">
        <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Search by name, email, phone..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="pl-8"
        />
      </div>

      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Phone</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>City</TableHead>
              <TableHead className="w-16 text-right">Edit</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && Array.from({ length: 5 }).map((_, i) => (
              <TableRow key={i}>
                <TableCell colSpan={5}><Skeleton className="h-5" /></TableCell>
              </TableRow>
            ))}
            {!loading && customers.length === 0 && (
              <TableRow>
                <TableCell colSpan={5}>
                  <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
                    <Users className="h-8 w-8 mb-2 opacity-40" />
                    <p className="text-sm">No customers found</p>
                  </div>
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              customers.map((c) => (
                <TableRow key={c.id} className="group">
                  <TableCell className="font-medium">{c.fullName}</TableCell>
                  <TableCell className="text-muted-foreground">{c.phone ?? "—"}</TableCell>
                  <TableCell className="text-muted-foreground">{c.email ?? "—"}</TableCell>
                  <TableCell className="text-muted-foreground">{c.city ?? "—"}</TableCell>
                  <TableCell className="text-right">
                    <Button
                      size="icon-sm"
                      variant="ghost"
                      nativeButton={false}
                      render={<Link to={`/customers/${c.id}`} aria-label="Edit" />}
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
