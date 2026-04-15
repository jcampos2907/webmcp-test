import { useEffect, useRef, useState } from "react"
import { Check, ChevronsUpDown, Globe, Store as StoreIcon } from "lucide-react"
import { cn } from "@/lib/utils"
import { useSession } from "@/lib/session"
import { CountryFlag } from "@/components/ui/country-select"

export function StoreSwitcher() {
  const { currentStore, stores, loading, setStore } = useSession()
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    function onDown(e: MouseEvent) {
      if (!rootRef.current) return
      if (!rootRef.current.contains(e.target as Node)) setOpen(false)
    }
    function onKey(e: KeyboardEvent) { if (e.key === "Escape") setOpen(false) }
    document.addEventListener("mousedown", onDown)
    document.addEventListener("keydown", onKey)
    return () => {
      document.removeEventListener("mousedown", onDown)
      document.removeEventListener("keydown", onKey)
    }
  }, [open])

  const grouped = stores.reduce<Record<string, typeof stores>>((acc, s) => {
    (acc[s.companyName] ??= []).push(s)
    return acc
  }, {})

  return (
    <div ref={rootRef} className="relative group-data-[collapsible=icon]:hidden">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        disabled={loading}
        className={cn(
          "flex w-full items-center gap-2 rounded-md border px-2 py-1.5 text-left text-sm",
          "hover:bg-muted/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/30",
          "disabled:opacity-50",
        )}
      >
        {currentStore ? (
          <CountryFlag code={currentStore.countryCode} size={22} />
        ) : (
          <span className="inline-flex h-[22px] w-[22px] shrink-0 items-center justify-center rounded-full bg-muted">
            <Globe className="h-3 w-3 text-muted-foreground" />
          </span>
        )}
        <div className="min-w-0 flex-1">
          <div className="truncate text-xs font-semibold">
            {currentStore ? currentStore.name : "Global view"}
          </div>
          <div className="truncate text-[10px] text-muted-foreground">
            {currentStore ? currentStore.companyName : "No store selected"}
          </div>
        </div>
        <ChevronsUpDown className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
      </button>

      {open && (
        <div className="absolute bottom-full left-0 right-0 z-50 mb-1 max-h-80 overflow-y-auto rounded-md border bg-popover text-popover-foreground shadow-md">
          <button
            type="button"
            onClick={() => { setStore(null); setOpen(false) }}
            className={cn(
              "flex w-full items-center gap-2 px-2.5 py-2 text-left text-sm hover:bg-muted/60",
              !currentStore && "bg-muted",
            )}
          >
            <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-muted">
              <Globe className="h-3 w-3 text-muted-foreground" />
            </span>
            <span className="flex-1">Global view</span>
            {!currentStore && <Check className="h-3.5 w-3.5" />}
          </button>
          {Object.entries(grouped).map(([company, list]) => (
            <div key={company} className="border-t">
              <div className="px-2.5 pt-2 pb-1 text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
                {company}
              </div>
              {list.map((s) => {
                const active = currentStore?.id === s.id
                return (
                  <button
                    type="button"
                    key={s.id}
                    onClick={() => { setStore(s.id); setOpen(false) }}
                    className={cn(
                      "flex w-full items-center gap-2 px-2.5 py-1.5 text-left text-sm hover:bg-muted/60",
                      active && "bg-muted",
                    )}
                  >
                    <CountryFlag code={s.countryCode} size={18} />
                    <StoreIcon className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                    <span className="flex-1 truncate">{s.name}</span>
                    {!s.isActive && (
                      <span className="text-[9px] rounded bg-muted px-1 py-0.5 text-muted-foreground">Off</span>
                    )}
                    {active && <Check className="h-3.5 w-3.5" />}
                  </button>
                )
              })}
            </div>
          ))}
          {stores.length === 0 && !loading && (
            <div className="px-2.5 py-3 text-xs text-muted-foreground">No stores available</div>
          )}
        </div>
      )}
    </div>
  )
}
