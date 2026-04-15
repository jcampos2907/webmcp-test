import { useEffect, useRef, useState } from "react"
import { ArrowUp, Sparkles, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { assistantApi, type ChatMessage } from "@/lib/api"
import { cn } from "@/lib/utils"

const SUGGESTIONS = [
  "Show me today's revenue",
  "Which mechanic has the most open tickets?",
  "List low-stock products",
  "Create a new ticket",
]

export function AiAssistant() {
  const [open, setOpen] = useState(false)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState("")
  const [thinking, setThinking] = useState(false)
  const scrollRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" })
  }, [messages, thinking])

  async function send(text: string) {
    const trimmed = text.trim()
    if (!trimmed || thinking) return
    const next: ChatMessage[] = [...messages, { role: "user", content: trimmed }]
    setMessages(next)
    setInput("")
    setThinking(true)
    try {
      const res = await assistantApi.chat(next)
      setMessages((m) => [...m, { role: "assistant", content: res.content }])
    } catch (e) {
      setMessages((m) => [...m, { role: "assistant", content: `Error: ${String(e)}` }])
    } finally {
      setThinking(false)
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
                <div className="text-sm font-semibold leading-tight">AI Assistant</div>
                <div className="text-[11px] opacity-80">Ask me anything</div>
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
            {messages.length === 0 ? (
              <div className="flex flex-col items-center justify-center text-center py-6">
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary mb-3">
                  <Sparkles className="h-6 w-6" />
                </div>
                <p className="text-sm text-muted-foreground mb-4">How can I help you today?</p>
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
                {messages.map((m, i) => (
                  <div key={i} className={cn("flex gap-2", m.role === "user" ? "justify-end" : "justify-start")}>
                    {m.role === "assistant" && (
                      <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                        <Sparkles className="h-3 w-3" />
                      </div>
                    )}
                    <div className={cn(
                      "max-w-[80%] rounded-2xl px-3 py-2 text-sm whitespace-pre-wrap",
                      m.role === "user" ? "ai-msg-user-bubble" : "bg-muted"
                    )}>
                      {m.content}
                    </div>
                  </div>
                ))}
                {thinking && (
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
              placeholder="Ask something…"
              disabled={thinking}
              className="h-9"
            />
            <button
              type="submit"
              disabled={!input.trim() || thinking}
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
        aria-label={open ? "Close AI Assistant" : "Open AI Assistant"}
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
