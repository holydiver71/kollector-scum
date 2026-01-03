"use client";

import { useEffect, useState } from "react";
import { exchangeGoogleIdToken, signOut, isAuthenticated, getUserProfile, type UserProfile } from "../lib/auth";

// Google Identity Services types
declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (response: { credential: string }) => void;
          }) => void;
          renderButton: (element: HTMLElement, config: {
            theme?: string;
            size?: string;
            text?: string;
          }) => void;
          prompt: () => void;
        };
      };
    };
  }
}

interface GoogleSignInProps {
  onSignIn?: (profile: UserProfile) => void;
  className?: string;
}

export function GoogleSignIn({ onSignIn, className }: GoogleSignInProps) {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Check if user is already authenticated
    const checkAuth = async () => {
      if (isAuthenticated()) {
        const userProfile = await getUserProfile();
        setProfile(userProfile);
      }
      setLoading(false);
    };

    checkAuth();
  }, []);

  useEffect(() => {
    // Load Google Identity Services script
    if (!profile && typeof window !== 'undefined') {
      const initialize = () => {
        // Small delay to ensure DOM is ready and script is fully processed
        setTimeout(() => {
          initializeGoogle();
        }, 100);
      };

      if (window.google) {
        initialize();
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      document.body.appendChild(script);

      script.onload = initialize;

      return () => {
        // Only remove if script is still in the DOM
        if (script.parentNode) {
          document.body.removeChild(script);
        }
      };
    }
  }, [profile]);

  const initializeGoogle = () => {
    const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
    
    if (!clientId) {
      console.error('Google Client ID is not configured');
      setError('Google Sign-In is not configured');
      return;
    }

    if (!window.google) {
      console.error('Google Identity Services failed to load');
      return;
    }

    window.google.accounts.id.initialize({
      client_id: clientId,
      callback: handleCredentialResponse,
    });

    const buttonDiv = document.getElementById('google-signin-button');
    if (buttonDiv) {
      window.google.accounts.id.renderButton(buttonDiv, {
        theme: 'outline',
        size: 'large',
        text: 'signin_with',
      });
    }
  };

  const handleCredentialResponse = async (response: { credential: string }) => {
    try {
      setLoading(true);
      setError(null);
      const authResponse = await exchangeGoogleIdToken(response.credential);
      setProfile(authResponse.profile);
      
      if (onSignIn) {
        onSignIn(authResponse.profile);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err);

      // Network/CORS/API-down errors are common during local dev.
      // Treat them as expected and avoid console.error noise.
      if (err instanceof TypeError && message.includes('Failed to fetch')) {
        console.warn('Authentication failed (API unreachable).');
        setError('Cannot reach the API. Is the backend running at NEXT_PUBLIC_API_BASE_URL?');
        return;
      }

      console.error('Authentication failed:', err);
      setError('Failed to sign in with Google');
    } finally {
      setLoading(false);
    }
  };

  const handleSignOut = () => {
    setProfile(null);
    signOut();
    // Redirect to home page which handles the logged-out state
    window.location.href = '/';
  };

  if (loading && !profile) {
    return <div className="text-sm text-gray-500">Loading...</div>;
  }

  if (profile) {
    return (
      <div className={`flex items-center gap-3 ${className || ''}`}>
        <span className="text-sm font-medium text-white">
          {profile.displayName || profile.email}
        </span>
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
        <div className="text-sm text-red-600 mb-2">{error}</div>
      )}
      <div id="google-signin-button"></div>
    </div>
  );
}
