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
import { SessionProvider } from "@/lib/session"
import { StoreSwitcher } from "@/components/StoreSwitcher"

type Item = { to: string; label: string; icon: ComponentType<{ className?: string }>; end?: boolean }

const main: Item[] = [
  { to: "/", label: "Home", icon: Home, end: true },
  { to: "/pos", label: "POS", icon: Store },
  { to: "/tickets", label: "Tickets", icon: Ticket },
  { to: "/customers", label: "Customers", icon: Users },
]

const admin: Item[] = [
  { to: "/mechanics", label: "Mechanics", icon: Wrench },
  { to: "/mechanics/workload", label: "Workload", icon: ClipboardList },
  { to: "/services", label: "Services", icon: Cog },
  { to: "/products", label: "Products", icon: Package },
  { to: "/reports", label: "Reports", icon: BarChart3 },
]

function NavItems({ items }: { items: Item[] }) {
  const { pathname } = useLocation()
  return (
    <SidebarMenu>
      {items.map((item) => {
        const isActive = item.end ? pathname === item.to : pathname.startsWith(item.to)
        return (
          <SidebarMenuItem key={item.to}>
            <SidebarMenuButton
              isActive={isActive}
              tooltip={item.label}
              render={<NavLink to={item.to} end={item.end} />}
            >
              <item.icon />
              <span>{item.label}</span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        )
      })}
    </SidebarMenu>
  )
}

export default function AppLayout() {
  const { pathname } = useLocation()
  return (
    <TooltipProvider delayDuration={0}>
      <SessionProvider>
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
              <SidebarMenuItem>
                <SidebarMenuButton
                  isActive={pathname.startsWith("/settings")}
                  tooltip="Settings"
                  render={<NavLink to="/settings" />}
                >
                  <Settings />
                  <span>Settings</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton tooltip="Logout">
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
          </header>
          <div className="flex-1 overflow-y-auto">
            <Outlet />
          </div>
        </SidebarInset>

        <Toaster richColors />
      </SidebarProvider>
      </SessionProvider>
    </TooltipProvider>
  )
}
