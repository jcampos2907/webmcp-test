import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Plus } from "lucide-react"
import { buttonVariants } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { PageHeader } from "@/components/PageHeader"
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
    <div className="p-8 max-w-6xl mx-auto">
      <PageHeader
        title="Customers"
        actions={
          <Link to="/customers/new" className={buttonVariants()}>
            <Plus className="h-4 w-4" />
            New customer
          </Link>
        }
      />

      <Input
        placeholder="Search by name, email, phone..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="max-w-xs mb-4"
      />

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Phone</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>City</TableHead>
              <TableHead className="w-20"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={5} className="text-center text-muted-foreground py-8">
                  Loading...
                </TableCell>
              </TableRow>
            )}
            {!loading && customers.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="text-center text-muted-foreground py-8">
                  No customers
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              customers.map((c) => (
                <TableRow key={c.id}>
                  <TableCell className="font-medium">{c.fullName}</TableCell>
                  <TableCell>{c.phone ?? "—"}</TableCell>
                  <TableCell>{c.email ?? "—"}</TableCell>
                  <TableCell>{c.city ?? "—"}</TableCell>
                  <TableCell>
                    <Link to={`/customers/${c.id}`} className={buttonVariants({ size: "sm", variant: "ghost" })}>Edit</Link>
                  </TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
