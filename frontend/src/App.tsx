import { BrowserRouter, Route, Routes } from "react-router-dom"
import AppLayout from "@/components/AppLayout"
import DashboardPage from "@/pages/DashboardPage"
import CustomersPage from "@/pages/CustomersPage"
import CustomerFormPage from "@/pages/CustomerFormPage"
import MechanicsPage from "@/pages/MechanicsPage"
import MechanicFormPage from "@/pages/MechanicFormPage"
import ServicesPage from "@/pages/ServicesPage"
import ServiceFormPage from "@/pages/ServiceFormPage"
import ProductsPage from "@/pages/ProductsPage"
import ProductFormPage from "@/pages/ProductFormPage"
import TicketsPage from "@/pages/TicketsPage"
import TicketDetailsPage from "@/pages/TicketDetailsPage"

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="tickets" element={<TicketsPage />} />
          <Route path="tickets/:id" element={<TicketDetailsPage />} />
          <Route path="customers" element={<CustomersPage />} />
          <Route path="customers/new" element={<CustomerFormPage />} />
          <Route path="customers/:id" element={<CustomerFormPage />} />
          <Route path="mechanics" element={<MechanicsPage />} />
          <Route path="mechanics/new" element={<MechanicFormPage />} />
          <Route path="mechanics/:id" element={<MechanicFormPage />} />
          <Route path="services" element={<ServicesPage />} />
          <Route path="services/new" element={<ServiceFormPage />} />
          <Route path="services/:id" element={<ServiceFormPage />} />
          <Route path="products" element={<ProductsPage />} />
          <Route path="products/new" element={<ProductFormPage />} />
          <Route path="products/:id" element={<ProductFormPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
