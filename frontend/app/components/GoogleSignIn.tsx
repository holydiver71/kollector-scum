"use client";

import { useState, useEffect } from "react";
import { GoogleLogin, CredentialResponse } from "@react-oauth/google";
import { exchangeGoogleIdToken, signOut, isAuthenticated, getUserProfile, type UserProfile } from "../lib/auth";

interface GoogleSignInProps {
  onSignIn?: (profile: UserProfile) => void;
  className?: string;
}

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
        } catch (e) {
          console.error("Failed to get user profile", e);
          signOut(); // Clear invalid token
        }
      }
      setLoading(false);
    };

    checkAuth();
  }, []);

  const handleSuccess = async (credentialResponse: CredentialResponse) => {
    console.log('[GoogleSignIn] onSuccess credentialResponse:', credentialResponse);
    if (!credentialResponse || !credentialResponse.credential) {
      setError("Login failed: No credential returned.");
      console.warn('[GoogleSignIn] No credential in response', credentialResponse);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const authResponse = await exchangeGoogleIdToken(credentialResponse.credential);
      setProfile(authResponse.profile);
      
      if (onSignIn) {
        onSignIn(authResponse.profile);
      }
    } catch (err) {
      console.error('[GoogleSignIn] exchangeGoogleIdToken error:', err);
      const message = err instanceof Error ? err.message : String(err);
      if (message.includes('403') || message.includes('invitation')) {
        setError('Access is by invitation only.');
      } else if (err instanceof TypeError && message.includes('Failed to fetch')) {
        setError('Cannot reach the API. Is the backend running?');
      } else {
        setError('Failed to sign in.');
      }
      console.error('Authentication failed:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleError = () => {
    setError("Google Sign-In failed. Please try again.");
    console.error('[GoogleSignIn] Google Sign-In failed (GoogleLogin onError)');
  };

  const handleSignOut = () => {
    signOut();
    setProfile(null);
    window.location.href = '/';
  };

  if (loading) {
    return <div className="text-sm text-white/80">Loading...</div>;
  }

  if (profile) {
    return (
      <div className={`flex items-center gap-3 ${className || ''}`}>
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
        <div className="text-sm text-red-400 bg-red-900/50 border border-red-600 px-3 py-2 rounded-md mb-2">{error}</div>
      )}
      <GoogleLogin
        onSuccess={handleSuccess}
        onError={handleError}
        theme="outline"
        size="large"
        text="signin_with"
      />
    </div>
  );
}
