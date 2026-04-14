import { NavLink, Outlet } from "react-router-dom"
import {
  LayoutDashboard,
  Users,
  Wrench,
  Tag,
  Package,
  ClipboardList,
} from "lucide-react"
import { cn } from "@/lib/utils"
import { Toaster } from "@/components/ui/sonner"

const navItems = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard, end: true },
  { to: "/tickets", label: "Tickets", icon: ClipboardList },
  { to: "/customers", label: "Customers", icon: Users },
  { to: "/mechanics", label: "Mechanics", icon: Wrench },
  { to: "/services", label: "Services", icon: Tag },
  { to: "/products", label: "Products", icon: Package },
]

export default function AppLayout() {
  return (
    <div className="flex min-h-screen">
      <aside className="w-60 shrink-0 border-r bg-sidebar text-sidebar-foreground">
        <div className="px-6 py-5 border-b">
          <h1 className="text-lg font-semibold tracking-tight">BikePOS</h1>
          <p className="text-xs text-muted-foreground">Service shop</p>
        </div>
        <nav className="p-3 space-y-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  "flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors",
                  isActive
                    ? "bg-sidebar-accent text-sidebar-accent-foreground"
                    : "hover:bg-sidebar-accent/60"
                )
              }
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>

      <Toaster richColors />
    </div>
  )
}
