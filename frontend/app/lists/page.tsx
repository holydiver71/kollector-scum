"use client";
import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { getLists, deleteList, updateList, createList, type ListSummaryDto } from "../lib/api";
import { LoadingSpinner } from "../components/LoadingComponents";
import { Plus, Edit2, Trash2, Check, X } from "lucide-react";
import { ConfirmDialog } from "../components/ConfirmDialog";

export default function ListsPage() {
  const router = useRouter();
  const [lists, setLists] = useState<ListSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editingName, setEditingName] = useState("");
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newListName, setNewListName] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    loadLists();
  }, []);

  const loadLists = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getLists();
      setLists(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load lists");
    } finally {
      setLoading(false);
    }
  };

  const handleCreateList = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newListName.trim()) return;

    try {
      setSubmitting(true);
      setError(null);
      const newList = await createList({ name: newListName.trim() });
      setLists(prev => [newList, ...prev]);
      setNewListName("");
      setShowCreateForm(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create list");
    } finally {
      setSubmitting(false);
    }
  };

  const handleStartEdit = (list: ListSummaryDto) => {
    setEditingId(list.id);
    setEditingName(list.name);
  };

  const handleCancelEdit = () => {
    setEditingId(null);
    setEditingName("");
  };

  const handleSaveEdit = async (listId: number) => {
    if (!editingName.trim()) {
      handleCancelEdit();
      return;
    }

    try {
      setSubmitting(true);
      setError(null);
      const updatedList = await updateList(listId, { name: editingName.trim() });
      setLists(prev => prev.map(l => l.id === listId ? { ...l, name: updatedList.name, lastModified: updatedList.lastModified } : l));
      setEditingId(null);
      setEditingName("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update list");
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeleteList = async () => {
    if (!deletingId) return;

    try {
      await deleteList(deletingId);
      setLists(prev => prev.filter(l => l.id !== deletingId));
      setDeletingId(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete list");
      setDeletingId(null);
    }
  };

  const getDeletingListName = () => {
    const list = lists.find(l => l.id === deletingId);
    return list?.name || "this list";
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-white flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-white">
      {/* Header */}
      <div className="border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-8 py-6">
          <div className="flex items-center justify-between">
            <h1 className="text-3xl font-bold text-gray-900">My Lists</h1>
            <button
              onClick={() => setShowCreateForm(true)}
              className="inline-flex items-center gap-2 px-4 py-2 bg-[#D93611] text-white rounded-lg hover:bg-[#C02F0F] transition-colors font-semibold"
            >
              <Plus className="w-5 h-5" />
              Create List
            </button>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-8 py-8">
        {error && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-sm text-red-600">{error}</p>
          </div>
        )}

        {/* Create List Form */}
        {showCreateForm && (
          <div className="mb-6 p-6 bg-gray-50 rounded-lg border border-gray-200">
            <form onSubmit={handleCreateList}>
              <label htmlFor="newListName" className="block text-sm font-semibold text-gray-700 mb-2">
                List Name
              </label>
              <input
                id="newListName"
                type="text"
                value={newListName}
                onChange={(e) => setNewListName(e.target.value)}
                placeholder="e.g., My Top 10 Metal Records"
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#D93611] focus:border-transparent mb-4"
                disabled={submitting}
                autoFocus
              />
              <div className="flex gap-2">
                <button
                  type="submit"
                  disabled={submitting || !newListName.trim()}
                  className="px-4 py-2 bg-[#D93611] text-white rounded-lg hover:bg-[#C02F0F] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-semibold"
                >
                  {submitting ? "Creating..." : "Create List"}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setShowCreateForm(false);
                    setNewListName("");
                  }}
                  disabled={submitting}
                  className="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 disabled:opacity-50 transition-colors"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Lists */}
        {lists.length === 0 ? (
          <div className="text-center py-16">
            <p className="text-gray-500 text-lg mb-4">You haven&apos;t created any lists yet.</p>
            <p className="text-gray-400 mb-8">
              Create lists to organize your music collection by theme, genre, or any other way you like!
            </p>
            <button
              onClick={() => setShowCreateForm(true)}
              className="inline-flex items-center gap-2 px-6 py-3 bg-[#D93611] text-white rounded-lg hover:bg-[#C02F0F] transition-colors font-semibold"
            >
              <Plus className="w-5 h-5" />
              Create Your First List
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {lists.map((list) => (
              <div
                key={list.id}
                className="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow"
              >
                {editingId === list.id ? (
                  <div className="mb-4">
                    <input
                      type="text"
                      value={editingName}
                      onChange={(e) => setEditingName(e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#D93611] focus:border-transparent"
                      disabled={submitting}
                      autoFocus
                    />
                    <div className="flex gap-2 mt-2">
                      <button
                        onClick={() => handleSaveEdit(list.id)}
                        disabled={submitting || !editingName.trim()}
                        className="flex items-center justify-center w-8 h-8 rounded bg-green-500 text-white hover:bg-green-600 disabled:opacity-50 transition-colors"
                        title="Save"
                      >
                        <Check className="w-4 h-4" />
                      </button>
                      <button
                        onClick={handleCancelEdit}
                        disabled={submitting}
                        className="flex items-center justify-center w-8 h-8 rounded bg-red-500 text-white hover:bg-red-600 disabled:opacity-50 transition-colors"
                        title="Cancel"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                ) : (
                  <Link href={`/lists/${list.id}`} className="block mb-4">
                    <h2 className="text-xl font-bold text-gray-900 hover:text-[#D93611] transition-colors mb-2">
                      {list.name}
                    </h2>
                    <p className="text-sm text-gray-600">
                      {list.releaseCount} {list.releaseCount === 1 ? "release" : "releases"}
                    </p>
                  </Link>
                )}

                <div className="flex items-center justify-between pt-4 border-t border-gray-200">
                  <p className="text-xs text-gray-500">
                    Updated {new Date(list.lastModified).toLocaleDateString()}
                  </p>
                  {editingId !== list.id && (
                    <div className="flex gap-2">
                      <button
                        onClick={() => handleStartEdit(list)}
                        className="p-2 text-gray-600 hover:text-[#D93611] transition-colors"
                        title="Rename list"
                      >
                        <Edit2 className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => setDeletingId(list.id)}
                        className="p-2 text-gray-600 hover:text-red-600 transition-colors"
                        title="Delete list"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <ConfirmDialog
        isOpen={!!deletingId}
        title="Delete List"
        message={`Are you sure you want to delete "${getDeletingListName()}"? This action cannot be undone.`}
        confirmLabel="Delete"
        isDangerous={true}
        onConfirm={handleDeleteList}
        onCancel={() => setDeletingId(null)}
      />
    </div>
  );
}
