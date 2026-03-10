"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { verifyMagicLink } from "../../lib/auth";

/**
 * Inner component that reads the token from the URL and verifies it.
 */
function MagicLinkHandler() {
  const searchParams = useSearchParams();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    const token = searchParams.get("token");

    if (!token) {
      setStatus("error");
      setErrorMessage("No sign-in token was found in the link. Please request a new one.");
      return;
    }

    verifyMagicLink(token)
      .then(() => {
        setStatus("success");
        // Use a full-page redirect so the app remounts with the token in localStorage
        window.location.replace("/");
      })
      .catch((err) => {
        console.error("Magic link verification failed", err);
        setStatus("error");
        if (err?.status === 401) {
          setErrorMessage("This sign-in link has expired or has already been used. Please request a new one.");
        } else if (err?.status === 403) {
          setErrorMessage("Access is by invitation only. Please contact the administrator.");
        } else {
          setErrorMessage("Sign-in failed. Please try again or request a new link.");
        }
      });
  }, [searchParams]);

  if (status === "loading") {
    return <div className="text-white/60 text-sm animate-pulse">Verifying your sign-in link…</div>;
  }

  if (status === "success") {
    return <div className="text-white/60 text-sm animate-pulse">Signing you in…</div>;
  }

  return (
    <div className="text-center space-y-4 max-w-sm">
      <div className="text-4xl">🔗</div>
      <p className="text-red-400 text-sm">{errorMessage}</p>
      <Link
        href="/"
        className="inline-block text-purple-400 hover:text-purple-300 underline text-sm"
      >
        Return to sign-in
      </Link>
    </div>
  );
}

/**
 * Magic link verification page.
 *
 * After the user clicks the magic link sent to their email, they land here
 * with a token query parameter:
 *   /auth/magic-link?token=<token>
 *
 * This page verifies the token with the backend, stores the resulting JWT
 * in localStorage, and redirects to the dashboard.
 */
export default function MagicLinkPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-900">
      <Suspense fallback={<div className="text-white/60 text-sm animate-pulse">Loading…</div>}>
        <MagicLinkHandler />
      </Suspense>
    </div>
  );
}
