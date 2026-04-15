import { createContext, useContext, useEffect, useState, type ReactNode } from "react"
import { sessionApi, STORE_ID_KEY, type SessionInfo, type SessionStore } from "./api"

type Ctx = {
  currentStore: SessionStore | null
  stores: SessionStore[]
  loading: boolean
  setStore: (id: string | null) => void
  refresh: () => Promise<void>
}

const SessionCtx = createContext<Ctx | null>(null)

export function SessionProvider({ children }: { children: ReactNode }) {
  const [info, setInfo] = useState<SessionInfo | null>(null)
  const [loading, setLoading] = useState(true)

  async function refresh() {
    try { setInfo(await sessionApi.get()) } finally { setLoading(false) }
  }

  useEffect(() => { refresh() }, [])

  function setStore(id: string | null) {
    if (id) localStorage.setItem(STORE_ID_KEY, id)
    else localStorage.removeItem(STORE_ID_KEY)
    window.location.reload()
  }

  const value: Ctx = {
    currentStore: info?.currentStore ?? null,
    stores: info?.availableStores ?? [],
    loading,
    setStore,
    refresh,
  }
  return <SessionCtx.Provider value={value}>{children}</SessionCtx.Provider>
}

export function useSession() {
  const ctx = useContext(SessionCtx)
  if (!ctx) throw new Error("useSession must be used inside SessionProvider")
  return ctx
}
