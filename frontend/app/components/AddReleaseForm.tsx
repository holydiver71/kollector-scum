"use client";

import { useState, useEffect } from "react";
import { fetchJson } from "../lib/api";
import ComboBox, { ComboBoxItem } from "./ComboBox";

// Lookup data interfaces
interface LookupItem {
  id: number;
  name: string;
}

interface CreateMusicReleaseDto {
  title: string;
  releaseYear?: string;
  origReleaseYear?: string;
  artistIds: number[];
  artistNames?: string[]; // New: for auto-creation
  genreIds: number[];
  genreNames?: string[]; // New: for auto-creation
  live: boolean;
  labelId?: number;
  labelName?: string; // New: for auto-creation
  countryId?: number;
  countryName?: string; // New: for auto-creation
  labelNumber?: string;
  upc?: string;
  lengthInSeconds?: number;
  formatId?: number;
  formatName?: string; // New: for auto-creation
  packagingId?: number;
  packagingName?: string; // New: for auto-creation
  purchaseInfo?: {
    storeId?: number;
    storeName?: string; // New: for auto-creation
    price?: number;
    currency?: string;
    purchaseDate?: string;
    notes?: string;
  };
  images?: {
    coverFront?: string;
    coverBack?: string;
    thumbnail?: string;
  };
  links?: Array<{
    url: string;
    type: string;
    description?: string;
  }>;
  media?: Array<{
    name?: string;
    tracks: Array<{
      title: string;
      index: number;
      lengthSecs?: number;
      artists?: string[];
      genres?: string[];
      live?: boolean;
    }>;
  }>;
}

interface AddReleaseFormProps {
  onSuccess?: (releaseId: number) => void;
  onCancel?: () => void;
}

export default function AddReleaseForm({ onSuccess, onCancel }: AddReleaseFormProps) {
  // Form state
  const [formData, setFormData] = useState<CreateMusicReleaseDto>({
    title: "",
    artistIds: [],
    artistNames: [],
    genreIds: [],
    genreNames: [],
    live: false,
    links: [],
    media: [],
  });

  // New values state (for auto-creation)
  const [newArtistNames, setNewArtistNames] = useState<string[]>([]);
  const [newGenreNames, setNewGenreNames] = useState<string[]>([]);
  const [newLabelName, setNewLabelName] = useState<string[]>([]);
  const [newCountryName, setNewCountryName] = useState<string[]>([]);
  const [newFormatName, setNewFormatName] = useState<string[]>([]);
  const [newPackagingName, setNewPackagingName] = useState<string[]>([]);
  const [newStoreName, setNewStoreName] = useState<string[]>([]);

  // Lookup data
  const [artists, setArtists] = useState<LookupItem[]>([]);
  const [genres, setGenres] = useState<LookupItem[]>([]);
  const [labels, setLabels] = useState<LookupItem[]>([]);
  const [countries, setCountries] = useState<LookupItem[]>([]);
  const [formats, setFormats] = useState<LookupItem[]>([]);
  const [packagings, setPackagings] = useState<LookupItem[]>([]);
  const [stores, setStores] = useState<LookupItem[]>([]);

  // UI state
  const [loading, setLoading] = useState(false);
  const [loadingLookups, setLoadingLookups] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  // Load lookup data on mount
  useEffect(() => {
    loadLookupData();
  }, []);

  const loadLookupData = async () => {
    setLoadingLookups(true);
    try {
      const [
        artistsData,
        genresData,
        labelsData,
        countriesData,
        formatsData,
        packagingsData,
        storesData,
      ] = await Promise.all([
        fetchJson<{ items: LookupItem[] }>("/api/artists?pageSize=1000"),
        fetchJson<{ items: LookupItem[] }>("/api/genres?pageSize=100"),
        fetchJson<{ items: LookupItem[] }>("/api/labels?pageSize=1000"),
        fetchJson<{ items: LookupItem[] }>("/api/countries?pageSize=300"),
        fetchJson<{ items: LookupItem[] }>("/api/formats?pageSize=100"),
        fetchJson<{ items: LookupItem[] }>("/api/packagings?pageSize=100"),
        fetchJson<{ items: LookupItem[] }>("/api/stores?pageSize=100"),
      ]);

      setArtists(artistsData.items || []);
      setGenres(genresData.items || []);
      setLabels(labelsData.items || []);
      setCountries(countriesData.items || []);
      setFormats(formatsData.items || []);
      setPackagings(packagingsData.items || []);
      setStores(storesData.items || []);
    } catch (err) {
      console.error("Error loading lookup data:", err);
      setError("Failed to load form data. Please refresh the page.");
    } finally {
      setLoadingLookups(false);
    }
  };

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!formData.title.trim()) {
      errors.title = "Title is required";
    }

    // Check if at least one artist is selected OR new artist names are provided
    if (formData.artistIds.length === 0 && (!formData.artistNames || formData.artistNames.length === 0)) {
      errors.artists = "At least one artist is required";
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetchJson<{ id: number }>("/api/musicreleases", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(formData),
      });

      if (onSuccess) {
        onSuccess(response.id);
      }
    } catch (err: any) {
      console.error("Error creating release:", err);
      setError(err.message || "Failed to create release. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  const updateField = <K extends keyof CreateMusicReleaseDto>(
    field: K,
    value: CreateMusicReleaseDto[K]
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    // Clear validation error for this field
    if (validationErrors[field as string]) {
      setValidationErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field as string];
        return newErrors;
      });
    }
  };

  if (loadingLookups) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          <p className="mt-2 text-gray-600">Loading form...</p>
        </div>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-md p-4">
          <p className="text-sm text-red-800">{error}</p>
        </div>
      )}

      {/* Basic Information */}
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Basic Information</h2>
        
        {/* Title */}
        <div className="mb-4">
          <label htmlFor="title" className="block text-sm font-medium text-gray-700 mb-1">
            Title <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            id="title"
            value={formData.title}
            onChange={(e) => updateField("title", e.target.value)}
            className={`w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500 ${
              validationErrors.title ? "border-red-500" : "border-gray-300"
            }`}
            placeholder="Enter album title"
          />
          {validationErrors.title && (
            <p className="mt-1 text-sm text-red-600">{validationErrors.title}</p>
          )}
        </div>

        {/* Artists */}
        <div className="mb-4">
          <ComboBox
            label="Artists"
            items={artists}
            value={formData.artistIds}
            newValues={newArtistNames}
            onChange={(selectedIds, selectedNames) => {
              updateField("artistIds", selectedIds);
              updateField("artistNames", selectedNames);
              setNewArtistNames(selectedNames);
            }}
            multiple={true}
            allowCreate={true}
            required={true}
            placeholder="Search or add artists..."
            error={validationErrors.artists}
          />
        </div>

        {/* Release Year */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <label htmlFor="releaseYear" className="block text-sm font-medium text-gray-700 mb-1">
              Release Year
            </label>
            <input
              type="text"
              id="releaseYear"
              value={formData.releaseYear || ""}
              onChange={(e) => updateField("releaseYear", e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="YYYY or YYYY-MM-DD"
            />
          </div>
          <div>
            <label htmlFor="origReleaseYear" className="block text-sm font-medium text-gray-700 mb-1">
              Original Release Year
            </label>
            <input
              type="text"
              id="origReleaseYear"
              value={formData.origReleaseYear || ""}
              onChange={(e) => updateField("origReleaseYear", e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="YYYY or YYYY-MM-DD"
            />
          </div>
        </div>

        {/* Live checkbox */}
        <div className="mb-4">
          <label className="flex items-center">
            <input
              type="checkbox"
              checked={formData.live}
              onChange={(e) => updateField("live", e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
            <span className="ml-2 text-sm text-gray-700">Live Recording</span>
          </label>
        </div>
      </div>

      {/* Classification */}
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Classification</h2>
        
        {/* Genres */}
        <div className="mb-4">
          <ComboBox
            label="Genres"
            items={genres}
            value={formData.genreIds}
            newValues={newGenreNames}
            onChange={(selectedIds, selectedNames) => {
              updateField("genreIds", selectedIds);
              updateField("genreNames", selectedNames);
              setNewGenreNames(selectedNames);
            }}
            multiple={true}
            allowCreate={true}
            placeholder="Search or add genres..."
          />
        </div>

        {/* Format, Packaging, Country */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <ComboBox
              label="Format"
              items={formats}
              value={formData.formatId || null}
              newValues={newFormatName}
              onChange={(selectedIds, selectedNames) => {
                updateField("formatId", selectedIds[0]);
                updateField("formatName", selectedNames[0]);
                setNewFormatName(selectedNames);
              }}
              multiple={false}
              allowCreate={true}
              placeholder="Select or add format..."
            />
          </div>

          <div>
            <ComboBox
              label="Packaging"
              items={packagings}
              value={formData.packagingId || null}
              newValues={newPackagingName}
              onChange={(selectedIds, selectedNames) => {
                updateField("packagingId", selectedIds[0]);
                updateField("packagingName", selectedNames[0]);
                setNewPackagingName(selectedNames);
              }}
              multiple={false}
              allowCreate={true}
              placeholder="Select or add packaging..."
            />
          </div>

          <div>
            <ComboBox
              label="Country"
              items={countries}
              value={formData.countryId || null}
              newValues={newCountryName}
              onChange={(selectedIds, selectedNames) => {
                updateField("countryId", selectedIds[0]);
                updateField("countryName", selectedNames[0]);
                setNewCountryName(selectedNames);
              }}
              multiple={false}
              allowCreate={true}
              placeholder="Select or add country..."
            />
          </div>
        </div>
      </div>

      {/* Label Information */}
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Label Information</h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <ComboBox
              label="Label"
              items={labels}
              value={formData.labelId || null}
              newValues={newLabelName}
              onChange={(selectedIds, selectedNames) => {
                updateField("labelId", selectedIds[0]);
                updateField("labelName", selectedNames[0]);
                setNewLabelName(selectedNames);
              }}
              multiple={false}
              allowCreate={true}
              placeholder="Select or add label..."
            />
          </div>

          <div>
            <label htmlFor="labelNumber" className="block text-sm font-medium text-gray-700 mb-1">
              Catalog Number
            </label>
            <input
              type="text"
              id="labelNumber"
              value={formData.labelNumber || ""}
              onChange={(e) => updateField("labelNumber", e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="e.g., ABC-12345"
            />
          </div>
        </div>

        <div className="mt-4">
          <label htmlFor="upc" className="block text-sm font-medium text-gray-700 mb-1">
            UPC/Barcode
          </label>
          <input
            type="text"
            id="upc"
            value={formData.upc || ""}
            onChange={(e) => updateField("upc", e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="Enter UPC/barcode"
          />
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex justify-end gap-3">
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="px-6 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
            disabled={loading}
          >
            Cancel
          </button>
        )}
        <button
          type="submit"
          disabled={loading}
          className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? "Creating..." : "Create Release"}
        </button>
      </div>
    </form>
  );
}
