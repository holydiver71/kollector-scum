"use client";

import { useEffect, useState } from 'react';
import {
  getInvitations,
  createInvitation,
  deleteInvitation,
  getUsers,
  revokeUserAccess,
  type UserInvitation,
  type UserAccess,
} from '../lib/admin';

export default function AdminDashboard() {
  const [invitations, setInvitations] = useState<UserInvitation[]>([]);
  const [users, setUsers] = useState<UserAccess[]>([]);
  const [newEmail, setNewEmail] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [invitationsData, usersData] = await Promise.all([
        getInvitations(),
        getUsers(),
      ]);
      setInvitations(invitationsData);
      setUsers(usersData);
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
      setSuccessMessage(`Invitation sent to ${newEmail}`);
      await loadData();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create invitation';
      setError(message);
    }
  };

  const handleDeleteInvitation = async (id: number, email: string) => {
    if (!confirm(`Are you sure you want to revoke the invitation for ${email}?`)) {
      return;
    }

    try {
      setError(null);
      setSuccessMessage(null);
      await deleteInvitation(id);
      setSuccessMessage(`Invitation for ${email} revoked`);
      await loadData();
    } catch (err) {
      setError('Failed to delete invitation');
    }
  };

  const handleRevokeAccess = async (userId: string, email: string) => {
    if (!confirm(`Are you sure you want to revoke access for ${email}? This will delete all their data.`)) {
      return;
    }

    try {
      setError(null);
      setSuccessMessage(null);
      await revokeUserAccess(userId);
      setSuccessMessage(`Access revoked for ${email}`);
      await loadData();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to revoke access';
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

      {/* Invite User Section */}
      <div className="mb-8 bg-gray-800 rounded-lg p-6 border border-gray-700">
        <h2 className="text-xl font-semibold mb-4 text-white">Invite New User</h2>
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
            Send Invitation
          </button>
        </form>
      </div>

      {/* Pending Invitations */}
      <div className="mb-8 bg-gray-800 rounded-lg p-6 border border-gray-700">
        <h2 className="text-xl font-semibold mb-4 text-white">
          Pending Invitations ({invitations.filter(i => !i.isUsed).length})
        </h2>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-gray-700">
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Email</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Created</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Status</th>
                <th className="text-right py-3 px-4 text-gray-300 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {invitations.length === 0 ? (
                <tr>
                  <td colSpan={4} className="text-center py-8 text-gray-500">
                    No invitations found
                  </td>
                </tr>
              ) : (
                invitations.map((invitation) => (
                  <tr key={invitation.id} className="border-b border-gray-700/50 hover:bg-gray-700/30">
                    <td className="py-3 px-4 text-white">{invitation.email}</td>
                    <td className="py-3 px-4 text-gray-400">
                      {new Date(invitation.createdAt).toLocaleDateString()}
                    </td>
                    <td className="py-3 px-4">
                      {invitation.isUsed ? (
                        <span className="inline-block px-2 py-1 text-xs bg-green-900/50 text-green-300 rounded">
                          Used
                        </span>
                      ) : (
                        <span className="inline-block px-2 py-1 text-xs bg-yellow-900/50 text-yellow-300 rounded">
                          Pending
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-right">
                      {!invitation.isUsed && (
                        <button
                          onClick={() => handleDeleteInvitation(invitation.id, invitation.email)}
                          className="text-red-400 hover:text-red-300 text-sm"
                        >
                          Revoke
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Active Users */}
      <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
        <h2 className="text-xl font-semibold mb-4 text-white">
          Active Users ({users.length})
        </h2>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-gray-700">
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Email</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Display Name</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Joined</th>
                <th className="text-left py-3 px-4 text-gray-300 font-medium">Role</th>
                <th className="text-right py-3 px-4 text-gray-300 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-center py-8 text-gray-500">
                    No users found
                  </td>
                </tr>
              ) : (
                users.map((user) => (
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
                    <td className="py-3 px-4 text-right">
                      {!user.isAdmin && (
                        <button
                          onClick={() => handleRevokeAccess(user.userId, user.email)}
                          className="text-red-400 hover:text-red-300 text-sm"
                        >
                          Revoke Access
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
