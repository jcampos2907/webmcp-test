import { useEffect, useState } from "react"
import { toast } from "sonner"
import { Download, FileBarChart } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  reportsApi,
  type DailySalesReportRow, type ServiceRevenueRow, type MechanicProductivityRow,
} from "@/lib/api"

function firstOfMonth() {
  const d = new Date()
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10)
}
function today() { return new Date().toISOString().slice(0, 10) }

function downloadCsv(filename: string, csv: string) {
  const blob = new Blob([csv], { type: "text/csv" })
  const url = URL.createObjectURL(blob)
  const a = document.createElement("a")
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

export default function ReportsPage() {
  const [from, setFrom] = useState(firstOfMonth())
  const [to, setTo] = useState(today())
  const [tab, setTab] = useState("daily")
  const [loading, setLoading] = useState(false)

  const [daily, setDaily] = useState<DailySalesReportRow[]>([])
  const [services, setServices] = useState<ServiceRevenueRow[]>([])
  const [mechanics, setMechanics] = useState<MechanicProductivityRow[]>([])

  async function generate() {
    setLoading(true)
    try {
      if (tab === "daily") setDaily(await reportsApi.dailySales(from, to))
      else if (tab === "services") setServices(await reportsApi.serviceRevenue(from, to))
      else setMechanics(await reportsApi.mechanicProductivity(from, to))
    } catch (err) { toast.error(String(err)) }
    finally { setLoading(false) }
  }

  useEffect(() => {
    generate()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, from, to])

  function exportCsv() {
    if (tab === "daily") {
      const csv = "Date,Revenue,Transactions,Cash,Card,Transfer\n" +
        daily.map((r) => `${r.date.slice(0, 10)},${r.revenue},${r.transactions},${r.cash},${r.card},${r.transfer}`).join("\n")
      downloadCsv(`daily-sales-${from}-${to}.csv`, csv)
    } else if (tab === "services") {
      const csv = "Service,Revenue,Tickets\n" +
        services.map((r) => `"${r.serviceName}",${r.revenue},${r.ticketCount}`).join("\n")
      downloadCsv(`service-revenue-${from}-${to}.csv`, csv)
    } else {
      const csv = "Mechanic,Tickets,AvgHours\n" +
        mechanics.map((r) => `"${r.mechanicName}",${r.ticketCount},${r.avgHoursToComplete}`).join("\n")
      downloadCsv(`mechanic-productivity-${from}-${to}.csv`, csv)
    }
  }

  const dailyTotal = {
    revenue: daily.reduce((s, r) => s + r.revenue, 0),
    transactions: daily.reduce((s, r) => s + r.transactions, 0),
    cash: daily.reduce((s, r) => s + r.cash, 0),
    card: daily.reduce((s, r) => s + r.card, 0),
    transfer: daily.reduce((s, r) => s + r.transfer, 0),
  }
  const serviceTotal = services.reduce((s, r) => s + r.revenue, 0)

  const hasData = tab === "daily" ? daily.length > 0 : tab === "services" ? services.length > 0 : mechanics.length > 0

  return (
    <div className="p-6 lg:p-8 max-w-6xl mx-auto space-y-6">
      <div className="flex flex-col md:flex-row md:items-end md:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Reports</h1>
          <p className="text-sm text-muted-foreground">Revenue and productivity analytics</p>
        </div>
        <div className="flex flex-wrap items-end gap-3">
          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">From</Label>
            <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="w-auto" />
          </div>
          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground">To</Label>
            <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="w-auto" />
          </div>
          {hasData && (
            <Button variant="outline" onClick={exportCsv}><Download className="h-4 w-4" /> Export CSV</Button>
          )}
          {loading && <span className="text-xs text-muted-foreground pb-2">Loading...</span>}
        </div>
      </div>

      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="daily">Daily sales</TabsTrigger>
          <TabsTrigger value="services">By service</TabsTrigger>
          <TabsTrigger value="mechanics">Mechanic productivity</TabsTrigger>
        </TabsList>

        <TabsContent value="daily">
          <Card><CardContent className="pt-6">
            {daily.length === 0 ? <Empty /> : (
              <Table>
                <TableHeader><TableRow>
                  <TableHead>Date</TableHead>
                  <TableHead className="text-right">Revenue</TableHead>
                  <TableHead className="text-right">Txns</TableHead>
                  <TableHead className="text-right">Cash</TableHead>
                  <TableHead className="text-right">Card</TableHead>
                  <TableHead className="text-right">Transfer</TableHead>
                </TableRow></TableHeader>
                <TableBody>
                  {daily.map((r) => (
                    <TableRow key={r.date}>
                      <TableCell>{new Date(r.date).toLocaleDateString()}</TableCell>
                      <TableCell className="text-right font-medium tabular-nums">${r.revenue.toFixed(2)}</TableCell>
                      <TableCell className="text-right tabular-nums">{r.transactions}</TableCell>
                      <TableCell className="text-right tabular-nums">${r.cash.toFixed(2)}</TableCell>
                      <TableCell className="text-right tabular-nums">${r.card.toFixed(2)}</TableCell>
                      <TableCell className="text-right tabular-nums">${r.transfer.toFixed(2)}</TableCell>
                    </TableRow>
                  ))}
                  <TableRow className="font-bold bg-muted/50">
                    <TableCell>Total</TableCell>
                    <TableCell className="text-right tabular-nums">${dailyTotal.revenue.toFixed(2)}</TableCell>
                    <TableCell className="text-right tabular-nums">{dailyTotal.transactions}</TableCell>
                    <TableCell className="text-right tabular-nums">${dailyTotal.cash.toFixed(2)}</TableCell>
                    <TableCell className="text-right tabular-nums">${dailyTotal.card.toFixed(2)}</TableCell>
                    <TableCell className="text-right tabular-nums">${dailyTotal.transfer.toFixed(2)}</TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            )}
          </CardContent></Card>
        </TabsContent>

        <TabsContent value="services">
          <Card><CardContent className="pt-6">
            {services.length === 0 ? <Empty /> : (
              <Table>
                <TableHeader><TableRow>
                  <TableHead>Service</TableHead>
                  <TableHead className="text-right">Revenue</TableHead>
                  <TableHead className="text-right">Tickets</TableHead>
                  <TableHead className="text-right">%</TableHead>
                </TableRow></TableHeader>
                <TableBody>
                  {services.map((r) => (
                    <TableRow key={r.serviceName}>
                      <TableCell>{r.serviceName}</TableCell>
                      <TableCell className="text-right font-medium tabular-nums">${r.revenue.toFixed(2)}</TableCell>
                      <TableCell className="text-right tabular-nums">{r.ticketCount}</TableCell>
                      <TableCell className="text-right tabular-nums">{serviceTotal > 0 ? ((r.revenue / serviceTotal) * 100).toFixed(1) : "0.0"}%</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent></Card>
        </TabsContent>

        <TabsContent value="mechanics">
          <Card><CardContent className="pt-6">
            {mechanics.length === 0 ? <Empty /> : (
              <Table>
                <TableHeader><TableRow>
                  <TableHead>Mechanic</TableHead>
                  <TableHead className="text-right">Tickets charged</TableHead>
                  <TableHead className="text-right">Avg hours to complete</TableHead>
                </TableRow></TableHeader>
                <TableBody>
                  {mechanics.map((r) => (
                    <TableRow key={r.mechanicName}>
                      <TableCell>{r.mechanicName}</TableCell>
                      <TableCell className="text-right tabular-nums">{r.ticketCount}</TableCell>
                      <TableCell className="text-right tabular-nums">{r.avgHoursToComplete.toFixed(1)} h</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent></Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}

function Empty() {
  return (
    <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
      <FileBarChart className="h-8 w-8 mb-2 opacity-40" />
      <p className="text-sm">No data for this range</p>
    </div>
  )
}
