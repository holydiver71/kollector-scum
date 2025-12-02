"use client";

import { useRouter } from "next/navigation";
import { useState, useEffect } from "react";
import AddReleaseForm, { type CreateMusicReleaseDto } from "../components/AddReleaseForm";
import DiscogsSearch from "../components/DiscogsSearch";
import DiscogsSearchResults from "../components/DiscogsSearchResults";
import DiscogsReleasePreview from "../components/DiscogsReleasePreview";
import type { DiscogsSearchResult, DiscogsRelease } from "../lib/discogs-types";
import { API_BASE_URL } from "../lib/api";

type Tab = "manual" | "discogs";

interface LookupItem {
  id: number;
  name: string;
}

export default function AddReleasePage() {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<Tab>("discogs");
  const [showSuccess, setShowSuccess] = useState(false);
  const [newReleaseId, setNewReleaseId] = useState<number | null>(null);
  
  // Discogs workflow state
  const [searchResults, setSearchResults] = useState<DiscogsSearchResult[]>([]);
  const [selectedResult, setSelectedResult] = useState<DiscogsSearchResult | null>(null);
  const [searchError, setSearchError] = useState("");
  const [discogsFormData, setDiscogsFormData] = useState<Partial<CreateMusicReleaseDto> | undefined>(undefined);
  const [discogsImageUrls, setDiscogsImageUrls] = useState<{ cover: string | null; thumbnail: string | null }>({ cover: null, thumbnail: null }); // Store original Discogs image URLs
  
  // Lookup data for detecting new entities
  const [existingArtists, setExistingArtists] = useState<string[]>([]);
  const [existingLabels, setExistingLabels] = useState<string[]>([]);
  const [existingGenres, setExistingGenres] = useState<string[]>([]);
  const [existingCountries, setExistingCountries] = useState<string[]>([]);
  const [existingFormats, setExistingFormats] = useState<string[]>([]);

  // Load lookup data for new entity detection
  useEffect(() => {
    const fetchLookupData = async () => {
      try {
        const [artistsRes, labelsRes, genresRes, countriesRes, formatsRes] = await Promise.all([
          fetch(`${API_BASE_URL}/api/artists`),
          fetch(`${API_BASE_URL}/api/labels`),
          fetch(`${API_BASE_URL}/api/genres`),
          fetch(`${API_BASE_URL}/api/countries`),
          fetch(`${API_BASE_URL}/api/formats`),
        ]);

        const [artists, labels, genres, countries, formats] = await Promise.all([
          artistsRes.json() as Promise<LookupItem[]>,
          labelsRes.json() as Promise<LookupItem[]>,
          genresRes.json() as Promise<LookupItem[]>,
          countriesRes.json() as Promise<LookupItem[]>,
          formatsRes.json() as Promise<LookupItem[]>,
        ]);

        // Ensure we have arrays before mapping
        setExistingArtists(Array.isArray(artists) ? artists.map((a) => a.name) : []);
        setExistingLabels(Array.isArray(labels) ? labels.map((l) => l.name) : []);
        setExistingGenres(Array.isArray(genres) ? genres.map((g) => g.name) : []);
        setExistingCountries(Array.isArray(countries) ? countries.map((c) => c.name) : []);
        setExistingFormats(Array.isArray(formats) ? formats.map((f) => f.name) : []);
      } catch (error) {
        console.error("Failed to load lookup data:", error);
        // Set empty arrays on error to prevent crashes
        setExistingArtists([]);
        setExistingLabels([]);
        setExistingGenres([]);
        setExistingCountries([]);
        setExistingFormats([]);
      }
    };

    fetchLookupData();
  }, []);

  // Helper function to sanitize filename
  const sanitizeFilename = (str: string): string => {
    return str
      .replace(/[^a-z0-9]/gi, '-') // Replace non-alphanumeric with dash
      .replace(/-+/g, '-') // Replace multiple dashes with single dash
      .replace(/^-|-$/g, ''); // Remove leading/trailing dashes
  };

  // Helper function to generate image filename
  const generateImageFilename = (artist: string, title: string, year?: number): string => {
    const artistPart = sanitizeFilename(artist);
    const titlePart = sanitizeFilename(title);
    const yearPart = year ? `-${year}` : '';
    return `${artistPart}-${titlePart}${yearPart}.jpg`;
  };

  // Map Discogs release data to form DTO
  const mapDiscogsToFormData = (release: DiscogsRelease): Partial<CreateMusicReleaseDto> => {
    // Map artists
    const artistNames = release.artists?.map(a => a.name) || [];
    
    // Map genres - combine genres and styles
    const genreNames = [
      ...(release.genres || []),
      ...(release.styles || [])
    ];
    
    // Map labels
    const labelName = release.labels?.[0]?.name;
    const labelNumber = release.labels?.[0]?.catalogNumber;
    
    // Map country
    const countryName = release.country;
    
    // Map formats
    const formatName = release.formats?.[0]?.name;
    
    // Extract barcode from identifiers
    const barcodeIdentifier = release.identifiers?.find(
      id => id.type.toLowerCase() === 'barcode'
    );
    const upc = barcodeIdentifier?.value;
    
    // Map tracklist to media
    const media = release.tracklist ? [{
      name: "Disc 1",
      tracks: release.tracklist.map((track, index) => ({
        title: track.title,
        index: index + 1,
        lengthSecs: track.duration ? parseDuration(track.duration) : undefined,
      }))
    }] : [];
    
    // Map images - generate local filename only (not full URL)
    let images = undefined;
    let sourceImageUrl = null;
    let sourceThumbnailUrl = null;
    if (release.images?.[0]) {
      const primaryArtist = release.artists?.[0]?.name || 'Unknown';
      const filename = generateImageFilename(primaryArtist, release.title, release.year);
      const thumbnailFilename = `thumb-${filename}`;
      
      sourceImageUrl = release.images[0].uri; // Store full-size image for later download
      sourceThumbnailUrl = release.images[0].uri150 || release.images[0].uri; // Use thumbnail if available, fallback to full image
      
      // Store just the filename - backend will serve via /api/images/{filename}
      images = {
        coverFront: filename,
        thumbnail: thumbnailFilename,
      };
    }
    
    return {
      title: release.title,
      releaseYear: release.year?.toString(),
      artistNames,
      artistIds: [],
      genreNames,
      genreIds: [],
      live: false,
      labelName,
      labelNumber,
      countryName,
      formatName,
      upc,
      images,
      media,
      links: release.uri ? [{ url: release.uri, type: "Discogs", description: "" }] : [],
      _meta: { sourceImageUrl, sourceThumbnailUrl }, // Store metadata separately
    } as any; // Type assertion to allow _meta
  };
  
  // Helper function to parse duration string (e.g., "3:45" -> 225 seconds)
  const parseDuration = (duration: string): number | undefined => {
    if (!duration) return undefined;
    const parts = duration.split(':').map(p => parseInt(p, 10));
    if (parts.length === 2) {
      return parts[0] * 60 + parts[1];
    } else if (parts.length === 3) {
      return parts[0] * 3600 + parts[1] * 60 + parts[2];
    }
    return undefined;
  };

  // Helper function to extract filename from API URL path
  const extractFilenameFromUrl = (url: string): string => {
    // Extract filename from URL like "http://localhost:5072/api/images/covers/Artist-Album-Year.jpg"
    const parts = url.split('/');
    return parts[parts.length - 1];
  };

  const handleSuccess = (releaseId: number) => {
    setNewReleaseId(releaseId);
    setShowSuccess(true);
    // Auto-redirect after 2 seconds
    setTimeout(() => {
      router.push(`/releases/${releaseId}`);
    }, 2000);
  };
  
  const handleFormSuccess = async (releaseId: number) => {
    // Download images if we have Discogs image URLs
    const downloadPromises: Promise<void>[] = [];

    // Download cover image
    if (discogsImageUrls.cover && discogsFormData?.images?.coverFront) {
      const coverPromise = (async () => {
        try {
          const filename = extractFilenameFromUrl(discogsFormData.images!.coverFront!);
          const imgResponse = await fetch(`${API_BASE_URL}/api/images/download`, {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({
              url: discogsImageUrls.cover,
              filename: filename,
            }),
          });
          
          if (imgResponse.ok) {
            const imgResult = await imgResponse.json();
            console.log("Cover image downloaded successfully:", imgResult.filename);
          }
        } catch (imgError) {
          console.error("Failed to download cover image:", imgError);
        }
      })();
      downloadPromises.push(coverPromise);
    }

    // Download thumbnail image
    if (discogsImageUrls.thumbnail && discogsFormData?.images?.thumbnail) {
      const thumbnailPromise = (async () => {
        try {
          const filename = extractFilenameFromUrl(discogsFormData.images!.thumbnail!);
          const imgResponse = await fetch(`${API_BASE_URL}/api/images/download`, {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({
              url: discogsImageUrls.thumbnail,
              filename: filename,
            }),
          });
          
          if (imgResponse.ok) {
            const imgResult = await imgResponse.json();
            console.log("Thumbnail image downloaded successfully:", imgResult.filename);
          }
        } catch (imgError) {
          console.error("Failed to download thumbnail image:", imgError);
        }
      })();
      downloadPromises.push(thumbnailPromise);
    }

    // Wait for all downloads to complete (don't fail if they error)
    await Promise.allSettled(downloadPromises);
    
    // Clear the image URL state
    setDiscogsImageUrls({ cover: null, thumbnail: null });
    
    // Call the regular success handler
    handleSuccess(releaseId);
  };

  const handleCancel = () => {
    // Clear the image URL state when cancelling
    setDiscogsImageUrls({ cover: null, thumbnail: null });
    router.push("/collection");
  };

  const handleResultsFound = (results: DiscogsSearchResult[]) => {
    setSearchResults(results);
    setSearchError("");
    setSelectedResult(null);
  };

  const handleSearchError = (error: string) => {
    setSearchError(error);
    setSearchResults([]);
  };

  const handleSelectResult = (result: DiscogsSearchResult) => {
    setSelectedResult(result);
    // Keep search results so we can go back to them
  };

  const handleBackToResults = () => {
    setSelectedResult(null);
    // Search results are preserved
  };

  const handleAddToCollection = async (release: DiscogsRelease) => {
    // Add directly to collection without showing edit form
    const formData = mapDiscogsToFormData(release);
    const sourceImageUrl = (formData as any)._meta?.sourceImageUrl;
    const sourceThumbnailUrl = (formData as any)._meta?.sourceThumbnailUrl;
    
    // Remove metadata before sending
    delete (formData as any)._meta;
    
    // Convert year strings to ISO DateTime format for the backend
    const cleanedData = {
      ...formData,
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
    
    try {
      const response = await fetch(`${API_BASE_URL}/api/musicreleases`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(cleanedData),
      });

      if (!response.ok) {
        let errorMessage = `Failed to add release (${response.status})`;
        try {
          const errorData = await response.text();
          if (errorData) {
            errorMessage = errorData;
          }
        } catch {
          // Ignore parse error
        }
        alert(`Error: ${errorMessage}`);
        return;
      }

      const result = await response.json();
      
      // Download images in parallel if we have source URLs
      const downloadPromises: Promise<void>[] = [];

      // Download cover image
      if (sourceImageUrl && cleanedData.images?.coverFront) {
        const coverPromise = (async () => {
          try {
            const filename = extractFilenameFromUrl(cleanedData.images!.coverFront!);
            const imgResponse = await fetch(`${API_BASE_URL}/api/images/download`, {
              method: "POST",
              headers: {
                "Content-Type": "application/json",
              },
              body: JSON.stringify({
                url: sourceImageUrl,
                filename: filename,
              }),
            });
            
            if (imgResponse.ok) {
              const imgResult = await imgResponse.json();
              console.log("Cover image downloaded successfully:", imgResult.filename);
            }
          } catch (imgError) {
            console.error("Failed to download cover image:", imgError);
          }
        })();
        downloadPromises.push(coverPromise);
      }

      // Download thumbnail image
      if (sourceThumbnailUrl && cleanedData.images?.thumbnail) {
        const thumbnailPromise = (async () => {
          try {
            const filename = extractFilenameFromUrl(cleanedData.images!.thumbnail!);
            const imgResponse = await fetch(`${API_BASE_URL}/api/images/download`, {
              method: "POST",
              headers: {
                "Content-Type": "application/json",
              },
              body: JSON.stringify({
                url: sourceThumbnailUrl,
                filename: filename,
              }),
            });
            
            if (imgResponse.ok) {
              const imgResult = await imgResponse.json();
              console.log("Thumbnail image downloaded successfully:", imgResult.filename);
            }
          } catch (imgError) {
            console.error("Failed to download thumbnail image:", imgError);
          }
        })();
        downloadPromises.push(thumbnailPromise);
      }

      // Wait for all downloads to complete (don't fail if they error)
      await Promise.allSettled(downloadPromises);
      
      // Redirect to the newly created release
      handleSuccess(result.release.id);
    } catch (error) {
      console.error("Error adding release:", error);
      alert("Failed to add release. Please try again.");
    }
  };

  const handleEditManually = (release: DiscogsRelease) => {
    const formData = mapDiscogsToFormData(release);
    const sourceImageUrl = (formData as any)._meta?.sourceImageUrl;
    const sourceThumbnailUrl = (formData as any)._meta?.sourceThumbnailUrl;
    
    // Store the image URLs for later download
    setDiscogsImageUrls({ 
      cover: sourceImageUrl || null, 
      thumbnail: sourceThumbnailUrl || null 
    });
    
    // Remove metadata before setting form data
    delete (formData as any)._meta;
    
    setDiscogsFormData(formData);
    setActiveTab("manual");
  };

  if (showSuccess && newReleaseId) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="bg-green-50 border border-green-200 rounded-lg p-8 text-center">
          <svg
            className="mx-auto h-16 w-16 text-green-500 mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M5 13l4 4L19 7"
            />
          </svg>
          <h2 className="text-2xl font-semibold text-gray-900 mb-2">
            Release Created Successfully!
          </h2>
          <p className="text-gray-600 mb-4">
            Redirecting to release details...
          </p>
          <button
            onClick={() => router.push(`/releases/${newReleaseId}`)}
            className="text-blue-600 hover:text-blue-800 underline"
          >
            View Now
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Add Release</h1>
        <p className="mt-2 text-gray-600">
          Add a new music release to your collection
        </p>
      </div>

      {/* Tab Navigation */}
      <div className="mb-6 border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          <button
            onClick={() => setActiveTab("discogs")}
            className={`py-4 px-1 border-b-2 font-medium text-sm transition-colors ${
              activeTab === "discogs"
                ? "border-blue-500 text-blue-600"
                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
            }`}
          >
            <span className="flex items-center">
              <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              Search Discogs
            </span>
          </button>
          <button
            onClick={() => setActiveTab("manual")}
            className={`py-4 px-1 border-b-2 font-medium text-sm transition-colors ${
              activeTab === "manual"
                ? "border-blue-500 text-blue-600"
                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
            }`}
          >
            <span className="flex items-center">
              <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
              Manual Entry
            </span>
          </button>
        </nav>
      </div>

      {/* Tab Content */}
      {activeTab === "discogs" && (
        <div>
          <DiscogsSearch
            onResultsFound={handleResultsFound}
            onError={handleSearchError}
          />

          {searchError && (
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-6">
              <div className="flex">
                <svg className="h-5 w-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                </svg>
                <div className="ml-3">
                  <p className="text-sm text-yellow-700">{searchError}</p>
                </div>
              </div>
            </div>
          )}

          {searchResults.length > 0 && !selectedResult && (
            <DiscogsSearchResults
              results={searchResults}
              onSelectResult={handleSelectResult}
            />
          )}

          {selectedResult && (
            <DiscogsReleasePreview
              searchResult={selectedResult}
              onAddToCollection={handleAddToCollection}
              onEditManually={handleEditManually}
              onBack={handleBackToResults}
              existingArtists={existingArtists}
              existingLabels={existingLabels}
              existingGenres={existingGenres}
              existingCountries={existingCountries}
              existingFormats={existingFormats}
            />
          )}
        </div>
      )}

      {activeTab === "manual" && (
        <AddReleaseForm 
          onSuccess={handleFormSuccess} 
          onCancel={handleCancel} 
          initialData={discogsFormData}
        />
      )}
    </div>
  );
}

