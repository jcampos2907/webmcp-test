import { createContext, useContext, useEffect, useState, type ReactNode } from "react"
import { authApi, sessionApi, STORE_ID_KEY, type Me, type Permission, type SessionInfo, type SessionStore } from "./api"

type Ctx = {
  user: Me | null
  currentStore: SessionStore | null
  stores: SessionStore[]
  loading: boolean
  setStore: (id: string | null) => void
  refresh: () => Promise<void>
  /** Check a permission flag. Backend remains the source of truth; this is UX only. */
  can: (perm: Permission) => boolean
  role: string | null
}

const SessionCtx = createContext<Ctx | null>(null)

export function SessionProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<Me | null>(null)
  const [info, setInfo] = useState<SessionInfo | null>(null)
  const [loading, setLoading] = useState(true)

  async function refresh() {
    try {
      const me = await authApi.me()
      setUser(me)
      if (me) setInfo(await sessionApi.get())
      else setInfo(null)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { refresh() }, [])

  function setStore(id: string | null) {
    if (id) localStorage.setItem(STORE_ID_KEY, id)
    else localStorage.removeItem(STORE_ID_KEY)
    window.location.reload()
  }

  const value: Ctx = {
    user,
    currentStore: info?.currentStore ?? null,
    stores: info?.availableStores ?? [],
    loading,
    setStore,
    refresh,
    can: (perm) => user?.permissions?.includes(perm) ?? false,
    role: user?.currentRole ?? null,
  }
  return <SessionCtx.Provider value={value}>{children}</SessionCtx.Provider>
}

export function useSession() {
  const ctx = useContext(SessionCtx)
  if (!ctx) throw new Error("useSession must be used inside SessionProvider")
  return ctx
}
