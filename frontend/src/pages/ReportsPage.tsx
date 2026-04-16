import { useEffect, useMemo, useRef, useState } from "react"
import { toast } from "sonner"
import {
  Area, AreaChart, Bar, BarChart, CartesianGrid, Line, LineChart, XAxis, YAxis,
} from "recharts"
import {
  Bot, Check, Download, FileBarChart, Loader2, Receipt, Send, Sparkles, TrendingUp, Users, Wrench, X,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Field, FieldLabel } from "@/components/ui/field"
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet"
import { cn } from "@/lib/utils"
import {
  ChartContainer, ChartLegend, ChartLegendContent, ChartTooltip, ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart"
import {
  reportsApi, streamReportSummary, streamReportsChat,
  type ChatMessage, type DailySalesReportRow, type ServiceRevenueRow, type MechanicProductivityRow,
  type ReportsUiAction, type ChatChart, type ChatDownload,
} from "@/lib/api"

type ChatAttachment =
  | ({ type: "chart" } & ChatChart)
  | ({ type: "download" } & ChatDownload)

type ChatTurn = { role: "user" | "assistant"; content: string; attachments?: ChatAttachment[] }

type SummaryToolCall = { id: string; name: string; status: "running" | "done"; result?: string }

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

const dailyChartConfig = {
  cash: { label: "Cash", color: "var(--chart-1)" },
  card: { label: "Card", color: "var(--chart-2)" },
  transfer: { label: "Transfer", color: "var(--chart-3)" },
} satisfies ChartConfig

const servicesChartConfig = {
  revenue: { label: "Revenue", color: "var(--chart-1)" },
} satisfies ChartConfig

const mechanicsChartConfig = {
  ticketCount: { label: "Tickets", color: "var(--chart-2)" },
} satisfies ChartConfig

export default function ReportsPage() {
  const [from, setFrom] = useState(firstOfMonth())
  const [to, setTo] = useState(today())
  const [tab, setTab] = useState("daily")
  const [loading, setLoading] = useState(false)

  const [daily, setDaily] = useState<DailySalesReportRow[]>([])
  const [services, setServices] = useState<ServiceRevenueRow[]>([])
  const [mechanics, setMechanics] = useState<MechanicProductivityRow[]>([])

  const [summaryText, setSummaryText] = useState("")
  const [summaryTools, setSummaryTools] = useState<SummaryToolCall[]>([])
  const [summarizing, setSummarizing] = useState(false)
  const [summaryOpen, setSummaryOpen] = useState(false)

  const [chatOpen, setChatOpen] = useState(false)
  const [chatMessages, setChatMessages] = useState<ChatTurn[]>([])
  const [chatInput, setChatInput] = useState("")
  const [chatStreaming, setChatStreaming] = useState(false)
  const chatScrollRef = useRef<HTMLDivElement | null>(null)

  function applyUiAction(a: ReportsUiAction) {
    if (a.action === "set_date_range") { setFrom(a.from); setTo(a.to) }
    else if (a.action === "select_tab") { setTab(a.tab) }
  }

  function appendToLastAssistant(update: (t: ChatTurn) => ChatTurn) {
    setChatMessages((m) => {
      const copy = [...m]
      copy[copy.length - 1] = update(copy[copy.length - 1])
      return copy
    })
  }

  async function sendChat() {
    const text = chatInput.trim()
    if (!text || chatStreaming) return
    const withUser: ChatTurn[] = [...chatMessages, { role: "user", content: text }]
    const next: ChatTurn[] = [...withUser, { role: "assistant", content: "" }]
    setChatMessages(next)
    setChatInput("")
    setChatStreaming(true)
    try {
      const wire: ChatMessage[] = withUser.map((t) => ({ role: t.role, content: t.content }))
      for await (const ev of streamReportsChat(wire)) {
        if (ev.type === "text_delta") {
          appendToLastAssistant((t) => ({ ...t, content: t.content + ev.data.text }))
        } else if (ev.type === "ui_action") {
          applyUiAction(ev.data)
        } else if (ev.type === "chart") {
          appendToLastAssistant((t) => ({
            ...t,
            attachments: [...(t.attachments ?? []), { type: "chart", ...ev.data }],
          }))
        } else if (ev.type === "download") {
          appendToLastAssistant((t) => ({
            ...t,
            attachments: [...(t.attachments ?? []), { type: "download", ...ev.data }],
          }))
        }
      }
    } catch (err) { toast.error(String(err)) }
    finally { setChatStreaming(false) }
  }

  useEffect(() => {
    const el = chatScrollRef.current
    if (el) el.scrollTop = el.scrollHeight
  }, [chatMessages])

  async function summarize() {
    setSummaryOpen(true)
    setSummaryText("")
    setSummaryTools([])
    setSummarizing(true)
    try {
      for await (const ev of streamReportSummary(tab as "daily" | "services" | "mechanics", from, to)) {
        if (ev.type === "text_delta") setSummaryText((t) => t + ev.data.text)
        else if (ev.type === "tool_call_start")
          setSummaryTools((tc) => [...tc, { id: ev.data.id, name: ev.data.name, status: "running" }])
        else if (ev.type === "tool_call_end")
          setSummaryTools((tc) => tc.map((x) =>
            x.id === ev.data.id ? { ...x, status: "done", result: ev.data.result } : x))
      }
    } catch (err) { toast.error(String(err)) }
    finally { setSummarizing(false) }
  }

  async function loadAll() {
    setLoading(true)
    try {
      const [d, s, m] = await Promise.all([
        reportsApi.dailySales(from, to),
        reportsApi.serviceRevenue(from, to),
        reportsApi.mechanicProductivity(from, to),
      ])
      setDaily(d); setServices(s); setMechanics(m)
    } catch (err) { toast.error(String(err)) }
    finally { setLoading(false) }
  }

  useEffect(() => { loadAll() // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [from, to])

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

  const kpis = useMemo(() => {
    const revenue = daily.reduce((s, r) => s + r.revenue, 0)
    const transactions = daily.reduce((s, r) => s + r.transactions, 0)
    const avgTicket = transactions > 0 ? revenue / transactions : 0
    const topService = [...services].sort((a, b) => b.revenue - a.revenue)[0]
    return { revenue, transactions, avgTicket, topService }
  }, [daily, services])

  const dailyChartData = useMemo(
    () => daily.map((r) => ({
      date: r.date.slice(0, 10),
      cash: r.cash,
      card: r.card,
      transfer: r.transfer,
    })),
    [daily],
  )

  const servicesChartData = useMemo(
    () => [...services].sort((a, b) => b.revenue - a.revenue).slice(0, 8)
      .map((r) => ({ name: r.serviceName, revenue: r.revenue })),
    [services],
  )

  const mechanicsChartData = useMemo(
    () => [...mechanics].sort((a, b) => b.ticketCount - a.ticketCount).slice(0, 8)
      .map((r) => ({ name: r.mechanicName, ticketCount: r.ticketCount })),
    [mechanics],
  )

  const serviceTotal = services.reduce((s, r) => s + r.revenue, 0)
  const dailyTotal = kpis
  const hasAnyData = daily.length > 0 || services.length > 0 || mechanics.length > 0

  return (
    <div className="p-6 lg:p-8 max-w-7xl mx-auto space-y-6">
      <div className="flex flex-col md:flex-row md:items-end md:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Reports</h1>
          <p className="text-sm text-muted-foreground">Revenue and productivity analytics</p>
        </div>
        <div className="flex flex-wrap items-end gap-3">
          <Field className="w-auto">
            <FieldLabel htmlFor="from">From</FieldLabel>
            <Input id="from" type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="w-auto" />
          </Field>
          <Field className="w-auto">
            <FieldLabel htmlFor="to">To</FieldLabel>
            <Input id="to" type="date" value={to} onChange={(e) => setTo(e.target.value)} className="w-auto" />
          </Field>
          <Button variant="outline" onClick={() => setChatOpen(true)} className="cadence-suggest-btn">
            <Bot className="cadence-icon h-4 w-4" /> Ask Cadence
          </Button>
          {hasAnyData && (
            <Button variant="outline" onClick={summarize} disabled={summarizing} className="cadence-suggest-btn">
              {summarizing
                ? <Loader2 className="h-4 w-4 animate-spin" />
                : <Sparkles className="cadence-icon h-4 w-4" />}
              Summarize
            </Button>
          )}
          {hasAnyData && (
            <Button variant="outline" onClick={exportCsv}><Download className="h-4 w-4" /> Export CSV</Button>
          )}
          {loading && <span className="text-xs text-muted-foreground pb-2">Loading…</span>}
        </div>
      </div>

      {summaryOpen && (
        <Card className="cadence-summary-card">
          <CardContent className="pt-5 pb-4 space-y-3">
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-center gap-2">
                <div className="cadence-summary-badge flex h-6 w-6 items-center justify-center rounded-full">
                  <Sparkles className="h-3.5 w-3.5" />
                </div>
                <span className="cadence-summary-title text-sm">Cadence</span>
              </div>
              <Button variant="ghost" size="icon-sm" onClick={() => setSummaryOpen(false)} aria-label="Dismiss">
                <X className="h-4 w-4" />
              </Button>
            </div>
            {summaryTools.length > 0 && (
              <div className="space-y-1">
                {summaryTools.map((tc) => (
                  <div key={tc.id} className="flex items-center gap-1.5 rounded-md border bg-background/60 px-2 py-1 text-[11px] font-mono">
                    {tc.status === "running"
                      ? <Loader2 className="h-3 w-3 animate-spin text-muted-foreground" />
                      : <Check className="h-3 w-3 text-emerald-600" />}
                    <Wrench className="h-3 w-3 text-muted-foreground" />
                    <span className="font-semibold">{tc.name}</span>
                    {tc.result && <span className="text-muted-foreground truncate">— {tc.result}</span>}
                  </div>
                ))}
              </div>
            )}
            <p className="text-sm whitespace-pre-wrap">
              {summaryText}
              {summarizing && (
                <span className="ml-0.5 inline-block h-3 w-1.5 translate-y-[2px] animate-pulse bg-current" />
              )}
            </p>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiTile
          icon={<TrendingUp className="h-4 w-4" />}
          label="Total revenue"
          value={`$${kpis.revenue.toFixed(2)}`}
          hint={`${daily.length} day${daily.length === 1 ? "" : "s"} in range`}
        />
        <KpiTile
          icon={<Receipt className="h-4 w-4" />}
          label="Transactions"
          value={kpis.transactions.toLocaleString()}
          hint={`Avg $${kpis.avgTicket.toFixed(2)} per txn`}
        />
        <KpiTile
          icon={<Wrench className="h-4 w-4" />}
          label="Top service"
          value={kpis.topService?.serviceName ?? "—"}
          hint={kpis.topService ? `$${kpis.topService.revenue.toFixed(2)} · ${kpis.topService.ticketCount} tickets` : "No services"}
        />
        <KpiTile
          icon={<Users className="h-4 w-4" />}
          label="Mechanics active"
          value={mechanics.length.toString()}
          hint={mechanics.length > 0
            ? `${mechanics.reduce((s, r) => s + r.ticketCount, 0)} tickets completed`
            : "No activity"}
        />
      </div>

      <Card className="pt-0">
        <CardHeader className="flex flex-col items-stretch space-y-0 border-b p-0 sm:flex-row">
          <div className="flex flex-1 flex-col justify-center gap-1 px-6 py-5">
            <CardTitle>Daily revenue</CardTitle>
            <CardDescription>Revenue by payment method over the selected range</CardDescription>
          </div>
        </CardHeader>
        <CardContent className="px-2 pt-4 sm:px-6 sm:pt-6">
          {daily.length === 0 ? <Empty /> : (
            <ChartContainer config={dailyChartConfig} className="aspect-auto h-[280px] w-full">
              <AreaChart data={dailyChartData}>
                <defs>
                  {(["cash", "card", "transfer"] as const).map((k) => (
                    <linearGradient key={k} id={`fill-${k}`} x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor={`var(--color-${k})`} stopOpacity={0.8} />
                      <stop offset="95%" stopColor={`var(--color-${k})`} stopOpacity={0.1} />
                    </linearGradient>
                  ))}
                </defs>
                <CartesianGrid vertical={false} />
                <XAxis
                  dataKey="date"
                  tickLine={false}
                  axisLine={false}
                  tickMargin={8}
                  minTickGap={32}
                  tickFormatter={(v) =>
                    new Date(v).toLocaleDateString("en-US", { month: "short", day: "numeric" })
                  }
                />
                <YAxis tickLine={false} axisLine={false} tickMargin={8} width={50}
                  tickFormatter={(v) => `$${v}`} />
                <ChartTooltip
                  cursor={false}
                  content={
                    <ChartTooltipContent
                      indicator="dot"
                      labelFormatter={(v) =>
                        new Date(v).toLocaleDateString("en-US", {
                          month: "short", day: "numeric", year: "numeric",
                        })
                      }
                    />
                  }
                />
                <Area dataKey="cash" type="natural" fill="url(#fill-cash)" stroke="var(--color-cash)" stackId="a" />
                <Area dataKey="card" type="natural" fill="url(#fill-card)" stroke="var(--color-card)" stackId="a" />
                <Area dataKey="transfer" type="natural" fill="url(#fill-transfer)" stroke="var(--color-transfer)" stackId="a" />
                <ChartLegend content={<ChartLegendContent />} />
              </AreaChart>
            </ChartContainer>
          )}
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Top services by revenue</CardTitle>
            <CardDescription>Top 8 in the selected range</CardDescription>
          </CardHeader>
          <CardContent>
            {services.length === 0 ? <Empty /> : (
              <ChartContainer config={servicesChartConfig} className="aspect-auto h-[280px] w-full">
                <BarChart data={servicesChartData} layout="vertical" margin={{ left: 12, right: 12 }}>
                  <CartesianGrid horizontal={false} />
                  <XAxis type="number" hide />
                  <YAxis
                    dataKey="name" type="category" tickLine={false} axisLine={false}
                    tickMargin={8} width={120}
                    tickFormatter={(v: string) => v.length > 16 ? v.slice(0, 15) + "…" : v}
                  />
                  <ChartTooltip
                    cursor={false}
                    content={<ChartTooltipContent />}
                  />
                  <Bar dataKey="revenue" fill="var(--color-revenue)" radius={4} />
                </BarChart>
              </ChartContainer>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Mechanic throughput</CardTitle>
            <CardDescription>Tickets charged per mechanic</CardDescription>
          </CardHeader>
          <CardContent>
            {mechanics.length === 0 ? <Empty /> : (
              <ChartContainer config={mechanicsChartConfig} className="aspect-auto h-[280px] w-full">
                <BarChart data={mechanicsChartData} layout="vertical" margin={{ left: 12, right: 12 }}>
                  <CartesianGrid horizontal={false} />
                  <XAxis type="number" hide />
                  <YAxis
                    dataKey="name" type="category" tickLine={false} axisLine={false}
                    tickMargin={8} width={120}
                    tickFormatter={(v: string) => v.length > 16 ? v.slice(0, 15) + "…" : v}
                  />
                  <ChartTooltip cursor={false} content={<ChartTooltipContent />} />
                  <Bar dataKey="ticketCount" fill="var(--color-ticketCount)" radius={4} />
                </BarChart>
              </ChartContainer>
            )}
          </CardContent>
        </Card>
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
                    <TableCell className="text-right tabular-nums">${daily.reduce((s, r) => s + r.cash, 0).toFixed(2)}</TableCell>
                    <TableCell className="text-right tabular-nums">${daily.reduce((s, r) => s + r.card, 0).toFixed(2)}</TableCell>
                    <TableCell className="text-right tabular-nums">${daily.reduce((s, r) => s + r.transfer, 0).toFixed(2)}</TableCell>
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

      <Sheet open={chatOpen} onOpenChange={setChatOpen}>
        <SheetContent className="flex flex-col gap-0 p-0 sm:max-w-md">
          <SheetHeader className="border-b p-4">
            <SheetTitle className="flex items-center gap-2">
              <Bot className="h-4 w-4" /> Ask Cadence
            </SheetTitle>
            <p className="text-xs text-muted-foreground">
              Try: "last 7 days", "this month", "top services", "mechanic productivity"
            </p>
          </SheetHeader>

          <div ref={chatScrollRef} className="flex-1 overflow-y-auto p-4 space-y-3">
            {chatMessages.length === 0 && (
              <div className="flex flex-col items-center justify-center h-full text-muted-foreground text-center gap-2 py-8">
                <Sparkles className="h-6 w-6 opacity-40" />
                <p className="text-sm">Ask me to reshape the dashboard.</p>
              </div>
            )}
            {chatMessages.map((m, i) => (
              <div key={i} className={cn("flex flex-col gap-2", m.role === "user" ? "items-end" : "items-start")}>
                {(m.content || m.role === "user") && (
                  <div
                    className={cn(
                      "rounded-lg px-3 py-2 text-sm max-w-[85%]",
                      m.role === "user"
                        ? "bg-primary text-primary-foreground"
                        : "bg-muted",
                    )}
                  >
                    {m.content || (chatStreaming && (
                      <Loader2 className="h-3.5 w-3.5 animate-spin" />
                    ))}
                  </div>
                )}
                {m.attachments?.map((att, ai) =>
                  att.type === "chart"
                    ? <ChatChartCard key={ai} chart={att} />
                    : <ChatDownloadChip key={ai} file={att} />
                )}
              </div>
            ))}
          </div>

          <form
            onSubmit={(e) => { e.preventDefault(); sendChat() }}
            className="flex items-center gap-2 border-t p-3"
          >
            <Input
              value={chatInput}
              onChange={(e) => setChatInput(e.target.value)}
              placeholder="Ask about the dashboard…"
              disabled={chatStreaming}
            />
            <Button type="submit" size="icon" disabled={chatStreaming || !chatInput.trim()}>
              {chatStreaming ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
            </Button>
          </form>
        </SheetContent>
      </Sheet>
    </div>
  )
}

function KpiTile({
  icon, label, value, hint,
}: { icon: React.ReactNode; label: string; value: string; hint?: string }) {
  return (
    <Card>
      <CardContent className="p-4 space-y-1">
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          {icon}
          <span>{label}</span>
        </div>
        <div className="text-2xl font-semibold tracking-tight tabular-nums truncate">{value}</div>
        {hint && <div className="text-xs text-muted-foreground truncate">{hint}</div>}
      </CardContent>
    </Card>
  )
}

function ChatChartCard({ chart }: { chart: ChatChart }) {
  const config: ChartConfig = Object.fromEntries(
    chart.series.map((s) => [s.key, { label: s.label, color: s.color ?? "var(--chart-1)" }]),
  )
  return (
    <div className="w-full max-w-[85%] rounded-lg border bg-background p-3">
      {chart.title && <div className="text-xs font-medium mb-2">{chart.title}</div>}
      <ChartContainer config={config} className="aspect-auto h-[140px] w-full">
        {chart.kind === "bar" ? (
          <BarChart data={chart.data} margin={{ left: 4, right: 4, top: 4, bottom: 0 }}>
            <CartesianGrid vertical={false} />
            <XAxis dataKey={chart.xKey} tickLine={false} axisLine={false} tickMargin={4}
              fontSize={10} interval={0} />
            <ChartTooltip cursor={false} content={<ChartTooltipContent />} />
            {chart.series.map((s) => (
              <Bar key={s.key} dataKey={s.key} fill={`var(--color-${s.key})`} radius={3} />
            ))}
          </BarChart>
        ) : chart.kind === "area" ? (
          <AreaChart data={chart.data} margin={{ left: 4, right: 4, top: 4, bottom: 0 }}>
            <CartesianGrid vertical={false} />
            <XAxis dataKey={chart.xKey} tickLine={false} axisLine={false} tickMargin={4} fontSize={10} />
            <ChartTooltip cursor={false} content={<ChartTooltipContent />} />
            {chart.series.map((s) => (
              <Area key={s.key} dataKey={s.key} type="natural"
                stroke={`var(--color-${s.key})`} fill={`var(--color-${s.key})`} fillOpacity={0.3} />
            ))}
          </AreaChart>
        ) : (
          <LineChart data={chart.data} margin={{ left: 4, right: 4, top: 4, bottom: 0 }}>
            <CartesianGrid vertical={false} />
            <XAxis dataKey={chart.xKey} tickLine={false} axisLine={false} tickMargin={4} fontSize={10} />
            <ChartTooltip cursor={false} content={<ChartTooltipContent />} />
            {chart.series.map((s) => (
              <Line key={s.key} dataKey={s.key} type="natural"
                stroke={`var(--color-${s.key})`} strokeWidth={2} dot={false} />
            ))}
          </LineChart>
        )}
      </ChartContainer>
    </div>
  )
}

function ChatDownloadChip({ file }: { file: ChatDownload }) {
  function go() {
    const blob = new Blob([file.content], { type: file.mime })
    const url = URL.createObjectURL(blob)
    const a = document.createElement("a")
    a.href = url
    a.download = file.filename
    a.click()
    URL.revokeObjectURL(url)
  }
  return (
    <button
      type="button"
      onClick={go}
      className="flex items-center gap-2 rounded-md border bg-background px-3 py-2 text-left text-xs hover:bg-muted transition-colors"
    >
      <Download className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
      <div className="min-w-0">
        <div className="font-medium truncate">{file.filename}</div>
        <div className="text-[10px] text-muted-foreground">
          {file.mime} · {(file.content.length / 1024).toFixed(1)} KB
        </div>
      </div>
    </button>
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
