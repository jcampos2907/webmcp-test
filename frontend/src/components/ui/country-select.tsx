import { useEffect, useMemo, useRef, useState } from "react"
import { ChevronDown, Globe, Search, X } from "lucide-react"
import { cn } from "@/lib/utils"
import { fetchCountries, findCountry, getCachedCountries, type Country } from "@/lib/countries"

export function CountryFlag({
  code, size = 20, flagUrl, className,
}: { code: string | null | undefined; size?: number; flagUrl?: string | null; className?: string }) {
  const url = flagUrl ?? (code ? findCountry(code)?.flagSvg ?? null : null)
  if (!url) {
    return (
      <span
        aria-hidden
        className={cn("inline-flex items-center justify-center rounded-full bg-muted text-muted-foreground shrink-0", className)}
        style={{ width: size, height: size }}
      >
        <Globe className="h-[55%] w-[55%]" />
      </span>
    )
  }
  return (
    <span
      className={cn("inline-block rounded-full overflow-hidden bg-muted shrink-0 ring-1 ring-border/40", className)}
      style={{ width: size, height: size }}
    >
      <img
        src={url}
        alt={code ?? ""}
        loading="lazy"
        className="h-full w-full object-cover"
        style={{ transform: "scale(1.6)" }}
      />
    </span>
  )
}

type Props = {
  value: string | null
  onChange: (code: string | null) => void
  placeholder?: string
  allowClear?: boolean
  className?: string
  disabled?: boolean
}

export function CountrySelect({
  value, onChange, placeholder = "Select country", allowClear = true, className, disabled,
}: Props) {
  const [countries, setCountries] = useState<Country[]>(() => getCachedCountries() ?? [])
  const [loading, setLoading] = useState(countries.length === 0)
  const [error, setError] = useState<string | null>(null)
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState("")
  const rootRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (countries.length > 0) return
    let cancelled = false
    fetchCountries()
      .then((list) => { if (!cancelled) { setCountries(list); setLoading(false) } })
      .catch((e) => { if (!cancelled) { setError(String(e)); setLoading(false) } })
    return () => { cancelled = true }
  }, [countries.length])

  useEffect(() => {
    if (!open) return
    function onDown(e: MouseEvent) {
      if (!rootRef.current) return
      if (!rootRef.current.contains(e.target as Node)) setOpen(false)
    }
    function onKey(e: KeyboardEvent) { if (e.key === "Escape") setOpen(false) }
    document.addEventListener("mousedown", onDown)
    document.addEventListener("keydown", onKey)
    const t = setTimeout(() => inputRef.current?.focus(), 10)
    return () => {
      document.removeEventListener("mousedown", onDown)
      document.removeEventListener("keydown", onKey)
      clearTimeout(t)
    }
  }, [open])

  const selected = value ? countries.find((c) => c.code === value.toUpperCase()) ?? null : null

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return countries
    return countries.filter((c) => c.name.toLowerCase().includes(q) || c.code.toLowerCase().includes(q))
  }, [countries, query])

  return (
    <div ref={rootRef} className={cn("relative", className)}>
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen((v) => !v)}
        className={cn(
          "flex h-9 w-full items-center gap-2 rounded-md border border-input bg-transparent px-3 text-sm shadow-xs transition-colors",
          "hover:bg-muted/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30",
          "disabled:opacity-50 disabled:cursor-not-allowed",
        )}
      >
        <CountryFlag code={selected?.code ?? value ?? null} flagUrl={selected?.flagSvg} size={18} />
        <span className={cn("flex-1 text-left truncate", !selected && "text-muted-foreground")}>
          {selected ? selected.name : loading ? "Loading..." : placeholder}
        </span>
        {allowClear && selected && (
          <span
            role="button"
            tabIndex={-1}
            onClick={(e) => { e.stopPropagation(); onChange(null) }}
            className="rounded p-0.5 hover:bg-muted text-muted-foreground"
            aria-label="Clear country"
          >
            <X className="h-3.5 w-3.5" />
          </span>
        )}
        <ChevronDown className="h-4 w-4 text-muted-foreground shrink-0" />
      </button>

      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover text-popover-foreground shadow-md">
          <div className="flex items-center gap-2 border-b px-2.5 py-2">
            <Search className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
            <input
              ref={inputRef}
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Search countries..."
              className="flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground"
            />
          </div>
          <div className="max-h-64 overflow-y-auto py-1">
            {error && <div className="px-3 py-4 text-xs text-destructive">Failed to load: {error}</div>}
            {!error && loading && <div className="px-3 py-4 text-xs text-muted-foreground">Loading countries...</div>}
            {!error && !loading && filtered.length === 0 && (
              <div className="px-3 py-4 text-xs text-muted-foreground">No matches</div>
            )}
            {filtered.map((c) => {
              const active = selected?.code === c.code
              return (
                <button
                  type="button"
                  key={c.code}
                  onClick={() => { onChange(c.code); setOpen(false); setQuery("") }}
                  className={cn(
                    "flex w-full items-center gap-2.5 px-2.5 py-1.5 text-left text-sm hover:bg-muted/60 transition-colors",
                    active && "bg-muted"
                  )}
                >
                  <CountryFlag code={c.code} flagUrl={c.flagSvg} size={20} />
                  <span className="flex-1 truncate">{c.name}</span>
                  <span className="text-[10px] font-mono text-muted-foreground">{c.code}</span>
                </button>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}
