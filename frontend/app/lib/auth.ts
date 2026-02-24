// Authentication helper for Google Sign-In and JWT management

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
 * Signs out the user
 */
export function signOut(): void {
  clearAuthToken();
  // Clear other user-specific localStorage items
  if (typeof window !== 'undefined') {
    localStorage.removeItem('kollectionId');
    window.location.reload();
  }
}
