import { useEffect, useRef, useState } from "react"
import { ArrowUp, Check, Loader2, Sparkles, Wrench, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { streamCadence, type ChatMessage } from "@/lib/api"
import { cn } from "@/lib/utils"

const SUGGESTIONS = [
  "Show me today's revenue",
  "Which mechanic has the most open tickets?",
  "List low-stock products",
  "Create a new ticket",
]

type ToolCall = { id: string; name: string; status: "running" | "done"; result?: string }

type Turn =
  | { role: "user"; content: string }
  | { role: "assistant"; content: string; toolCalls: ToolCall[] }

export function Cadence() {
  const [open, setOpen] = useState(false)
  const [turns, setTurns] = useState<Turn[]>([])
  const [input, setInput] = useState("")
  const [streaming, setStreaming] = useState(false)
  const scrollRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" })
  }, [turns, streaming])

  async function send(text: string) {
    const trimmed = text.trim()
    if (!trimmed || streaming) return

    const userTurn: Turn = { role: "user", content: trimmed }
    const assistantTurn: Turn = { role: "assistant", content: "", toolCalls: [] }
    setTurns((t) => [...t, userTurn, assistantTurn])
    setInput("")
    setStreaming(true)

    const history: ChatMessage[] = [...turns, userTurn].map((t) => ({
      role: t.role,
      content: t.content,
    }))

    try {
      for await (const ev of streamCadence(history)) {
        setTurns((current) => {
          const copy = [...current]
          const last = copy[copy.length - 1]
          if (last.role !== "assistant") return current
          if (ev.type === "text_delta") {
            copy[copy.length - 1] = { ...last, content: last.content + ev.data.text }
          } else if (ev.type === "tool_call_start") {
            copy[copy.length - 1] = {
              ...last,
              toolCalls: [...last.toolCalls, { id: ev.data.id, name: ev.data.name, status: "running" }],
            }
          } else if (ev.type === "tool_call_end") {
            copy[copy.length - 1] = {
              ...last,
              toolCalls: last.toolCalls.map((tc) =>
                tc.id === ev.data.id ? { ...tc, status: "done", result: ev.data.result } : tc,
              ),
            }
          }
          return copy
        })
      }
    } catch (e) {
      setTurns((current) => {
        const copy = [...current]
        const last = copy[copy.length - 1]
        if (last.role === "assistant") {
          copy[copy.length - 1] = { ...last, content: last.content + `\n\nError: ${String(e)}` }
        }
        return copy
      })
    } finally {
      setStreaming(false)
    }
  }

  return (
    <>
      {open && (
        <div className="fixed bottom-24 right-6 z-50 w-[380px] max-w-[calc(100vw-3rem)] h-[560px] max-h-[calc(100vh-8rem)] rounded-xl border bg-background shadow-2xl flex flex-col overflow-hidden">
          <div className="ai-chat-header flex items-center justify-between px-4 py-3">
            <div className="flex items-center gap-2">
              <div className="ai-header-icon flex h-8 w-8 items-center justify-center rounded-full">
                <Sparkles className="h-4 w-4" />
              </div>
              <div>
                <div className="text-sm font-semibold leading-tight">Cadence</div>
                <div className="text-[11px] opacity-80">Your shop assistant</div>
              </div>
            </div>
            <Button
              variant="ghost" size="icon-sm"
              onClick={() => setOpen(false)} aria-label="Close"
              className="text-white hover:bg-white/20 hover:text-white"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>

          <div ref={scrollRef} className="flex-1 overflow-y-auto p-4 space-y-3">
            {turns.length === 0 ? (
              <div className="flex flex-col items-center justify-center text-center py-6">
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary mb-3">
                  <Sparkles className="h-6 w-6" />
                </div>
                <p className="text-sm text-muted-foreground mb-4">Hi, I'm Cadence. How can I help?</p>
                <div className="grid grid-cols-2 gap-2 w-full">
                  {SUGGESTIONS.map((s) => (
                    <Button
                      key={s}
                      variant="outline" size="sm"
                      onClick={() => send(s)}
                      className="h-auto py-2 px-3 text-xs justify-start whitespace-normal text-left"
                    >
                      {s}
                    </Button>
                  ))}
                </div>
              </div>
            ) : (
              <>
                {turns.map((t, i) => (
                  <div key={i} className={cn("flex gap-2", t.role === "user" ? "justify-end" : "justify-start")}>
                    {t.role === "assistant" && (
                      <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                        <Sparkles className="h-3 w-3" />
                      </div>
                    )}
                    <div className={cn(
                      "max-w-[80%] space-y-2",
                      t.role === "user" ? "" : "flex-1",
                    )}>
                      {t.role === "assistant" && t.toolCalls.length > 0 && (
                        <div className="space-y-1">
                          {t.toolCalls.map((tc) => (
                            <div
                              key={tc.id}
                              className="flex items-center gap-1.5 rounded-md border bg-muted/40 px-2 py-1 text-[11px] font-mono"
                            >
                              {tc.status === "running" ? (
                                <Loader2 className="h-3 w-3 animate-spin text-muted-foreground" />
                              ) : (
                                <Check className="h-3 w-3 text-emerald-600" />
                              )}
                              <Wrench className="h-3 w-3 text-muted-foreground" />
                              <span className="font-semibold">{tc.name}</span>
                              {tc.result && (
                                <span className="text-muted-foreground truncate">— {tc.result}</span>
                              )}
                            </div>
                          ))}
                        </div>
                      )}
                      {(t.role === "user" || t.content) && (
                        <div className={cn(
                          "rounded-2xl px-3 py-2 text-sm whitespace-pre-wrap",
                          t.role === "user" ? "ai-msg-user-bubble" : "bg-muted",
                        )}>
                          {t.content}
                          {t.role === "assistant" && streaming && i === turns.length - 1 && (
                            <span className="ml-0.5 inline-block h-3 w-1.5 translate-y-[2px] animate-pulse bg-current" />
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
                {streaming && turns[turns.length - 1]?.role === "assistant" &&
                  (turns[turns.length - 1] as Turn & { role: "assistant" }).content === "" &&
                  (turns[turns.length - 1] as Turn & { role: "assistant" }).toolCalls.length === 0 && (
                  <div className="flex gap-2">
                    <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                      <Sparkles className="h-3 w-3" />
                    </div>
                    <div className="rounded-2xl bg-muted px-3 py-2.5 flex gap-1">
                      <span className="h-1.5 w-1.5 rounded-full bg-muted-foreground/50 animate-bounce [animation-delay:0ms]" />
                      <span className="h-1.5 w-1.5 rounded-full bg-muted-foreground/50 animate-bounce [animation-delay:150ms]" />
                      <span className="h-1.5 w-1.5 rounded-full bg-muted-foreground/50 animate-bounce [animation-delay:300ms]" />
                    </div>
                  </div>
                )}
              </>
            )}
          </div>

          <form
            onSubmit={(e) => { e.preventDefault(); send(input) }}
            className="flex items-center gap-2 border-t p-3"
          >
            <Input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Ask Cadence…"
              disabled={streaming}
              className="h-9"
            />
            <button
              type="submit"
              disabled={!input.trim() || streaming}
              aria-label="Send"
              className="ai-send-btn flex h-9 w-9 shrink-0 items-center justify-center rounded-md disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <ArrowUp className="h-4 w-4" />
            </button>
          </form>
        </div>
      )}

      <button
        onClick={() => setOpen((o) => !o)}
        aria-label={open ? "Close Cadence" : "Open Cadence"}
        className={cn(
          "ai-fab fixed bottom-6 right-6 z-50 flex h-14 w-14 items-center justify-center rounded-full shadow-lg transition-transform hover:scale-105 active:scale-95",
          open && "ai-fab-open"
        )}
      >
        {open ? (
          <X className="h-6 w-6 text-white" />
        ) : (
          <Sparkles className="h-5 w-5 text-[#1e1b4b]/70" strokeWidth={1.75} />
        )}
      </button>
    </>
  )
}
