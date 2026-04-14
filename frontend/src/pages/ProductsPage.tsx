import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Plus } from "lucide-react"
import { buttonVariants } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { PageHeader } from "@/components/PageHeader"
import { productsApi, type Product } from "@/lib/api"

export default function ProductsPage() {
  const [items, setItems] = useState<Product[]>([])
  const [search, setSearch] = useState("")
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const t = setTimeout(() => {
      setLoading(true)
      productsApi.list(search).then(setItems).finally(() => setLoading(false))
    }, 200)
    return () => clearTimeout(t)
  }, [search])

  return (
    <div className="p-8 max-w-6xl mx-auto">
      <PageHeader
        title="Products"
        actions={<Link to="/products/new" className={buttonVariants()}><Plus className="h-4 w-4" />New product</Link>}
      />

      <Input placeholder="Search..." value={search} onChange={(e) => setSearch(e.target.value)} className="max-w-xs mb-4" />

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>SKU</TableHead>
              <TableHead>Category</TableHead>
              <TableHead className="text-right">Price</TableHead>
              <TableHead className="text-right">Stock</TableHead>
              <TableHead className="w-20"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && <TableRow><TableCell colSpan={6} className="text-center py-8 text-muted-foreground">Loading...</TableCell></TableRow>}
            {!loading && items.length === 0 && <TableRow><TableCell colSpan={6} className="text-center py-8 text-muted-foreground">No products</TableCell></TableRow>}
            {!loading && items.map((p) => (
              <TableRow key={p.id}>
                <TableCell className="font-medium">{p.name}</TableCell>
                <TableCell className="text-muted-foreground text-sm">{p.sku ?? "—"}</TableCell>
                <TableCell>{p.category ?? "—"}</TableCell>
                <TableCell className="text-right tabular-nums">${p.price.toFixed(2)}</TableCell>
                <TableCell className="text-right">
                  <Badge variant={p.quantityInStock === 0 ? "destructive" : p.quantityInStock < 5 ? "secondary" : "default"}>
                    {p.quantityInStock}
                  </Badge>
                </TableCell>
                <TableCell>
                  <Link to={`/products/${p.id}`} className={buttonVariants({ size: "sm", variant: "ghost" })}>Edit</Link>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
