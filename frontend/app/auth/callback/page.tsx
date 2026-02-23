 "use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { setAuthToken } from "../../lib/auth";

/**
 * Inner component that reads the token from the URL and stores it.
 */
function CallbackHandler() {
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = searchParams.get("token");

    if (!token) {
      setError("Authentication failed: no token received.");
      return;
    }

    try {
      setAuthToken(token);
      // Use a full-page redirect so the app remounts at "/" with the token
      // already in localStorage. This guarantees GoogleSignIn reads the
      // correct auth state on its initial mount without any event-timing issues.
      window.location.replace("/");
    } catch (e) {
      console.error("Failed to store auth token", e);
      setError("Authentication failed: could not store session.");
    }
  }, [searchParams]);

  if (error) {
    return (
      <div className="text-center space-y-4">
        <p className="text-red-400 text-sm">{error}</p>
        <Link href="/" className="text-blue-400 hover:underline text-sm">
          Return to home
        </Link>
      </div>
    );
  }

  return <div className="text-white/60 text-sm animate-pulse">Signing you in…</div>;
}

/**
 * Auth callback page.
 *
 * After the user authenticates with Google via the backend OAuth flow,
 * the backend redirects here with a JWT token as a query parameter:
 *   /auth/callback?token=<jwt>
 *
 * This page reads the token, stores it in localStorage and redirects
 * to the dashboard.
 */
export default function AuthCallbackPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-900">
      <Suspense fallback={<div className="text-white/60 text-sm animate-pulse">Loading…</div>}>
        <CallbackHandler />
      </Suspense>
    </div>
  );
}
