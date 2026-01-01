"use client";
import { useState, useEffect } from "react";
import Link from "next/link";
import { getLists, deleteList, updateList, createList, type ListSummaryDto } from "../lib/api";
import { LoadingSpinner } from "../components/LoadingComponents";
import { Plus, Edit2, Trash2, Check, X } from "lucide-react";
import { ConfirmDialog } from "../components/ConfirmDialog";

export default function ListsPage() {
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
      // Convert ListDto to ListSummaryDto
      const newListSummary: ListSummaryDto = {
        id: newList.id,
        name: newList.name,
        releaseCount: 0, // New list has no releases
        createdAt: newList.createdAt,
        lastModified: newList.lastModified
      };
      setLists(prev => [newListSummary, ...prev]);
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
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-7xl mx-auto">
          <div className="text-center py-12">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-[#D93611]"></div>
            <p className="mt-4 text-gray-600">Loading lists...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">My Lists</h1>
            <p className="mt-2 text-gray-600">
              Organize your music collection by theme or style
            </p>
          </div>
          {!showCreateForm && (
            <button
              onClick={() => setShowCreateForm(true)}
              className="inline-flex items-center gap-2 px-4 py-2 bg-[#D93611] text-white rounded-md hover:bg-[#B82D0E] transition-colors"
            >
              <Plus className="h-5 w-5" />
              New List
            </button>
          )}
        </div>
        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md">
            {error}
          </div>
        )}

        {/* Create List Form */}
        {showCreateForm && (
          <div className="mb-6 bg-white border border-gray-200 rounded-lg p-6 shadow-sm">
            <h2 className="text-xl font-semibold mb-4">
              Create List
            </h2>
            <form onSubmit={handleCreateList}>
              <div className="mb-4">
                <label
                  htmlFor="newListName"
                  className="block text-sm font-medium text-gray-700 mb-2"
                >
                  Name
                </label>
                <input
                  id="newListName"
                  type="text"
                  value={newListName}
                  onChange={(e) => setNewListName(e.target.value)}
                  placeholder="e.g., My Top 10 Metal Records"
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#D93611]"
                  disabled={submitting}
                  autoFocus
                  required
                />
              </div>

              <div className="flex gap-3">
                <button
                  type="submit"
                  disabled={submitting || !newListName.trim()}
                  className="inline-flex items-center gap-2 px-4 py-2 bg-[#D93611] text-white rounded-md hover:bg-[#B82D0E] transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <Check className="h-5 w-5" />
                  {submitting ? "Creating..." : "Save"}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setShowCreateForm(false);
                    setNewListName("");
                  }}
                  disabled={submitting}
                  className="inline-flex items-center gap-2 px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300 transition-colors disabled:opacity-50"
                >
                  <X className="h-5 w-5" />
                  Cancel
                </button>
              </div>
            </form>
          </div>
        )}

        {/* List */}
        {lists.length === 0 ? (
          <div className="bg-white border border-gray-200 rounded-lg p-12 text-center">
            <p className="text-gray-500 mb-4">No lists yet</p>
            {!showCreateForm && (
              <button
                onClick={() => setShowCreateForm(true)}
                className="inline-flex items-center gap-2 px-4 py-2 bg-[#D93611] text-white rounded-md hover:bg-[#B82D0E] transition-colors"
              >
                <Plus className="h-5 w-5" />
                Create Your First List
              </button>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {lists.map((list) => (
              <div
                key={list.id}
                className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow"
              >
                {editingId === list.id ? (
                  <div className="mb-3">
                    <input
                      type="text"
                      value={editingName}
                      onChange={(e) => setEditingName(e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#D93611] mb-2"
                      disabled={submitting}
                      autoFocus
                    />
                    <div className="flex gap-2">
                      <button
                        onClick={() => handleSaveEdit(list.id)}
                        disabled={submitting || !editingName.trim()}
                        className="p-1 text-blue-600 hover:bg-blue-50 rounded disabled:opacity-50 transition-colors"
                        title="Save"
                      >
                        <Check className="h-4 w-4" />
                      </button>
                      <button
                        onClick={handleCancelEdit}
                        disabled={submitting}
                        className="p-1 text-red-600 hover:bg-red-50 rounded disabled:opacity-50 transition-colors"
                        title="Cancel"
                      >
                        <X className="h-4 w-4" />
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className="mb-3">
                    <Link href={`/lists/${list.id}`}>
                      <h3 className="text-lg font-semibold text-gray-900 hover:text-[#D93611] transition-colors mb-1">
                        {list.name}
                      </h3>
                    </Link>
                    <p className="text-sm text-gray-500">
                      {list.releaseCount} {list.releaseCount === 1 ? "release" : "releases"}
                    </p>
                  </div>
                )}

                {editingId !== list.id && (
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleStartEdit(list)}
                      className="p-1 text-blue-600 hover:bg-blue-50 rounded"
                      title="Edit"
                      disabled={showCreateForm}
                    >
                      <Edit2 className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => setDeletingId(list.id)}
                      className="p-1 text-red-600 hover:bg-red-50 rounded"
                      title="Delete"
                      disabled={showCreateForm}
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}

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
    </div>
  );
}
