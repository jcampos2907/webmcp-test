import { useEffect, useState } from "react"
import { Link } from "react-router-dom"
import { Package, Pencil, Plus, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { productsApi, type Product } from "@/lib/api"
import { useSession } from "@/lib/session"

export default function ProductsPage() {
  const { can } = useSession()
  const canManage = can("products.manage")
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

  const lowStock = items.filter((p) => p.quantityInStock > 0 && p.quantityInStock < 5).length
  const outOfStock = items.filter((p) => p.quantityInStock === 0).length

  return (
    <div className="p-6 lg:p-8 max-w-6xl mx-auto space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Products</h1>
          <p className="text-sm text-muted-foreground">
            {items.length} total{lowStock ? ` · ${lowStock} low stock` : ""}{outOfStock ? ` · ${outOfStock} out` : ""}
          </p>
        </div>
        {canManage && (
          <Button asChild>
            <Link to="/products/new">
              <Plus className="h-4 w-4" /> New product
            </Link>
          </Button>
        )}
      </div>

      <div className="relative max-w-sm">
        <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input placeholder="Search by name or SKU..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-8" />
      </div>

      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>SKU</TableHead>
              <TableHead>Category</TableHead>
              <TableHead className="text-right">Price</TableHead>
              <TableHead className="text-right">Stock</TableHead>
              {canManage && <TableHead className="w-16 text-right">Edit</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && Array.from({ length: 5 }).map((_, i) => (
              <TableRow key={i}><TableCell colSpan={canManage ? 6 : 5}><Skeleton className="h-5" /></TableCell></TableRow>
            ))}
            {!loading && items.length === 0 && (
              <TableRow>
                <TableCell colSpan={canManage ? 6 : 5}>
                  <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
                    <Package className="h-8 w-8 mb-2 opacity-40" />
                    <p className="text-sm">No products</p>
                  </div>
                </TableCell>
              </TableRow>
            )}
            {!loading && items.map((p) => (
              <TableRow key={p.id}>
                <TableCell className="font-medium">{p.name}</TableCell>
                <TableCell className="text-muted-foreground font-mono text-xs">{p.sku ?? "—"}</TableCell>
                <TableCell className="text-muted-foreground">{p.category ?? "—"}</TableCell>
                <TableCell className="text-right tabular-nums">${p.price.toFixed(2)}</TableCell>
                <TableCell className="text-right">
                  <Badge variant={p.quantityInStock === 0 ? "destructive" : p.quantityInStock < 5 ? "secondary" : "outline"}>
                    {p.quantityInStock}
                  </Badge>
                </TableCell>
                {canManage && (
                  <TableCell className="text-right">
                    <Button asChild size="icon-sm" variant="ghost">
                      <Link to={`/products/${p.id}`} aria-label="Edit">
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
