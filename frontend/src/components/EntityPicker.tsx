import { useState } from "react"
import { Check, ChevronsUpDown, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  CommandDialog, CommandEmpty, CommandGroup, CommandInput,
  CommandItem, CommandList,
} from "@/components/ui/command"
import { cn } from "@/lib/utils"

export type PickerOption = {
  id: string
  label: string
  sublabel?: string
  keywords?: string[]
}

type Props = {
  value: string
  onChange: (id: string) => void
  options: PickerOption[]
  placeholder?: string
  searchPlaceholder?: string
  emptyText?: string
  allowClear?: boolean
  clearLabel?: string
  disabled?: boolean
}

export function EntityPicker({
  value, onChange, options,
  placeholder = "Select…",
  searchPlaceholder = "Search…",
  emptyText = "No results.",
  allowClear = true,
  clearLabel = "None",
  disabled,
}: Props) {
  const [open, setOpen] = useState(false)
  const selected = options.find((o) => o.id === value)

  return (
    <>
      <Button
        type="button"
        variant="outline"
        role="combobox"
        disabled={disabled}
        onClick={() => setOpen(true)}
        className={cn(
          "w-full justify-between font-normal",
          !selected && "text-muted-foreground",
        )}
      >
        <span className="truncate">{selected ? selected.label : placeholder}</span>
        {selected && allowClear ? (
          <span
            role="button"
            tabIndex={0}
            onClick={(e) => { e.stopPropagation(); onChange("") }}
            onKeyDown={(e) => { if (e.key === "Enter") { e.stopPropagation(); onChange("") } }}
            className="ml-2 inline-flex h-5 w-5 items-center justify-center rounded hover:bg-muted"
            aria-label="Clear"
          >
            <X className="h-3.5 w-3.5 opacity-60" />
          </span>
        ) : (
          <ChevronsUpDown className="h-4 w-4 opacity-50 shrink-0" />
        )}
      </Button>

      <CommandDialog open={open} onOpenChange={setOpen} title={placeholder} description={searchPlaceholder}>
        <CommandInput placeholder={searchPlaceholder} />
        <CommandList>
          <CommandEmpty>{emptyText}</CommandEmpty>
          <CommandGroup>
            {allowClear && (
              <CommandItem
                value="__clear__"
                onSelect={() => { onChange(""); setOpen(false) }}
              >
                <span className="text-muted-foreground">{clearLabel}</span>
                {!value && <Check className="ml-auto h-4 w-4" />}
              </CommandItem>
            )}
            {options.map((o) => (
              <CommandItem
                key={o.id}
                value={`${o.label} ${o.sublabel ?? ""} ${(o.keywords ?? []).join(" ")}`}
                onSelect={() => { onChange(o.id); setOpen(false) }}
              >
                <div className="flex-1 min-w-0">
                  <div className="truncate">{o.label}</div>
                  {o.sublabel && (
                    <div className="truncate text-xs text-muted-foreground">{o.sublabel}</div>
                  )}
                </div>
                {value === o.id && <Check className="ml-2 h-4 w-4" />}
              </CommandItem>
            ))}
          </CommandGroup>
        </CommandList>
      </CommandDialog>
    </>
  )
}
