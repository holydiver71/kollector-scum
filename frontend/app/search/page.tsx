"use client";

"use client";
import { useState } from "react";
import { SearchAndFilter } from "../components/SearchAndFilter";
import { MusicReleaseList } from "../components/MusicReleaseList";
import { QuickSearch } from "../components/SearchAndFilter";

interface SearchFilters {
  search?: string;
  artistId?: number;
  genreId?: number;
  labelId?: number;
  countryId?: number;
  formatId?: number;
  live?: boolean;
}

export default function SearchPage() {
  const [filters, setFilters] = useState<SearchFilters>({});
  const [showResults, setShowResults] = useState(false);

  const handleFiltersChange = (newFilters: SearchFilters) => {
    setFilters(newFilters);
    // Show results if any filter is applied
    const hasFilters = Object.values(newFilters).some(value => 
      value !== undefined && value !== null && value !== ''
    );
    setShowResults(hasFilters);
  };

  const handleQuickSearch = (query: string) => {
    const newFilters = { search: query || undefined };
    setFilters(newFilters);
    setShowResults(!!query);
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Page Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 py-6">
          <h1 className="text-2xl font-bold text-gray-900">Search Music</h1>
          <p className="text-gray-600 mt-1">Find specific releases in your collection</p>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 py-6">
        {!showResults ? (
          /* Search Landing */
          <div className="max-w-2xl mx-auto">
            <div className="text-center mb-8">
              <div className="text-6xl mb-4">🔍</div>
              <h2 className="text-xl font-semibold text-gray-900 mb-2">Search Your Collection</h2>
              <p className="text-gray-600">
                Use the search below to quickly find releases, or use advanced filters for more specific results.
              </p>
            </div>

            {/* Quick Search */}
            <div className="mb-8">
              <QuickSearch 
                onSearch={handleQuickSearch}
                placeholder="Search by title, artist, label..."
              />
            </div>

            {/* Advanced Search */}
            <SearchAndFilter 
              onFiltersChange={handleFiltersChange}
              initialFilters={filters}
            />
          </div>
        ) : (
          /* Search Results */
          <>
            {/* Search Interface */}
            <SearchAndFilter 
              onFiltersChange={handleFiltersChange}
              initialFilters={filters}
            />

            {/* Results */}
            <MusicReleaseList 
              filters={filters}
              pageSize={20}
            />
          </>
        )}
      </div>
    </div>
  );
}
