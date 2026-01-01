"use client";

import { useState, useEffect } from "react";
import { Plus, Pencil, Trash2, X, Check } from "lucide-react";
import {
  getKollections,
  createKollection,
  updateKollection,
  deleteKollection,
  type KollectionDto,
  type CreateKollectionDto,
  type UpdateKollectionDto,
} from "../lib/api";
import { useLookupData } from "../components/LookupComponents";

interface KollectionFormData {
  name: string;
  genreIds: number[];
}

export default function KollectionsPage() {
  const [kollections, setKollections] = useState<KollectionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState<KollectionFormData>({
    name: "",
    genreIds: [],
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  interface Genre {
    id: number;
    name: string;
  }

  const { data: genres, loading: genresLoading } = useLookupData<Genre>("genres");

  useEffect(() => {
    loadKollections();
  }, []);

  const loadKollections = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await getKollections();
      setKollections(response.items);
    } catch (err) {
      setError("Failed to load kollections");
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setFormData({ name: "", genreIds: [] });
    setEditingId(null);
    setShowForm(true);
    setFormError(null);
  };

  const handleEdit = (kollection: KollectionDto) => {
    setFormData({
      name: kollection.name,
      genreIds: kollection.genreIds,
    });
    setEditingId(kollection.id);
    setShowForm(true);
    setFormError(null);
  };

  const handleCancelForm = () => {
    setShowForm(false);
    setEditingId(null);
    setFormData({ name: "", genreIds: [] });
    setFormError(null);
  };

  const handleSubmitForm = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    // Validation
    if (!formData.name.trim()) {
      setFormError("Name is required");
      return;
    }
    if (formData.genreIds.length === 0) {
      setFormError("At least one genre must be selected");
      return;
    }

    try {
      if (editingId !== null) {
        // Update existing
        const updateData: UpdateKollectionDto = {
          name: formData.name.trim(),
          genreIds: formData.genreIds,
        };
        await updateKollection(editingId, updateData);
      } else {
        // Create new
        const createData: CreateKollectionDto = {
          name: formData.name.trim(),
          genreIds: formData.genreIds,
        };
        await createKollection(createData);
      }
      await loadKollections();
      handleCancelForm();
    } catch (err: any) {
      setFormError(err.message || "Failed to save kollection");
      console.error(err);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Are you sure you want to delete this kollection?")) {
      return;
    }

    try {
      setDeletingId(id);
      await deleteKollection(id);
      await loadKollections();
    } catch (err) {
      setError("Failed to delete kollection");
      console.error(err);
    } finally {
      setDeletingId(null);
    }
  };

  const handleGenreToggle = (genreId: number) => {
    setFormData((prev) => {
      const newGenreIds = prev.genreIds.includes(genreId)
        ? prev.genreIds.filter((id) => id !== genreId)
        : [...prev.genreIds, genreId];
      return { ...prev, genreIds: newGenreIds };
    });
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-7xl mx-auto">
          <div className="text-center py-12">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-[#D93611]"></div>
            <p className="mt-4 text-gray-600">Loading kollections...</p>
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
            <h1 className="text-3xl font-bold text-gray-900">Kollections</h1>
            <p className="mt-2 text-gray-600">
              Manage your genre-based music collections
            </p>
          </div>
          {!showForm && (
            <button
              onClick={handleCreate}
              className="inline-flex items-center gap-2 px-4 py-2 bg-[#D93611] text-white rounded-md hover:bg-[#B82D0E] transition-colors"
            >
              <Plus className="h-5 w-5" />
              New Kollection
            </button>
          )}
        </div>

        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md">
            {error}
          </div>
        )}

        {/* Form */}
        {showForm && (
          <div className="mb-6 bg-white border border-gray-200 rounded-lg p-6 shadow-sm">
            <h2 className="text-xl font-semibold mb-4">
              {editingId !== null ? "Edit Kollection" : "Create Kollection"}
            </h2>
            <form onSubmit={handleSubmitForm}>
              <div className="mb-4">
                <label
                  htmlFor="name"
                  className="block text-sm font-medium text-gray-700 mb-2"
                >
                  Name
                </label>
                <input
                  id="name"
                  type="text"
                  value={formData.name}
                  onChange={(e) =>
                    setFormData({ ...formData, name: e.target.value })
                  }
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-[#D93611]"
                  placeholder="e.g., My Metal Collection"
                  required
                />
              </div>

              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Genres ({formData.genreIds.length} selected)
                </label>
                {genresLoading ? (
                  <p className="text-gray-500">Loading genres...</p>
                ) : (
                  <div className="max-h-64 overflow-y-auto border border-gray-300 rounded-md p-3 bg-gray-50">
                    <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2">
                      {genres.map((genre: Genre) => (
                        <label
                          key={genre.id}
                          className="flex items-center space-x-2 cursor-pointer hover:bg-gray-100 p-2 rounded"
                        >
                          <input
                            type="checkbox"
                            checked={formData.genreIds.includes(genre.id)}
                            onChange={() => handleGenreToggle(genre.id)}
                            className="h-4 w-4 text-[#D93611] focus:ring-[#D93611] border-gray-300 rounded"
                          />
                          <span className="text-sm text-gray-700">
                            {genre.name}
                          </span>
                        </label>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              {formError && (
                <div className="mb-4 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md text-sm">
                  {formError}
                </div>
              )}

              <div className="flex gap-3">
                <button
                  type="submit"
                  className="inline-flex items-center gap-2 px-4 py-2 bg-[#D93611] text-white rounded-md hover:bg-[#B82D0E] transition-colors"
                >
                  <Check className="h-5 w-5" />
                  Save
                </button>
                <button
                  type="button"
                  onClick={handleCancelForm}
                  className="inline-flex items-center gap-2 px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300 transition-colors"
                >
                  <X className="h-5 w-5" />
                  Cancel
                </button>
              </div>
            </form>
          </div>
        )}

        {/* List */}
        {kollections.length === 0 ? (
          <div className="bg-white border border-gray-200 rounded-lg p-12 text-center">
            <p className="text-gray-500 text-lg mb-4">You haven&apos;t created any lists yet.</p>
            <p className="text-gray-400 mb-8">
              Create Kollections to organize your music collection genre!
            </p>
            {!showForm && (
              <button
                onClick={handleCreate}
                className="inline-flex items-center gap-2 px-4 py-2 bg-[#D93611] text-white rounded-md hover:bg-[#B82D0E] transition-colors"
              >
                <Plus className="h-5 w-5" />
                Create Your First Kollection
              </button>
            )}
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {kollections.map((kollection) => (
              <div
                key={kollection.id}
                className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow"
              >
                <div className="flex justify-between items-start mb-3">
                  <h3 className="text-lg font-semibold text-gray-900">
                    {kollection.name}
                  </h3>
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleEdit(kollection)}
                      className="p-1 text-blue-600 hover:bg-blue-50 rounded"
                      title="Edit"
                      disabled={showForm}
                    >
                      <Pencil className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => handleDelete(kollection.id)}
                      className="p-1 text-red-600 hover:bg-red-50 rounded"
                      title="Delete"
                      disabled={deletingId === kollection.id || showForm}
                    >
                      {deletingId === kollection.id ? (
                        <div className="h-4 w-4 border-2 border-red-600 border-t-transparent rounded-full animate-spin"></div>
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </button>
                  </div>
                </div>
                <div>
                  <p className="text-sm text-gray-500 mb-2">
                    {kollection.genreIds.length} genre
                    {kollection.genreIds.length !== 1 ? "s" : ""}
                  </p>
                  <div className="flex flex-wrap gap-1">
                    {kollection.genreNames.map((genreName) => (
                      <span
                        key={genreName}
                        className="inline-block px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded"
                      >
                        {genreName}
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
