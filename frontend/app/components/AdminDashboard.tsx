"use client";

import { useEffect, useState } from 'react';
import {
  getInvitations,
  createInvitation,
  deleteInvitation,
  activateInvitation,
  getUsers,
  revokeUserAccess,
  impersonateUser,
  getAllUserCollectionCounts,
  type UserInvitation,
  type UserAccess,
} from '../lib/admin';
import { useImpersonation } from '../contexts/ImpersonationContext';
import { DeleteUserCollectionDialog } from './DeleteUserCollectionDialog';

export default function AdminDashboard() {
  const [invitations, setInvitations] = useState<UserInvitation[]>([]);
  const [users, setUsers] = useState<UserAccess[]>([]);
  const [newEmail, setNewEmail] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [deleteCollectionTarget, setDeleteCollectionTarget] = useState<{ userId: string; email: string } | null>(null);
  const [collectionCounts, setCollectionCounts] = useState<Map<string, number>>(new Map());
  const { startImpersonation } = useImpersonation();

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [invitationsData, usersData, countsData] = await Promise.all([
        getInvitations(),
        getUsers(),
        getAllUserCollectionCounts(),
      ]);
      setInvitations(invitationsData);
      setUsers(usersData);
      setCollectionCounts(new Map(countsData.map(c => [c.userId, c.count])));
    } catch (err) {
      setError('Failed to load data. Please try again.');
      console.error('Error loading admin data:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateInvitation = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newEmail) return;

    try {
      setError(null);
      setSuccessMessage(null);
      await createInvitation(newEmail);
      setNewEmail('');
      setSuccessMessage(`${newEmail} registered successfully`);
      await loadData();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create invitation';
      setError(message);
    }
  };

  const handleDeleteInvitation = async (id: number, email: string) => {
    if (!confirm(`Are you sure you want to remove registration for ${email}?`)) {
      return;
    }

    try {
      setError(null);
      setSuccessMessage(null);
      await deleteInvitation(id);
      setSuccessMessage(`Registration for ${email} removed`);
      await loadData();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete invitation';
      setError(message);
      console.error('Error deleting invitation:', err);
    }
  };

  const handleRevokeAccess = async (userId: string, email: string) => {
    if (!confirm(`Are you sure you want to deactivate ${email}? Their collection will be preserved and can be restored by reactivating their account.`)) {
      return;
    }

    try {
      setError(null);
      setSuccessMessage(null);
      await revokeUserAccess(userId);
      setSuccessMessage(`Deactivated ${email}`);
      await loadData();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to deactivate user';
      setError(message);
    }
  };

  const handleActivateRegistration = async (id: number, email: string) => {
    if (!confirm(`Are you sure you want to activate registration for ${email}?`)) {
      return;
    }

    try {
      setError(null);
      setSuccessMessage(null);
      await activateInvitation(id);
      setSuccessMessage(`Registration activated for ${email}`);
      await loadData();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to activate registration';
      setError(message);
    }
  };

  const handleDeleteUserCollection = (userId: string, email: string) => {
    setDeleteCollectionTarget({ userId, email });
  };

  const handleImpersonate = async (userId: string) => {
    try {
      setError(null);
      const user = await impersonateUser(userId);
      startImpersonation(user);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to impersonate user';
      setError(message);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <div className="text-gray-400">Loading...</div>
      </div>
    );
  }

  const activeUserEmails = new Set(users.filter(u => u.isActive).map(u => u.email.toLowerCase()));

  return (
    <div className="max-w-6xl mx-auto p-6">
      <h1 className="text-3xl font-bold mb-6 text-white">Admin Dashboard</h1>

      {error && (
        <div className="mb-4 p-4 bg-red-900/50 border border-red-500 rounded text-red-200">
          {error}
        </div>
      )}

      {successMessage && (
        <div className="mb-4 p-4 bg-green-900/50 border border-green-500 rounded text-green-200">
          {successMessage}
        </div>
      )}

      {/* Register User Section */}
      <div className="mb-8 bg-gray-800 rounded-lg p-6 border border-gray-700">
        <h2 className="text-xl font-semibold mb-4 text-white">Register New User</h2>
        <form onSubmit={handleCreateInvitation} className="flex gap-3">
          <input
            type="email"
            value={newEmail}
            onChange={(e) => setNewEmail(e.target.value)}
            placeholder="user@example.com"
            className="flex-1 px-4 py-2 bg-gray-700 border border-gray-600 rounded text-white placeholder-gray-400 focus:outline-none focus:border-blue-500"
            required
          />
          <button
            type="submit"
            className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded font-medium transition-colors"
          >
            Register User
          </button>
        </form>
      </div>

      {/* Pending Registrations */}
      <div className="mb-8 bg-gray-800 rounded-lg p-6 border border-gray-700">
        <h2 className="text-xl font-semibold mb-4 text-white">
          Registrations ({invitations.filter(i => !i.isUsed).length} active)
        </h2>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-gray-700">
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Email</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Created</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Status</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Releases</th>
                <th className="text-right py-3 px-4 text-gray-300 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {invitations.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-center py-8 text-gray-500">
                    No registrations found
                  </td>
                </tr>
              ) : (
                invitations.map((invitation) => {
                  const userExists = activeUserEmails.has(invitation.email.toLowerCase());
                  const inactiveUser = users.find(u => u.email.toLowerCase() === invitation.email.toLowerCase() && !u.isActive);
                  const isDeactivated = invitation.isUsed && (!!inactiveUser || !userExists);
                  const invitedUser = users.find(u => u.email.toLowerCase() === invitation.email.toLowerCase());
                  const invitedUserCount = invitedUser ? (collectionCounts.get(invitedUser.userId) ?? 0) : null;

                  return (
                  <tr key={invitation.id} className="border-b border-gray-700/50 hover:bg-gray-700/30">
                    <td className="py-3 px-4 text-white">{invitation.email}</td>
                    <td className="py-3 px-4 text-gray-400">
                      {new Date(invitation.createdAt).toLocaleDateString()}
                    </td>
                    <td className="py-3 px-4">
                      {isDeactivated ? (
                        <span className="inline-block px-2 py-1 text-xs bg-red-900/50 text-red-300 rounded">
                          Deactivated
                        </span>
                      ) : invitation.isUsed ? (
                        <span className="inline-block px-2 py-1 text-xs bg-green-900/50 text-green-300 rounded">
                          Activated
                        </span>
                      ) : (
                        <span className="inline-block px-2 py-1 text-xs bg-yellow-900/50 text-yellow-300 rounded">
                          Active
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-gray-300 tabular-nums">
                      {invitedUserCount !== null ? invitedUserCount : <span className="text-gray-600">—</span>}
                    </td>
                    <td className="py-3 px-4 text-right">
                      {isDeactivated ? (
                        <div className="flex justify-end gap-4">
                          <button
                            onClick={() => handleActivateRegistration(invitation.id, invitation.email)}
                            className="text-blue-400 hover:text-blue-300 text-sm"
                          >
                            Activate
                          </button>
                          {inactiveUser && (collectionCounts.get(inactiveUser.userId) ?? 0) > 0 && (
                            <button
                              onClick={() => handleDeleteUserCollection(inactiveUser.userId, inactiveUser.email)}
                              className="text-orange-400 hover:text-orange-300 text-sm"
                              title="Permanently delete all releases and images for this user"
                            >
                              Delete Collection
                            </button>
                          )}
                          <button
                            onClick={() => handleDeleteInvitation(invitation.id, invitation.email)}
                            className="text-red-400 hover:text-red-300 text-sm"
                          >
                            Remove
                          </button>
                        </div>
                      ) : !invitation.isUsed ? (
                        <button
                          onClick={() => handleDeleteInvitation(invitation.id, invitation.email)}
                          className="text-red-400 hover:text-red-300 text-sm"
                        >
                          Remove
                        </button>
                      ) : null}
                    </td>
                  </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Active Users */}
      <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
        <h2 className="text-xl font-semibold mb-4 text-white">
          Active Users ({users.filter(u => u.isActive).length})
        </h2>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-gray-700">
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Email</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Display Name</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Joined</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Role</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Releases</th>
                <th className="text-right py-3 px-4 text-gray-300 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.filter(u => u.isActive).length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-8 text-gray-500">
                    No users found
                  </td>
                </tr>
              ) : (
                users.filter(u => u.isActive).map((user) => (
                  <tr key={user.userId} className="border-b border-gray-700/50 hover:bg-gray-700/30">
                    <td className="py-3 px-4 text-white">{user.email}</td>
                    <td className="py-3 px-4 text-gray-400">{user.displayName || '-'}</td>
                    <td className="py-3 px-4 text-gray-400">
                      {new Date(user.createdAt).toLocaleDateString()}
                    </td>
                    <td className="py-3 px-4">
                      {user.isAdmin ? (
                        <span className="inline-block px-2 py-1 text-xs bg-purple-900/50 text-purple-300 rounded">
                          Admin
                        </span>
                      ) : (
                        <span className="inline-block px-2 py-1 text-xs bg-gray-700 text-gray-300 rounded">
                          User
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-gray-300 tabular-nums">
                      {collectionCounts.get(user.userId) ?? 0}
                    </td>
                    <td className="py-3 px-4 text-right">
                      {!user.isAdmin && (
                        <div className="flex justify-end gap-4">
                          <button
                            onClick={() => handleImpersonate(user.userId)}
                            className="text-blue-400 hover:text-blue-300 text-sm font-medium"
                            disabled={loading}
                          >
                            Impersonate
                          </button>
                          <button
                            onClick={() => handleRevokeAccess(user.userId, user.email)}
                            className="text-red-400 hover:text-red-300 text-sm"
                          >
                            Deactivate
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Delete User Collection confirmation dialog */}
      {deleteCollectionTarget && (
        <DeleteUserCollectionDialog
          isOpen={true}
          userId={deleteCollectionTarget.userId}
          userEmail={deleteCollectionTarget.email}
          onDeleted={async (deletedCount) => {
            setDeleteCollectionTarget(null);
            setSuccessMessage(`Deleted ${deletedCount} release(s) from ${deleteCollectionTarget.email}'s collection.`);
            await loadData();
          }}
          onCancel={() => setDeleteCollectionTarget(null)}
        />
      )}
    </div>
  );
}
