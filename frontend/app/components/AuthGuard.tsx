"use client";

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { getUserProfile, isAuthenticated } from '../lib/auth';

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [validated, setValidated] = useState(false);
  const isLandingPage = pathname === '/';

  useEffect(() => {
    let cancelled = false;

    const validate = async () => {
      // Landing page is public.
      if (isLandingPage) {
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
  }, [pathname, router, isLandingPage]);

  // Landing page - always show
  if (isLandingPage) {
    return <>{children}</>;
  }

  // Protected routes - block render until validated
  if (!validated) {
    return null;
  }

  return <>{children}</>;
}
