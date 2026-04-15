import { NavLink, Outlet, useLocation } from "react-router-dom"
import type { ComponentType } from "react"
import {
  Home,
  Ticket,
  Users,
  Wrench,
  ClipboardList,
  Cog,
  Package,
  BarChart3,
  Settings,
  Bike,
  Store,
  LogOut,
} from "lucide-react"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar"
import { TooltipProvider } from "@/components/ui/tooltip"
import { Toaster } from "@/components/ui/sonner"
import { useSession } from "@/lib/session"
import { authApi, type Permission } from "@/lib/api"
import { StoreSwitcher } from "@/components/StoreSwitcher"

type Item = { to: string; label: string; icon: ComponentType<{ className?: string }>; end?: boolean; requires?: Permission }

const main: Item[] = [
  { to: "/", label: "Home", icon: Home, end: true },
  { to: "/pos", label: "POS", icon: Store, requires: "pos.use" },
  { to: "/tickets", label: "Tickets", icon: Ticket, requires: "tickets.view" },
  { to: "/customers", label: "Customers", icon: Users, requires: "customers.view" },
]

const admin: Item[] = [
  { to: "/mechanics", label: "Mechanics", icon: Wrench, requires: "mechanics.view" },
  { to: "/mechanics/workload", label: "Workload", icon: ClipboardList, requires: "mechanics.view" },
  { to: "/services", label: "Services", icon: Cog, requires: "services.view" },
  { to: "/products", label: "Products", icon: Package, requires: "products.view" },
  { to: "/reports", label: "Reports", icon: BarChart3, requires: "reports.view.own" },
]

function NavItems({ items }: { items: Item[] }) {
  const { pathname } = useLocation()
  const { can } = useSession()
  const visible = items.filter((i) => !i.requires || can(i.requires))
  if (visible.length === 0) return null
  return (
    <SidebarMenu>
      {visible.map((item) => {
        const isActive = item.end ? pathname === item.to : pathname.startsWith(item.to)
        return (
          <SidebarMenuItem key={item.to}>
            <SidebarMenuButton asChild isActive={isActive} tooltip={item.label}>
              <NavLink to={item.to} end={item.end}>
                <item.icon />
                <span>{item.label}</span>
              </NavLink>
            </SidebarMenuButton>
          </SidebarMenuItem>
        )
      })}
    </SidebarMenu>
  )
}

export default function AppLayout() {
  const { pathname } = useLocation()
  const { user, can, role } = useSession()
  const showSettings = can("settings.manage")
  return (
    <TooltipProvider delayDuration={0}>
      <SidebarProvider>
        <Sidebar collapsible="icon">
          <SidebarHeader className="border-b">
            <div className="flex items-center gap-2 px-2 py-2 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:px-0">
              <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-primary text-primary-foreground">
                <Bike className="h-4 w-4" />
              </div>
              <div className="flex flex-col leading-tight group-data-[collapsible=icon]:hidden">
                <span className="text-sm font-semibold">BikePOS</span>
                <span className="text-[10px] text-muted-foreground">Service shop</span>
              </div>
            </div>
          </SidebarHeader>

          <SidebarContent>
            <SidebarGroup>
              <SidebarGroupLabel>Workspace</SidebarGroupLabel>
              <SidebarGroupContent>
                <NavItems items={main} />
              </SidebarGroupContent>
            </SidebarGroup>

            <SidebarGroup>
              <SidebarGroupLabel>Admin</SidebarGroupLabel>
              <SidebarGroupContent>
                <NavItems items={admin} />
              </SidebarGroupContent>
            </SidebarGroup>
          </SidebarContent>

          <SidebarFooter>
            <div className="px-1 pb-1">
              <StoreSwitcher />
            </div>
            <SidebarMenu>
              {showSettings && (
                <SidebarMenuItem>
                  <SidebarMenuButton asChild isActive={pathname.startsWith("/settings")} tooltip="Settings">
                    <NavLink to="/settings">
                      <Settings />
                      <span>Settings</span>
                    </NavLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              )}
              <SidebarMenuItem>
                <SidebarMenuButton
                  tooltip={user?.email ? `Logout (${user.email})` : "Logout"}
                  onClick={() => { window.location.href = authApi.logoutUrl() }}
                >
                  <LogOut />
                  <span>Logout</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarFooter>
        </Sidebar>

        <SidebarInset>
          <header className="sticky top-0 z-10 flex h-12 items-center gap-2 border-b bg-background/80 backdrop-blur px-4">
            <SidebarTrigger />
            <div className="ml-auto flex items-center gap-2 text-xs text-muted-foreground">
              <span>{user?.displayName || user?.email}</span>
              {role && <span className="rounded-md border px-1.5 py-0.5 font-medium">{role}</span>}
            </div>
          </header>
          <div className="flex-1 overflow-y-auto">
            <Outlet />
          </div>
        </SidebarInset>

        <Toaster richColors />
      </SidebarProvider>
    </TooltipProvider>
  )
}
