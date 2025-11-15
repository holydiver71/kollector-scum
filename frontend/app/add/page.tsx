"use client";

import { useRouter } from "next/navigation";
import { useState, useEffect } from "react";
import AddReleaseForm from "../components/AddReleaseForm";
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

        setExistingArtists(artists.map((a) => a.name));
        setExistingLabels(labels.map((l) => l.name));
        setExistingGenres(genres.map((g) => g.name));
        setExistingCountries(countries.map((c) => c.name));
        setExistingFormats(formats.map((f) => f.name));
      } catch (error) {
        console.error("Failed to load lookup data:", error);
      }
    };

    fetchLookupData();
  }, []);

  const handleSuccess = (releaseId: number) => {
    setNewReleaseId(releaseId);
    setShowSuccess(true);
    // Auto-redirect after 2 seconds
    setTimeout(() => {
      router.push(`/releases/${releaseId}`);
    }, 2000);
  };

  const handleCancel = () => {
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
    setSearchResults([]);
  };

  const handleBackToResults = () => {
    setSelectedResult(null);
  };

  const handleAddToCollection = (release: DiscogsRelease) => {
    // TODO: Implement direct add to collection
    // For now, switch to manual form with pre-filled data
    console.log("Add to collection:", release);
    setActiveTab("manual");
  };

  const handleEditManually = (release: DiscogsRelease) => {
    console.log("Edit manually:", release);
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

          {searchResults.length > 0 && (
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
        <AddReleaseForm onSuccess={handleSuccess} onCancel={handleCancel} />
      )}
    </div>
  );
}

