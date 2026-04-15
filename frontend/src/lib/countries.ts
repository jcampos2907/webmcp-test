export type Country = { code: string; name: string; flagSvg: string; flagPng: string }

const ENDPOINT = "https://restcountries.com/v3.1/all?fields=name,cca2,flags"

type Raw = { cca2: string; name: { common: string }; flags: { svg: string; png: string } }

let cache: Country[] | null = null
let inflight: Promise<Country[]> | null = null

export function fetchCountries(): Promise<Country[]> {
  if (cache) return Promise.resolve(cache)
  if (inflight) return inflight
  inflight = fetch(ENDPOINT)
    .then((r) => {
      if (!r.ok) throw new Error(`restcountries ${r.status}`)
      return r.json() as Promise<Raw[]>
    })
    .then((raw) => {
      const list = raw
        .filter((c) => c.cca2 && c.name?.common && c.flags?.svg)
        .map<Country>((c) => ({
          code: c.cca2.toUpperCase(),
          name: c.name.common,
          flagSvg: c.flags.svg,
          flagPng: c.flags.png,
        }))
        .sort((a, b) => a.name.localeCompare(b.name))
      cache = list
      return list
    })
    .finally(() => { inflight = null })
  return inflight
}

export function getCachedCountries(): Country[] | null { return cache }

export function findCountry(code: string | null | undefined): Country | null {
  if (!code || !cache) return null
  return cache.find((c) => c.code === code.toUpperCase()) ?? null
}

import { useEffect, useState } from "react"
export function useCountries(): { countries: Country[]; loading: boolean; error: string | null } {
  const [countries, setCountries] = useState<Country[]>(() => cache ?? [])
  const [loading, setLoading] = useState(!cache)
  const [error, setError] = useState<string | null>(null)
  useEffect(() => {
    if (cache) return
    let cancelled = false
    fetchCountries()
      .then((list) => { if (!cancelled) { setCountries(list); setLoading(false) } })
      .catch((e) => { if (!cancelled) { setError(String(e)); setLoading(false) } })
    return () => { cancelled = true }
  }, [])
  return { countries, loading, error }
}
