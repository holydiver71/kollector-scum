
import { useState } from "react";
import { searchDiscogs } from "../lib/api";
import type { DiscogsSearchRequest, DiscogsSearchResult } from "../lib/discogs-types";

interface DiscogsSearchProps {
  onResultsFound: (results: DiscogsSearchResult[]) => void;
  onError: (error: string) => void;
}

export default function DiscogsSearch({ onResultsFound, onError }: DiscogsSearchProps) {
  const [catalogNumber, setCatalogNumber] = useState("");
  const [format, setFormat] = useState("");
  const [country, setCountry] = useState("");
  const [year, setYear] = useState("");
  const [isSearching, setIsSearching] = useState(false);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!catalogNumber.trim()) {
      onError("Please enter a catalog number");
      return;
    }

    setIsSearching(true);
    onError(""); // Clear previous errors

    try {
      const request: DiscogsSearchRequest = {
        catalogNumber: catalogNumber.trim(),
        ...(format && { format }),
        ...(country && { country }),
        ...(year && { year: parseInt(year, 10) }),
      };

      const results = await searchDiscogs(request);
      
      if (results.length === 0) {
        onError(`No results found for catalog number "${catalogNumber}"`);
      } else {
        onResultsFound(results);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to search Discogs";
      onError(errorMessage);
    } finally {
      setIsSearching(false);
    }
  };

  const handleClear = () => {
    setCatalogNumber("");
    setFormat("");
    setCountry("");
    setYear("");
    onResultsFound([]);
    onError("");
  };

  return (
    <div className="bg-white rounded-lg shadow p-6 mb-6">
      <h2 className="text-xl font-semibold mb-4">Search Discogs</h2>
      
      <form onSubmit={handleSearch} className="space-y-4">
        <div>
          <label htmlFor="catalogNumber" className="block text-sm font-medium text-gray-700 mb-1">
            Catalog Number <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            id="catalogNumber"
            value={catalogNumber}
            onChange={(e) => setCatalogNumber(e.target.value)}
            placeholder="e.g., MOVLP001, ABC-12345"
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
            disabled={isSearching}
          />
          <p className="mt-1 text-sm text-gray-500">
            Enter the catalog number from the release (usually found on the spine or back cover)
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label htmlFor="format" className="block text-sm font-medium text-gray-700 mb-1">
              Format (Optional)
            </label>
            <input
              type="text"
              id="format"
              value={format}
              onChange={(e) => setFormat(e.target.value)}
              placeholder="e.g., Vinyl, CD"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
              disabled={isSearching}
            />
          </div>

          <div>
            <label htmlFor="country" className="block text-sm font-medium text-gray-700 mb-1">
              Country (Optional)
            </label>
            <input
              type="text"
              id="country"
              value={country}
              onChange={(e) => setCountry(e.target.value)}
              placeholder="e.g., US, UK"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
              disabled={isSearching}
            />
          </div>

          <div>
            <label htmlFor="year" className="block text-sm font-medium text-gray-700 mb-1">
              Year (Optional)
            </label>
            <input
              type="number"
              id="year"
              value={year}
              onChange={(e) => setYear(e.target.value)}
              placeholder="e.g., 2020"
              min="1900"
              max={new Date().getFullYear() + 1}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
              disabled={isSearching}
            />
          </div>
        </div>

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={isSearching || !catalogNumber.trim()}
            className="flex-1 bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700 disabled:bg-gray-300 disabled:cursor-not-allowed font-medium transition-colors"
          >
            {isSearching ? (
              <span className="flex items-center justify-center">
                <svg className="animate-spin -ml-1 mr-2 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Searching...
              </span>
            ) : (
              "Search Discogs"
            )}
          </button>

          <button
            type="button"
            onClick={handleClear}
            disabled={isSearching}
            className="px-6 py-2 border border-gray-300 rounded-md hover:bg-gray-50 disabled:bg-gray-100 disabled:cursor-not-allowed font-medium transition-colors"
          >
            Clear
          </button>
        </div>
      </form>
    </div>
  );
}
