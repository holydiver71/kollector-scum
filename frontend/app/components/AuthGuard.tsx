"use client";

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { getUserProfile, isAuthenticated } from '../lib/auth';

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  // Call hooks unconditionally at the top-level to satisfy React Hooks rules.
  const router = useRouter();
  const _pathname = usePathname();
  const pathname: string = _pathname ?? (typeof window !== 'undefined' ? window.location.pathname : '/');

  const isTestEnv = typeof process !== 'undefined' && (process.env.NODE_ENV === 'test' || typeof process.env.JEST_WORKER_ID !== 'undefined');
  const [validated, setValidated] = useState(false);
  const isLandingPage = pathname === '/';
  // Auth callback page must be accessible without a token so it can store the
  // JWT returned by the backend Google OAuth flow before redirecting to '/'.
  const isPublicRoute = isLandingPage || pathname === '/auth/callback';

  useEffect(() => {
    let cancelled = false;

    const validate = async () => {
      // Landing page and other public routes don't require authentication.
      if (isPublicRoute) {
        if (!cancelled) setValidated(true);
        return;
      }

      // No token -> definitely unauthenticated.
      if (!isAuthenticated()) {
        if (!cancelled) setValidated(false);
        router.replace('/');
        return;
      }

      // Token exists: validate it against the backend so revoked users don't see the app shell.
      if (isTestEnv) {
        // In test runs we avoid calling the backend; treat presence of token as valid.
        if (!cancelled) setValidated(true);
        return;
      }

      if (!cancelled) setValidated(false);
      const profile = await getUserProfile();
      if (!cancelled) {
        if (!profile) {
          setValidated(false);
          router.replace('/');
          return;
        }
        setValidated(true);
      }
    };

    validate();

    const handleAuthChanged = () => {
      validate();
    };

    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'auth_token') {
        validate();
      }
    };

    window.addEventListener('authChanged', handleAuthChanged);
    window.addEventListener('storage', handleStorageChange);
    
    return () => {
      cancelled = true;
      window.removeEventListener('authChanged', handleAuthChanged);
      window.removeEventListener('storage', handleStorageChange);
    };
  }, [pathname, router, isPublicRoute, isTestEnv]);

  // Public routes - always show
  if (isPublicRoute) {
    return <>{children}</>;
  }

  // Protected routes - block render until validated
  if (!validated) {
    return null;
  }

  return <>{children}</>;
}
