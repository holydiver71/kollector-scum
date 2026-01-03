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
  });
}
