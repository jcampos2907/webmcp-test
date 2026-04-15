import { useLocation } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { authApi } from "@/lib/api"

export default function LoginPage() {
  const location = useLocation()
  const returnUrl = new URLSearchParams(location.search).get("returnUrl") ?? "/"
  const error = new URLSearchParams(location.search).get("error")

  function signIn() {
    window.location.href = authApi.loginUrl(returnUrl)
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-8 bg-muted/30">
      <Card className="w-full max-w-sm">
        <CardContent className="pt-8 pb-6 space-y-6 text-center">
          <div className="space-y-1">
            <h1 className="text-xl font-semibold">BikePOS</h1>
            <p className="text-sm text-muted-foreground">Sign in to continue</p>
          </div>
          {error && (
            <p className="text-sm text-destructive">Login failed. Please try again.</p>
          )}
          <Button className="w-full" onClick={signIn}>Sign in with Keycloak</Button>
        </CardContent>
      </Card>
    </div>
  )
}
