"use client";
import { useState, useEffect } from "react";
import { 
  getKollections, 
  addReleaseToKollection, 
  KollectionSummaryDto 
} from "../lib/api";
import { X, Plus, Layers } from "lucide-react";

interface AddToKollectionDialogProps {
  releaseId: number;
  releaseTitle: string;
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

export function AddToKollectionDialog({
  releaseId,
  releaseTitle,
  isOpen,
  onClose,
  onSuccess,
}: AddToKollectionDialogProps) {
  const [kollections, setKollections] = useState<KollectionSummaryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedKollectionId, setSelectedKollectionId] = useState<number | null>(null);
  const [isCreatingNew, setIsCreatingNew] = useState(false);
  const [newKollectionName, setNewKollectionName] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (isOpen) {
      loadKollections();
    }
  }, [isOpen]);

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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!isCreatingNew && selectedKollectionId === null) {
      setError("Please select a kollection");
      return;
    }

    if (isCreatingNew && !newKollectionName.trim()) {
      setError("Please enter a name for the new kollection");
      return;
    }

    try {
      setSubmitting(true);
      setError(null);
      
      await addReleaseToKollection({
        musicReleaseId: releaseId,
        kollectionId: isCreatingNew ? undefined : selectedKollectionId!,
        newKollectionName: isCreatingNew ? newKollectionName : undefined,
      });

      // Reset form
      setSelectedKollectionId(null);
      setIsCreatingNew(false);
      setNewKollectionName("");
      
      if (onSuccess) {
        onSuccess();
      }
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add release to kollection");
    } finally {
      setSubmitting(false);
    }
  };

  const handleClose = () => {
    setSelectedKollectionId(null);
    setIsCreatingNew(false);
    setNewKollectionName("");
    setError(null);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-6 border-b border-gray-200 sticky top-0 bg-white">
          <div>
            <h2 className="text-xl font-bold text-gray-900">Add to Kollection</h2>
            <p className="text-sm text-gray-600 mt-1 truncate">{releaseTitle}</p>
          </div>
          <button
            onClick={handleClose}
            className="text-gray-400 hover:text-gray-600 flex-shrink-0"
            disabled={submitting}
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6">
          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
              {error}
            </div>
          )}

          {loading ? (
            <div className="text-center py-8 text-gray-600">Loading kollections...</div>
          ) : (
            <>
              {/* Toggle between existing and new */}
              <div className="flex gap-2 mb-4">
                <button
                  type="button"
                  onClick={() => {
                    setIsCreatingNew(false);
                    setError(null);
                  }}
                  className={`flex-1 px-4 py-2 rounded-lg font-semibold transition-colors ${
                    !isCreatingNew
                      ? 'bg-red-500 text-white'
                      : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                  }`}
                >
                  Existing
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setIsCreatingNew(true);
                    setSelectedKollectionId(null);
                    setError(null);
                  }}
                  className={`flex-1 px-4 py-2 rounded-lg font-semibold transition-colors ${
                    isCreatingNew
                      ? 'bg-red-500 text-white'
                      : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                  }`}
                >
                  <Plus className="w-4 h-4 inline mr-1" />
                  New
                </button>
              </div>

              {/* Existing Kollections */}
              {!isCreatingNew && (
                <div>
                  {kollections.length === 0 ? (
                    <div className="text-center py-8">
                      <Layers className="w-12 h-12 mx-auto text-gray-400 mb-3" />
                      <p className="text-gray-600 mb-4">No kollections yet</p>
                      <button
                        type="button"
                        onClick={() => setIsCreatingNew(true)}
                        className="text-red-500 hover:text-red-600 font-semibold"
                      >
                        Create your first kollection
                      </button>
                    </div>
                  ) : (
                    <div className="space-y-2 max-h-64 overflow-y-auto">
                      {kollections.map((kollection) => (
                        <label
                          key={kollection.id}
                          className={`flex items-center p-3 rounded-lg border cursor-pointer transition-colors ${
                            selectedKollectionId === kollection.id
                              ? 'border-red-500 bg-red-50'
                              : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                          }`}
                        >
                          <input
                            type="radio"
                            name="kollection"
                            value={kollection.id}
                            checked={selectedKollectionId === kollection.id}
                            onChange={() => setSelectedKollectionId(kollection.id)}
                            className="mr-3"
                          />
                          <div className="flex-1">
                            <div className="font-semibold text-gray-900">{kollection.name}</div>
                            <div className="text-xs text-gray-600">
                              {kollection.itemCount} {kollection.itemCount === 1 ? 'release' : 'releases'}
                            </div>
                          </div>
                        </label>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* New Kollection */}
              {isCreatingNew && (
                <div>
                  <label htmlFor="newKollectionName" className="block text-sm font-semibold text-gray-700 mb-2">
                    Kollection Name
                  </label>
                  <input
                    type="text"
                    id="newKollectionName"
                    value={newKollectionName}
                    onChange={(e) => setNewKollectionName(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-red-500"
                    placeholder="e.g., My Top 10 Metal Records"
                    autoFocus
                  />
                </div>
              )}
            </>
          )}

          <div className="flex justify-end gap-2 mt-6">
            <button
              type="button"
              onClick={handleClose}
              disabled={submitting}
              className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting || loading || (!isCreatingNew && selectedKollectionId === null) || (isCreatingNew && !newKollectionName.trim())}
              className="px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed font-semibold"
            >
              {submitting ? 'Adding...' : 'Add to Kollection'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
