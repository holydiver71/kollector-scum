"use client";
import { useState, useEffect } from "react";
import { X, Plus, Check } from "lucide-react";
import { getLists, addReleaseToList, createList, getListsForRelease, type ListSummaryDto } from "../lib/api";

interface AddToListDialogProps {
  releaseId: number;
  releaseTitle: string;
  isOpen: boolean;
  onClose: () => void;
}

export function AddToListDialog({ releaseId, releaseTitle, isOpen, onClose }: AddToListDialogProps) {
  const [lists, setLists] = useState<ListSummaryDto[]>([]);
  const [releaseLists, setReleaseLists] = useState<number[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showNewListForm, setShowNewListForm] = useState(false);
  const [newListName, setNewListName] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [addingToList, setAddingToList] = useState<number | null>(null);

  useEffect(() => {
    if (isOpen) {
      loadLists();
    }
  }, [isOpen, releaseId]);

  const loadLists = async () => {
    try {
      setLoading(true);
      setError(null);
      const [allLists, releaseListsData] = await Promise.all([
        getLists(),
        getListsForRelease(releaseId),
      ]);
      setLists(allLists);
      setReleaseLists(releaseListsData.map(l => l.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load lists");
    } finally {
      setLoading(false);
    }
  };

  const handleAddToList = async (listId: number) => {
    try {
      setAddingToList(listId);
      await addReleaseToList(listId, releaseId);
      setReleaseLists(prev => [...prev, listId]);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add release to list");
    } finally {
      setAddingToList(null);
    }
  };

  const handleCreateList = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newListName.trim()) return;

    try {
      setSubmitting(true);
      setError(null);
      const newList = await createList({ name: newListName.trim() });
      await addReleaseToList(newList.id, releaseId);
      setLists(prev => [newList, ...prev]);
      setReleaseLists(prev => [...prev, newList.id]);
      setNewListName("");
      setShowNewListForm(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create list");
    } finally {
      setSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full max-h-[80vh] overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <h2 className="text-lg font-bold text-gray-900">Add to List</h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
            aria-label="Close dialog"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-4">
          {/* Release Title */}
          <div className="mb-4 p-3 bg-gray-50 rounded-lg">
            <p className="text-sm text-gray-600 mb-1">Adding:</p>
            <p className="font-semibold text-gray-900">{releaseTitle}</p>
          </div>

          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm text-red-600">{error}</p>
            </div>
          )}

          {/* Create New List Button */}
          {!showNewListForm && (
            <button
              onClick={() => setShowNewListForm(true)}
              className="w-full mb-4 flex items-center justify-center gap-2 px-4 py-3 bg-[#D93611] text-white rounded-lg hover:bg-[#C02F0F] transition-colors font-semibold"
            >
              <Plus className="w-5 h-5" />
              Create New List
            </button>
          )}

          {/* New List Form */}
          {showNewListForm && (
            <form onSubmit={handleCreateList} className="mb-4 p-4 bg-gray-50 rounded-lg">
              <label htmlFor="newListName" className="block text-sm font-semibold text-gray-700 mb-2">
                New List Name
              </label>
              <input
                id="newListName"
                type="text"
                value={newListName}
                onChange={(e) => setNewListName(e.target.value)}
                placeholder="e.g., My Top 10 Metal Records"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-[#D93611] focus:border-transparent mb-3"
                disabled={submitting}
                autoFocus
              />
              <div className="flex gap-2">
                <button
                  type="submit"
                  disabled={submitting || !newListName.trim()}
                  className="flex-1 px-4 py-2 bg-[#D93611] text-white rounded-lg hover:bg-[#C02F0F] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-semibold"
                >
                  {submitting ? "Creating..." : "Create & Add"}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setShowNewListForm(false);
                    setNewListName("");
                  }}
                  disabled={submitting}
                  className="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 disabled:opacity-50 transition-colors"
                >
                  Cancel
                </button>
              </div>
            </form>
          )}

          {/* Lists */}
          {loading ? (
            <div className="text-center py-8 text-gray-500">Loading lists...</div>
          ) : lists.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <p>No lists yet. Create your first list!</p>
            </div>
          ) : (
            <div className="space-y-2">
              <p className="text-sm font-semibold text-gray-700 mb-2">Your Lists:</p>
              {lists.map((list) => {
                const isInList = releaseLists.includes(list.id);
                const isAdding = addingToList === list.id;

                return (
                  <button
                    key={list.id}
                    onClick={() => !isInList && !isAdding && handleAddToList(list.id)}
                    disabled={isInList || isAdding}
                    className={`w-full flex items-center justify-between p-3 rounded-lg border transition-all ${
                      isInList
                        ? "bg-green-50 border-green-200 cursor-default"
                        : "bg-white border-gray-200 hover:border-[#D93611] hover:bg-gray-50"
                    } ${isAdding ? "opacity-50 cursor-wait" : ""}`}
                  >
                    <div className="flex-1 text-left">
                      <p className="font-semibold text-gray-900">{list.name}</p>
                      <p className="text-xs text-gray-500">
                        {list.releaseCount} {list.releaseCount === 1 ? "release" : "releases"}
                      </p>
                    </div>
                    {isInList ? (
                      <Check className="w-5 h-5 text-green-600 flex-shrink-0" />
                    ) : isAdding ? (
                      <div className="w-5 h-5 border-2 border-[#D93611] border-t-transparent rounded-full animate-spin flex-shrink-0" />
                    ) : (
                      <Plus className="w-5 h-5 text-gray-400 flex-shrink-0" />
                    )}
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-gray-200">
          <button
            onClick={onClose}
            className="w-full px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-colors font-semibold"
          >
            Done
          </button>
        </div>
      </div>
    </div>
  );
}
