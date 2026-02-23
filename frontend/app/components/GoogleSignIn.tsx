"use client";

import { useState, useEffect } from "react";
import { signOut, isAuthenticated, getUserProfile, type UserProfile } from "../lib/auth";
import { API_BASE_URL } from "../lib/api";

interface GoogleSignInProps {
  onSignIn?: (profile: UserProfile) => void;
  className?: string;
}

/**
 * Google Sign-In component.
 *
 * Redirects the browser to the backend OAuth login endpoint so that
 * Google credentials never touch the frontend (important for static hosts
 * such as Cloudflare Pages where the Google Identity Services SDK has
 * limited support).
 */
export function GoogleSignIn({ onSignIn, className }: GoogleSignInProps) {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const checkAuth = async () => {
      if (isAuthenticated()) {
        try {
          const userProfile = await getUserProfile();
          setProfile(userProfile);
          // Do NOT call onSignIn here — this is silent session restoration on
          // mount, not an active sign-in event. Calling it here causes
          // components that use onSignIn (e.g. Header) to re-run side effects
          // they only intend to run after a fresh login.
        } catch (e) {
          console.error("Failed to get user profile", e);
          signOut(); // Clear invalid token
        }
      } else {
        setProfile(null);
      }
      setLoading(false);
    };

    checkAuth();

    // Re-run auth check whenever another part of the app signals an auth change
    // (e.g. the OAuth callback page after it stores the JWT token).
    window.addEventListener("authChanged", checkAuth);
    return () => window.removeEventListener("authChanged", checkAuth);
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Read error from query string set by backend on auth failure
  useEffect(() => {
    if (typeof window === "undefined") return;
    const params = new URLSearchParams(window.location.search);
    const authError = params.get("error");
    if (authError === "not_invited") {
      setError("Access is by invitation only. Please contact the administrator.");
    } else if (authError === "access_deactivated") {
      setError("Your access has been deactivated. Please contact the administrator.");
    } else if (
      authError === "invalid_token" ||
      authError === "google_auth_failed" ||
      authError === "auth_failed"
    ) {
      setError("Google Sign-In failed. Please try again.");
    }
  }, []);

  const handleSignOut = () => {
    signOut();
    setProfile(null);
    window.location.href = "/";
  };

  const handleSignIn = () => {
    window.location.href = `${API_BASE_URL}/api/auth/google/login`;
  };

  if (loading) {
    return <div className="text-sm text-white/80">Loading...</div>;
  }

  if (profile) {
    return (
      <div className={`flex items-center gap-3 ${className || ""}`}>
        <span className="text-sm font-medium text-white">
          {profile.displayName || profile.email}
        </span>
        {profile.isAdmin && (
          <a
            href="/admin"
            className="text-sm text-purple-300 hover:text-purple-200 underline"
          >
            Admin
          </a>
        )}
        <button
          onClick={handleSignOut}
          className="text-sm text-blue-300 hover:text-blue-200 underline"
        >
          Sign Out
        </button>
      </div>
    );
  }

  return (
    <div className={className}>
      {error && (
        <div className="text-sm text-red-400 bg-red-900/50 border border-red-600 px-3 py-2 rounded-md mb-2">
          {error}
        </div>
      )}
      <button
        onClick={handleSignIn}
        className="flex items-center gap-2 bg-white text-gray-700 font-medium text-sm px-4 py-2 rounded-md shadow hover:shadow-md hover:bg-gray-50 transition-all duration-150 border border-gray-200"
      >
        {/* Google "G" logo */}
        <svg width="18" height="18" viewBox="0 0 18 18" aria-hidden="true">
          <path
            fill="#4285F4"
            d="M17.64 9.2c0-.637-.057-1.251-.164-1.84H9v3.481h4.844c-.209 1.125-.843 2.078-1.796 2.717v2.258h2.908c1.702-1.567 2.684-3.874 2.684-6.615z"
          />
          <path
            fill="#34A853"
            d="M9 18c2.43 0 4.467-.806 5.956-2.184l-2.908-2.258c-.806.54-1.837.86-3.048.86-2.344 0-4.328-1.584-5.036-3.711H.957v2.332A8.997 8.997 0 0 0 9 18z"
          />
          <path
            fill="#FBBC05"
            d="M3.964 10.707A5.41 5.41 0 0 1 3.682 9c0-.593.102-1.17.282-1.707V4.961H.957A8.996 8.996 0 0 0 0 9c0 1.452.348 2.827.957 4.039l3.007-2.332z"
          />
          <path
            fill="#EA4335"
            d="M9 3.58c1.321 0 2.508.454 3.44 1.345l2.582-2.58C13.463.891 11.426 0 9 0A8.997 8.997 0 0 0 .957 4.961L3.964 6.293C4.672 4.166 6.656 3.58 9 3.58z"
          />
        </svg>
        Sign in with Google
      </button>
    </div>
  );
}
