// Admin API helpers for invitation and user management

import { fetchJson } from './api';

export interface UserInvitation {
  id: number;
  email: string;
  createdAt: string;
  isUsed: boolean;
  usedAt?: string;
}

export interface UserAccess {
  userId: string;
  email: string;
  displayName?: string;
  createdAt: string;
  isAdmin: boolean;
  isActive: boolean;
}

/**
 * Gets all invitations (admin only)
 */
export async function getInvitations(): Promise<UserInvitation[]> {
  return fetchJson<UserInvitation[]>('/api/admin/invitations');
}

/**
 * Creates a new invitation (admin only)
 */
export async function createInvitation(email: string): Promise<UserInvitation> {
  return fetchJson<UserInvitation>('/api/admin/invitations', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ email }),
  });
}

/**
 * Deletes an invitation (admin only)
 */
export async function deleteInvitation(id: number): Promise<void> {
  await fetchJson(`/api/admin/invitations/${id}`, {
    method: 'DELETE',
    parse: false, // DELETE returns 204 No Content, no JSON body to parse
  });
}

/**
 * Activates (re-enables) a previously used invitation (admin only)
 */
export async function activateInvitation(id: number): Promise<UserInvitation> {
  return fetchJson<UserInvitation>(`/api/admin/invitations/${id}/activate`, {
    method: 'POST',
  });
}

/**
 * Gets all users with access (admin only)
 */
export async function getUsers(): Promise<UserAccess[]> {
  return fetchJson<UserAccess[]>('/api/admin/users');
}

/**
 * Revokes a user's access (admin only)
 */
export async function revokeUserAccess(userId: string): Promise<void> {
  await fetchJson(`/api/admin/users/${userId}`, {
    method: 'DELETE',
    parse: false, // DELETE returns 204 No Content, no JSON body to parse
  });
}

export interface DeleteUserCollectionResponse {
  albumsDeleted: number;
  success: boolean;
  message?: string;
}

/**
 * Permanently deletes all music releases for a deactivated user (admin only).
 * The user account is preserved. This action is irreversible.
 */
export async function deleteUserCollection(userId: string): Promise<DeleteUserCollectionResponse> {
  return fetchJson<DeleteUserCollectionResponse>(`/api/admin/users/${userId}/collection`, {
    method: 'DELETE',
  });
}

export interface UserCollectionCount {
  userId: string;
  email: string;
  count: number;
}

/**
 * Returns the number of releases in a user's collection (admin only).
 * Used to populate the deletion confirmation dialog before destructive action.
 */
export async function getUserCollectionCount(userId: string): Promise<UserCollectionCount> {
  return fetchJson<UserCollectionCount>(`/api/admin/users/${userId}/collection-count`);
}

export interface UserCollectionCountEntry {
  userId: string;
  count: number;
}

/**
 * Returns the release counts for all registered users in a single request (admin only).
 * Returns an array of { userId, count } objects; users with no releases have count 0.
 */
export async function getAllUserCollectionCounts(): Promise<UserCollectionCountEntry[]> {
  return fetchJson<UserCollectionCountEntry[]>('/api/admin/users/collection-counts');
}

export interface ImpersonationUser {
  userId: string;
  email: string;
  displayName?: string;
}

/**
 * Initiates admin impersonation of a non-admin user.
 * Returns the user's basic info for display in the impersonation banner.
 */
export async function impersonateUser(userId: string): Promise<ImpersonationUser> {
  return fetchJson<ImpersonationUser>(`/api/admin/impersonate/${userId}`, {
    method: 'POST',
  });
}
