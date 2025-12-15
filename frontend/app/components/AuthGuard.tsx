"use client";

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { isAuthenticated } from '../lib/auth';

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [isAuthorized, setIsAuthorized] = useState(false);

  useEffect(() => {
    // Always allow access to the landing page
    if (pathname === '/') {
      setIsAuthorized(true);
      return;
    }

    // Check authentication for all other routes
    if (!isAuthenticated()) {
      setIsAuthorized(false);
      router.replace('/');
    } else {
      setIsAuthorized(true);
    }
  }, [pathname, router]);

  // If we're on the landing page, render immediately to avoid flash
  if (pathname === '/') {
    return <>{children}</>;
  }

  // For protected routes, wait until authorization is confirmed
  if (!isAuthorized) {
    return null;
  }

  return <>{children}</>;
}
