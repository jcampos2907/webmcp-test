export type Customer = {
  id: string
  firstName: string
  lastName: string
  fullName: string
  phone: string | null
  email: string | null
  city: string | null
}

export async function fetchCustomers(search?: string): Promise<Customer[]> {
  const qs = search ? `?search=${encodeURIComponent(search)}` : ""
  const res = await fetch(`/api/customers${qs}`)
  if (!res.ok) throw new Error(`API ${res.status}`)
  return res.json()
}
