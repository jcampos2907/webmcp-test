import { BrowserRouter, Route, Routes } from "react-router-dom"
import AppLayout from "@/components/AppLayout"
import ProtectedRoute from "@/components/ProtectedRoute"
import { SessionProvider } from "@/lib/session"
import DashboardPage from "@/pages/DashboardPage"
import CustomersPage from "@/pages/CustomersPage"
import CustomerFormPage from "@/pages/CustomerFormPage"
import MechanicsPage from "@/pages/MechanicsPage"
import MechanicFormPage from "@/pages/MechanicFormPage"
import MechanicWorkloadPage from "@/pages/MechanicWorkloadPage"
import ServicesPage from "@/pages/ServicesPage"
import ServiceFormPage from "@/pages/ServiceFormPage"
import ProductsPage from "@/pages/ProductsPage"
import ProductFormPage from "@/pages/ProductFormPage"
import TicketsPage from "@/pages/TicketsPage"
import TicketDetailsPage from "@/pages/TicketDetailsPage"
import TicketCreatePage from "@/pages/TicketCreatePage"
import SettingsPage from "@/pages/SettingsPage"
import MetaFieldsPage from "@/pages/MetaFieldsPage"
import PosTerminalPage from "@/pages/PosTerminalPage"
import ReportsPage from "@/pages/ReportsPage"
import LoginPage from "@/pages/LoginPage"

export default function App() {
  return (
    <BrowserRouter>
      <SessionProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route index element={<DashboardPage />} />
              <Route path="tickets" element={<TicketsPage />} />
              <Route path="tickets/new" element={<TicketCreatePage />} />
              <Route path="tickets/:id" element={<TicketDetailsPage />} />
              <Route path="customers" element={<CustomersPage />} />
              <Route path="customers/new" element={<CustomerFormPage />} />
              <Route path="customers/:id" element={<CustomerFormPage />} />
              <Route path="mechanics" element={<MechanicsPage />} />
              <Route path="mechanics/new" element={<MechanicFormPage />} />
              <Route path="mechanics/workload" element={<MechanicWorkloadPage />} />
              <Route path="mechanics/:id" element={<MechanicFormPage />} />
              <Route path="services" element={<ServicesPage />} />
              <Route path="services/new" element={<ServiceFormPage />} />
              <Route path="services/:id" element={<ServiceFormPage />} />
              <Route path="products" element={<ProductsPage />} />
              <Route path="products/new" element={<ProductFormPage />} />
              <Route path="products/:id" element={<ProductFormPage />} />
              <Route path="settings" element={<SettingsPage />} />
              <Route path="settings/meta-fields" element={<MetaFieldsPage />} />
              <Route path="pos" element={<PosTerminalPage />} />
              <Route path="reports" element={<ReportsPage />} />
            </Route>
          </Route>
        </Routes>
      </SessionProvider>
    </BrowserRouter>
  )
}
