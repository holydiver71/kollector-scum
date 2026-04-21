"use client";
import { useState, useEffect } from "react";
import Link from "next/link";
import { getLists, deleteList, updateList, createList, type ListSummaryDto } from "../lib/api";
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
      <div className="min-h-screen bg-transparent p-6">
        <div className="max-w-7xl mx-auto">
          <div className="text-center py-12">
            <div
              className="inline-block animate-spin rounded-full h-8 w-8 border-b-2"
              style={{ borderColor: 'var(--theme-accent)' }}
            ></div>
            <p className="mt-4" style={{ color: 'var(--theme-muted-text)' }}>Loading lists...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-transparent p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-3xl font-bold" style={{ color: 'var(--theme-foreground)' }}>My Lists</h1>
            <p className="mt-2" style={{ color: 'var(--theme-muted-text)' }}>
              Organise your music collection by theme or style
            </p>
          </div>
          {!showCreateForm && (
            <button
              onClick={() => setShowCreateForm(true)}
              className="inline-flex items-center gap-2 px-4 py-2 rounded-md btn-theme-accent"
            >
              <Plus className="h-5 w-5" />
              New List
            </button>
          )}
        </div>
        {error && (
          <div className="mb-6 bg-red-500/10 border border-red-500/20 text-red-400 px-4 py-3 rounded-md">
            {error}
          </div>
        )}

        {/* Create List Form */}
        {showCreateForm && (
          <div
            className="mb-6 rounded-lg p-6 shadow-sm border"
            style={{ background: 'var(--theme-card-bg)', borderColor: 'var(--theme-card-border)' }}
          >
            <h2 className="text-xl font-semibold mb-4" style={{ color: 'var(--theme-foreground)' }}>
              Create List
            </h2>
            <form onSubmit={handleCreateList}>
              <div className="mb-4">
                <label
                  htmlFor="newListName"
                  className="block text-sm font-medium mb-2"
                  style={{ color: 'var(--theme-foreground)' }}
                >
                  Name
                </label>
                <input
                  id="newListName"
                  type="text"
                  value={newListName}
                  onChange={(e) => setNewListName(e.target.value)}
                  placeholder="e.g., My Top 10 Metal Records"
                  className="w-full px-3 py-2 border rounded-md focus:outline-none themed-input"
                  disabled={submitting}
                  autoFocus
                  required
                />
              </div>

              <div className="flex gap-3">
                <button
                  type="submit"
                  disabled={submitting || !newListName.trim()}
                  className="inline-flex items-center gap-2 px-4 py-2 rounded-md btn-theme-accent"
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
                  className="inline-flex items-center gap-2 px-4 py-2 rounded-md btn-theme-secondary"
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
          <div
            className="rounded-lg p-12 text-center border"
            style={{ background: 'var(--theme-card-bg)', borderColor: 'var(--theme-card-border)' }}
          >
            <p className="mb-4" style={{ color: 'var(--theme-muted-text)' }}>No lists yet</p>
            {!showCreateForm && (
              <button
                onClick={() => setShowCreateForm(true)}
                className="inline-flex items-center gap-2 px-4 py-2 rounded-md btn-theme-accent"
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
                className="rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow border"
                style={{ background: 'var(--theme-card-bg)', borderColor: 'var(--theme-card-border)' }}
              >
                {editingId === list.id ? (
                  <div className="mb-3">
                    <input
                      type="text"
                      value={editingName}
                      onChange={(e) => setEditingName(e.target.value)}
                      className="w-full px-3 py-2 border rounded-md focus:outline-none themed-input mb-2"
                      disabled={submitting}
                      autoFocus
                    />
                    <div className="flex gap-2">
                      <button
                        onClick={() => handleSaveEdit(list.id)}
                        disabled={submitting || !editingName.trim()}
                        className="p-1 rounded disabled:opacity-50 transition-colors hover:bg-white/10"
                        style={{ color: 'var(--theme-accent)' }}
                        title="Save"
                      >
                        <Check className="h-4 w-4" />
                      </button>
                      <button
                        onClick={handleCancelEdit}
                        disabled={submitting}
                        className="p-1 rounded disabled:opacity-50 transition-colors text-red-400 hover:bg-red-500/10"
                        title="Cancel"
                      >
                        <X className="h-4 w-4" />
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className="mb-3">
                    <Link href={`/lists/${list.id}`}>
                      <h3
                        className="text-lg font-semibold transition-colors mb-1 hover:opacity-75"
                        style={{ color: 'var(--theme-foreground)' }}
                      >
                        {list.name}
                      </h3>
                    </Link>
                    <p className="text-sm" style={{ color: 'var(--theme-muted-text)' }}>
                      {list.releaseCount} {list.releaseCount === 1 ? "release" : "releases"}
                    </p>
                  </div>
                )}

                {editingId !== list.id && (
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleStartEdit(list)}
                      className="p-1 rounded transition-colors hover:bg-white/10"
                      style={{ color: 'var(--theme-accent)' }}
                      title="Edit"
                      disabled={showCreateForm}
                    >
                      <Edit2 className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => setDeletingId(list.id)}
                      className="p-1 rounded transition-colors text-red-400 hover:bg-red-500/10"
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
