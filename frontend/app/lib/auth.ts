// Authentication helper for Google Sign-In, JWT management, and magic link authentication

import { fetchJson } from './api';

// Store token in localStorage (fallback, prefer httpOnly cookies in production)
export const TOKEN_KEY = 'auth_token';

/**
 * Gets the stored JWT token
 */
export function getAuthToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(TOKEN_KEY);
}

/**
 * Stores the JWT token
 */
export function setAuthToken(token: string): void {
  if (typeof window === 'undefined') return;
  localStorage.setItem(TOKEN_KEY, token);
}

/**
 * Clears the stored JWT token
 */
export function clearAuthToken(): void {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(TOKEN_KEY);
}

/**
 * Checks if user is authenticated
 */
export function isAuthenticated(): boolean {
  return getAuthToken() !== null;
}

/**
 * User profile data returned from the API
 */
export interface UserProfile {
  userId: string;
  email: string;
  displayName?: string;
  selectedKollectionId?: number;
  selectedTheme: string;
  isAdmin: boolean;
}

/**
 * Response from Google authentication endpoint
 */
export interface AuthResponse {
  token: string;
  profile: UserProfile;
}

/**
 * Exchanges a Google ID token for a JWT token
 * @param idToken The Google ID token
 * @returns Authentication response with JWT token and user profile
 */
export async function exchangeGoogleIdToken(idToken: string): Promise<AuthResponse> {
  const response = await fetchJson<AuthResponse>('/api/auth/google', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ idToken }),
  });

  // Store the token
  setAuthToken(response.token);

  return response;
}

/**
 * Requests a magic link to be sent to the provided email address.
 * Only invited users will actually receive an email; the response is
 * deliberately vague to prevent email enumeration.
 * @param email The email address to send the magic link to
 */
export async function requestMagicLink(email: string): Promise<void> {
  await fetchJson<{ message: string }>('/api/auth/magic-link/request', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ email }),
  });
}

/**
 * Verifies a magic link token and signs the user in.
 * @param token The token from the magic link URL
 * @returns Authentication response with JWT token and user profile
 */
export async function verifyMagicLink(token: string): Promise<AuthResponse> {
  const response = await fetchJson<AuthResponse>('/api/auth/magic-link/verify', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ token }),
  });

  // Store the token
  setAuthToken(response.token);

  return response;
}

/**
 * Gets the current user's profile
 * @returns The user profile or null if not authenticated
 */
export async function getUserProfile(): Promise<UserProfile | null> {
  if (!isAuthenticated()) {
    return null;
  }

  try {
    const profile = await fetchJson<UserProfile>('/api/profile', {
      swallowErrors: true,
    });
    return profile;
  } catch (error) {
    console.error('Failed to get user profile:', error);
    clearAuthToken();
    return null;
  }
}

/**
 * Updates the user's profile
 * @param selectedKollectionId The selected kollection ID
 * @param selectedTheme The selected UI theme name
 * @returns The updated user profile
 */
export async function updateUserProfile(
  selectedKollectionId: number | null,
  selectedTheme?: string
): Promise<UserProfile> {
  return fetchJson<UserProfile>('/api/profile', {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ selectedKollectionId, selectedTheme }),
  });
}

/**
 * Signs out the user.
 * Clears all user-specific localStorage data, notifies listeners via the
 * 'authChanged' event, then redirects to the home page.
 */
export function signOut(): void {
  clearAuthToken();
  if (typeof window !== 'undefined') {
    // Clear all user-specific data so a subsequent sign-in as a different
    // user never sees stale cache from the previous session.
    const userKeys = [
      'kollectionId',
      'user_profile',
    ];
    userKeys.forEach((key) => localStorage.removeItem(key));

    // Notify all listeners (CollectionContext, Header, Dashboard, etc.) that
    // the auth state has changed BEFORE navigating so they can reset cleanly.
    window.dispatchEvent(new Event('authChanged'));

    // Hard-navigate to root so the entire React tree remounts fresh.
    window.location.href = '/';
  }
}
