"use client";

import { useImpersonation } from '../contexts/ImpersonationContext';

/**
 * Sticky banner displayed at the top of the viewport when an admin is impersonating a user.
 * Shows the impersonated user's identity and provides an exit button.
 */
export default function ImpersonationBanner() {
  const { isImpersonating, impersonatedUserDisplayName, impersonatedUserEmail, endImpersonation } = useImpersonation();

  if (!isImpersonating) return null;

  const identity = impersonatedUserDisplayName || impersonatedUserEmail || 'Unknown User';

  return (
    <div
      className="fixed top-0 left-0 right-0 z-50 bg-amber-500 text-black px-4 py-2 flex items-center justify-between shadow-lg"
      role="alert"
      aria-live="polite"
    >
      <span className="font-semibold text-sm">
        ⚠ Impersonating: <strong>{identity}</strong>
      </span>
      <button
        onClick={endImpersonation}
        className="ml-4 bg-black text-amber-400 px-3 py-1 rounded text-sm font-semibold hover:bg-gray-900 transition-colors"
      >
        Exit Impersonation
      </button>
    </div>
  );
}
