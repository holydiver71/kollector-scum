"use client";
import { useState, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { 
  getKollections, 
  createKollection, 
  updateKollection, 
  deleteKollection,
  KollectionSummaryDto 
} from "../lib/api";
import { LoadingSpinner } from "../components/LoadingComponents";
import { Layers, Plus, Edit, Trash2, X } from "lucide-react";

export default function KollectionsPage() {
  const router = useRouter();
  const [kollections, setKollections] = useState<KollectionSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [newKollectionName, setNewKollectionName] = useState("");
  const [editingKollection, setEditingKollection] = useState<KollectionSummaryDto | null>(null);
  const [editName, setEditName] = useState("");
  const [deletingKollectionId, setDeletingKollectionId] = useState<number | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    loadKollections();
  }, []);

  const loadKollections = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getKollections();
      setKollections(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load kollections");
    } finally {
      setLoading(false);
    }
  };

  const handleCreateKollection = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newKollectionName.trim()) return;

    try {
      setActionError(null);
      await createKollection({ name: newKollectionName });
      setNewKollectionName("");
      setShowCreateDialog(false);
      loadKollections();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to create kollection");
    }
  };

  const handleUpdateKollection = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingKollection || !editName.trim()) return;

    try {
      setActionError(null);
      await updateKollection(editingKollection.id, { name: editName });
      setEditingKollection(null);
      setEditName("");
      loadKollections();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to update kollection");
    }
  };

  const handleDeleteKollection = async (id: number) => {
    try {
      setActionError(null);
      await deleteKollection(id);
      setDeletingKollectionId(null);
      loadKollections();
    } catch (err) {
      setActionError(err instanceof Error ? err.message : "Failed to delete kollection");
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="text-red-600 text-xl mb-4">Error loading kollections</div>
          <p className="text-gray-600 mb-4">{error}</p>
          <button
            onClick={loadKollections}
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Page Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-black text-gray-900">Kollections</h1>
              <p className="text-gray-600 mt-1 font-medium">Manage your custom music lists</p>
            </div>
            <button
              onClick={() => setShowCreateDialog(true)}
              className="inline-flex items-center gap-2 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors font-semibold"
            >
              <Plus className="w-5 h-5" />
              Create Kollection
            </button>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        {actionError && (
          <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
            {actionError}
          </div>
        )}

        {kollections.length === 0 ? (
          <div className="text-center py-12">
            <Layers className="w-16 h-16 mx-auto text-gray-400 mb-4" />
            <h3 className="text-lg font-semibold text-gray-900 mb-2">No kollections yet</h3>
            <p className="text-gray-600 mb-4">Create your first kollection to organize your music</p>
            <button
              onClick={() => setShowCreateDialog(true)}
              className="inline-flex items-center gap-2 px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors font-semibold"
            >
              <Plus className="w-5 h-5" />
              Create Kollection
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {kollections.map((kollection) => (
              <div
                key={kollection.id}
                className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow p-6"
              >
                <div className="flex items-start justify-between mb-4">
                  <div className="flex-1">
                    <Link
                      href={`/kollections/${kollection.id}`}
                      className="text-lg font-bold text-gray-900 hover:text-red-600 transition-colors"
                    >
                      {kollection.name}
                    </Link>
                    <p className="text-sm text-gray-600 mt-1">
                      {kollection.itemCount} {kollection.itemCount === 1 ? 'release' : 'releases'}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => {
                        setEditingKollection(kollection);
                        setEditName(kollection.name);
                      }}
                      className="p-2 text-gray-600 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors"
                      title="Rename"
                    >
                      <Edit className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => setDeletingKollectionId(kollection.id)}
                      className="p-2 text-gray-600 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                      title="Delete"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
                <div className="text-xs text-gray-500">
                  Created {new Date(kollection.createdAt).toLocaleDateString()}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Create Dialog */}
      {showCreateDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
            <div className="flex items-center justify-between p-6 border-b border-gray-200">
              <h2 className="text-xl font-bold text-gray-900">Create New Kollection</h2>
              <button
                onClick={() => {
                  setShowCreateDialog(false);
                  setNewKollectionName("");
                  setActionError(null);
                }}
                className="text-gray-400 hover:text-gray-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleCreateKollection} className="p-6">
              <div className="mb-4">
                <label htmlFor="name" className="block text-sm font-semibold text-gray-700 mb-2">
                  Kollection Name
                </label>
                <input
                  type="text"
                  id="name"
                  value={newKollectionName}
                  onChange={(e) => setNewKollectionName(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
                  placeholder="e.g., My Top 10 Metal Records"
                  autoFocus
                />
              </div>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setShowCreateDialog(false);
                    setNewKollectionName("");
                    setActionError(null);
                  }}
                  className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={!newKollectionName.trim()}
                  className="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed font-semibold"
                >
                  Create
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Dialog */}
      {editingKollection && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
            <div className="flex items-center justify-between p-6 border-b border-gray-200">
              <h2 className="text-xl font-bold text-gray-900">Rename Kollection</h2>
              <button
                onClick={() => {
                  setEditingKollection(null);
                  setEditName("");
                  setActionError(null);
                }}
                className="text-gray-400 hover:text-gray-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleUpdateKollection} className="p-6">
              <div className="mb-4">
                <label htmlFor="editName" className="block text-sm font-semibold text-gray-700 mb-2">
                  Kollection Name
                </label>
                <input
                  type="text"
                  id="editName"
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
                  autoFocus
                />
              </div>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setEditingKollection(null);
                    setEditName("");
                    setActionError(null);
                  }}
                  className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={!editName.trim()}
                  className="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed font-semibold"
                >
                  Save
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Confirmation Dialog */}
      {deletingKollectionId !== null && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 p-6">
            <h2 className="text-xl font-bold text-gray-900 mb-4">Delete Kollection?</h2>
            <p className="text-gray-600 mb-6">
              Are you sure you want to delete this kollection? This action cannot be undone.
            </p>
            <div className="flex justify-end gap-2">
              <button
                onClick={() => {
                  setDeletingKollectionId(null);
                  setActionError(null);
                }}
                className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={() => handleDeleteKollection(deletingKollectionId)}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors font-semibold"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
