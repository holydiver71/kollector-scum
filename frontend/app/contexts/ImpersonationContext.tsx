"use client";

import React, { createContext, useContext, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';

const IMPERSONATION_USER_ID_KEY = 'impersonation_userId';
const IMPERSONATION_EMAIL_KEY = 'impersonation_email';
const IMPERSONATION_DISPLAY_NAME_KEY = 'impersonation_displayName';

interface ImpersonationUser {
  userId: string;
  email: string;
  displayName?: string;
}

interface ImpersonationContextType {
  isImpersonating: boolean;
  impersonatedUserId: string | null;
  impersonatedUserEmail: string | null;
  impersonatedUserDisplayName: string | null;
  startImpersonation: (user: ImpersonationUser) => void;
  endImpersonation: () => void;
}

const ImpersonationContext = createContext<ImpersonationContextType | undefined>(undefined);

/**
 * Provider component for admin user impersonation state.
 * Persists impersonation state in localStorage to survive page refreshes.
 */
export function ImpersonationProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [isImpersonating, setIsImpersonating] = useState(false);
  const [impersonatedUserId, setImpersonatedUserId] = useState<string | null>(null);
  const [impersonatedUserEmail, setImpersonatedUserEmail] = useState<string | null>(null);
  const [impersonatedUserDisplayName, setImpersonatedUserDisplayName] = useState<string | null>(null);

  // Hydrate from localStorage on mount (survives page refresh)
  useEffect(() => {
    if (typeof window === 'undefined') return;
    const userId = localStorage.getItem(IMPERSONATION_USER_ID_KEY);
    const email = localStorage.getItem(IMPERSONATION_EMAIL_KEY);
    const displayName = localStorage.getItem(IMPERSONATION_DISPLAY_NAME_KEY);
    if (userId && email) {
      setIsImpersonating(true);
      setImpersonatedUserId(userId);
      setImpersonatedUserEmail(email);
      setImpersonatedUserDisplayName(displayName);
    }
  }, []);

  /**
   * Starts impersonating a user. Stores state in localStorage, fires authChanged event,
   * and redirects to /dashboard.
   */
  const startImpersonation = (user: ImpersonationUser) => {
    localStorage.setItem(IMPERSONATION_USER_ID_KEY, user.userId);
    localStorage.setItem(IMPERSONATION_EMAIL_KEY, user.email);
    if (user.displayName) {
      localStorage.setItem(IMPERSONATION_DISPLAY_NAME_KEY, user.displayName);
    } else {
      localStorage.removeItem(IMPERSONATION_DISPLAY_NAME_KEY);
    }
    setIsImpersonating(true);
    setImpersonatedUserId(user.userId);
    setImpersonatedUserEmail(user.email);
    setImpersonatedUserDisplayName(user.displayName ?? null);
    window.dispatchEvent(new Event('authChanged'));
    router.push('/dashboard');
  };

  /**
   * Ends impersonation. Clears localStorage state, fires authChanged event,
   * and redirects to /admin.
   */
  const endImpersonation = () => {
    localStorage.removeItem(IMPERSONATION_USER_ID_KEY);
    localStorage.removeItem(IMPERSONATION_EMAIL_KEY);
    localStorage.removeItem(IMPERSONATION_DISPLAY_NAME_KEY);
    setIsImpersonating(false);
    setImpersonatedUserId(null);
    setImpersonatedUserEmail(null);
    setImpersonatedUserDisplayName(null);
    window.dispatchEvent(new Event('authChanged'));
    router.push('/admin');
  };

  return (
    <ImpersonationContext.Provider value={{
      isImpersonating,
      impersonatedUserId,
      impersonatedUserEmail,
      impersonatedUserDisplayName,
      startImpersonation,
      endImpersonation,
    }}>
      {children}
    </ImpersonationContext.Provider>
  );
}

/**
 * Hook to access impersonation context. Must be used within ImpersonationProvider.
 */
export function useImpersonation(): ImpersonationContextType {
  const context = useContext(ImpersonationContext);
  if (context === undefined) {
    throw new Error('useImpersonation must be used within an ImpersonationProvider');
  }
  return context;
}
