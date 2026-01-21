"use client";
import { useState, useEffect, useCallback } from "react";
import { createPortal } from "react-dom";
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
  const [portalContainer, setPortalContainer] = useState<HTMLElement | null>(null);

  const loadLists = useCallback(async () => {
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
  }, [releaseId]);

  useEffect(() => {
    if (isOpen) {
      loadLists();
    }
  }, [isOpen, releaseId, loadLists]);

  // Create a container element on mount for the portal so the modal is rendered
  // at the document body level and is not affected by transformed ancestors.
  useEffect(() => {
    const el = document.createElement('div');
    document.body.appendChild(el);
    setPortalContainer(el);
    return () => {
      try {
        document.body.removeChild(el);
      } catch {
        // ignore
      }
      setPortalContainer(null);
    };
  }, []);
 

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
      // Convert ListDto to ListSummaryDto
      const newListSummary: ListSummaryDto = {
        id: newList.id,
        name: newList.name,
        releaseCount: 1, // We just added one release
        createdAt: newList.createdAt,
        lastModified: newList.lastModified
      };
      setLists(prev => [newListSummary, ...prev]);
      setReleaseLists(prev => [...prev, newList.id]);
      setNewListName("");
      setShowNewListForm(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create list");
    } finally {
      setSubmitting(false);
    }
  };

  if (!isOpen || !portalContainer) return null;

  return createPortal(
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4 transition-opacity duration-300">
      <div className="bg-white rounded-xl shadow-2xl max-w-md w-full max-h-[80vh] overflow-hidden flex flex-col transform transition-all duration-300 scale-100">
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-gray-100">
          <h2 className="text-xl font-bold text-gray-900">Add to List</h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-[#D93611] transition-colors p-1 hover:bg-gray-50 rounded-full"
            aria-label="Close dialog"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-5">
          {/* Release Title */}
          <div className="mb-6 p-4 bg-gray-50 rounded-xl border border-gray-100">
            <p className="text-xs font-bold text-[#D93611] uppercase tracking-wider mb-1">Adding Release</p>
            <p className="font-bold text-lg text-gray-900 leading-tight">{releaseTitle}</p>
          </div>

          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-100 rounded-xl flex items-start gap-3">
              <div className="text-red-500 mt-0.5">
                <X className="w-5 h-5" />
              </div>
              <p className="text-sm text-red-600">{error}</p>
            </div>
          )}

          {/* Create New List Button */}
          {!showNewListForm && (
            <button
              onClick={() => setShowNewListForm(true)}
              className="w-full mb-6 flex items-center justify-center gap-2 px-4 py-3.5 bg-[#D93611] text-white rounded-xl hover:bg-[#C02F0F] transition-all shadow-md hover:shadow-lg font-bold text-sm uppercase tracking-wide"
            >
              <Plus className="w-5 h-5" />
              Create New List
            </button>
          )}

          {/* New List Form */}
          {showNewListForm && (
            <form onSubmit={handleCreateList} className="mb-6 p-5 bg-gray-50 rounded-xl border border-gray-100 animate-in fade-in slide-in-from-top-2 duration-200">
              <label htmlFor="newListName" className="block text-sm font-bold text-gray-700 mb-2">
                New List Name
              </label>
              <input
                id="newListName"
                type="text"
                value={newListName}
                onChange={(e) => setNewListName(e.target.value)}
                placeholder="e.g., My Top 10 Metal Records"
                className="w-full px-4 py-3 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-[#D93611]/20 focus:border-[#D93611] mb-4 bg-white transition-all"
                disabled={submitting}
                autoFocus
              />
              <div className="flex gap-3">
                <button
                  type="submit"
                  disabled={submitting || !newListName.trim()}
                  className="flex-1 px-4 py-2.5 bg-[#D93611] text-white rounded-xl hover:bg-[#C02F0F] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-bold text-sm"
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
                  className="px-4 py-2.5 bg-white border border-gray-200 text-gray-700 rounded-xl hover:bg-gray-50 hover:text-gray-900 disabled:opacity-50 transition-colors font-bold text-sm"
                >
                  Cancel
                </button>
              </div>
            </form>
          )}

          {/* Lists */}
          {loading ? (
            <div className="flex flex-col items-center justify-center py-12 text-gray-400">
              <div className="w-8 h-8 border-4 border-[#D93611]/20 border-t-[#D93611] rounded-full animate-spin mb-3" />
              <p className="text-sm font-medium">Loading lists...</p>
            </div>
          ) : lists.length === 0 ? (
            <div className="text-center py-12 text-gray-500 bg-gray-50 rounded-xl border border-dashed border-gray-200">
              <p className="font-medium">No lists yet</p>
              <p className="text-sm mt-1">Create your first list to get started!</p>
            </div>
          ) : (
            <div className="space-y-3">
              <div className="flex items-center justify-between mb-2 px-1">
                <p className="text-sm font-bold text-gray-900">Your Lists</p>
                <span className="text-xs font-medium text-gray-500 bg-gray-100 px-2 py-1 rounded-full">{lists.length}</span>
              </div>
              <div className="space-y-2 max-h-[300px] overflow-y-auto pr-1 custom-scrollbar">
                {lists.map((list) => {
                  const isInList = releaseLists.includes(list.id);
                  const isAdding = addingToList === list.id;

                  return (
                    <button
                      key={list.id}
                      onClick={() => !isInList && !isAdding && handleAddToList(list.id)}
                      disabled={isInList || isAdding}
                      className={`w-full flex items-center justify-between p-4 rounded-xl border transition-all duration-200 group ${
                        isInList
                          ? "bg-green-50 border-green-200 cursor-default"
                          : "bg-white border-gray-100 hover:border-[#D93611] hover:shadow-md hover:-translate-y-0.5"
                      } ${isAdding ? "opacity-70 cursor-wait" : ""}`}
                    >
                      <div className="flex-1 text-left">
                        <p className={`font-bold ${isInList ? "text-green-800" : "text-gray-900 group-hover:text-[#D93611] transition-colors"}`}>{list.name}</p>
                        <p className={`text-xs ${isInList ? "text-green-600" : "text-gray-500"}`}>
                          {list.releaseCount} {list.releaseCount === 1 ? "release" : "releases"}
                        </p>
                      </div>
                      {isInList ? (
                        <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center">
                          <Check className="w-5 h-5 text-green-600" />
                        </div>
                      ) : isAdding ? (
                        <div className="w-5 h-5 border-2 border-[#D93611] border-t-transparent rounded-full animate-spin flex-shrink-0 mr-1.5" />
                      ) : (
                        <div className="w-8 h-8 bg-gray-50 rounded-full flex items-center justify-center group-hover:bg-[#D93611] transition-colors">
                          <Plus className="w-5 h-5 text-gray-400 group-hover:text-white transition-colors" />
                        </div>
                      )}
                    </button>
                  );
                })}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-5 border-t border-gray-100 bg-gray-50/50">
          <button
            onClick={onClose}
            className="w-full px-4 py-3.5 bg-white border border-gray-200 text-gray-700 rounded-xl hover:bg-gray-50 hover:text-gray-900 hover:border-gray-300 transition-all font-bold text-sm shadow-sm"
          >
            Done
          </button>
        </div>
      </div>
    </div>,
    portalContainer
  );
}
