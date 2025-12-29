"use client";

import { useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useCollection } from '../contexts/CollectionContext';
import { isAuthenticated } from '../lib/auth';

// Routes that are allowed when collection is empty
const ALLOWED_EMPTY_ROUTES = [
  '/',
  '/profile',
  '/settings',
];

export function CollectionGuard({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { hasCollection, isReady } = useCollection();

  useEffect(() => {
    // Only check if we're authenticated and have loaded collection status
    if (!isAuthenticated() || !isReady) {
      return;
    }

    // If collection is empty and user tries to access a restricted route
    if (hasCollection === false && !ALLOWED_EMPTY_ROUTES.includes(pathname)) {
      router.push('/');
    }
  }, [hasCollection, isReady, pathname, router]);

  return <>{children}</>;
}
