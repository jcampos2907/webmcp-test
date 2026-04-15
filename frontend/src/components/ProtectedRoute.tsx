import { Navigate, Outlet, useLocation } from "react-router-dom"
import { useSession } from "@/lib/session"

export default function ProtectedRoute() {
  const { user, loading } = useSession()
  const location = useLocation()

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center text-muted-foreground text-sm">
        Loading…
      </div>
    )
  }

  if (!user) {
    const returnUrl = location.pathname + location.search
    return <Navigate to={`/login?returnUrl=${encodeURIComponent(returnUrl)}`} replace />
  }

  return <Outlet />
}
