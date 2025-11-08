"use client";

import { useState, useEffect } from "react";
import { fetchJson } from "../lib/api";
import ComboBox, { ComboBoxItem } from "./ComboBox";
import TrackListEditor, { Media } from "./TrackListEditor";

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

  const validateUrl = (url: string): boolean => {
    if (!url) return true; // Empty is okay for optional fields
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  };

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    // Required fields
    if (!formData.title.trim()) {
      errors.title = "Title is required";
    }

    // Check if at least one artist is selected OR new artist names are provided
    if (formData.artistIds.length === 0 && (!formData.artistNames || formData.artistNames.length === 0)) {
      errors.artists = "At least one artist is required";
    }

    // Validate image URLs
    if (formData.images?.coverFront && !validateUrl(formData.images.coverFront)) {
      errors.coverFront = "Invalid URL format";
    }
    if (formData.images?.coverBack && !validateUrl(formData.images.coverBack)) {
      errors.coverBack = "Invalid URL format";
    }
    if (formData.images?.thumbnail && !validateUrl(formData.images.thumbnail)) {
      errors.thumbnail = "Invalid URL format";
    }

    // Validate external links
    formData.links?.forEach((link, index) => {
      if (link.url && !validateUrl(link.url)) {
        errors[`link${index}`] = "Invalid URL format";
      }
      if (link.url && !link.type) {
        errors[`linkType${index}`] = "Link type is required when URL is provided";
      }
    });

    // Validate purchase info
    if (formData.purchaseInfo?.price !== undefined) {
      if (formData.purchaseInfo.price < 0) {
        errors.price = "Price cannot be negative";
      }
    }

    // Validate tracks
    formData.media?.forEach((disc, discIndex) => {
      disc.tracks.forEach((track, trackIndex) => {
        if (!track.title.trim()) {
          errors[`track${discIndex}_${trackIndex}`] = "Track title is required";
        }
      });
    });

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
      // Clean up the data before sending - remove empty arrays and convert years to DateTime
      const cleanedData = {
        ...formData,
        // Convert year strings to ISO DateTime format for the backend
        releaseYear: formData.releaseYear 
          ? new Date(parseInt(formData.releaseYear), 0, 1).toISOString() 
          : undefined,
        origReleaseYear: formData.origReleaseYear 
          ? new Date(parseInt(formData.origReleaseYear), 0, 1).toISOString() 
          : undefined,
        artistIds: formData.artistIds?.length ? formData.artistIds : undefined,
        artistNames: formData.artistNames?.length ? formData.artistNames : undefined,
        genreIds: formData.genreIds?.length ? formData.genreIds : undefined,
        genreNames: formData.genreNames?.length ? formData.genreNames : undefined,
        links: formData.links?.length ? formData.links : undefined,
        media: formData.media?.length ? formData.media : undefined,
      };
      
      console.log("Submitting form data:", cleanedData);
      const response = await fetchJson<{ release: { id: number } }>("/api/musicreleases", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(cleanedData),
      });

      if (onSuccess) {
        onSuccess(response.release.id);
      }
    } catch (err: any) {
      console.error("Error creating release:", err);
      // Show validation details if available
      let errorMessage = err.message || "Failed to create release. Please try again.";
      if (err.details) {
        console.error("Validation details:", err.details);
        console.error("Errors object:", err.details.errors);
        // Handle FluentValidation/ASP.NET Core ValidationProblemDetails format
        if (err.details.errors && typeof err.details.errors === 'object') {
          const validationErrors = Object.entries(err.details.errors)
            .map(([field, messages]) => {
              const msgs = Array.isArray(messages) ? messages : [messages];
              return `${field}: ${msgs.join(', ')}`;
            })
            .join('\n');
          errorMessage = `Validation failed:\n${validationErrors}`;
        } else if (err.details.title || err.details.detail) {
          // Handle ProblemDetails format
          errorMessage = err.details.title || err.details.detail || errorMessage;
        } else if (typeof err.details === 'string') {
          errorMessage = err.details;
        }
      }
      setError(errorMessage);
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

      {/* Purchase Information */}
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Purchase Information (Optional)</h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <ComboBox
              label="Store"
              items={stores}
              value={formData.purchaseInfo?.storeId || null}
              newValues={newStoreName}
              onChange={(selectedIds, selectedNames) => {
                updateField("purchaseInfo", {
                  ...formData.purchaseInfo,
                  storeId: selectedIds[0],
                  storeName: selectedNames[0],
                });
                setNewStoreName(selectedNames);
              }}
              multiple={false}
              allowCreate={true}
              placeholder="Select or add store..."
            />
          </div>

          <div>
            <label htmlFor="purchaseDate" className="block text-sm font-medium text-gray-700 mb-1">
              Purchase Date
            </label>
            <input
              type="date"
              id="purchaseDate"
              value={formData.purchaseInfo?.purchaseDate || ""}
              onChange={(e) => updateField("purchaseInfo", {
                ...formData.purchaseInfo,
                purchaseDate: e.target.value,
              })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <label htmlFor="price" className="block text-sm font-medium text-gray-700 mb-1">
              Price
            </label>
            <input
              type="number"
              id="price"
              step="0.01"
              min="0"
              value={formData.purchaseInfo?.price || ""}
              onChange={(e) => updateField("purchaseInfo", {
                ...formData.purchaseInfo,
                price: parseFloat(e.target.value) || undefined,
              })}
              className={`w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500 ${
                validationErrors.price ? "border-red-500" : "border-gray-300"
              }`}
              placeholder="0.00"
            />
            {validationErrors.price && (
              <p className="mt-1 text-sm text-red-600">{validationErrors.price}</p>
            )}
          </div>

          <div>
            <label htmlFor="currency" className="block text-sm font-medium text-gray-700 mb-1">
              Currency
            </label>
            <select
              id="currency"
              value={formData.purchaseInfo?.currency || "USD"}
              onChange={(e) => updateField("purchaseInfo", {
                ...formData.purchaseInfo,
                currency: e.target.value,
              })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="USD">USD ($)</option>
              <option value="EUR">EUR (€)</option>
              <option value="GBP">GBP (£)</option>
              <option value="JPY">JPY (¥)</option>
              <option value="CAD">CAD ($)</option>
              <option value="AUD">AUD ($)</option>
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="purchaseNotes" className="block text-sm font-medium text-gray-700 mb-1">
            Purchase Notes
          </label>
          <textarea
            id="purchaseNotes"
            rows={2}
            value={formData.purchaseInfo?.notes || ""}
            onChange={(e) => updateField("purchaseInfo", {
              ...formData.purchaseInfo,
              notes: e.target.value,
            })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="Additional purchase details..."
          />
        </div>
      </div>

      {/* Images */}
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Images (Optional)</h2>
        
        <div className="space-y-4">
          <div>
            <label htmlFor="coverFront" className="block text-sm font-medium text-gray-700 mb-1">
              Front Cover URL
            </label>
            <input
              type="url"
              id="coverFront"
              value={formData.images?.coverFront || ""}
              onChange={(e) => updateField("images", {
                ...formData.images,
                coverFront: e.target.value,
              })}
              className={`w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500 ${
                validationErrors.coverFront ? "border-red-500" : "border-gray-300"
              }`}
              placeholder="https://example.com/front-cover.jpg"
            />
            {validationErrors.coverFront && (
              <p className="mt-1 text-sm text-red-600">{validationErrors.coverFront}</p>
            )}
            {formData.images?.coverFront && (
              <div className="mt-2">
                <img 
                  src={formData.images.coverFront} 
                  alt="Front cover preview" 
                  className="h-32 w-32 object-cover rounded border"
                  onError={(e) => {
                    (e.target as HTMLImageElement).style.display = 'none';
                  }}
                />
              </div>
            )}
          </div>

          <div>
            <label htmlFor="coverBack" className="block text-sm font-medium text-gray-700 mb-1">
              Back Cover URL
            </label>
            <input
              type="url"
              id="coverBack"
              value={formData.images?.coverBack || ""}
              onChange={(e) => updateField("images", {
                ...formData.images,
                coverBack: e.target.value,
              })}
              className={`w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500 ${
                validationErrors.coverBack ? "border-red-500" : "border-gray-300"
              }`}
              placeholder="https://example.com/back-cover.jpg"
            />
            {validationErrors.coverBack && (
              <p className="mt-1 text-sm text-red-600">{validationErrors.coverBack}</p>
            )}
            {formData.images?.coverBack && (
              <div className="mt-2">
                <img 
                  src={formData.images.coverBack} 
                  alt="Back cover preview" 
                  className="h-32 w-32 object-cover rounded border"
                  onError={(e) => {
                    (e.target as HTMLImageElement).style.display = 'none';
                  }}
                />
              </div>
            )}
          </div>

          <div>
            <label htmlFor="thumbnail" className="block text-sm font-medium text-gray-700 mb-1">
              Thumbnail URL
            </label>
            <input
              type="url"
              id="thumbnail"
              value={formData.images?.thumbnail || ""}
              onChange={(e) => updateField("images", {
                ...formData.images,
                thumbnail: e.target.value,
              })}
              className={`w-full px-3 py-2 border rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500 ${
                validationErrors.thumbnail ? "border-red-500" : "border-gray-300"
              }`}
              placeholder="https://example.com/thumbnail.jpg"
            />
            {validationErrors.thumbnail && (
              <p className="mt-1 text-sm text-red-600">{validationErrors.thumbnail}</p>
            )}
            {formData.images?.thumbnail && (
              <div className="mt-2">
                <img 
                  src={formData.images.thumbnail} 
                  alt="Thumbnail preview" 
                  className="h-20 w-20 object-cover rounded border"
                  onError={(e) => {
                    (e.target as HTMLImageElement).style.display = 'none';
                  }}
                />
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Track Listing */}
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Track Listing (Optional)</h2>
        <TrackListEditor
          media={formData.media || []}
          onChange={(newMedia) => updateField("media", newMedia)}
        />
      </div>

      {/* External Links */}
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">External Links (Optional)</h2>
        
        <div className="space-y-3">
          {formData.links?.map((link, index) => (
            <div key={index} className="flex gap-2 items-start p-3 bg-gray-50 rounded">
              <div className="flex-1 space-y-2">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">
                      URL
                    </label>
                    <input
                      type="url"
                      value={link.url}
                      onChange={(e) => {
                        const newLinks = [...(formData.links || [])];
                        newLinks[index] = { ...newLinks[index], url: e.target.value };
                        updateField("links", newLinks);
                      }}
                      className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:ring-1 focus:ring-blue-500"
                      placeholder="https://..."
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">
                      Type
                    </label>
                    <select
                      value={link.type}
                      onChange={(e) => {
                        const newLinks = [...(formData.links || [])];
                        newLinks[index] = { ...newLinks[index], type: e.target.value };
                        updateField("links", newLinks);
                      }}
                      className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:ring-1 focus:ring-blue-500"
                    >
                      <option value="">Select type...</option>
                      <option value="Discogs">Discogs</option>
                      <option value="Spotify">Spotify</option>
                      <option value="Apple Music">Apple Music</option>
                      <option value="YouTube">YouTube</option>
                      <option value="Bandcamp">Bandcamp</option>
                      <option value="Official">Official Website</option>
                      <option value="Other">Other</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">
                    Description (Optional)
                  </label>
                  <input
                    type="text"
                    value={link.description || ""}
                    onChange={(e) => {
                      const newLinks = [...(formData.links || [])];
                      newLinks[index] = { ...newLinks[index], description: e.target.value };
                      updateField("links", newLinks);
                    }}
                    className="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:ring-1 focus:ring-blue-500"
                    placeholder="Optional description..."
                  />
                </div>
              </div>
              <button
                type="button"
                onClick={() => {
                  const newLinks = formData.links?.filter((_, i) => i !== index);
                  updateField("links", newLinks);
                }}
                className="mt-5 px-2 py-1 text-sm text-red-600 hover:text-red-800 hover:bg-red-50 rounded"
              >
                Remove
              </button>
            </div>
          ))}
          
          <button
            type="button"
            onClick={() => {
              const newLinks = [...(formData.links || []), { url: "", type: "", description: "" }];
              updateField("links", newLinks);
            }}
            className="w-full px-4 py-2 border-2 border-dashed border-gray-300 rounded-md text-gray-600 hover:border-gray-400 hover:text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            + Add Link
          </button>
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
